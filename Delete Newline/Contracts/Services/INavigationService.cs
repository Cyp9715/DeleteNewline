using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Delete_Newline.Contracts.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack { get; }
    
    IList<object>? MenuItems { get; }
    
    object? SettingsItem { get; }

    Frame? Frame { get; set; }

    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    bool GoBack();
    
    void InitializeNavigationView(NavigationView navigationView);
    
    void UnregisterNavigationViewEvents();
    
    NavigationViewItem? GetSelectedItem(Type pageType);
}
