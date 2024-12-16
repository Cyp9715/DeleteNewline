using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class KeybindViewModel : ObservableRecipient
{
    [ObservableProperty]
    private KeybindPageConfiguration? _currentKeybindConfig;

    [RelayCommand]
    private void AddRegexItem()
    {
        if(CurrentKeybindConfig is null || CurrentKeybindConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentKeybindConfig is null.");
        }

        CurrentKeybindConfig.RegexChain.AddChainItem();
    }

    [RelayCommand]
    private void RemoveRegexItem(ChainItem item)
    {
        if (CurrentKeybindConfig is null || CurrentKeybindConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentKeybindConfig is null.");
        }

        CurrentKeybindConfig.RegexChain.ChainItems.Remove(item);
    }
}
