# WinGet Store - Development Roadmap

## Technology Stack

| Component | Version |
|-----------|---------|
| .NET SDK | 8.0.424 (LTS) |
| Target Framework | `net8.0-windows10.0.22621.0` |
| Min OS Version | Windows 10 1809 (build 17763) |
| Windows App SDK | 1.6.240923002 |
| CommunityToolkit.Mvvm | 8.4.0 |
| WinUI 3 | via Windows App SDK |
| Test Framework | xUnit 2.9.2 + Moq 4.20.72 |
| Build | `dotnet build` (no Visual Studio IDE required) |

## Environment Notes

- **OS**: Windows 10 IoT Enterprise LTSC, build 19044 (21H2)
- **WinGet**: v1.29.290 (installed and working)
- **VS Build Tools 2022**: Installed (17.14.39) with MSBuild + Windows SDK 22621
- **Workaround**: `EnableCoreMrtTooling=false` in `Directory.Build.props` to disable MRT Core PRI generation (requires VS IDE MSBuild extensions not present in Build Tools)

---

## Phase 0 - Environment & Baseline ✅

- [x] Install .NET 8 SDK via winget (`Microsoft.DotNet.SDK.8` v8.0.424)
- [x] Initialize git repository
- [x] Create `.gitignore` (Visual Studio + .NET + WinUI 3 template)
- [x] Create solution structure (`WinGetStore.sln`)
- [x] Create `WinGetStore.csproj` with WinUI 3 + Windows App SDK
- [x] Create `WinGetStore.Tests.csproj` with xUnit + Moq
- [x] Create minimal `App.xaml` + `App.xaml.cs`
- [x] Create minimal `MainWindow.xaml` + `MainWindow.xaml.cs`
- [x] Create `Package.appxmanifest` and `app.manifest`
- [x] Configure `Directory.Build.props` (MRT Core workaround)
- [x] Verify Debug build succeeds
- [x] Verify Release build succeeds

**Build Status**: ✅ Both Debug and Release build successfully

---

## Phase 1 - Foundation & Navigation ✅

- [x] Implement `INavigationService` interface
- [x] Implement `NavigationService` class
- [x] Implement `IThemeService` interface (Light/Dark/System)
- [x] Implement `ThemeService` class with settings persistence
- [x] Implement `MainWindow` with `NavigationView` sidebar
  - Home, Discover, Installed, Updates, About, Settings
  - Fluent icons for each navigation item
  - Proper selected/unselected states
- [x] Create `HomePage.xaml` placeholder
- [x] Create `DiscoverPage.xaml` placeholder
- [x] Create `InstalledPage.xaml` placeholder
- [x] Create `UpdatesPage.xaml` placeholder
- [x] Create `AboutPage.xaml` placeholder
- [x] Create `SettingsPage.xaml` with theme selector
- [x] Implement `App.xaml.cs` with service initialization
- [x] Implement page navigation via NavigationView
- [x] xUnit test project compiles and links to main project

**Build Status**: ✅ Both Debug and Release build successfully

---

## Phase 2 - WinGet Backend ✅

- [x] `IWinGetService` interface
- [x] `WinGetService` implementation with text-based table parsing
- [x] `IProcessService` interface
- [x] `ProcessService` safe async process execution with timeout/cancellation
- [x] `IProcessService` / `ProcessResult` abstractions
- [x] `WinGetService` detection via `winget --version`
- [x] Version parsing with regex
- [x] Text-based output parsing (`ParseTableOutput<T>`, `ParseUpgradeOutput`, `ParseShowOutput`)
- [x] Model classes: `Package`, `PackageDetails`, `PackageUpdate`, `PackageHistoryEntry`, `WinGetResult`, `OperationResult`, `WinGetVersionInfo`
- [x] `WinGetStore.Core` class library (no WinUI dependency, testable)
- [x] 23 unit tests passing (parsing, process, models)

**Note**: WinGet v1.29.290 does not support `--output-format json`; all parsing uses text table format.

---

## Phase 3 - Installed Page ✅

