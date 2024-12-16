using Delete_Newline.Contracts.Structures;
using System.Runtime.InteropServices;
using static Delete_Newline.Services.User32;

namespace Delete_Newline.Services;

public sealed partial class User32
{
    public enum Modifiers
    {
        MOD_ALT = 1,
        MOD_CONTROL = 2,
        MOD_SHIFT = 4,
        MOD_WIN = 8,
        MOD_NOREPEAT = 0x4000,
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, Modifiers fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

public sealed record Hotkey
{
    public Hotkey(KeyModifiers modifiers, Key key)
    {
        if (!Validate(modifiers, key))
            throw new ArgumentOutOfRangeException(null, "Invalid hotkey combination.");

        Modifiers = modifiers;
        Key = key;
    }

    public Hotkey()
    {
    }

    public static Func<Key, string> KeyPrinter { get; set; } = key => key.ToString();
    public static Hotkey Empty { get; } = new();
    public KeyModifiers Modifiers { get; init; } = default;
    public Key Key { get; init; } = default;

    public static bool Validate(KeyModifiers modifiers, Key key)
    {
        if ((modifiers, key) != default)
        {
            if (!modifiers.HasFlag(KeyModifiers.Control)
                && !modifiers.HasFlag(KeyModifiers.Menu))
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString()
    {
        var keys = new List<string>(4);

        Span<KeyModifiers> mods = stackalloc KeyModifiers[4]
        {
            KeyModifiers.Control,
            KeyModifiers.Shift,
            KeyModifiers.Menu,
            KeyModifiers.Windows,
        };

        foreach (var mod in mods)
        {
            if (Modifiers.HasFlag(mod))
                keys.Add(GetPrettyModifier(mod));
        }

        if (!Modifiers.HasFlag(KeyModifiers.Control) &&
            !Modifiers.HasFlag(KeyModifiers.Menu))
        {
            return string.Empty;
        }

        keys.Add(KeyPrinter(Key));
        return string.Join(" + ", keys);
    }

    private static string GetPrettyModifier(KeyModifiers modifier)
    {
        return modifier switch
        {
            KeyModifiers.Control => "Ctrl",
            KeyModifiers.Shift => "Shift",
            KeyModifiers.Menu => "Alt",
            KeyModifiers.Windows => "Windows",
            KeyModifiers.None => "None",
            _ => throw new NotImplementedException(),
        };
    }
}

public static class Mapping
{
    public static Modifiers ToModifiers(this KeyModifiers modifiers)
    {
        Modifiers result = 0;

        if (modifiers.HasFlag(KeyModifiers.Control))
            result |= Modifiers.MOD_CONTROL;

        if (modifiers.HasFlag(KeyModifiers.Menu))
            result |= Modifiers.MOD_ALT;

        if (modifiers.HasFlag(KeyModifiers.Shift))
            result |= Modifiers.MOD_SHIFT;

        if (modifiers.HasFlag(KeyModifiers.Windows))
            result |= Modifiers.MOD_WIN;

        return result;
    }
}

public sealed class HotkeyManager
{
    private readonly IntPtr hwnd;

    public HotkeyManager(IntPtr hwnd)
    {
        this.hwnd = hwnd;
    }

    public void RegisterHotkey(int id, Hotkey hotkey)
    {
        if (hotkey != null)
            User32.RegisterHotKey(hwnd, id, hotkey.Modifiers.ToModifiers(), (uint)hotkey.Key);
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
    public static bool IsValidate(KeyModifiers modifiers, Key key)
    {
        if ((modifiers, key) != default)
        {
            if (!modifiers.HasFlag(KeyModifiers.Control)&&
                !modifiers.HasFlag(KeyModifiers.Menu))
            {
                return false;
            }
        }

        return true;
    }
}