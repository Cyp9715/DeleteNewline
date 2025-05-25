using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Services;

public static class HotkeyFormatterService
{
    public static string FormatHotkey(HotkeyStructure Hotkey)
    {
        if (Hotkey == null || Hotkey.Modifiers == VirtualKeyModifiers.None || Hotkey.Key == VirtualKey.None)
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
            .Where(flag => Hotkey.Modifiers.HasFlag(flag))
            .Select(mod => GetModifierString(mod))
            .Where(str => !string.IsNullOrEmpty(str));

        // Key to string
        var keyString = GetKeyString(Hotkey.Key);

        return string.Join(" + ", modifierStrings.Concat(new[] { keyString }));
    }

    // Modifier to string
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

    // Key to string
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
} 