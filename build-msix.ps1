param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$SdkVersion = "",
    [string]$CertificatePath = "",
    [SecureString]$CertificatePassword
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$msixOutput = Join-Path $scriptDir "dist"
$publishDir = Join-Path $env:TEMP "WinGetStore-msix-build"

Write-Host "WinGet Store - MSIX build" -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

dotnet publish (Join-Path $scriptDir "src\WinGetStore") `
    -c $Configuration -r $Runtime --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }

New-Item -ItemType Directory -Path (Join-Path $publishDir "Assets") -Force | Out-Null
Copy-Item (Join-Path $scriptDir "src\WinGetStore\Package.appxmanifest") `
    (Join-Path $publishDir "AppxManifest.xml") -Force
Copy-Item (Join-Path $scriptDir "src\WinGetStore\Assets\*") `
    (Join-Path $publishDir "Assets") -Recurse -Force

New-Item -ItemType Directory -Path $msixOutput -Force | Out-Null
$msixPath = Join-Path $msixOutput "WinGetStore.msix"
$kitRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
if ([string]::IsNullOrWhiteSpace($SdkVersion)) {
    $SdkVersion = (Get-ChildItem $kitRoot -Directory | Sort-Object Name -Descending |
        Select-Object -First 1).Name
}
$makeAppx = Join-Path $kitRoot "$SdkVersion\x64\makeappx.exe"
if (-not (Test-Path $makeAppx)) {
    throw "makeappx.exe was not found. Specify -SdkVersion or install the Windows SDK."
}

& $makeAppx pack /d $publishDir /p $msixPath /o
if ($LASTEXITCODE -ne 0) { throw "MSIX creation failed." }

if (-not [string]::IsNullOrWhiteSpace($CertificatePath)) {
    if (-not (Test-Path $CertificatePath)) { throw "Certificate not found: $CertificatePath" }
    if ($null -eq $CertificatePassword) {
        throw "CertificatePassword is required when signing."
    }

    $signTool = Join-Path $kitRoot "$SdkVersion\x64\signtool.exe"
    if (-not (Test-Path $signTool)) { throw "signtool.exe was not found." }
    $passwordPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($CertificatePassword)
    try {
        $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPtr)
        & $signTool sign /fd SHA256 /f $CertificatePath /p $password $msixPath
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPtr)
    }
    if ($LASTEXITCODE -ne 0) { throw "Signing failed." }
}
else {
    Write-Host "MSIX is unsigned; provide an organization-managed certificate for distribution." `
        -ForegroundColor Yellow
}

Write-Host "MSIX: $msixPath" -ForegroundColor Green
Write-Host "Install with: Add-AppxPackage -Path `"$msixPath`"" -ForegroundColor Gray
