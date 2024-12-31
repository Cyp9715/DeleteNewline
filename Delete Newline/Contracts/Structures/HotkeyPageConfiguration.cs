using CommunityToolkit.Mvvm.ComponentModel;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyPageConfiguration : ObservableObject
{
    [ObservableProperty]
    public string _hotkeyName = "New Hotkey";

    [ObservableProperty]
    public string _hotkeyComment = "Comment";

    [ObservableProperty]
    public Hotkey? _hotkey;

    [ObservableProperty]
    public RegexChain? _regexChain;

    [ObservableProperty]
    public string? _testText;
}