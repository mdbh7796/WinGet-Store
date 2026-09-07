using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinGetStore.Models;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.ViewModels;

public partial class InstalledPageViewModel : ObservableObject
{
    private readonly IWinGetService _winGetService;
    private List<Package> _allPackages = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedSortIndex;

    [ObservableProperty]
    private int _packageCount;

    public ObservableCollection<Package> Packages { get; } = new();

    public string[] SortOptions { get; } = new[]
    {
        "Name (A-Z)",
        "Name (Z-A)",
        "Publisher (A-Z)",
        "Version (Newest)",
        "Version (Oldest)"
    };

    public InstalledPageViewModel(IWinGetService winGetService)
    {
        _winGetService = winGetService;
    }

    [RelayCommand]
    private async Task LoadPackagesAsync(CancellationToken cancellationToken)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;
        Packages.Clear();
        _allPackages.Clear();

        try
        {
            var packages = await _winGetService.GetInstalledPackagesAsync(cancellationToken);
            _allPackages = packages.ToList();
            PackageCount = _allPackages.Count;
            ApplyFilterAndSort();
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Loading was cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load installed packages: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ApplyFilterAndSort()
    {
        var filtered = _allPackages.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Publisher.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SelectedSortIndex switch
        {
            0 => filtered.OrderBy(p => p.Name),
            1 => filtered.OrderByDescending(p => p.Name),
            2 => filtered.OrderBy(p => p.Publisher),
            3 => SortByVersionDescending(filtered),
            4 => SortByVersionAscending(filtered),
            _ => filtered.OrderBy(p => p.Name)
        };

        Packages.Clear();
        foreach (var pkg in filtered)
        {
            Packages.Add(pkg);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilterAndSort();
    }

    partial void OnSelectedSortIndexChanged(int value)
    {
        ApplyFilterAndSort();
    }

    [RelayCommand]
    private async Task UninstallPackageAsync(Package package, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _winGetService.UninstallPackageAsync(package.Id, cancellationToken);
            if (result.Success)
            {
                _allPackages.RemoveAll(p => p.Id == package.Id);
                PackageCount = _allPackages.Count;
                ApplyFilterAndSort();
            }
            else
            {
                ErrorMessage = $"Failed to uninstall {package.Name}: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error uninstalling {package.Name}: {ex.Message}";
        }
    }

    private static IOrderedEnumerable<Package> SortByVersionDescending(IEnumerable<Package> packages)
    {
        return packages
            .OrderByDescending(p => TryParseVersion(p.Version));
    }

    private static IOrderedEnumerable<Package> SortByVersionAscending(IEnumerable<Package> packages)
    {
        return packages
            .OrderBy(p => TryParseVersion(p.Version));
    }

    private static Version? TryParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        if (Version.TryParse(version, out var v)) return v;
        return null;
    }
}
