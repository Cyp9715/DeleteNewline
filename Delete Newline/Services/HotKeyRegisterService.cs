using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.System;
using Delete_Newline.Helpers;

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

public enum HotkeyType
{
    Regex,
    Ocr
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
    private readonly Dictionary<HotkeyType, HashSet<(VirtualKeyModifiers, VirtualKey)>> _registeredHotkeys;

    public HotkeyRegisterService()
    {
        _registeredHotkeys = new Dictionary<HotkeyType, HashSet<(VirtualKeyModifiers, VirtualKey)>>();
        
        // Initialize HashSets for all HotkeyType values
        foreach (HotkeyType type in Enum.GetValues<HotkeyType>())
        {
            _registeredHotkeys[type] = new HashSet<(VirtualKeyModifiers, VirtualKey)>();
        }
    }

    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
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

    public bool IsHotkeyRegistered((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        // Check if hotkey is registered in any type using LINQ
        return _registeredHotkeys.Values.Any(hotkeySet => hotkeySet.Contains(hotkey));
    }

    public bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) hotkey, HotkeyType type = HotkeyType.Regex)
    {
        // Check if hotkey is registered in any type
        if (IsHotkeyRegistered(hotkey))
        {
            Debug.WriteLine($"Hotkey already registered: {hotkey.Item1} + {hotkey.Item2}");
            return false;
        }

        int hotkeyId = HotkeyHasher.GenerateHotkeyHash(hotkey);
        Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(hotkey.Item1);
        
        // Try to unregister any existing hotkey with this ID first
        UnregisterHotKey(_hwnd, hotkeyId);
        
        bool result = RegisterHotKey(_hwnd, hotkeyId, (uint)win32Modifiers, (uint)hotkey.Item2);

        if (!result)
        {
            Debug.WriteLine($"RegisterHotkey failed. Type={type}, ID={hotkeyId}");
        }
        else
        {
            Debug.WriteLine($"RegisterHotkey succeeded. Type={type}, ID={hotkeyId}");
            _registeredHotkeys[type].Add(hotkey);
        }

        return result;
    }

    public void UnregisterHotkey((VirtualKeyModifiers, VirtualKey) hotkey, HotkeyType type = HotkeyType.Regex)
    {
        int hotkeyId = HotkeyHasher.GenerateHotkeyHash(hotkey);

        if (UnregisterHotKey(_hwnd, hotkeyId))
        {
            Debug.WriteLine($"UnregisterHotkey succeeded. Type={type}, ID={hotkeyId}");
            _registeredHotkeys[type].Remove(hotkey);
        }
        else
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"UnregisterHotkey failed with error code: {errorCode}, ID: {hotkeyId}, _hwnd: {_hwnd}");

            // Even if OS unregistration fails, remove from our tracking
            if (errorCode == 1419) // ERROR_HOTKEY_NOT_REGISTERED
            {
                _registeredHotkeys[type].Remove(hotkey);
            }
        }
    }

    public int? GetOcrHotkeyId()
    {
        var ocrHotkey = _registeredHotkeys[HotkeyType.Ocr].FirstOrDefault();
        return ocrHotkey != default ? HotkeyHasher.GenerateHotkeyHash(ocrHotkey) : null;
    }
}
