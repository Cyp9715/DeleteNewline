using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Windows.System;

namespace Delete_Newline.Services;

public sealed partial class User32
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

public sealed class HotkeyRegister : IHotkeyRegister
{
    private readonly IntPtr hwnd;

    public HotkeyRegister(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public bool RegisterHotkey(int id, (VirtualKeyModifiers, VirtualKey) hotKey)
    {
        return User32.RegisterHotKey(hwnd, id, (uint)hotKey.Item1, (uint)hotKey.Item2);
    }

    public void UnregisterHotkey(int id)
    {
        while (User32.UnregisterHotKey(hwnd, id))
        {
        }
    }
}

public sealed class HotkeySaver
{
    private ILocalSettingsService _localSettingsService;
    private const string HotkeyModifiersSettingsKey = "Modifiers";
    private const string HotkeyKeySettingsKey = "Key";

    public HotkeySaver(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public void SaveHoykey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        
    }
}