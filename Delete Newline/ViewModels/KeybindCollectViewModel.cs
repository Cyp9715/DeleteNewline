using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using System.Collections.ObjectModel;

namespace Delete_Newline.ViewModels;

public partial class KeybindCollectViewModel : ObservableRecipient
{
    private readonly KeybindCollectManagerService _keybindCollectManagerService;

    [ObservableProperty]
    public ObservableCollection<KeybindInfo> _keybindInfos;

    public KeybindCollectViewModel(KeybindCollectManagerService keybindCollectManagerService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
        KeybindInfos = _keybindCollectManagerService.KeybindInfos;
    }

    [RelayCommand]
    private void AddKeybind()
    {
        _keybindCollectManagerService.AddKeybindInfo();
    }

    [RelayCommand]
    private void RemoveKeybind(KeybindInfo keybindInfo)
    {
        if (keybindInfo is not null)
        {
            _keybindCollectManagerService.RemoveKeybindInfo(keybindInfo);
        }
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
