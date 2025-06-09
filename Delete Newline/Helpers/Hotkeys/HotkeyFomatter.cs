using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Helpers;


public static class HotkeyFormatter
{
    public static string FormatHotkey(HotkeyStructure hotkey)
    {
        if (hotkey == null || hotkey.Modifiers == VirtualKeyModifiers.None || hotkey.Key == VirtualKey.None)
        {
            return string.Empty;
        }

        var allModifiers = new[]
        {
            VirtualKeyModifiers.Control,
            VirtualKeyModifiers.Menu,
            VirtualKeyModifiers.Shift,
            VirtualKeyModifiers.Windows
        };

        // Modifier to string
        var modifierStrings = allModifiers
            .Where(flag => hotkey.Modifiers.HasFlag(flag))
            .Select(mod => GetModifierString(mod))
            .Where(str => !string.IsNullOrEmpty(str));

        // Key to string
        var keyString = GetKeyString(hotkey.Key);

        return string.Join(" + ", modifierStrings.Concat(new[] { keyString }));
    }

    private static string GetModifierString(VirtualKeyModifiers modifier)
    {
        return modifier switch
        {
            VirtualKeyModifiers.Control => "Ctrl",
            VirtualKeyModifiers.Menu => "Alt",
            VirtualKeyModifiers.Shift => "Shift",
            VirtualKeyModifiers.Windows => "Win",
            _ => string.Empty,
        };
    }

    private static string GetKeyString(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.Space => "Space",
            VirtualKey.Escape => "Esc",
            VirtualKey.Left => "Left Arrow",
            VirtualKey.Right => "Right Arrow",
            _ => key.ToString(),
        };
    }

    public static string GetDisplayText(RegexPageStructure config)
    {
        if (config.Hotkey == null)
        {
            return string.Empty;
        }

        return FormatHotkey(config.Hotkey);
    }

    public static string GetDisplayText(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (modifiers == VirtualKeyModifiers.None && key == VirtualKey.None)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        if (modifiers.HasFlag(VirtualKeyModifiers.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
            parts.Add("Alt");
        if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
            parts.Add("Win");

        if (key != VirtualKey.None)
            parts.Add(key.ToString());

        return string.Join(" + ", parts);
    }

    public static string GetDisplayText((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return GetDisplayText(hotkey.Item1, hotkey.Item2);
    }
}


