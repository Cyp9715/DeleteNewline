using Windows.System;

public interface IHotKeyRegister
{
    bool RegisterHotKey((VirtualKeyModifiers, VirtualKey) HotKey);
    void UnregisterHotKey((VirtualKeyModifiers, VirtualKey) HotKey);
}