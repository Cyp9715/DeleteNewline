using Windows.System;

public interface IHotkeyManager
{
    bool RegisterHotkey(int id, (VirtualKeyModifiers, VirtualKey) hotKey);
    void UnregisterHotkey(int id);
}