- [x] List installed packages via `winget list`
- [x] Package cards with icon placeholder, name, publisher, version
- [x] Search/filter with AutoSuggestBox
- [x] Sorting (Name A-Z, Name Z-A, Publisher, Version Newest/Oldest)
- [x] Loading/error/empty states
- [x] `InstalledPageViewModel` with MVVM (CommunityToolkit.Mvvm)
- [x] Refresh button
- [x] Package count display

---

## Phase 4 - Updates ✅

- [x] Update detection via `winget upgrade`
- [x] Individual update support with button per package
- [x] Update All functionality with confirmation dialog
- [x] `UpdatesPageViewModel` with MVVM
- [x] Version display with arrow (current -> available)
- [x] Loading/error/empty/updating states
- [x] Search/filter updates
- [x] Refresh button
- [x] Status messages for operations

---

## Phase 5 - Discover ✅

- [x] Search with debouncing (500ms) + async
- [x] Package result cards with Install button
- [x] `DiscoverPageViewModel` with search, details, install commands
- [x] Install flow with confirmation and status feedback
- [x] Loading/error/empty states
- [x] Status messages for operations

## Phase 5b - Home Dashboard ✅

- [x] WinGet version detection + status display
- [x] Update count stat card
- [x] Last checked time
- [x] Check for Updates quick action button
- [x] `HomePageViewModel` with dashboard loading
- [x] Error display with InfoBar

---

## Phase 6 - History ✅

- [x] `HistoryService` with JSON file persistence
- [x] Record install/uninstall/update operations
- [x] `HistoryPageViewModel` with search/filter
- [x] `HistoryPage` with operation cards (icon, name, operation, version, timestamp)
- [x] Clear history with confirmation dialog
- [x] History navigation item in sidebar
- [x] Unit tests for HistoryService

---

## Phase 7 - Polish ✅

- [x] About page with app info, technology stack, disclaimer
- [x] Loading/error/empty states on all pages
- [x] Consistent card-based UI across all pages
- [x] Status messages for all operations
- [x] Confirmation dialogs for destructive actions

---

## Phase 8 - Settings ✅

- [x] Theme selection (System/Light/Dark) with persistence
- [x] Auto-update check toggle with persistence
- [x] WinGet version display with detection
- [x] `SettingsPageViewModel` with MVVM
- [x] App version display

---

## Phase 9 - Testing ✅

- [x] 39 unit tests passing
- [x] ProcessService tests (7 tests: success, failure, cancellation, timeout, output capture)
- [x] WinGetService parsing tests (10 tests: table parsing, empty, whitespace, versions)
- [x] HistoryService tests (6 tests: record, clear, ordering)
- [x] Model tests (10 tests: defaults, factory methods)
- [x] BasicTests (6 tests: types, enums, operations)

---

## Phase 10 - Release ✅

- [x] Release build verification (0 errors, 0 warnings)
- [x] Application icon (StoreLogo.ico + PNG variants)
- [x] Version metadata (1.0.0) in csproj
- [x] Window icon set on startup
- [x] Asset files for all required sizes (30x30, 44x44, 150x150, 310x150, SplashScreen)
- [x] All 39 tests pass in Release mode
- [x] MSIX package created via `build-msix.ps1` (10.29 MB)
- [x] Package.appxmanifest configured with valid language resource
- [x] Self-signed certificate created for code signing
- [x] MSIX signed with SHA256 certificate
- [x] Application successfully installed and verified in Start Menu
- [x] Crash logging added (App.xaml.cs) for production diagnostics
- [x] Fixed XAML entities (`&amp;#xE9CF;` → `&#xE9CF;` in MainWindow.xaml)
- [x] App confirmed running from portable build
- [x] Upgraded to Windows App SDK 2.1.3 (stable 2.x release)
- [x] Added `XamlControlsResources` to App.xaml for WASDK 2.x theme compatibility
- [x] Added `EnableMsixTooling=true` for WASDK 2.x unpackaged support

### Known Limitation: MSIX Activation

MSIX packaging has a known issue with WinUI 3 apps using `WindowsPackageType=None`:
- The app builds and installs fine as MSIX
- The MSIX registers in Start Menu
- Activation fails because WinUI 3 XAML runtime initialization differs between packaged/unpacked modes

