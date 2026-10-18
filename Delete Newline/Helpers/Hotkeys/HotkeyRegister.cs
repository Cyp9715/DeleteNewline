using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.System;
using Hotkey = (Windows.System.VirtualKeyModifiers Modifiers, Windows.System.VirtualKey Key);

namespace Delete_Newline.Helpers.Hotkeys;

public static class HotkeyRegister
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private static readonly HotkeyRegistrationManager Registrations = new(RegisterNative, UnregisterNative);

    private static bool RegisterNative(Hotkey hotkey)
    {
        uint modifiers = 0x4000; // MOD_NOREPEAT
        if (hotkey.Modifiers.HasFlag(VirtualKeyModifiers.Menu)) modifiers |= 0x0001;
        if (hotkey.Modifiers.HasFlag(VirtualKeyModifiers.Control)) modifiers |= 0x0002;
        if (hotkey.Modifiers.HasFlag(VirtualKeyModifiers.Shift)) modifiers |= 0x0004;
        if (hotkey.Modifiers.HasFlag(VirtualKeyModifiers.Windows)) modifiers |= 0x0008;

        bool result = RegisterHotKey(WindowHandle, HotkeyHasher.GenerateHotkeyHash(hotkey), modifiers, (uint)hotkey.Key);
        if (!result) Debug.WriteLine($"RegisterHotKey failed: {hotkey}, error={Marshal.GetLastWin32Error()}");
        return result;
    }

    private static void UnregisterNative(Hotkey hotkey) =>
        _ = UnregisterHotKey(WindowHandle, HotkeyHasher.GenerateHotkeyHash(hotkey));

    private static nint WindowHandle => MainWindow.hwnd != nint.Zero
        ? MainWindow.hwnd
        : throw new InvalidOperationException("MainWindow hwnd is not initialized.");

    public static bool RegisterHotkey(Hotkey hotkey, HotkeyType type = HotkeyType.Regex) => Registrations.Register(hotkey, type);
    public static void UnregisterHotkey(Hotkey hotkey, HotkeyType type = HotkeyType.Regex) => Registrations.Unregister(hotkey, type);
    public static bool IsHotkeyRegistered(Hotkey hotkey, HotkeyType type) => Registrations.Contains(hotkey, type);

    // Includes temporarily suspended app shortcuts; RegisterNative checks other apps.
    public static bool IsHotkeyRegisteredGlobally(Hotkey hotkey) => Registrations.Contains(hotkey);

    public static int? GetOcrHotkeyId() => Registrations.GetHotkey(HotkeyType.Ocr) is { } hotkey
        ? HotkeyHasher.GenerateHotkeyHash(hotkey) : null;

    public static void SetRecording(object recorder, bool recording) => Registrations.SetRecording(recorder, recording);
    public static void SetWindowActive(bool active) => Registrations.SetWindowActive(active);
    public static void RefreshAllHotkeys() => Registrations.Refresh();
}
