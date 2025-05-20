using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public class KeyboardInputEventArgs
{
    public VirtualKeyModifiers Modifiers { get; set; }
    public VirtualKey Key { get; set; }
}
