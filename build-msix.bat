@echo off
setlocal

echo ========================================
echo  WinGet Store - MSIX Build Script
echo ========================================
echo.

set PROJECT_DIR=%~dp0..
set PUBLISH_DIR=%PROJECT_DIR%\src\WinGetStore\bin\x64\Release\net8.0-windows10.0.22621.0\publish
set MSIX_OUTPUT=%PROJECT_DIR%\dist
set MAKEAPPX="C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\makeappx.exe"

echo [1/3] Publishing application...
dotnet publish "%PROJECT_DIR%\src\WinGetStore\WinGetStore.csproj" -c Release -r win-x64 --self-contained false
if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed!
    exit /b 1
)

echo.
echo [2/3] Creating output directory...
if not exist "%MSIX_OUTPUT%" mkdir "%MSIX_OUTPUT%"

echo.
echo [3/3] Creating MSIX package...
%MAKEAPPX% pack /d "%PUBLISH_DIR%" /p "%MSIX_OUTPUT%\WinGetStore.msix" /o
if %ERRORLEVEL% neq 0 (
    echo ERROR: MSIX creation failed!
    exit /b 1
)

echo.
echo ========================================
echo  Build complete!
echo  MSIX: %MSIX_OUTPUT%\WinGetStore.msix
echo ========================================
echo.
echo To install: Add-AppxPackage -Path "%MSIX_OUTPUT%\WinGetStore.msix"
echo To sign: Use signtool.exe with a certificate
echo.
