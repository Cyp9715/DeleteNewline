using Windows.System;

public interface IHotkeyRegister
{
    bool RegisterHotkey(int id, (VirtualKeyModifiers, VirtualKey) hotKey);
    void UnregisterHotkey(int id);
}