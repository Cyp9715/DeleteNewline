using CommunityToolkit.Mvvm.ComponentModel;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyPageStructure : ObservableObject
{
    [ObservableProperty]
    public string _hotkeyName = "New Hotkey";

    [ObservableProperty]
    public string _hotkeyComment = "Comment";

    [ObservableProperty]
    public HotkeyStructure? _hotkey;

    [ObservableProperty]
    public RegexChainStructure? _regexChain;
}