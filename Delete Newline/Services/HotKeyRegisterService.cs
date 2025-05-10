using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.System;

namespace Delete_Newline.Services;

[Flags]
public enum Win32Modifiers
{
    None = 0x0000,
    MOD_ALT = 0x0001,
    MOD_CONTROL = 0x0002,
    MOD_SHIFT = 0x0004,
    MOD_WIN = 0x0008,
    MOD_NOREPEAT = 0x4000,
}

public sealed class HotkeyRegisterService
{
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    private IntPtr _hwnd;
    private readonly string _salt = "Delete Newline";

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
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

    private Win32Modifiers MapVirtualModifiersToWin32(VirtualKeyModifiers virtualModifiers)
    {
        Win32Modifiers win32Modifiers = Win32Modifiers.None;

        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Control))
            win32Modifiers |= Win32Modifiers.MOD_CONTROL;
        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Menu))
            win32Modifiers |= Win32Modifiers.MOD_ALT;
        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Shift))
            win32Modifiers |= Win32Modifiers.MOD_SHIFT;
        if (virtualModifiers.HasFlag(VirtualKeyModifiers.Windows))
            win32Modifiers |= Win32Modifiers.MOD_WIN;

        return win32Modifiers;
    }

    public bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        int HotkeyId = HotkeyToHash(Hotkey);
        Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(Hotkey.Item1);
        
        // Try to unregister any existing Hotkey with this ID first
        UnregisterHotKey(_hwnd, HotkeyId);
        
        bool result = RegisterHotKey(_hwnd, HotkeyId, (uint)win32Modifiers, (uint)Hotkey.Item2);

        if (!result)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"RegisterHotkey failed with error code: {errorCode}, ID: {HotkeyId}, _hwnd: {_hwnd}");
        }
        else
        {
            Debug.WriteLine($"RegisterHotkey succeeded. ID={HotkeyId}");
        }
        return result;
    }

    public void UnRegisterHotkey((VirtualKeyModifiers, VirtualKey) Hotkey)
    {
        int HotkeyId = HotkeyToHash(Hotkey);
        if (UnregisterHotKey(_hwnd, HotkeyId) is false)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"UnregisterHotkey failed with error code: {errorCode}, ID: {HotkeyId}, _hwnd: {_hwnd}");
        }
        else
        {
            Debug.WriteLine($"UnregisterHotkey succeeded. ID={HotkeyId}");
        }
    }
}
