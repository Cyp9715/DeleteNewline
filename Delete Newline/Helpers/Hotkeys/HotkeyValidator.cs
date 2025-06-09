using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers.Hotkeys;
using Delete_Newline.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Delete_Newline.Helpers;

public static class HotkeyValidator
{
    public static bool ValidateHotkey(KeyboardInputEventArgs args,
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
        if (HotkeyRegister.IsHotkeyRegistered((args.Modifiers, args.Key)))
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

