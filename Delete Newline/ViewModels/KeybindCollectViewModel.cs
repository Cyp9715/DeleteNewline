using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class KeybindCollectViewModel : ObservableRecipient
{
    private readonly KeybindCollectManagerService _keybindCollectManagerService;
    public INavigationService NavigationService { get; }

    [ObservableProperty]
    public ObservableCollection<KeybindPageConfiguration> _keybindConfigs;

    public KeybindCollectViewModel(KeybindCollectManagerService keybindCollectManagerService,
        INavigationService navigationService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
        NavigationService = navigationService;
        KeybindConfigs = _keybindCollectManagerService.KeybindConfigs;
    }

    [RelayCommand]
    private void AddKeybind()
    {
        _keybindCollectManagerService.AddKeybindConfig();
    }

    [RelayCommand]
    private void RemoveKeybind(KeybindPageConfiguration keybindConfig)
    {
        if (keybindConfig is not null)
        {
            _keybindCollectManagerService.RemoveKeybindConfig(keybindConfig);
        }
    }

    [RelayCommand]
    private void NavigateToKeybindPage(KeybindPageConfiguration keybindConfig)
    {
        App.GetService<KeybindViewModel>().CurrentKeybindConfig = keybindConfig;
        NavigationService.NavigateTo(typeof(KeybindViewModel).FullName!);
    }

    public static bool isDragEnded = true;

    [RelayCommand]
    private void DragItemsStarting()
    {
        isDragEnded = false;
    }

    [RelayCommand]
    private void DragItemsCompleted()
    {
        isDragEnded = true;
    }
}
