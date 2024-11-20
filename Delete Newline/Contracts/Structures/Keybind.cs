using CommunityToolkit.Mvvm.ComponentModel;
using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public partial class Keybind : ObservableObject
{
    [ObservableProperty]
    public VirtualKey _key1;
    public VirtualKey _key2;

    public Keybind()
    {

    }
}