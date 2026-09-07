using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinGetStore.Models;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.ViewModels;

public partial class UpdatesPageViewModel : ObservableObject
{
    private readonly IWinGetService _winGetService;
    private List<PackageUpdate> _allUpdates = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _updateCount;

    [ObservableProperty]
    private bool _isUpdatingAll;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ObservableCollection<PackageUpdate> Updates { get; } = new();

    public UpdatesPageViewModel(IWinGetService winGetService)
    {
        _winGetService = winGetService;
    }

    [RelayCommand]
    private async Task LoadUpdatesAsync(CancellationToken cancellationToken)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Checking for updates...";
        Updates.Clear();
        _allUpdates.Clear();

        try
        {
            var updates = await _winGetService.GetAvailableUpdatesAsync(cancellationToken);
            _allUpdates = updates.ToList();
            UpdateCount = _allUpdates.Count;
            StatusMessage = UpdateCount == 0
                ? "All packages are up to date."
                : $"{UpdateCount} update{(UpdateCount != 1 ? "s" : "")} available";
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Checking for updates was cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to check for updates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var filtered = _allUpdates.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Updates.Clear();
        foreach (var pkg in filtered.OrderBy(p => p.Name))
        {
            Updates.Add(pkg);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private async Task UpdatePackageAsync(PackageUpdate package, CancellationToken cancellationToken)
    {
        try
        {
            StatusMessage = $"Updating {package.Name}...";
            var result = await _winGetService.UpdatePackageAsync(package.Id, cancellationToken);
            if (result.Success)
            {
                _allUpdates.RemoveAll(p => p.Id == package.Id);
                UpdateCount = _allUpdates.Count;
                ApplyFilter();
                StatusMessage = $"{package.Name} updated successfully.";
            }
            else
            {
                ErrorMessage = $"Failed to update {package.Name}: {result.ErrorMessage}";
                StatusMessage = $"Failed to update {package.Name}.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating {package.Name}: {ex.Message}";
            StatusMessage = $"Error updating {package.Name}.";
        }
    }

    [RelayCommand]
    private async Task UpdateAllAsync(CancellationToken cancellationToken)
    {
        if (IsUpdatingAll || UpdateCount == 0) return;

        IsUpdatingAll = true;
        ErrorMessage = null;
        StatusMessage = "Updating all packages...";

        try
        {
            var result = await _winGetService.UpdateAllPackagesAsync(cancellationToken);
            if (result.Success)
            {
                _allUpdates.Clear();
                UpdateCount = 0;
                Updates.Clear();
                StatusMessage = "All packages updated successfully.";
            }
            else
            {
                ErrorMessage = $"Failed to update all packages: {result.ErrorMessage}";
                StatusMessage = "Failed to update all packages.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating all packages: {ex.Message}";
            StatusMessage = "Error updating all packages.";
        }
        finally
        {
            IsUpdatingAll = false;
        }
    }
}
