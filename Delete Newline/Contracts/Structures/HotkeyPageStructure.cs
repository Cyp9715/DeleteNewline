using CommunityToolkit.Mvvm.ComponentModel;

namespace Delete_Newline.Contracts.Structures;

public partial class HotKeyPageStructure : ObservableObject
{
    [ObservableProperty]
    public string _HotKeyName = "New HotKey";

    [ObservableProperty]
    public string _HotKeyComment = "Comment";

    [ObservableProperty]
    public HotKeyStructure _HotKey = new HotKeyStructure();

    [ObservableProperty]
    public RegexChainStructure _regexChain = new RegexChainStructure();

    [ObservableProperty]
    public string _inputText = "";
}