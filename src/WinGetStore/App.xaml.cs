using Microsoft.UI.Xaml;
using WinGetStore.Services.Interfaces;

namespace WinGetStore;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public static IThemeService? ThemeService { get; private set; }
    public static INavigationService? NavigationService { get; private set; }

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
        System.IO.File.WriteAllText(logPath, $"App UnhandledException: {e.Exception}\n{e.Exception?.StackTrace}");
        e.Handled = true;
    }

    private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
        var ex = e.ExceptionObject as Exception;
        System.IO.File.WriteAllText(logPath, $"Domain UnhandledException: {ex}\n{ex?.StackTrace}");
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
        System.IO.File.WriteAllText(logPath, $"Task UnobservedTaskException: {e.Exception}\n{e.Exception?.StackTrace}");
        e.SetObserved();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();

            // Initialize services
            ThemeService = new Services.ThemeService();
            NavigationService = new Services.NavigationService();

            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
            System.IO.File.WriteAllText(logPath, $"OnLaunched exception: {ex}\n{ex.StackTrace}");
        }
    }
}
