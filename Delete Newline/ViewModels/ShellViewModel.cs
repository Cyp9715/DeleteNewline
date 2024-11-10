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
using System.Collections.Specialized;

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
    private NavigationViewItem? _keybindsPageItem;

    public ShellViewModel(KeybindCollectManagerService keybindCollectManagerService,
        INavigationService navigationService,
        INavigationViewService navigationViewService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;

        KeybindConfigs = _keybindCollectManagerService.KeybindConfigs;
        KeybindConfigs.CollectionChanged += KeybindConfigs_CollectionChanged;
    }

    public void InitializeKeybindsPageItem(NavigationViewItem keybindsPageItem)
    {
        _keybindsPageItem = keybindsPageItem;
        SyncKeybindsPageItem();
    }

    private void KeybindConfigs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncKeybindsPageItem();
    }

    private void SyncKeybindsPageItem()
    {
        if (_keybindsPageItem != null)
        {
            _keybindsPageItem.MenuItems.Clear();
            foreach (var config in KeybindConfigs)
            {
                var item = new NavigationViewItem
                {
                    Content = config.RegexChain.ChainName,
                    Icon = new FontIcon { Glyph = "\uE8D3" }
                };
                item.SetValue(NavigationHelper.NavigateToProperty, "Delete_Newline.ViewModels.KeybindViewModel");
                _keybindsPageItem.MenuItems.Add(item);
            }
        }
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
