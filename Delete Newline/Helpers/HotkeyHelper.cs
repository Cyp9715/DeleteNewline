using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using Microsoft.UI.Xaml.Controls;
using System.Security.Cryptography;
using System.Text;
using Windows.System;

namespace Delete_Newline.Helpers;

public static class HotkeyHasher
{
    private const string SALT = "Delete Newline";

    public static int GenerateHotkeyHash(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        string hotkeyString = $"{SALT}:{modifiers}:{key}";

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hotkeyString));
            int hashInt = BitConverter.ToInt32(hashBytes, 0);
            return Math.Abs(hashInt);
        }
    }

    public static int GenerateHotkeyHash((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return GenerateHotkeyHash(hotkey.Item1, hotkey.Item2);
    }
}

public static class HotkeyValidator
{
    public static bool ValidateHotkey(KeyboardInputEventArgs args,
        HotkeyRegisterService hotkeyRegisterService,
        InAppNotificationService inAppNotificationService)
    {
        // Check for forbidden hotkey combinations
        if (IsShiftAlone(args.Modifiers))
        {
            inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_InvalidHotkey_ShiftAlone_NotAllowed_Title",
                messageKey: "Notification_InvalidHotkey_ShiftAlone_NotAllowed_Message",
                severity: InfoBarSeverity.Warning
            );
        }

        // Check for system hotkey
        if (IsSystemHotkey(args.Modifiers, args.Key))
        {
            inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_InvalidHotkey_Title",
                messageKey: "Notification_InvalidHotkey_SystemKey_Message",
                severity: InfoBarSeverity.Error
            );
            return false;
        }

        // Check if hotkey is already registered
        if (hotkeyRegisterService.IsHotkeyRegistered((args.Modifiers, args.Key)))
        {
            inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_InvalidHotkey_Title",
                messageKey: "Notification_InvalidHotkey_AlreadyRegistered_Message",
                severity: InfoBarSeverity.Error
            );
            return false;
        }

        return true;
    }

    private static bool IsShiftAlone(VirtualKeyModifiers modifiers)
    {
        return modifiers == VirtualKeyModifiers.Shift;
    }

    private static bool IsSystemHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        switch (modifiers)
        {
            case VirtualKeyModifiers.Control:
                return key switch
                {
                    VirtualKey.C or VirtualKey.V or VirtualKey.X or
                    VirtualKey.Z or VirtualKey.Y or VirtualKey.A or
                    VirtualKey.S or VirtualKey.O or VirtualKey.P or
                    VirtualKey.N or VirtualKey.F or VirtualKey.H => true,
                    _ => false
                };

            case VirtualKeyModifiers.Windows:
                return key switch
                {
                    VirtualKey.L or VirtualKey.R or VirtualKey.D or
                    VirtualKey.E or VirtualKey.I or VirtualKey.X or
                    VirtualKey.Tab => true,
                    _ => false
                };

            case VirtualKeyModifiers.Menu:
                return key switch
                {
                    VirtualKey.Tab or VirtualKey.F4 => true,
                    _ => false
                };

        }

        return false;
    }
}

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