using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;
    private readonly Stack<Type> _navigationHistory = new();

    public bool CanGoBack => _frame?.CanGoBack == true;

    public event EventHandler<Type>? Navigated;

    public void SetFrame(Frame frame)
    {
        _frame = frame;
        _frame.Navigated += OnFrameNavigated;
    }

    public bool NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null)
            return false;

        if (_frame.CurrentSourcePageType == pageType)
            return false;

        bool result = _frame.Navigate(pageType, parameter);
        if (result)
        {
            _navigationHistory.Push(pageType);
            Navigated?.Invoke(this, pageType);
        }
        return result;
    }

    public bool GoBack()
    {
        if (_frame == null || !_frame.CanGoBack)
            return false;

        _frame.GoBack();
        if (_navigationHistory.Count > 0)
        {
            _navigationHistory.Pop();
        }
        return true;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        // Navigation completed
    }
}
