using CommunityToolkit.Mvvm.ComponentModel;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using System.Collections.ObjectModel;

namespace Delete_Newline.ViewModels;

public partial class KeybindCollectViewModel : ObservableRecipient
{
    private readonly KeybindCollectManagerService _keybindCollectManagerService;

    [ObservableProperty]
    private ObservableCollection<RegexChain> _regexChains;

    public KeybindCollectViewModel(KeybindCollectManagerService keybindCollectManagerService)
    {
        _keybindCollectManagerService = keybindCollectManagerService;
        RegexChains = new ObservableCollection<RegexChain>(_keybindCollectManagerService.GetRegexChains());
    }
}
