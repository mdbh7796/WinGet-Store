using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinGetStore.Models;
using WinGetStore.Services;
using WinGetStore.Services.Interfaces;
using WinGetStore.ViewModels;

namespace WinGetStore.Views;

public sealed partial class DiscoverPage : Page
{
    public DiscoverPageViewModel ViewModel { get; }

    private CancellationTokenSource? _loadingCts;
    private DispatcherTimer? _debounceTimer;

    public DiscoverPage()
    {
        this.InitializeComponent();

        var processService = new ProcessService();
        var winGetService = new WinGetService(processService);
        ViewModel = new DiscoverPageViewModel(winGetService);

        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

        _debounceTimer?.Stop();
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            await PerformSearchAsync();
        };
        _debounceTimer.Start();
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        _debounceTimer?.Stop();
        await PerformSearchAsync();
    }

    private async System.Threading.Tasks.Task PerformSearchAsync()
    {
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();

        ShowLoadingState();

        try
        {
            ViewModel.SearchText = SearchBox.Text;
            await ViewModel.SearchCommand.ExecuteAsync(_loadingCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        UpdateVisualState();
        PopulateResults();
    }

    private void PopulateResults()
    {
        ResultsListView.Items.Clear();

        foreach (var pkg in ViewModel.SearchResults)
        {
            var card = CreateResultCard(pkg);
            ResultsListView.Items.Add(card);
        }
    }

    private Grid CreateResultCard(Package pkg)
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 2, 0, 2),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

        var iconBorder = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(4),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            Margin = new Thickness(0, 0, 12, 0),
            Child = new TextBlock
            {
                Text = "\uE7B3",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
            }
        };
        Grid.SetColumn(iconBorder, 0);

        var infoStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 2
        };

        var nameBlock = new TextBlock
        {
            Text = pkg.Name,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        infoStack.Children.Add(nameBlock);

        var detailBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = pkg.Id });
        if (!string.IsNullOrEmpty(pkg.Version))
        {
            detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = $" \u00B7 v{pkg.Version}" });
        }
        infoStack.Children.Add(detailBlock);

        Grid.SetColumn(infoStack, 1);

        var installButton = new Button
        {
            Content = "Install",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Tag = pkg
        };
        installButton.Click += InstallButton_Click;
        Grid.SetColumn(installButton, 2);

        grid.Children.Add(iconBorder);
        grid.Children.Add(infoStack);
        grid.Children.Add(installButton);

        return grid;
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Package pkg)
        {
            button.IsEnabled = false;
            button.Content = "...";

            try
            {
                _loadingCts?.Cancel();
                _loadingCts = new CancellationTokenSource();

                await ViewModel.InstallPackageCommand.ExecuteAsync((pkg.Id, _loadingCts.Token));
            }
            catch (OperationCanceledException) { }

            button.Content = ViewModel.InstallStatus.Contains("success", StringComparison.OrdinalIgnoreCase)
                ? "\uE73E"
                : "Install";

            if (ViewModel.InstallStatus.Contains("success", StringComparison.OrdinalIgnoreCase))
            {
                button.Foreground = (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
            }
            else
            {
                button.IsEnabled = true;
            }
        }
    }

    private void UpdateVisualState()
    {
        LoadingRing.IsActive = false;

        if (ViewModel.ErrorMessage != null)
        {
            ErrorText.Text = ViewModel.ErrorMessage;
            ErrorPanel.Visibility = Visibility.Visible;
            WelcomePanel.Visibility = Visibility.Collapsed;
            ResultsListView.Visibility = Visibility.Collapsed;
        }
        else if (!ViewModel.HasSearched)
        {
            WelcomePanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ResultsListView.Visibility = Visibility.Collapsed;
        }
        else if (ViewModel.SearchResults.Count == 0)
        {
            StatusText.Text = ViewModel.StatusMessage;
            WelcomePanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            ResultsListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            ResultsListView.Visibility = Visibility.Visible;
            WelcomePanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        StatusText.Text = ViewModel.StatusMessage;
    }

    private void ShowLoadingState()
    {
        LoadingRing.IsActive = true;
        ErrorPanel.Visibility = Visibility.Collapsed;
        WelcomePanel.Visibility = Visibility.Collapsed;
        ResultsListView.Visibility = Visibility.Collapsed;
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformSearchAsync();
    }
}
