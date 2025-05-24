using System.Security.Cryptography;
using System.Text;
using Windows.System;

namespace Delete_Newline.Helpers;

/// <summary>
/// Comprehensive helper class for all hotkey-related operations including validation, hashing, and display
/// </summary>
public static class HotkeyHelper
{
    private const string SALT = "Delete Newline";

    #region Hotkey Hash Generation
    
    /// <summary>
    /// Generate a unique hash ID for a hotkey combination
    /// </summary>
    /// <param name="modifiers">Virtual key modifiers</param>
    /// <param name="key">Virtual key</param>
    /// <returns>Unique integer hash for the hotkey combination</returns>
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

    /// <summary>
    /// Generate a unique hash ID for a hotkey tuple
    /// </summary>
    /// <param name="hotkey">Hotkey tuple (modifiers, key)</param>
    /// <returns>Unique integer hash for the hotkey combination</returns>
    public static int GenerateHotkeyHash((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return GenerateHotkeyHash(hotkey.Item1, hotkey.Item2);
    }

    #endregion

    #region Hotkey Validation
    
    /// <summary>
    /// Check if the given hotkey combination is a system hotkey that should not be overridden
    /// </summary>
    /// <param name="modifiers">Virtual key modifiers</param>
    /// <param name="key">Virtual key</param>
    /// <returns>True if this is a forbidden system hotkey</returns>
    public static bool IsSystemHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        // Only block if Control is the only modifier
        if (modifiers == VirtualKeyModifiers.Control)
        {
            return key switch
            {
                VirtualKey.C or VirtualKey.V or VirtualKey.X or 
                VirtualKey.Z or VirtualKey.Y or VirtualKey.A or
                VirtualKey.S or VirtualKey.O or VirtualKey.P or
                VirtualKey.N or VirtualKey.F or VirtualKey.H => true,
                _ => false
            };
        }
        
        // Block common Windows system hotkeys
        if (modifiers == VirtualKeyModifiers.Windows)
        {
            return key switch
            {
                VirtualKey.L or VirtualKey.R or VirtualKey.D or
                VirtualKey.E or VirtualKey.I or VirtualKey.X or
                VirtualKey.Tab => true,
                _ => false
            };
        }
        
        // Block Alt+Tab and Alt+F4
        if (modifiers == VirtualKeyModifiers.Menu)
        {
            return key switch
            {
                VirtualKey.Tab or VirtualKey.F4 => true,
                _ => false
            };
        }
        
        return false;
    }

    /// <summary>
    /// Check if Shift key is used alone (which should be forbidden)
    /// </summary>
    /// <param name="modifiers">Virtual key modifiers</param>
    /// <returns>True if only Shift modifier is used</returns>
    public static bool IsShiftAlone(VirtualKeyModifiers modifiers)
    {
        return modifiers == VirtualKeyModifiers.Shift;
    }

    /// <summary>
    /// Get a user-friendly error message for forbidden hotkey types
    /// </summary>
    /// <param name="modifiers">Virtual key modifiers</param>
    /// <param name="key">Virtual key</param>
    /// <returns>Error message key for localization</returns>
    public static (string titleKey, string messageKey) GetForbiddenHotkeyError(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (IsShiftAlone(modifiers))
        {
            return ("Notification_InvalidHotkey_ShiftAlone_NotAllowed_Title", 
                    "Notification_InvalidHotkey_ShiftAlone_NotAllowed_Message");
        }
        
        if (IsSystemHotkey(modifiers, key))
        {
            return ("Notification_InvalidHotkey_Title", 
                    "Notification_InvalidHotkey_SystemKey_Message");
        }
        
        return ("Notification_InvalidHotkey_Title", 
                "Notification_InvalidHotkey_AlreadyRegistered_Message");
    }

    #endregion

    #region Hotkey Display

    /// <summary>
    /// Get a user-friendly display string for a hotkey combination
    /// </summary>
    /// <param name="modifiers">Virtual key modifiers</param>
    /// <param name="key">Virtual key</param>
    /// <returns>Display string like "Ctrl + Shift + A"</returns>
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

    /// <summary>
    /// Get a user-friendly display string for a hotkey tuple
    /// </summary>
    /// <param name="hotkey">Hotkey tuple (modifiers, key)</param>
    /// <returns>Display string like "Ctrl + Shift + A"</returns>
    public static string GetDisplayText((VirtualKeyModifiers, VirtualKey) hotkey)
    {
        return GetDisplayText(hotkey.Item1, hotkey.Item2);
    }

    #endregion
} 