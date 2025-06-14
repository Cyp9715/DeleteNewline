using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.System;

namespace Delete_Newline.Helpers.Hotkeys;


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

public static class HotkeyRegister
{
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    private static extern bool RegisterHotKey(
        nint hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    private static extern bool UnregisterHotKey(
        nint hWnd,
        int id);

    private static nint _hwnd;
    private static readonly Dictionary<HotkeyType, HashSet<(VirtualKeyModifiers, VirtualKey)>> _registeredHotkeys;

    static HotkeyRegister()
    {
        _registeredHotkeys = new Dictionary<HotkeyType, HashSet<(VirtualKeyModifiers, VirtualKey)>>();

        // Initialize HashSets for all HotkeyType values
        foreach (HotkeyType type in Enum.GetValues<HotkeyType>())
        {
            _registeredHotkeys[type] = new HashSet<(VirtualKeyModifiers, VirtualKey)>();
        }

        if (MainWindow.hwnd != nint.Zero)
            _hwnd = MainWindow.hwnd;
        else
            // If hwnd is not set, we need to ensure it's initialized properly
            throw new InvalidOperationException("MainWindow hwnd is not initialized. Ensure MainWindow is created before using HotkeyRegister.");
    }

    private static Win32Modifiers MapVirtualModifiersToWin32(VirtualKeyModifiers virtualModifiers)
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

    public static bool IsHotkeyRegistered((VirtualKeyModifiers, VirtualKey) hotkey, HotkeyType type)
    {
        return _registeredHotkeys[type].Contains(hotkey);
    }

    public static bool RegisterHotkey((VirtualKeyModifiers, VirtualKey) hotkey, HotkeyType type = HotkeyType.Regex)
    {
        if (IsHotkeyRegistered(hotkey, type))
            return false;

        int hotkeyId = HotkeyHasher.GenerateHotkeyHash(hotkey);
        Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(hotkey.Item1);

        bool result = RegisterHotKey(_hwnd, hotkeyId, (uint)win32Modifiers, (uint)hotkey.Item2);

        if (result)
        {
            _registeredHotkeys[type].Add(hotkey);
            Debug.WriteLine($"RegisterHotkey succeeded. Type={type}, ID={hotkeyId}");
        }
        else
        {
            Debug.WriteLine($"RegisterHotkey failed. Type={type}, ID={hotkeyId}");
        }

        return result;
    }

    public static void UnregisterHotkey((VirtualKeyModifiers, VirtualKey) hotkey, HotkeyType type = HotkeyType.Regex)
    {
        if (!_registeredHotkeys[type].Contains(hotkey))
            return;

        int hotkeyId = HotkeyHasher.GenerateHotkeyHash(hotkey);

        // Always try to unregister from OS
        if (UnregisterHotKey(_hwnd, hotkeyId))
        {
            Debug.WriteLine($"UnregisterHotkey succeeded. Type={type}, ID={hotkeyId}");
            _registeredHotkeys[type].Remove(hotkey);
        }
        else
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.WriteLine($"UnregisterHotkey failed with error code: {errorCode}, ID: {hotkeyId}, _hwnd: {_hwnd}");
        }
    }

    public static int? GetOcrHotkeyId()
    {
        var ocrHotkey = _registeredHotkeys[HotkeyType.Ocr].FirstOrDefault();
        return ocrHotkey != default ? HotkeyHasher.GenerateHotkeyHash(ocrHotkey) : null;
    }

    public static void DisableAllHotkeys()
    {
        foreach (var type in _registeredHotkeys.Keys)
        {
            foreach (var hotkey in _registeredHotkeys[type])
            {
                int hotkeyId = HotkeyHasher.GenerateHotkeyHash(hotkey);
                UnregisterHotKey(_hwnd, hotkeyId);
            }
        }
        Debug.WriteLine("All hotkeys disabled");
    }

    public static void EnableAllHotkeys()
    {
        foreach (var type in _registeredHotkeys.Keys)
        {
            foreach (var hotkey in _registeredHotkeys[type])
            {
                int hotkeyId = HotkeyHasher.GenerateHotkeyHash(hotkey);
                Win32Modifiers win32Modifiers = MapVirtualModifiersToWin32(hotkey.Item1);
                RegisterHotKey(_hwnd, hotkeyId, (uint)win32Modifiers, (uint)hotkey.Item2);
            }
        }
        Debug.WriteLine("All hotkeys re-enabled");
    }
}