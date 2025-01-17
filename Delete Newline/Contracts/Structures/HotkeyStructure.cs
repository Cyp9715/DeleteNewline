using CommunityToolkit.Mvvm.ComponentModel;
using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public partial class HotKeyStructure : ObservableRecipient
{
    [ObservableProperty]
    public VirtualKeyModifiers _modifiers;

    [ObservableProperty]
    public VirtualKey _key;

    [ObservableProperty]
    public string? _displayHotKey;

    public HotKeyStructure()
    {
    }

    partial void OnKeyChanged(VirtualKey value)
    {
        UpdateDisplayHotKey();
    }

    partial void OnModifiersChanged(VirtualKeyModifiers value)
    {
        UpdateDisplayHotKey();
    }

    private void UpdateDisplayHotKey()
    {
        if (Modifiers == VirtualKeyModifiers.None || Key == VirtualKey.None)
        {
            DisplayHotKey = string.Empty;
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

        DisplayHotKey = string.Join(" + ", modifierStrings.Concat(new[] { keyString }));
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
