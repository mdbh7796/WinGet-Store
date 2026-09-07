using Microsoft.UI.Xaml.Controls;

namespace WinGetStore.Services.Interfaces;

public interface INavigationService
{
    bool NavigateTo(Type pageType, object? parameter = null);
    bool GoBack();
    bool CanGoBack { get; }
    event EventHandler<Type>? Navigated;
}
