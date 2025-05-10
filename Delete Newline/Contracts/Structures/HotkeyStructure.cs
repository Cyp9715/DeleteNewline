using CommunityToolkit.Mvvm.ComponentModel;
using Delete_Newline.Helpers;
using System.Text.Json.Serialization;
using Windows.System;
using System.Runtime.InteropServices;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyStructure : ObservableRecipient
{
    [ObservableProperty]
    public VirtualKeyModifiers _modifiers;

    [ObservableProperty]
    public VirtualKey _key;

    [JsonIgnore]
    public new bool IsActive
    {
        get => base.IsActive;
        set => base.IsActive = value;
    }

    public HotkeyStructure()
    {
    }

    public override string ToString()
    {
        return HotkeyDisplayHelper.FormatHotkey(this);
    }
}
