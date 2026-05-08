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

    public string DisplayText => HotkeyFormatter.GetDisplayText(Modifiers, Key);

    public HotkeyStructure()
    {
    }

    partial void OnModifiersChanged(VirtualKeyModifiers value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    partial void OnKeyChanged(VirtualKey value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    public override string ToString()
    {
        return DisplayText;
    }
}
