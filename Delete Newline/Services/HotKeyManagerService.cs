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

public sealed class HotkeyManager : IHotkeyManager
{
    private readonly IntPtr hwnd;

    public HotkeyManager(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public bool RegisterHotkey(int id, (VirtualKeyModifiers, VirtualKey) hotKey)
    {
        if (Enum.IsDefined(typeof(VirtualKeyModifiers), hotKey.Item1) is false ||
            Enum.IsDefined(typeof(VirtualKey), hotKey.Item2) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(hotKey.Item1), "Invalid VirtualKey value.");
        }

        return User32.RegisterHotKey(hwnd, id, (uint)hotKey.Item1, (uint)hotKey.Item2);
    }

    public void UnregisterHotkey(int id)
    {
        while (User32.UnregisterHotKey(hwnd, id))
        {
        }
    }
}