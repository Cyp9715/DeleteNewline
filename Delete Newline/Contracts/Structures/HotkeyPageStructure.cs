using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyPageStructure : ObservableObject
{
    [ObservableProperty]
    public string _HotkeyName = "New Hotkey";

    [ObservableProperty]
    public string _HotkeyComment = "Comment";

    [ObservableProperty]
    public HotkeyStructure _Hotkey = new HotkeyStructure();

    [ObservableProperty]
    public RegexChainStructure _regexChain = new RegexChainStructure();

    [ObservableProperty]
    public string _inputText = "";
}