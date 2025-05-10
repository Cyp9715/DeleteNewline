using CommunityToolkit.Mvvm.ComponentModel;
using Delete_Newline.Helpers;
using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyStructure : ObservableObject
{
    [ObservableProperty]
    public VirtualKeyModifiers _modifiers;

    [ObservableProperty]
    public VirtualKey _key;

    public HotkeyStructure()
    {
    }

    public override string ToString()
    {
        return HotkeyDisplayHelper.FormatHotkey(this);
    }
}
