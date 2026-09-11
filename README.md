# WinGet Store

WinGet Store is a WinUI 3 desktop interface for Windows Package Manager. It provides package discovery, installation, installed-package management, updates, operation history, and theme settings while continuing to use the official `winget.exe` client.

## Requirements

- Windows 10 version 1809 (build 17763) or later
- .NET 8 SDK for building
- WinGet / App Installer available as `winget.exe`
- Windows SDK 10.0.22621 or later for MSIX packaging

## Build and run

```powershell
dotnet build WinGetStore.sln -c Release
dotnet run --project src\WinGetStore\WinGetStore.csproj -c Release
```

The supported release path is a portable publish:

```powershell
dotnet publish src\WinGetStore -c Release -r win-x64 --self-contained false -o dist\WinGetStore-Portable
```

Run `WinGetStore.exe` from the published directory. The target machine must have the .NET 8 Desktop Runtime and WinGet installed.

## Tests

```powershell
dotnet test WinGetStore.sln -c Release --no-restore
```

Tests cover process execution, cancellation and timeouts, WinGet parsing, models, and history persistence.

## MSIX packaging

`build-msix.ps1` creates an unsigned MSIX using the Windows SDK found on the machine:

```powershell
.\build-msix.ps1 -Configuration Release
```

For signing, provide an organization-managed certificate and password securely. Do not commit `.pfx` files, passwords, or self-signed production certificates:

```powershell
$password = Read-Host "Certificate password" -AsSecureString
.\build-msix.ps1 -CertificatePath .\signing\WinGetStore.pfx -CertificatePassword $password
```

Portable distribution remains the supported release path until packaged activation has been verified across the supported Windows App SDK/runtime combinations.

## Troubleshooting

- Run `winget --version` if the app reports that WinGet is unavailable.
- WinGet output is version- and locale-dependent. If a command succeeds but no packages appear, capture the command output and WinGet version.
- Package operations may require administrator approval depending on the installer.
- Runtime crash details are written to `crash.log` beside the executable.

## Project layout

- `src\WinGetStore.Core`: process integration, parsing, models, and persistence
- `src\WinGetStore`: WinUI 3 application and MVVM view models
- `tests\WinGetStore.Tests`: Core unit tests
- `docs\ROADMAP.md`: implementation and release status
