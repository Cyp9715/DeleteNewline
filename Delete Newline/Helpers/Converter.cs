using Windows.System;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Helpers;

public static class Converter
{
    public static KeyModifiers ToKeyModifiers(this VirtualKeyModifiers key)
    {
        return (KeyModifiers)key;
    }

    public static Key ToKey(this VirtualKey key)
    {
        return (Key)key;
    }
}

