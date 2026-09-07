using WinGetStore.Models;

namespace WinGetStore.Services.Interfaces;

public interface IWinGetService
{
    Task<WinGetVersionInfo> DetectWinGetAsync(CancellationToken cancellationToken = default);

    Task<WinGetResult> SearchPackagesAsync(
        string query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PackageUpdate>> GetAvailableUpdatesAsync(
        CancellationToken cancellationToken);

    Task<OperationResult> InstallPackageAsync(
        string packageId,
        string? source = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdatePackageAsync(
        string packageId,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAllPackagesAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult> UninstallPackageAsync(
        string packageId,
        CancellationToken cancellationToken = default);

    Task<PackageDetails?> GetPackageDetailsAsync(
        string packageId,
        string? source = null,
        CancellationToken cancellationToken = default);
}
