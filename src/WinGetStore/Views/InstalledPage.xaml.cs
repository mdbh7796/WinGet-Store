using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WinGetStore.Models;
using WinGetStore.Services;
using WinGetStore.Services.Interfaces;
using WinGetStore.ViewModels;

namespace WinGetStore.Views;

public sealed partial class InstalledPage : Page
{
    public InstalledPageViewModel ViewModel { get; }

    private CancellationTokenSource? _loadingCts;

    public InstalledPage()
    {
        this.InitializeComponent();

        var processService = new ProcessService();
        var winGetService = new WinGetService(processService);
        ViewModel = new InstalledPageViewModel(winGetService);

        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadPackagesAsync();
    }

    private async System.Threading.Tasks.Task LoadPackagesAsync()
    {
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();

        ShowLoadingState();

        try
        {
            await ViewModel.LoadPackagesCommand.ExecuteAsync(_loadingCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        UpdateVisualState();
        PopulatePackageList();
    }

    private void PopulatePackageList()
    {
        PackageListView.Items.Clear();

        foreach (var pkg in ViewModel.Packages)
        {
            var card = CreatePackageCard(pkg);
            PackageListView.Items.Add(card);
        }
    }

    private Grid CreatePackageCard(Package pkg)
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
        detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = pkg.Publisher });
        detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = " \u00B7 " });
        detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = pkg.Id });
        infoStack.Children.Add(detailBlock);

        Grid.SetColumn(infoStack, 1);

        var versionBlock = new TextBlock
        {
            Text = pkg.Version,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
            Margin = new Thickness(12, 0, 0, 0)
        };
        Grid.SetColumn(versionBlock, 2);

        grid.Children.Add(iconBorder);
        grid.Children.Add(infoStack);
        grid.Children.Add(versionBlock);

        return grid;
    }

    private void UpdateVisualState()
    {
        LoadingRing.IsActive = false;

        if (ViewModel.ErrorMessage != null)
        {
            ErrorText.Text = ViewModel.ErrorMessage;
            ErrorPanel.Visibility = Visibility.Visible;
            EmptyPanel.Visibility = Visibility.Collapsed;
            PackageListView.Visibility = Visibility.Collapsed;
        }
        else if (ViewModel.Packages.Count == 0)
        {
            EmptySubText.Text = string.IsNullOrWhiteSpace(ViewModel.SearchText)
                ? "No installed packages found."
                : "No installed packages match your search.";
            EmptyPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            PackageListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            PackageListView.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
        }

        PackageCountText.Text = $"{ViewModel.PackageCount} package{(ViewModel.PackageCount != 1 ? "s" : "")} installed";
    }

    private void ShowLoadingState()
    {
        LoadingRing.IsActive = true;
        ErrorPanel.Visibility = Visibility.Collapsed;
        EmptyPanel.Visibility = Visibility.Collapsed;
        PackageListView.Visibility = Visibility.Collapsed;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
            UpdateVisualState();
            PopulatePackageList();
        }
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox.SelectedIndex >= 0)
        {
            ViewModel.SelectedSortIndex = SortComboBox.SelectedIndex;
            UpdateVisualState();
            PopulatePackageList();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadPackagesAsync();
    }
}
