using CommunityToolkit.Mvvm.ComponentModel;

namespace Delete_Newline.Contracts.Structures;

public partial class KeybindPageConfiguration : ObservableObject
{
    [ObservableProperty]
    public string _keybindName = "New Keybind";

    [ObservableProperty]
    public string _keybindComment = "Comment";

    [ObservableProperty]
    public Keybind? _keybind;

    [ObservableProperty]
    public RegexChain? _regexChain;

    [ObservableProperty]
    public string? _testText;
}