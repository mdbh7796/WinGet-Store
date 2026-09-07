namespace WinGetStore.Services.Interfaces;

public enum ThemeType
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    ThemeType CurrentTheme { get; }
    void SetTheme(ThemeType theme);
    ThemeType GetSavedTheme();
    void SaveTheme(ThemeType theme);
}
