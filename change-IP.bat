@echo off
:: Check for admin rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo 🔐 Administrator privileges required. Relaunching...
    powershell -Command "Start-Process -Verb runAs cmd -ArgumentList '/c \"%~f0\"'"
    exit /b
)

:: Run the PowerShell script
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Change-IP.ps1"

:: Keep window open
pause
