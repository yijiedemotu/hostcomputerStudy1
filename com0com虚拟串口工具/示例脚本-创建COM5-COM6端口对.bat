@echo off
chcp 65001 >nul
title com0com - create COM5 / COM6 pair

rem ============================================================
rem  WARNING: this script CHANGES SYSTEM SERIAL PORT CONFIGURATION
rem  - Requires com0com 3.0.0.0 already installed
rem  - Must be run as Administrator
rem  - Running it repeatedly creates MORE port pairs; run "setupc list" first
rem  See ../03-create-port-pair tutorial for details.
rem ============================================================

net session >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Please right-click this file and choose "Run as administrator".
  pause
  exit /b 1
)

set "CNCDIR=%ProgramFiles%\com0com"
if not exist "%CNCDIR%\setupc.exe" set "CNCDIR=%ProgramFiles(x86)%\com0com"
if not exist "%CNCDIR%\setupc.exe" (
  echo [ERROR] setupc.exe not found. Is com0com installed?
  echo         Checked: "%ProgramFiles%\com0com" and "%ProgramFiles(x86)%\com0com"
  pause
  exit /b 1
)

cd /d "%CNCDIR%"

echo.
echo Current port pairs:
setupc list
echo.
echo Press any key to CREATE a new pair and rename it to COM5 / COM6 ...
pause >nul

setupc install - -
setupc change CNCA0 PortName=COM5
setupc change CNCB0 PortName=COM6
setupc update

echo.
echo Result:
setupc list
echo.
echo Done. Open Device Manager - Ports (COM ^& LPT) to verify.
echo If a third-party tool still cannot see the ports, use PortName=COM# (Ports class),
echo see ../03-create-port-pair tutorial section 3.3.
echo.
pause
