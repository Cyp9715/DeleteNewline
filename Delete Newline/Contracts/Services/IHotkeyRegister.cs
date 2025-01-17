using Windows.System;

public interface IHotkeyRegister
{
    bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey);
    void UnregisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey);
}