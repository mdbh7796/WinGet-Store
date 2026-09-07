@echo off
echo ========================================
echo  WinGet Store Uninstaller
echo ========================================
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0install.ps1" -Uninstall
