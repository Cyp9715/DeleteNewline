using CommunityToolkit.Mvvm.ComponentModel;

namespace Delete_Newline.Contracts.Structures;

public partial class KeybindPageConfiguration : ObservableObject
{
    [ObservableProperty]
    public Keybind? _keybind;

    [ObservableProperty]
    public RegexChain? _regexChain;

    [ObservableProperty]
    public string? _testText;
}