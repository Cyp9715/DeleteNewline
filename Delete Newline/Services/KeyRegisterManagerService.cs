using System.Runtime.InteropServices;
using Windows.System;
using static Delete_Newline.Services.User32;

namespace Delete_Newline.Services;

public sealed partial class User32
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

public sealed record Hotkey
{
    public Hotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (Validate(modifiers, key) is false)
            throw new ArgumentOutOfRangeException(null, "Invalid hotkey combination.");

        Modifiers = modifiers;
        Key = key;
    }

    public Hotkey()
    {
    }

    public static Func<VirtualKey, string> KeyPrinter { get; set; } = key => key.ToString();
    public static Hotkey Empty { get; } = new();
    public VirtualKeyModifiers Modifiers { get; init; } = default;
    public VirtualKey Key { get; init; } = default;

    public static bool Validate(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if ((modifiers, key) != default)
        {
            if (modifiers.HasFlag(VirtualKeyModifiers.Control) is false && 
                modifiers.HasFlag(VirtualKeyModifiers.Menu) is false)
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString()
    {
        var keys = new List<string>(4);

        Span<VirtualKeyModifiers> mods = stackalloc VirtualKeyModifiers[4]
        {
            VirtualKeyModifiers.Control,
            VirtualKeyModifiers.Shift,
            VirtualKeyModifiers.Menu,
            VirtualKeyModifiers.Windows,
        };

        foreach (var mod in mods)
        {
            if (Modifiers.HasFlag(mod))
                keys.Add(GetPrettyModifier(mod));
        }

        if (!Modifiers.HasFlag(VirtualKeyModifiers.Control) &&
            !Modifiers.HasFlag(VirtualKeyModifiers.Menu))
        {
            return string.Empty;
        }

        keys.Add(KeyPrinter(Key));
        return string.Join(" + ", keys);
    }

    private static string GetPrettyModifier(VirtualKeyModifiers modifier)
    {
        return modifier switch
        {
            VirtualKeyModifiers.Control => "Ctrl",
            VirtualKeyModifiers.Shift => "Shift",
            VirtualKeyModifiers.Menu => "Alt",
            VirtualKeyModifiers.Windows => "Windows",
            VirtualKeyModifiers.None => "None",
            _ => throw new NotImplementedException(),
        };
    }
}

public sealed class HotkeyManager
{
    private readonly IntPtr hwnd;

    public HotkeyManager(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public bool RegisterHotkey(int id, (VirtualKeyModifiers, VirtualKey) hotKey)
    {
        if (Enum.IsDefined(typeof(VirtualKeyModifiers), hotKey.Item1) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(hotKey.Item1), "Invalid VirtualKeyModifiers value.");
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

public sealed class KeyRegisterManagerService
{
    public static bool IsValidate(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (modifiers is VirtualKeyModifiers.None || key is VirtualKey.None)
            return false;

        // IntPtr.Zero is used to register a global hotkey across the entire system, not tied to any specific window handle.
        if (RegisterHotKey(IntPtr.Zero, 0, (uint)modifiers, (uint)key))
        {
            UnregisterHotKey(IntPtr.Zero, 0);
            return true;
        }

        return false;
    }
}