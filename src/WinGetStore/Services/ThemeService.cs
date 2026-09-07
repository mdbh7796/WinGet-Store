using Microsoft.UI;
using Microsoft.UI.Xaml;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.Services;

public class ThemeService : IThemeService
{
    private ThemeType _currentTheme = ThemeType.System;

    public ThemeType CurrentTheme => _currentTheme;

    public void SetTheme(ThemeType theme)
    {
        _currentTheme = theme;

        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme switch
            {
                ThemeType.Light => ElementTheme.Light,
                ThemeType.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }

        SaveTheme(theme);
    }

    public ThemeType GetSavedTheme()
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (settings.Values["Theme"] is string themeStr && Enum.TryParse<ThemeType>(themeStr, out var theme))
            {
                return theme;
            }
        }
        catch
        {
            // Settings not available
        }

        return ThemeType.System;
    }

    public void SaveTheme(ThemeType theme)
    {
        try
        {
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings;
            settings.Values["Theme"] = theme.ToString();
        }
        catch
        {
            // Settings not available
        }
    }

    public void ApplySavedTheme()
    {
        var savedTheme = GetSavedTheme();
        SetTheme(savedTheme);
    }
}
