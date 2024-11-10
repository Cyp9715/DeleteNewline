using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Navigation;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Views;
using Delete_Newline.Services;
using System.Collections.ObjectModel;
using Delete_Newline.Contracts.Structures;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Helpers;

namespace Delete_Newline.ViewModels;

public partial class ShellViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object? selectedItem;

    [ObservableProperty]
    private ObservableCollection<KeybindPageConfiguration> _keybindConfigs;

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

        KeybindConfigs = _keybindCollectManagerService.KeybindConfigs;
    }

    [RelayCommand]
    private void PageLoaded()
    {
        // Set MemoPage as the default selected item
        SelectedItem = NavigationViewService.GetSelectedItem(typeof(MemoPage));
    }

    [RelayCommand]
    private void NavigationViewItem_Tapped(object sender)
    {
        // sender가 NavigationViewItem 타입으로 캐스팅 가능한지 확인
        if (sender is NavigationViewItem item)
        {
            // 선택된 아이템을 업데이트
            SelectedItem = item;

            // NavigationHelper에서 설정한 타겟 ViewModel로 내비게이션 수행
            var targetViewModel = item.GetValue(NavigationHelper.NavigateToProperty) as Type;
            if (targetViewModel != null)
            {
                SelectedItem = targetViewModel;
            }
        }
    }
}
