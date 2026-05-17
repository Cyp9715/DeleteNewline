using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Helpers;


public static class HotkeyFormatter
{
    private const int OemSemicolon = 0xBA;
    private const int OemEquals = 0xBB;
    private const int OemComma = 0xBC;
    private const int OemMinus = 0xBD;
    private const int OemPeriod = 0xBE;
    private const int OemSlash = 0xBF;
    private const int OemBacktick = 0xC0;
    private const int OemLeftBracket = 0xDB;
    private const int OemBackslash = 0xDC;
    private const int OemRightBracket = 0xDD;
    private const int OemQuote = 0xDE;

    private static readonly Dictionary<int, (string KeyText, string DisplayText)> OemKeyTexts = new()
    {
        [OemSemicolon] = ("Semicolon", ";"),
        [OemEquals] = ("Equals", "="),
        [OemComma] = ("Comma", ","),
        [OemMinus] = ("Minus", "-"),
        [OemPeriod] = ("Period", "."),
        [OemSlash] = ("Slash", "/"),
        [OemBacktick] = ("Backtick", "`"),
        [OemLeftBracket] = ("LeftBracket", "["),
        [OemBackslash] = ("Backslash", "\\"),
        [OemRightBracket] = ("RightBracket", "]"),
        [OemQuote] = ("Quote", "'")
    };

    public static string FormatHotkey(HotkeyStructure? hotkey)
    {
        if (hotkey == null || hotkey.Modifiers == VirtualKeyModifiers.None || hotkey.Key == VirtualKey.None)
        {
            return string.Empty;
        }

        return GetDisplayText(hotkey.Modifiers, hotkey.Key);
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
            parts.Add(GetDisplayKeyText(key));

        return string.Join(" + ", parts);
    }

    public static string GetDisplayText((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return GetDisplayText(hotkey.Item1, hotkey.Item2);
    }

    public static string GetKeyText(VirtualKey key)
    {
        if (key == VirtualKey.None)
        {
            return string.Empty;
        }

        if (TryGetDigitKeyNumber(key, out int digit))
        {
            return $"Number{digit}";
        }

        if (TryGetNumberPadKeyNumber(key, out int numberPadDigit))
        {
            return $"NumberPad{numberPadDigit}";
        }

        if (OemKeyTexts.TryGetValue((int)key, out var oemText))
        {
            return oemText.KeyText;
        }

        return key switch
        {
            VirtualKey.Back => "Backspace",
            VirtualKey.Space => "Space",
            VirtualKey.Escape => "Escape",
            VirtualKey.Enter => "Enter",
            VirtualKey.Delete => "Delete",
            VirtualKey.Insert => "Insert",
            VirtualKey.Left => "LeftArrow",
            VirtualKey.Up => "UpArrow",
            VirtualKey.Right => "RightArrow",
            VirtualKey.Down => "DownArrow",
            VirtualKey.PageUp => "PageUp",
            VirtualKey.PageDown => "PageDown",
            VirtualKey.CapitalLock => "CapsLock",
            VirtualKey.Scroll => "ScrollLock",
            VirtualKey.Snapshot => "PrintScreen",
            _ => Enum.IsDefined(typeof(VirtualKey), key) ? key.ToString() : $"VirtualKey{(int)key}"
        };
    }

    public static HotkeyStructure ParseHotkeyText(string hotkeyText)
    {
        string[] tokens = SplitHotkeyTokens(hotkeyText);
        if (tokens.Length == 0)
        {
            throw new ArgumentException("hotkey string cannot be empty.");
        }

        VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            modifiers |= ParseModifierToken(tokens[i]);
        }

        return new HotkeyStructure
        {
            Modifiers = modifiers,
            Key = ParseKeyText(tokens[^1])
        };
    }

    public static VirtualKeyModifiers ParseModifiersText(string modifiersText)
    {
        VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
        foreach (string token in SplitHotkeyTokens(modifiersText))
        {
            modifiers |= ParseModifierToken(token);
        }

        return modifiers;
    }

    public static VirtualKey ParseKeyText(string keyText)
    {
        string trimmed = StripDisplayParenthetical(keyText.Trim());
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("hotkey key cannot be empty.");
        }

        string compact = CompactKeyToken(trimmed);
        if (compact.Length == 1 && char.IsDigit(compact[0]))
        {
            return ParseKeyCode(compact[0] - '0');
        }

        if (int.TryParse(compact, out int keyCode))
        {
            return ParseKeyCode(keyCode);
        }

        if (compact.StartsWith("virtualkey", StringComparison.Ordinal) &&
            int.TryParse(compact["virtualkey".Length..], out int virtualKeyCode))
        {
            return ParseKeyCode(virtualKeyCode);
        }

        if (TryParseNamedKey(compact, out VirtualKey namedKey))
        {
            return namedKey;
        }

        if (compact.StartsWith("numpad", StringComparison.Ordinal) &&
            compact.Length == "numpad0".Length &&
            char.IsDigit(compact[^1]))
        {
            return (VirtualKey)((int)VirtualKey.NumberPad0 + compact[^1] - '0');
        }

        if (compact.Length == 1 && char.IsLetter(compact[0]))
        {
            compact = compact.ToUpperInvariant();
        }

        if (Enum.TryParse(compact, ignoreCase: true, out VirtualKey key))
        {
            return key;
        }

