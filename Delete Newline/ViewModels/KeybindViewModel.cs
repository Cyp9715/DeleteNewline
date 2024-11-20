using CommunityToolkit.Mvvm.ComponentModel;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.ViewModels;

public partial class KeybindViewModel : ObservableRecipient
{
    [ObservableProperty]
    public KeybindPageConfiguration? _currentKeybindConfig;
}
