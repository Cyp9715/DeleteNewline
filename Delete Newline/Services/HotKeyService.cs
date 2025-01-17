using Windows.System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Cryptography;

namespace Delete_Newline.Services;

public sealed partial class User32
{
    public const int WM_Hotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    public static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    public static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);
}


public sealed class HotkeyRegister : IHotkeyRegister
{
    private readonly IntPtr _hwnd;
    private readonly string _salt = "Delete Newline";

    public HotkeyRegister(IntPtr hwnd)
    {
        this._hwnd = hwnd;
    }

    private int HotkeyToHash((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        string HotkeyString = $"{_salt}:{Hotkey.Item1}:{Hotkey.Item2}";

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(HotkeyString));
            int hashInt = BitConverter.ToInt32(hashBytes, 0);
            return Math.Abs(hashInt);
        }
    }

    public bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        int hotkeyId = HotkeyToHash(Hotkey);
        bool result = User32.RegisterHotKey(_hwnd, hotkeyId, (uint)Hotkey.Item1, (uint)Hotkey.Item2);
        System.Diagnostics.Debug.WriteLine($"_hwnd : {_hwnd}");
        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"RegisterHotkey failed with error code: {errorCode}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"RegisterHotkey result: {result}");
        }
        return result;
    }

    public void UnregisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        while (User32.UnregisterHotKey(_hwnd, HotkeyToHash(Hotkey)))
        {
        }
    }
}