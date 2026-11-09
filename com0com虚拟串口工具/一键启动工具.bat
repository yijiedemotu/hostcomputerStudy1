@echo off
chcp 65001 >nul
title VComPair - Virtual Serial Port Pair (no driver)

cd /d "%~dp0"

echo ==========================================================
echo   VComPair - driver-free virtual serial port pair
echo   Building and starting, please wait a few seconds...
echo ==========================================================
echo.
echo   After the window opens: click "Start Service",
echo   then run SerialAssistant and choose VCOM1.
echo   Keep this console open while using the tool.
echo.

dotnet run --project "VComPair\VComPair.csproj"
if errorlevel 1 (
  echo.
  echo [ERROR] Failed to start. Please check that .NET 8 SDK is installed.
  echo         https://dotnet.microsoft.com/download/dotnet/8.0
  pause
)
