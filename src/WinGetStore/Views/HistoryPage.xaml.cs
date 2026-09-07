using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinGetStore.Models;
using WinGetStore.Services;
using WinGetStore.ViewModels;

namespace WinGetStore.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryPageViewModel ViewModel { get; }

    public HistoryPage()
    {
        this.InitializeComponent();

        ViewModel = new HistoryPageViewModel(new HistoryService());
        this.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadHistoryCommand.Execute(null);
        PopulateList();
    }

    private void PopulateList()
    {
        HistoryListView.Items.Clear();

        foreach (var entry in ViewModel.Entries)
        {
            var card = CreateHistoryCard(entry);
            HistoryListView.Items.Add(card);
        }

        EmptyPanel.Visibility = ViewModel.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryListView.Visibility = ViewModel.Entries.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        CountText.Text = $"{ViewModel.EntryCount} entr{(ViewModel.EntryCount != 1 ? "ies" : "y")}";
    }

    private Grid CreateHistoryCard(PackageHistoryEntry entry)
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

        var opGlyph = entry.Operation switch
        {
            "Install" => "\uE710",
            "Uninstall" => "\uE74D",
            "Update" => "\uE777",
            _ => "\uE7BA"
        };

        var opColor = entry.Success
            ? (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"]
            : (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"];

        var iconBorder = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(4),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
            Margin = new Thickness(0, 0, 12, 0),
            Child = new TextBlock
            {
                Text = opGlyph,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = opColor
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
            Text = entry.Name,
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
        detailBlock.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = $"{entry.Operation} · v{entry.Version}" });
        infoStack.Children.Add(detailBlock);

        Grid.SetColumn(infoStack, 1);

        var timeBlock = new TextBlock
        {
            Text = entry.Timestamp.ToString("MM/dd HH:mm"),
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumBrush"],
            Margin = new Thickness(12, 0, 0, 0)
        };
        Grid.SetColumn(timeBlock, 2);

        grid.Children.Add(iconBorder);
        grid.Children.Add(infoStack);
        grid.Children.Add(timeBlock);

        return grid;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
            PopulateList();
        }
    }

    private async void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Clear History",
            Content = "Are you sure you want to clear all history entries?",
            PrimaryButtonText = "Clear",
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
            ViewModel.ClearHistoryCommand.Execute(null);
            PopulateList();
        }
    }
}
