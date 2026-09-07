using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinGetStore.Views;

namespace WinGetStore;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Set title bar
        Title = "WinGet Store";

        // Set window icon
        try
        {
            var iconPath = System.IO.Path.Combine(
                AppContext.BaseDirectory, "Assets", "StoreLogo.ico");
            if (System.IO.File.Exists(iconPath))
            {
                var iconFile = Windows.Storage.StorageFile.GetFileFromPathAsync(iconPath).GetAwaiter().GetResult();
                this.AppWindow.SetIcon(iconFile.Path);
            }
        }
        catch
        {
            // Icon not available
        }

        // Set minimum window size
        var appWindow = this.AppWindow;
        if (appWindow != null)
        {
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                // Minimum size is set through the AppWindow
                appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 800, Height = 600 });
            }
        }

        // Navigate to Home page on startup
        ContentFrame.Navigate(typeof(HomePage));
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Set initial selected item
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var item = args.InvokedItemContainer as NavigationViewItem;
        if (item?.Tag is string tag)
        {
            Type? pageType = tag switch
            {
                "Home" => typeof(HomePage),
                "Discover" => typeof(DiscoverPage),
                "Installed" => typeof(InstalledPage),
                "Updates" => typeof(UpdatesPage),
                "About" => typeof(AboutPage),
                "History" => typeof(HistoryPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
