using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinGetStore.Services;
using WinGetStore.Services.Interfaces;
using WinGetStore.ViewModels;

namespace WinGetStore.Views;

public sealed partial class HomePage : Page
{
    public HomePageViewModel ViewModel { get; }

    private CancellationTokenSource? _loadingCts;

    public HomePage()
    {
        this.InitializeComponent();

        var processService = new ProcessService();
        var winGetService = new WinGetService(processService);
        ViewModel = new HomePageViewModel(winGetService);

        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync();
    }

    private async System.Threading.Tasks.Task LoadDashboardAsync()
    {
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();

        StatusLoadingRing.IsActive = true;
        CheckUpdatesButton.IsEnabled = false;

        try
        {
            await ViewModel.LoadDashboardCommand.ExecuteAsync(_loadingCts.Token);
        }
        catch (OperationCanceledException) { }

        StatusLoadingRing.IsActive = false;
        CheckUpdatesButton.IsEnabled = true;

        WinGetVersionText.Text = ViewModel.WinGetVersion;
        UpdateCountText.Text = ViewModel.UpdateCount.ToString();
        LastCheckedText.Text = ViewModel.LastChecked;

        if (ViewModel.ErrorMessage != null)
        {
            ErrorBar.Message = ViewModel.ErrorMessage;
            ErrorBar.IsOpen = true;
            ErrorBar.Visibility = Visibility.Visible;
        }
        else
        {
            ErrorBar.Visibility = Visibility.Collapsed;
        }
    }

    private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync();
    }
}
