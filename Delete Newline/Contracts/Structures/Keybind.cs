using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public class Keybind
{
    public VirtualKey Key1 { get; set; } = VirtualKey.None;
    public VirtualKey Key2 { get; set; } = VirtualKey.None;

    public Keybind()
    {

    }
}