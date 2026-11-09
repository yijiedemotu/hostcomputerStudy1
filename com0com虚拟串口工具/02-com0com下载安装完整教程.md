# 02 · com0com 下载与安装完整教程

> 面向 Windows 10 / 11 64 位。全程需要**管理员权限**。
> 如果只是为了把本仓库的串口调试助手跑通，请直接看 [06 号教程](06-VComPair免驱动工具完全教程.md)，本页可以跳过。

---

## 1. 安装前检查清单

| 检查项 | 怎么查 | 不通过怎么办 |
|---|---|---|
| 系统架构 | `Win + R` → `msinfo32` → "系统类型" | ARM64（如骁龙本）**没有官方包**，请改用 VComPair |
| 管理员权限 | 开始菜单搜 `cmd` → 右键"以管理员身份运行" → 标题栏应显示"管理员" | 找 IT / 用有管理员权限的账号 |
| Secure Boot | `msinfo32` → "安全启动状态" | 若为"已启用"，测试模式可能被拦，需先进 BIOS 关闭（见 §5） |
| BitLocker | 控制面板 → BitLocker 驱动器加密 | 若已开启，**先备份恢复密钥**再改启动设置 |
| .NET Framework 2.0/3.5 | 仅 `setupg.exe` 图形工具需要 | 启用"Windows 功能 → .NET Framework 3.5"，或只用命令行 `setupc.exe` |
| IT 策略 | 公司电脑是否允许装第三方内核驱动 | 不允许就走路线 A（VComPair） |

> ⚠️ **内核驱动拥有系统最高权限**，安装前请确认来源。用完不需要时按 [05 号教程](05-故障排查与卸载.md) 干净卸载并关闭测试模式。

---

## 2. 去哪里下载（只认官方）

**官方项目页**：<https://sourceforge.net/projects/com0com/>
**官方文件目录**：<https://sourceforge.net/projects/com0com/files/com0com/3.0.0.0/>

目录里常见两个 ZIP：

| 文件 | 说明 |
|---|---|
| `com0com-3.0.0.0-i386-and-x64-signed.zip` | **优先选这个**。2017-07-13 补传的签名包（旧式交叉签名），兼容 x86/x64 |
| `com0com-3.0.0.0-i386-and-x64.zip` | 未签名版，现代 Windows 上大概率被拒 |

版本号 **3.0.0.0**（2012-06-04）就是最终版本，项目已休眠，**没有 4.0、没有 Windows 11 专用版**。

### 安全提醒

- 浏览器可能把驱动安装包标为"不常见下载"，这是内核驱动类文件的常见误报；**但前提是文件确实来自官方 SourceForge**。
- 搜索结果里会出现 `com0com.com` 之类的第三方站，宣称"2026 已签名版 / Win11 签名补丁"。**它们不是官方项目**。若确实要用，务必：
  1. 核对 SHA-256；
  2. 用杀软/VirusTotal 扫描；
  3. 先在虚拟机里装一遍；
  4. 不要装到生产或办公主力机。
- 下载后先校验哈希再解压：

```powershell
cd $env:USERPROFILE\Downloads
Get-FileHash .\com0com-3.0.0.0-i386-and-x64-signed.zip -Algorithm SHA256
```

把结果和发布页给出的哈希对照。哈希对不上就删掉重下。

### 解压

右键 ZIP → **"全部解压缩"**，解到一个不含中文和空格的路径更保险，例如 `C:\Temp\com0com`。
**不要直接在压缩包里双击 setup.exe**，驱动安装过程要读同目录的 INF/SYS 文件，压缩包里运行会报"找不到 INF 文件"。

---

## 3. 标准安装步骤

