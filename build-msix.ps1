# WinGet Store - MSIX Build Script
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
$MsixOutput = Join-Path $ScriptDir "dist"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " WinGet Store - MSIX Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Publish
Write-Host "[1/5] Publishing application ($Configuration)..." -ForegroundColor Yellow
$publishDir = "C:\Users\MDBH\Temp\msix-build"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish "src\WinGetStore" -c $Configuration -r win-x64 --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Build failed!" }

# Step 2: Copy manifest and assets
Write-Host "[2/5] Copying manifest and assets..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "$publishDir\Assets" -Force | Out-Null
Copy-Item "src\WinGetStore\Package.appxmanifest" "$publishDir\AppxManifest.xml" -Force
Copy-Item "src\WinGetStore\Assets\*" "$publishDir\Assets" -Recurse -Force

# Step 3: Create MSIX
Write-Host "[3/5] Creating MSIX package..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $MsixOutput -Force | Out-Null
$msixPath = Join-Path $MsixOutput "WinGetStore.msix"

$sdkVersion = "10.0.22621.0"
$makeappx = "C:\Program Files (x86)\Windows Kits\10\bin\$sdkVersion\x64\makeappx.exe"
& $makeappx pack /d $publishDir /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "MSIX creation failed!" }

# Step 4: Create/reuse signing certificate
Write-Host "[4/5] Signing MSIX..." -ForegroundColor Yellow

$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=WinGetStore" } | Select-Object -First 1
if (-not $cert) {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=WinGetStore" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -KeyUsage DigitalSignature `
        -HashAlgorithm SHA256 `
        -NotAfter (Get-Date).AddYears(5) `
        -FriendlyName "WinGet Store"
    Write-Host "  Created signing certificate." -ForegroundColor Green
}

# Step 5: Sign
$pfxPath = Join-Path $MsixOutput "WinGetStore.pfx"
$password = "WinGetStore123"
$secPwd = ConvertTo-SecureString -String $password -Force -AsPlainText
if (-not (Test-Path $pfxPath)) {
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secPwd
}

$signtool = "C:\Program Files (x86)\Windows Kits\10\bin\$sdkVersion\x64\signtool.exe"
& $signtool sign /fd SHA256 /f $pfxPath /p $password $msixPath
if ($LASTEXITCODE -ne 0) { throw "Signing failed!" }

# Step 6: Export certificate for distribution
$cerPath = Join-Path $MsixOutput "WinGetStore.cer"
Export-Certificate -Cert $cert -FilePath $cerPath -Type CERT

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host " MSIX:  $msixPath" -ForegroundColor White
Write-Host " Cert:  $cerPath" -ForegroundColor White
Write-Host ""
Write-Host " To install (run as Administrator):" -ForegroundColor Yellow
Write-Host "   certutil -addstore TrustedPeople `"$cerPath`"" -ForegroundColor Gray
Write-Host "   Add-AppxPackage -Path `"$msixPath`"" -ForegroundColor Gray
Write-Host ""
Write-Host " Or run: .\dist\install.ps1 (as Administrator)" -ForegroundColor Yellow
