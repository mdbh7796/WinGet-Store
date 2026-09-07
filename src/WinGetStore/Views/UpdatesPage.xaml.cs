using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinGetStore.Models;
using WinGetStore.Services;
using WinGetStore.Services.Interfaces;
using WinGetStore.ViewModels;

namespace WinGetStore.Views;

public sealed partial class UpdatesPage : Page
{
    public UpdatesPageViewModel ViewModel { get; }

    private CancellationTokenSource? _loadingCts;

    public UpdatesPage()
    {
        this.InitializeComponent();

        var processService = new ProcessService();
        var winGetService = new WinGetService(processService);
        ViewModel = new UpdatesPageViewModel(winGetService);

        this.Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await LoadUpdatesAsync();
    }

    private async System.Threading.Tasks.Task LoadUpdatesAsync()
    {
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();

        ShowLoadingState();

        try
        {
            await ViewModel.LoadUpdatesCommand.ExecuteAsync(_loadingCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        UpdateVisualState();
        PopulateUpdateList();
    }

    private void PopulateUpdateList()
    {
        UpdatesListView.Items.Clear();

        foreach (var update in ViewModel.Updates)
        {
            var card = CreateUpdateCard(update);
            UpdatesListView.Items.Add(card);
        }

        UpdateAllButton.IsEnabled = ViewModel.UpdateCount > 0 && !ViewModel.IsUpdatingAll;
    }

    private Grid CreateUpdateCard(PackageUpdate update)
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
                Text = "\uE777",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"]
            }
        };
        Grid.SetColumn(iconBorder, 0);

        var infoStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 4
        };

        var nameBlock = new TextBlock
        {
            Text = update.Name,
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
        detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = update.Id });
        infoStack.Children.Add(detailBlock);

        var versionBlock = new TextBlock
        {
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        };
        versionBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run
        {
            Text = update.CurrentVersion,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        });
        versionBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run
        {
            Text = " \u2192 ",
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"]
        });
        versionBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run
        {
            Text = update.AvailableVersion,
            Foreground = (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"],
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        infoStack.Children.Add(versionBlock);

        Grid.SetColumn(infoStack, 1);

        var updateButton = new Button
        {
            Content = "Update",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Tag = update
        };
        updateButton.Click += UpdateButton_Click;
        Grid.SetColumn(updateButton, 2);

        grid.Children.Add(iconBorder);
        grid.Children.Add(infoStack);
        grid.Children.Add(updateButton);

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
            UpdatesListView.Visibility = Visibility.Collapsed;
            UpdatingPanel.Visibility = Visibility.Collapsed;
        }
        else if (ViewModel.IsUpdatingAll)
        {
            UpdatingText.Text = ViewModel.StatusMessage;
            UpdatingPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
            UpdatesListView.Visibility = Visibility.Collapsed;
        }
        else if (ViewModel.UpdateCount == 0)
        {
            EmptyPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            UpdatesListView.Visibility = Visibility.Collapsed;
            UpdatingPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            UpdatesListView.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;
            EmptyPanel.Visibility = Visibility.Collapsed;
            UpdatingPanel.Visibility = Visibility.Collapsed;
        }

        StatusText.Text = ViewModel.StatusMessage;
        UpdateAllButton.IsEnabled = ViewModel.UpdateCount > 0 && !ViewModel.IsUpdatingAll;
    }

    private void ShowLoadingState()
    {
        LoadingRing.IsActive = true;
        ErrorPanel.Visibility = Visibility.Collapsed;
        EmptyPanel.Visibility = Visibility.Collapsed;
        UpdatesListView.Visibility = Visibility.Collapsed;
        UpdatingPanel.Visibility = Visibility.Collapsed;
        UpdateAllButton.IsEnabled = false;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
            PopulateUpdateList();
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadUpdatesAsync();
    }

    private async void UpdateAllButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Update All Packages",
            Content = $"Are you sure you want to update all {ViewModel.UpdateCount} packages?",
            PrimaryButtonText = "Update All",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary
        };

        if (Window.Current != null)
        {
            dialog.XamlRoot = this.XamlRoot;
        }

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                await ViewModel.UpdateAllCommand.ExecuteAsync(_loadingCts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { }

            UpdateVisualState();
            PopulateUpdateList();
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PackageUpdate update)
        {
            button.IsEnabled = false;
            button.Content = "...";

            try
            {
                await ViewModel.UpdatePackageCommand.ExecuteAsync(
                    (update, _loadingCts?.Token ?? CancellationToken.None));
            }
            catch (OperationCanceledException) { }

            UpdateVisualState();
            PopulateUpdateList();
        }
    }
}
