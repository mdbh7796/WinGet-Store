# Stop any running instances
Get-Process WinGetStore -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove ALL old versions
$oldPkgs = Get-AppxPackage -AllUsers | Where-Object { $_.PackageFullName -like "*WinGetStore*" }
foreach ($pkg in $oldPkgs) {
    Write-Output "Removing: $($pkg.PackageFullName)"
    Remove-AppxPackage -Package $pkg.PackageFullName -AllUsers -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 3

# Verify removal
$remaining = Get-AppxPackage | Where-Object { $_.PackageFullName -like "*WinGetStore*" }
if ($remaining) {
    Write-Output "WARNING: Still installed:"
    $remaining | ForEach-Object { Write-Output "  $($_.PackageFullName)" }
} else {
    Write-Output "All old packages removed."
}

# Install new
Write-Output ""
Write-Output "Installing new MSIX..."
Add-AppxPackage -Path "C:\Users\MDBH\Documents\Projects\Winget-App\dist\WinGetStore.msix" -ForceApplicationShutdown
Write-Output "Done!"

# Verify
$newPkg = Get-AppxPackage | Where-Object { $_.PackageFullName -like "*WinGetStore*" }
Write-Output "Installed: $($newPkg.PackageFullName)"

Write-Output ""
Write-Output "Press any key..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
