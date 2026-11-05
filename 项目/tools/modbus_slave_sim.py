#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Modbus TCP 从站模拟器（纯 Python 标准库，零依赖）
=================================================

作用：没有 PLC / 没有硬件时，用它冒充现场设备，让上位机项目能真实跑起来。

用法：
    python modbus_slave_sim.py                  # 监听 0.0.0.0:502
    python modbus_slave_sim.py --port 1502      # 换端口（Windows 上 502 被占用时常用）
    python modbus_slave_sim.py --host 127.0.0.1 --verbose

模拟设备：
    站号 1（1#注塑机）：保持寄存器 0..9
        0  模温            Int16   0.1℃
        1  注射压力        UInt16  0.01MPa
        2  周期时间        UInt16  0.1s
        3~4 累计产量       UInt32  ABCD 字序
        5  温度设定值      UInt16  0.1℃（可写，用来测上位机写寄存器）
    站号 2（2#空压机）：
        0  排气压力        UInt16  0.01MPa
        1  排气温度        Int16   0.1℃

支持功能码：01/02 读线圈、03/04 读寄存器、05 写单线圈、06 写单寄存器、10 批量写寄存器。
异常应答：非法地址返回 0x02，非法功能码返回 0x01。

协议小抄（Modbus TCP）：
    MBAP 头 7 字节 = 事务号2 + 协议号2(0) + 长度2(单元号+PDU) + 单元号1
    读保持寄存器请求 PDU = 03 起始地址2 数量2
    应答 PDU            = 03 字节数1 数据N
