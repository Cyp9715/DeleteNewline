using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Services;
using System.Collections.ObjectModel;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object? selectedItem;

    [ObservableProperty]
    private ObservableCollection<RegexChain> _regexChains;

    public INavigationService NavigationService { get; }
    public INavigationViewService NavigationViewService { get; }
    private KeybindCollectManagerService _keybindCollectManagerService;

    public ShellViewModel(KeybindCollectManagerService keybindCollectManagerService,
        INavigationService navigationService,
        INavigationViewService navigationViewService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;

        NavigationService.Navigated += OnNavigated;
        _regexChains = _keybindCollectManagerService.RegexChains;
    }

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
