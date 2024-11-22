using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.ViewModels;

public partial class KeybindViewModel : ObservableRecipient
{
    [ObservableProperty]
    private KeybindPageConfiguration _currentKeybindConfig;

    [RelayCommand]
    private void AddRegexItem()
    {
        CurrentKeybindConfig.RegexChain.AddChainItem();
    }

    [RelayCommand]
    private void RemoveRegexItem(ChainItem item)
    {
        CurrentKeybindConfig.RegexChain.ChainItems.Remove(item);
    }
}
