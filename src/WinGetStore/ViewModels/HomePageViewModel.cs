using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinGetStore.Models;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.ViewModels;

public partial class HomePageViewModel : ObservableObject
{
    private readonly IWinGetService _winGetService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _winGetVersion = "Detecting...";

    [ObservableProperty]
    private bool _isWinGetAvailable;

    [ObservableProperty]
    private int _installedCount;

    [ObservableProperty]
    private int _updateCount;

    [ObservableProperty]
    private string _lastChecked = "Never";

    public HomePageViewModel(IWinGetService winGetService)
    {
        _winGetService = winGetService;
    }

    [RelayCommand]
    private async Task LoadDashboardAsync(CancellationToken cancellationToken)
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var versionInfo = await _winGetService.DetectWinGetAsync(cancellationToken);
            IsWinGetAvailable = versionInfo.IsAvailable;
            WinGetVersion = versionInfo.IsAvailable
                ? $"v{versionInfo.Version}"
                : $"Not found: {versionInfo.ErrorMessage}";

            if (versionInfo.IsAvailable)
            {
                var updates = await _winGetService.GetAvailableUpdatesAsync(cancellationToken);
                UpdateCount = updates.Count;
                LastChecked = DateTime.Now.ToString("HH:mm");
            }
        }
        catch (OperationCanceledException)
        {
            // Silently handle
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
