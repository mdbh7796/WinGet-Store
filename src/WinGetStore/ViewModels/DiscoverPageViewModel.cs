using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinGetStore.Models;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.ViewModels;

public partial class DiscoverPageViewModel : ObservableObject
{
    private readonly IWinGetService _winGetService;
    private CancellationTokenSource? _searchCts;
    private int _searchGeneration;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Search for packages to discover";

    [ObservableProperty]
    private bool _hasSearched;

    [ObservableProperty]
    private PackageDetails? _selectedPackage;

    [ObservableProperty]
    private bool _isInstalling;

    [ObservableProperty]
    private string _installStatus = string.Empty;

    public ObservableCollection<Package> SearchResults { get; } = new();

    public DiscoverPageViewModel(IWinGetService winGetService)
    {
        _winGetService = winGetService;
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            HasSearched = false;
            StatusMessage = "Search for packages to discover";
            return;
        }

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var generation = ++_searchGeneration;
        var searchToken = _searchCts.Token;

        IsLoading = true;
        ErrorMessage = null;
        HasSearched = true;
        StatusMessage = "Searching...";
        SearchResults.Clear();

        try
        {
            var result = await _winGetService.SearchPackagesAsync(SearchText.Trim(), searchToken);
            if (generation != _searchGeneration || searchToken.IsCancellationRequested)
                return;

            if (!string.IsNullOrEmpty(result.Error))
            {
                ErrorMessage = result.Error;
                StatusMessage = "Search failed.";
            }
            else
            {
                foreach (var pkg in result.Packages)
                {
                    SearchResults.Add(pkg);
                }
                StatusMessage = SearchResults.Count == 0
                    ? "No packages found."
                    : $"{SearchResults.Count} package{(SearchResults.Count != 1 ? "s" : "")} found";
            }
        }
        catch (OperationCanceledException)
        {
            if (generation == _searchGeneration)
                StatusMessage = "Search cancelled.";
        }
        catch (Exception ex)
        {
            if (generation != _searchGeneration)
                return;
            ErrorMessage = $"Search failed: {ex.Message}";
            StatusMessage = "Search failed.";
        }
        finally
        {
            if (generation == _searchGeneration)
                IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(Package package, CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = null;
        SelectedPackage = null;

        try
        {
            var details = await _winGetService.GetPackageDetailsAsync(package.Id, package.Source, cancellationToken);
            if (details != null)
            {
                SelectedPackage = details;
            }
            else
            {
                SelectedPackage = new PackageDetails
                {
                    Name = package.Name,
                    Id = package.Id,
                    Version = package.Version,
                    Publisher = package.Publisher
                };
            }
        }
        catch (Exception ex)
        {
            SelectedPackage = new PackageDetails
            {
                Name = package.Name,
                Id = package.Id,
                Version = package.Version,
                Publisher = package.Publisher,
                Description = $"Could not load details: {ex.Message}"
            };
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task InstallPackageAsync(string packageId, CancellationToken cancellationToken)
    {
        IsInstalling = true;
        InstallStatus = "Installing...";
        ErrorMessage = null;

        try
        {
            var result = await _winGetService.InstallPackageAsync(packageId, null, cancellationToken);
            if (result.Success)
            {
                InstallStatus = "Installed successfully!";
            }
            else
            {
                InstallStatus = $"Install failed: {result.ErrorMessage}";
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            InstallStatus = $"Install error: {ex.Message}";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    public void CancelSearch()
    {
        _searchCts?.Cancel();
    }
}
