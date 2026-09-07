@echo off
echo ========================================
echo  WinGet Store Installer
echo ========================================
echo.
echo This will install WinGet Store on your system.
echo You must run this as Administrator.
echo.
pause
powershell -ExecutionPolicy Bypass -File "%~dp0install.ps1"
