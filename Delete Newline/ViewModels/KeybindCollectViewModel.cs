using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using System.Collections.ObjectModel;

namespace Delete_Newline.ViewModels;

public partial class KeybindCollectViewModel : ObservableRecipient
{
    private readonly KeybindCollectManagerService _keybindCollectManagerService;

    [ObservableProperty]
    public ObservableCollection<RegexChain> _regexChains;


    public KeybindCollectViewModel(KeybindCollectManagerService keybindCollectManagerService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
        RegexChains = _keybindCollectManagerService.RegexChains;
    }

    [RelayCommand]
    public void AddKeybind()
    {
        _keybindCollectManagerService.AddRegexChain();
    }

    [RelayCommand]
    public void RemoveKeybind(RegexChain chain)
    {
        if (chain is not null)
        {
            _keybindCollectManagerService.RemoveRegexChain(chain);
        }
    }
}
