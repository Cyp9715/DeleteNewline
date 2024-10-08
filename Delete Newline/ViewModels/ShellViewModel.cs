using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object? selectedItem;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
    }

    private bool _isInitialNavigation = true;

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (e.SourcePageType == typeof(SettingsPage))
        {
            SelectedItem = NavigationViewService.SettingsItem;
            return;
        }

        var item = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (item != null)
        {
            SelectedItem = item;
        }
    }
}
