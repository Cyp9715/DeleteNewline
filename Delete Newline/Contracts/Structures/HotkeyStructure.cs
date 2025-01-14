using CommunityToolkit.Mvvm.ComponentModel;
using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public partial class HotkeyStructure : ObservableRecipient
{
    [ObservableProperty]
    public VirtualKeyModifiers _modifiers;

    [ObservableProperty]
    public VirtualKey _key;

    [ObservableProperty]
    public string? _displayHotkey;

    public HotkeyStructure()
    {
    }

    partial void OnKeyChanged(VirtualKey value)
    {
        UpdateDisplayHotkey();
    }

    partial void OnModifiersChanged(VirtualKeyModifiers value)
    {
        UpdateDisplayHotkey();
    }

    private void UpdateDisplayHotkey()
    {
        if (Modifiers == VirtualKeyModifiers.None || Key == VirtualKey.None)
        {
            DisplayHotkey = string.Empty;
            return;
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
            .Where(flag => Modifiers.HasFlag(flag))
            .Select(mod => GetModifierString(mod))
            .Where(str => !string.IsNullOrEmpty(str));

        // Key to string
        var keyString = GetKeyString(Key);

        DisplayHotkey = string.Join(" + ", modifierStrings.Concat(new[] { keyString }));
    }


    // Modifier to string
    private string GetModifierString(VirtualKeyModifiers modifier)
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
    private string GetKeyString(VirtualKey key)
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
}