"""

import argparse
import math
import socket
import struct
import sys
import threading
import time

# Windows 控制台默认 GBK，重定向一下防止中文报错
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


def crc16(data: bytes) -> int:
    """Modbus RTU 的 CRC16（多项式 0xA001，初值 0xFFFF）。"""
    crc = 0xFFFF
    for byte in data:
        crc ^= byte
        for _ in range(8):
            if crc & 1:
                crc = (crc >> 1) ^ 0xA001
            else:
                crc >>= 1
    return crc & 0xFFFF


class SlaveDevice:
    """一个从站的寄存器区 + 模拟量发生器。"""

    def __init__(self, unit_id: int):
        self.unit_id = unit_id
        self.registers = [0] * 128
        self.coils = [False] * 64
        self.lock = threading.Lock()
        self.started = time.time()
        self.production = 1200

    def update(self):
        """按时间生成连续变化的模拟量，让曲线看起来像真设备。"""
        t = time.time() - self.started
        with self.lock:
            if self.unit_id == 1:
                self.registers[0] = int(900 + 120 * math.sin(t / 9.0))      # 90.0℃ ± 12
                self.registers[1] = int(8500 + 1200 * math.sin(t / 7.0))    # 85MPa ± 12
                self.registers[2] = 285                                     # 28.5s
                self.production = 1200 + int(t / 5)                         # 每 5 秒 +1 件
                self.registers[3] = (self.production >> 16) & 0xFFFF        # UInt32 高字
                self.registers[4] = self.production & 0xFFFF                # UInt32 低字
                # 5 号寄存器（设定值）由上位机写入，模拟器不覆盖
            elif self.unit_id == 2:
                self.registers[0] = int(700 + 45 * math.sin(t / 5.0))       # 7.00MPa ± 0.45
                self.registers[1] = int(780 + 25 * math.sin(t / 11.0))      # 78.0℃ ± 2.5


class ModbusTcpServer:
    def __init__(self, host: str, port: int, verbose: bool = False):
        self.host = host
        self.port = port
        self.verbose = verbose
        self.devices = {1: SlaveDevice(1), 2: SlaveDevice(2)}
        self.running = True

        self._updater = threading.Thread(target=self._update_loop, daemon=True)
        self._updater.start()

    def _update_loop(self):
        while self.running:
            for device in self.devices.values():
                device.update()
            time.sleep(0.5)

    def serve_forever(self):
        server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server.bind((self.host, self.port))
        server.listen(16)
        print(f"[模拟器] 已启动：{self.host}:{self.port}")
        print("[模拟器] 站号 1 = 注塑机，站号 2 = 空压机；Ctrl+C 退出")
        print(f"[模拟器] CRC16 自检：01 03 00 00 00 0A -> "
              f"{crc16(bytes.fromhex('01030000000A')):04X}（应为 CDC5）")

        try:
            while True:
                conn, addr = server.accept()
                threading.Thread(target=self._handle_client, args=(conn, addr), daemon=True).start()
        except KeyboardInterrupt:
            print("\n[模拟器] 收到 Ctrl+C，退出")
        finally:
            self.running = False
            server.close()

    def _handle_client(self, conn: socket.socket, addr):
        print(f"[连接] {addr[0]}:{addr[1]}")
        conn.settimeout(30)
        try:
            while True:
                header = self._recv_exact(conn, 7)
                if header is None:
                    break
                transaction_id, protocol_id, length, unit_id = struct.unpack(">HHHB", header)
                if protocol_id != 0 or length < 2 or length > 260:
                    print(f"[警告] 非法 MBAP 头：proto={protocol_id} len={length}")
                    break

                pdu = self._recv_exact(conn, length - 1)
                if pdu is None:
                    break

                if self.verbose:
                    print(f"[请求] {addr[0]} unit={unit_id} PDU={pdu.hex(' ').upper()}")

                response_pdu = self._process(unit_id, pdu)
                response = struct.pack(">HHHB", transaction_id, 0, len(response_pdu) + 1, unit_id) + response_pdu
                conn.sendall(response)

                if self.verbose:
                    print(f"[应答] unit={unit_id} PDU={response_pdu.hex(' ').upper()}")
        except (ConnectionResetError, socket.timeout, OSError):
            pass
        finally:
            conn.close()
            print(f"[断开] {addr[0]}:{addr[1]}")

    @staticmethod
    def _recv_exact(conn: socket.socket, count: int):
        buffer = b""
        while len(buffer) < count:
            chunk = conn.recv(count - len(buffer))
            if not chunk:
                return None
            buffer += chunk
        return buffer

    def _process(self, unit_id: int, pdu: bytes) -> bytes:
        if not pdu:
            return b""
        function = pdu[0]
        device = self.devices.get(unit_id)
        if device is None:
            return bytes([function | 0x80, 0x0B])  # 网关目标设备无响应

        try:
            if function in (0x03, 0x04):
                start, count = struct.unpack(">HH", pdu[1:5])
                if count < 1 or count > 125 or start + count > len(device.registers):
                    return bytes([function | 0x80, 0x02])
                with device.lock:
                    values = device.registers[start:start + count]
                payload = struct.pack(f">{count}H", *values)
                return bytes([function, count * 2]) + payload

            if function in (0x01, 0x02):
                start, count = struct.unpack(">HH", pdu[1:5])
                if count < 1 or count > 2000 or start + count > len(device.coils):
                    return bytes([function | 0x80, 0x02])
                with device.lock:
                    bits = device.coils[start:start + count]
                byte_count = (count + 7) // 8
                payload = bytearray(byte_count)
                for i, bit in enumerate(bits):
                    if bit:
                        payload[i // 8] |= 1 << (i % 8)
                return bytes([function, byte_count]) + bytes(payload)

            if function == 0x05:
                address, value = struct.unpack(">HH", pdu[1:5])
                if address >= len(device.coils):
                    return bytes([function | 0x80, 0x02])
                with device.lock:
                    device.coils[address] = value == 0xFF00
                return pdu[:5]

            if function == 0x06:
                address, value = struct.unpack(">HH", pdu[1:5])
                if address >= len(device.registers):
                    return bytes([function | 0x80, 0x02])
                with device.lock:
                    device.registers[address] = value
                return pdu[:5]

            if function == 0x10:
                start, count, byte_count = struct.unpack(">HHB", pdu[1:6])
                if count < 1 or count > 123 or byte_count != count * 2 or start + count > len(device.registers):
                    return bytes([function | 0x80, 0x02])
                values = struct.unpack(f">{count}H", pdu[6:6 + byte_count])
                with device.lock:
                    device.registers[start:start + count] = list(values)
                return bytes([function]) + struct.pack(">HH", start, count)

            return bytes([function | 0x80, 0x01])  # 非法功能码
        except Exception as exc:  # 模拟器自身异常也不能把连接搞崩
            print(f"[错误] 处理请求失败：{exc}")
            return bytes([function | 0x80, 0x04])


def main():
    parser = argparse.ArgumentParser(description="Modbus TCP 从站模拟器")
    parser.add_argument("--host", default="0.0.0.0", help="监听地址，默认 0.0.0.0")
    parser.add_argument("--port", type=int, default=502, help="监听端口，默认 502")
    parser.add_argument("--verbose", action="store_true", help="打印每一帧请求/应答")
    args = parser.parse_args()

    if args.port < 1024 and sys.platform != "win32":
        print("[提示] 非 Windows 系统监听 1024 以下端口需要 root 权限")

    ModbusTcpServer(args.host, args.port, args.verbose).serve_forever()


if __name__ == "__main__":
    main()
