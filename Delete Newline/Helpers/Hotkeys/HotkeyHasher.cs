using Delete_Newline.Contracts.Structures;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