        throw new ArgumentException($"Unsupported hotkey key '{keyText}'. Use names such as A, Q, F1, Enter, Escape, Space, Number1, Backspace, Left Arrow, Page Up, Backtick, Semicolon, or Slash.");
    }

    public static VirtualKey ParseKeyCode(int keyCode)
    {
        if (keyCode is >= 0 and <= 9)
        {
            return (VirtualKey)((int)VirtualKey.Number0 + keyCode);
        }

        return (VirtualKey)keyCode;
    }

    private static string GetDisplayKeyText(VirtualKey key)
    {
        if (TryGetDigitKeyNumber(key, out int digit))
        {
            return digit.ToString();
        }

        if (TryGetNumberPadKeyNumber(key, out int numberPadDigit))
        {
            return $"Numpad {numberPadDigit}";
        }

        if (OemKeyTexts.TryGetValue((int)key, out var oemText))
        {
            return oemText.DisplayText;
        }

        return key switch
        {
            VirtualKey.Back => "Backspace",
            VirtualKey.Space => "Space",
            VirtualKey.Escape => "Esc",
            VirtualKey.Left => "Left Arrow",
            VirtualKey.Up => "Up Arrow",
            VirtualKey.Right => "Right Arrow",
            VirtualKey.Down => "Down Arrow",
            VirtualKey.PageUp => "Page Up",
            VirtualKey.PageDown => "Page Down",
            VirtualKey.CapitalLock => "Caps Lock",
            VirtualKey.Scroll => "Scroll Lock",
            VirtualKey.Snapshot => "Print Screen",
            _ => Enum.IsDefined(typeof(VirtualKey), key) ? key.ToString() : $"VirtualKey {(int)key}"
        };
    }

    private static bool TryGetDigitKeyNumber(VirtualKey key, out int digit)
    {
        int keyValue = (int)key;
        int number0 = (int)VirtualKey.Number0;
        digit = keyValue - number0;
        return digit is >= 0 and <= 9;
    }

    private static bool TryGetNumberPadKeyNumber(VirtualKey key, out int digit)
    {
        int keyValue = (int)key;
        int numberPad0 = (int)VirtualKey.NumberPad0;
        digit = keyValue - numberPad0;
        return digit is >= 0 and <= 9;
    }

    private static VirtualKeyModifiers ParseModifierToken(string token)
    {
        return CompactKeyToken(token) switch
        {
            "" or "none" => VirtualKeyModifiers.None,
            "ctrl" or "control" => VirtualKeyModifiers.Control,
            "shift" => VirtualKeyModifiers.Shift,
            "alt" or "menu" => VirtualKeyModifiers.Menu,
            "win" or "windows" or "meta" => VirtualKeyModifiers.Windows,
            _ => throw new ArgumentException($"Unsupported hotkey modifier '{token}'. Supported modifiers: Control/Ctrl, Shift, Alt/Menu, Windows/Win.")
        };
    }

    private static bool TryParseNamedKey(string compact, out VirtualKey key)
    {
        key = compact switch
        {
            "esc" or "escape" => VirtualKey.Escape,
            "backspace" or "back" => VirtualKey.Back,
            "spacebar" or "space" => VirtualKey.Space,
            "return" or "enter" => VirtualKey.Enter,
            "del" or "delete" => VirtualKey.Delete,
            "ins" or "insert" => VirtualKey.Insert,
            "pgup" or "pageup" => VirtualKey.PageUp,
            "pgdn" or "pagedown" => VirtualKey.PageDown,
            "leftarrow" or "arrowleft" => VirtualKey.Left,
            "rightarrow" or "arrowright" => VirtualKey.Right,
            "uparrow" or "arrowup" => VirtualKey.Up,
            "downarrow" or "arrowdown" => VirtualKey.Down,
            "printscreen" or "prtsc" or "prtscr" => VirtualKey.Snapshot,
            "capslock" => VirtualKey.CapitalLock,
            "scrolllock" => VirtualKey.Scroll,
            "semicolon" or "semi" or "oem1" or ";" or ":" => (VirtualKey)OemSemicolon,
            "equals" or "equal" or "plus" or "oemplus" or "=" or "+" => (VirtualKey)OemEquals,
            "comma" or "oemcomma" or "," or "<" => (VirtualKey)OemComma,
            "minus" or "hyphen" or "dash" or "oemminus" or "-" or "_" => (VirtualKey)OemMinus,
            "period" or "dot" or "oemperiod" or "." or ">" => (VirtualKey)OemPeriod,
            "slash" or "forwardslash" or "oem2" or "/" or "?" => (VirtualKey)OemSlash,
            "backtick" or "grave" or "graveaccent" or "tilde" or "oem3" or "`" or "~" => (VirtualKey)OemBacktick,
            "leftbracket" or "openbracket" or "openingbracket" or "lbracket" or "oem4" or "[" or "{" => (VirtualKey)OemLeftBracket,
            "backslash" or "oem5" or "\\" or "|" => (VirtualKey)OemBackslash,
            "rightbracket" or "closebracket" or "closingbracket" or "rbracket" or "oem6" or "]" or "}" => (VirtualKey)OemRightBracket,
            "quote" or "apostrophe" or "singlequote" or "doublequote" or "oem7" or "'" or "\"" => (VirtualKey)OemQuote,
            _ => VirtualKey.None
        };

        return key != VirtualKey.None;
    }

    private static string StripDisplayParenthetical(string keyText)
    {
        int parentheticalStart = keyText.IndexOf(" (", StringComparison.Ordinal);
        return parentheticalStart > 0 && keyText.EndsWith(')')
            ? keyText[..parentheticalStart]
            : keyText;
    }

    private static string CompactKeyToken(string keyText)
    {
        string trimmed = keyText.Trim();
        bool isSingleMinusKey = trimmed is "-" or "_";
        return new string(trimmed
            .Where(character => !char.IsWhiteSpace(character) && (isSingleMinusKey || character != '_' && character != '-'))
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string[] SplitHotkeyTokens(string text)
    {
        if (text.IndexOfAny(['+', '|']) >= 0)
        {
            return text.Split(['+', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        if (text.Contains(','))
        {
            return text.Split([','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return text.Split([' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

