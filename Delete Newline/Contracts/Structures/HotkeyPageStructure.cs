using CommunityToolkit.Mvvm.ComponentModel;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyPageStructure : ObservableObject
{
    [ObservableProperty]
    public string _HotkeyName = "New Hotkey";

    [ObservableProperty]
    public string _HotkeyComment = "Comment";

    [ObservableProperty]
    public HotkeyStructure? _Hotkey;

    [ObservableProperty]
    public RegexChainStructure? _regexChain;

    [ObservableProperty]
    public string _inputText = "";
}