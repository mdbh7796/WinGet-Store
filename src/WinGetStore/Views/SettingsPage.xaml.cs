using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinGetStore.Services;
using WinGetStore.Services.Interfaces;
using WinGetStore.ViewModels;

namespace WinGetStore.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();

        var themeService = new ThemeService();
        var processService = new ProcessService();
        var winGetService = new WinGetService(processService);
        ViewModel = new SettingsPageViewModel(themeService, winGetService);

        ThemeComboBox.SelectedIndex = ViewModel.SelectedThemeIndex;
        AutoUpdateCheckBox.IsChecked = ViewModel.AutoCheckUpdates;

        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        VersionLoadingRing.IsActive = true;

        try
        {
            await ViewModel.LoadVersionInfoCommand.ExecuteAsync(null);
        }
        catch { }

        VersionLoadingRing.IsActive = false;
        WinGetVersionText.Text = $"WinGet {ViewModel.WinGetVersion}";
        AppVersionText.Text = $"WinGet Store v{ViewModel.AppVersion}";
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedIndex >= 0)
        {
            ViewModel.SelectedThemeIndex = ThemeComboBox.SelectedIndex;
        }
    }

    private void AutoUpdateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        ViewModel.AutoCheckUpdates = AutoUpdateCheckBox.IsChecked == true;
    }
}
