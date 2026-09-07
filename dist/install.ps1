# WinGet Store - Install Script
# Run as Administrator

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " WinGet Store Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  1. Right-click PowerShell -> Run as administrator" -ForegroundColor Yellow
    Write-Host "  2. Or run: Start-Process powershell -Verb RunAs" -ForegroundColor Yellow
    Write-Host ""
    pause
    exit 1
}

# Step 1: Create and trust certificate
Write-Host "[1/3] Setting up signing certificate..." -ForegroundColor Yellow

$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=WinGet Store" } | Select-Object -First 1
if (-not $cert) {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=WinGet Store" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyUsage DigitalSignature `
        -FriendlyName "WinGet Store" `
        -NotAfter (Get-Date).AddYears(5)
    Write-Host "  Created new certificate." -ForegroundColor Green
} else {
    Write-Host "  Certificate already exists." -ForegroundColor Green
}

# Export and trust
$certPath = Join-Path $ScriptDir "WinGetStore.cer"
Export-Certificate -Cert $cert -FilePath $certPath -Type CERT
certutil -addstore TrustedPeople $certPath 2>&1 | Out-Null
Write-Host "  Certificate trusted." -ForegroundColor Green

# Also add to Trusted Root for good measure
certutil -addstore Root $certPath 2>&1 | Out-Null

# Step 2: Remove old version
Write-Host "[2/3] Checking for existing installation..." -ForegroundColor Yellow
$existing = Get-AppxPackage | Where-Object { $_.Name -like "*WinGetStore*" -or $_.Name -like "*WinGet Store*" }
if ($existing) {
    $existing | Remove-AppxPackage
    Write-Host "  Removed old version." -ForegroundColor Green
} else {
    Write-Host "  No existing installation found." -ForegroundColor Green
}

# Step 3: Install MSIX
Write-Host "[3/3] Installing WinGet Store..." -ForegroundColor Yellow
$msixPath = Join-Path $ScriptDir "WinGetStore.msix"

if (-not (Test-Path $msixPath)) {
    Write-Host "  MSIX not found: $msixPath" -ForegroundColor Red
    Write-Host "  Run build-msix.ps1 first!" -ForegroundColor Yellow
    pause
    exit 1
}

Add-AppxPackage -Path $msixPath -ForceApplicationShutdown
Write-Host "  Installed successfully!" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " WinGet Store is ready!" -ForegroundColor Green
Write-Host " Find it in the Start Menu." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
pause
