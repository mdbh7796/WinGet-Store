certutil -addstore TrustedPeople "C:\Users\MDBH\Documents\Projects\Winget-App\dist\new_cert.cer"
Add-AppxPackage -Path "C:\Users\MDBH\Documents\Projects\Winget-App\dist\WinGetStore.msix" -ForceApplicationShutdown
Write-Output "Installation complete!"
Write-Output ""
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