**Workaround**: Use the portable distribution (`dist\WinGetStore-Portable.zip`).
Extract and run `WinGetStore.exe` directly. No installation required.

### Distribution

**Portable (Recommended)**:
```
dotnet publish src\WinGetStore -c Release -r win-x64 --self-contained false -o dist\WinGetStore-Portable
```
Output: `dist\WinGetStore-Portable.zip` (26.45 MB)

**Run portable**:
Extract the zip and double-click `WinGetStore.exe` or `WinGetStore.bat`.

**MSIX package**:
```
.\build-msix.ps1 -Configuration Release
```
Output: `dist\WinGetStore.msix` (installs but activation needs fix)

---

## Project Structure

```
Winget-App/
├── .gitignore
├── Directory.Build.props
├── WinGetStore.sln
├── docs/
│   └── ROADMAP.md
├── src/
│   ├── WinGetStore.Core/
│   │   ├── WinGetStore.Core.csproj
│   │   ├── Models/
│   │   │   ├── Package.cs
│   │   │   ├── PackageDetails.cs
│   │   │   ├── PackageHistoryEntry.cs
│   │   │   ├── PackageUpdate.cs
│   │   │   ├── WinGetResult.cs
│   │   │   ├── OperationResult.cs
│   │   │   └── WinGetVersionInfo.cs
│   │   └── Services/
│   │       ├── Interfaces/
│   │       │   ├── IProcessService.cs
│   │       │   ├── IThemeService.cs
│   │       │   └── IWinGetService.cs
│   │       ├── HistoryService.cs
│   │       ├── ProcessService.cs
│   │       └── WinGetService.cs
│   └── WinGetStore/
│       ├── WinGetStore.csproj
│       ├── app.manifest
│       ├── Package.appxmanifest
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── Services/
│       │   ├── Interfaces/
│       │   │   └── INavigationService.cs
│       │   ├── NavigationService.cs
│       │   └── ThemeService.cs
│       ├── ViewModels/
│       │   ├── DiscoverPageViewModel.cs
│       │   ├── HomePageViewModel.cs
│       │   ├── HistoryPageViewModel.cs
│       │   ├── InstalledPageViewModel.cs
│       │   ├── SettingsPageViewModel.cs
│       │   └── UpdatesPageViewModel.cs
│       └── Views/
│           ├── AboutPage.xaml(.cs)
│           ├── DiscoverPage.xaml(.cs)
│           ├── HistoryPage.xaml(.cs)
│           ├── HomePage.xaml(.cs)
│           ├── InstalledPage.xaml(.cs)
│           ├── SettingsPage.xaml(.cs)
│           └── UpdatesPage.xaml(.cs)
├── tests/
│   └── WinGetStore.Tests/
│       ├── WinGetStore.Tests.csproj
│       ├── BasicTests.cs
│       ├── HistoryServiceTests.cs
│       ├── ModelTests.cs
│       ├── ProcessServiceTests.cs
│       └── WinGetServiceParsingTests.cs
└── dist/
    ├── WinGetStore.msix          (signed MSIX package)
    ├── WinGetStore.cer           (signing certificate)
    ├── WinGetStore.pfx           (certificate with private key)
    ├── WinGetStore-Portable.zip  (portable distribution)
    ├── WinGetStore-Portable/     (extracted portable)
    ├── install.ps1               (admin install script)
    ├── install.bat               (batch installer)
    └── uninstall.bat             (batch uninstaller)
```

---

## Summary

| Phase | Status | Tests |
|-------|--------|-------|
| Phase 0 - Environment | ✅ Complete | - |
| Phase 1 - Foundation | ✅ Complete | - |
| Phase 2 - WinGet Backend | ✅ Complete | 23 |
| Phase 3 - Installed Page | ✅ Complete | - |
| Phase 4 - Updates | ✅ Complete | - |
| Phase 5 - Discover + Home | ✅ Complete | - |
| Phase 6 - History | ✅ Complete | 6 |
| Phase 7 - Polish | ✅ Complete | - |
| Phase 8 - Settings | ✅ Complete | - |
| Phase 9 - Testing | ✅ Complete | 39 total |
| Phase 10 - Release | ✅ Complete | - |

**Overall Progress**: 10/10 phases complete
