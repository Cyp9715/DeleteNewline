using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class KeybindCollectViewModel : ObservableRecipient
{
    private readonly KeybindCollectManagerService _keybindCollectManagerService;

    [ObservableProperty]
    public ObservableCollection<KeybindPageConfiguration> _keybindConfigs;

    public KeybindCollectViewModel(KeybindCollectManagerService keybindCollectManagerService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
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
