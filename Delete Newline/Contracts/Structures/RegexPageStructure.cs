using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace Delete_Newline.Contracts.Structures;

public partial class RegexPageStructure : ObservableObject
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

    [ObservableProperty]
    private bool _isRegistrationFailed = false;

    public bool ShouldSerializeIsRegistrationFailed()
    {
        return false;
    }
}