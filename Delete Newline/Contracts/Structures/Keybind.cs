using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public partial class Keybind : ObservableRecipient
{
    [ObservableProperty]
    public ObservableCollection<VirtualKeyModifiers>? _keyModifiers;
    [ObservableProperty]
    public VirtualKey _key;
    [ObservableProperty]
    public string? _displayHotKeys;

    public Keybind()
    {

    }
}