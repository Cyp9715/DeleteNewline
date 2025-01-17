using Windows.System;

public interface IHotKeyRegister
{
    bool RegistHotKey((VirtualKeyModifiers, VirtualKey) HotKey);
    void UnregistHotKey((VirtualKeyModifiers, VirtualKey) HotKey);
}