1. 进入解压目录，找到 `setup.exe`（x64 系统在根目录或 `x64\` 子目录，以包内结构为准）。
2. **右键 `setup.exe` → 以管理员身份运行**。
3. UAC 弹窗点"是"。
4. 安装向导：
   - 选择安装目录，默认 `C:\Program Files\com0com`（32 位程序可能在 `Program Files (x86)`）；
   - 组件页建议保留 **"CNCA0 ↔ CNCB0"** 与 **"Setup Command Prompt / Setup"** 快捷方式；
   - 如果看到 **"COM# ↔ COM#"** 选项，勾上会自动把第一对端口注册为标准 `COMx` 名字（第三方软件友好）。
5. 安装过程中 Windows 可能弹"发现新硬件向导"：
   - 选 **"否，暂时不"** → 下一步；
   - 选 **"自动安装软件（推荐）"** → 下一步。
6. 出现"Windows 无法验证此驱动程序软件的发布者"时，选择 **"始终安装此驱动程序软件"**（仅在你确认安装包来源可信时）。
7. 完成后**重启一次电脑**（内核驱动需要重新枚举设备）。

### 静默安装（批量部署用）

```powershell
setup.exe /S
setup.exe /S /D=C:\Tools\com0com
```

可用环境变量控制行为（执行前设置）：

| 变量 | 值 | 作用 |
|---|---|---|
| `CNC_INSTALL_START_MENU_SHORTCUTS` | `YES` / `NO` | 是否建开始菜单快捷方式 |
| `CNC_INSTALL_CNCA0_CNCB0_PORTS` | `YES` / `NO` | 是否创建默认的 CNCA0↔CNCB0 对 |
| `CNC_INSTALL_COMX_COMX_PORTS` | `YES` / `NO` | 是否创建 COM#↔COM# 对 |
| `CNC_INSTALL_SKIP_SETUP_PREINSTALL` | `YES` / `NO` | 跳过驱动预安装 |

> 静默安装默认**不创建任何端口对**，需要之后用 `setupc install - -` 手动创建，或显式设置上面两个环境变量。

---

## 4. 验证安装是否成功

### 4.1 设备管理器

`Win + X` → 设备管理器，展开：

- **端口 (COM 和 LPT)** —— 如果安装时勾了 COM# 选项，这里能看到 `com0com - serial port emulator (COM5)` 之类；
- **多端口串行适配器** —— 能看到 `com0com - bus for serial port pair emulator`。

没有黄色感叹号 / 禁止图标才算成功。

### 4.2 命令行列出端口对

从开始菜单 **com0com → Setup Command Prompt**（这个快捷方式已经是管理员、且工作目录正确），执行：

```
command> list
CNCA0 PortName=-
CNCB0 PortName=-
```

能列出 CNCA0/CNCB0 就说明驱动和端口对都在。

### 4.3 PowerShell 枚举端口

```powershell
[System.IO.Ports.SerialPort]::GetPortNames()
Get-PnpDevice -Class Ports | Select-Object FriendlyName, Status
```

如果是 COM# 对，这里应该能看到 `COM5`、`COM6`。

> 只看到 `CNCA0` 看不到 `COM5` 属正常：默认名不是标准 COM 名，见 [03 号教程](03-创建串口对与命令行详解.md)。

---

## 5. 驱动签名 / Code 52 —— 现代 Windows 最大的坑

### 症状

设备管理器里 com0com 设备带**黄色感叹号**，属性里写：

> **代码 52：Windows 无法验证此设备所需驱动程序的数字签名。**
> （Windows cannot verify the digital signature for the drivers required for this device.）

原因：`com0com.sys` 是**测试签名（test-signed）**的内核驱动，Windows Vista 之后的 x64 系统默认不允许加载；3.0.0.0 的"签名 ZIP"用的是旧式交叉签名，在开了 Secure Boot 的 Win10/11 上仍可能被拒。

### 方案 A：优先换用官方签名包（最省事）

卸载旧版 → 重启 → 安装 `com0com-3.0.0.0-i386-and-x64-signed.zip` → 重启 → 复查。
如果不再报 Code 52，就到此为止，**不需要动系统启动设置**。

### 方案 B：开启测试模式（TESTSIGNING）

以管理员身份打开命令提示符：

```cmd
bcdedit /set testsigning on
```

看到"操作成功完成"后**重启**。桌面右下角出现"测试模式 / Test Mode"水印即生效，驱动应该能加载。

关闭（用完务必关）：

```cmd
bcdedit /set testsigning off
bcdedit /deletevalue loadoptions
```

再重启，水印消失。

> ⚠️ 风险：测试模式允许加载未正式签名的内核驱动，**降低系统安全性**；部分游戏反作弊、银行控件会拒绝运行；不要长期开着。
> 若 `bcdedit /set testsigning on` 提示"该值受安全启动策略保护"，说明 Secure Boot 开着，需进 BIOS/UEFI 把 **Secure Boot** 临时设为 Disabled，改完再恢复（改之前备好 BitLocker 恢复密钥）。

### 方案 C：一次性禁用驱动签名（临时、重启即失效）

适合"只想验证一下、不想改 BCD"：

1. 按住 **Shift**，点开始菜单 → 电源 → **重启**；
2. 蓝屏菜单：**疑难解答 → 高级选项 → 启动设置 → 重启**；
3. 重启后按 **F7**（或数字 7）选择"禁用驱动程序强制签名"；
4. 进系统后立刻安装/验证 com0com。

下次正常重启会自动恢复强制签名，所以只适合临时验证。

### 方案 D：公司电脑 / 不想动系统

**别装驱动**，直接用 [VComPair 免驱动工具](06-VComPair免驱动工具完全教程.md)。这是本仓库专门为这种场景准备的方案。

---

## 6. 安装阶段常见失败

| 现象 | 原因 | 处理 |
|---|---|---|
| "找不到 INF 文件" | 在 ZIP 压缩包里直接运行 setup.exe | 先完整解压到磁盘目录 |
| "拒绝访问 / Access Denied" | 没用管理员 | 右键"以管理员身份运行"；公司电脑查 IT 策略 |
| 安装完设备管理器什么都没有 | 驱动被签名拦、或没重启 | 看 §5；重启后再查 |
| 代码 52 | 驱动签名不被信任 | §5 方案 A → B → C |
| 代码 10（设备无法启动） | 旧版本残留 / 驱动文件版本不一致 | [05 号教程](05-故障排查与卸载.md) 干净卸载后重装 |
| 代码 31 / 56 | 驱动文件损坏或系统策略冲突 | 重新解压官方包、换签名包、查 Secure Boot |
| 设备管理器有，程序枚举不到 | 默认名是 CNCA0，不在 Ports 类 | [03 号教程](03-创建串口对与命令行详解.md) 改 `PortName=COM#` |
| 装完系统蓝屏 / 不稳定 | 内核驱动兼容性问题 | 立刻卸载并关测试模式；改用 VComPair |

---

## 7. 装完之后的检查清单

- [ ] 设备管理器里 com0com 设备无感叹号
- [ ] `setupc list` 能列出至少一对端口
- [ ] `[System.IO.Ports.SerialPort]::GetPortNames()` 能看到期望的 COM 号（若用了 COM# 对）
- [ ] 记下安装目录（后面 `setupc.exe` 要用）
- [ ] 若开了测试模式，记着**用完关掉**
- [ ] 下一步：[03 · 创建串口对与命令行详解](03-创建串口对与命令行详解.md)

> 只要本仓库的串口助手能跑通，其实不必装 com0com。走 [06 号教程](06-VComPair免驱动工具完全教程.md) 更快。
