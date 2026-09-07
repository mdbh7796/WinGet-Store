using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.ViewModels;

public partial class SettingsPageViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IWinGetService _winGetService;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private bool _autoCheckUpdates;

    [ObservableProperty]
    private string _winGetVersion = "Detecting...";

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    public string[] ThemeOptions { get; } = new[] { "System", "Light", "Dark" };

    public SettingsPageViewModel(IThemeService themeService, IWinGetService winGetService)
    {
        _themeService = themeService;
        _winGetService = winGetService;

        var savedTheme = _themeService.GetSavedTheme();
        SelectedThemeIndex = savedTheme switch
        {
            ThemeType.Light => 1,
            ThemeType.Dark => 2,
            _ => 0
        };

        AutoCheckUpdates = GetAutoCheckSetting();
    }

    [RelayCommand]
    private async Task LoadVersionInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await _winGetService.DetectWinGetAsync(cancellationToken);
            WinGetVersion = info.IsAvailable ? $"v{info.Version}" : "Not installed";
        }
        catch
        {
            WinGetVersion = "Unknown";
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            1 => ThemeType.Light,
            2 => ThemeType.Dark,
            _ => ThemeType.System
        };
        _themeService.SetTheme(theme);
    }

    partial void OnAutoCheckUpdatesChanged(bool value)
    {
        SaveAutoCheckSetting(value);
    }

    private static bool GetAutoCheckSetting()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (settings.Values["AutoCheckUpdates"] is bool val)
                return val;
        }
        catch { }
        return true;
    }

    private static void SaveAutoCheckSetting(bool value)
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values["AutoCheckUpdates"] = value;
        }
        catch { }
    }
}
