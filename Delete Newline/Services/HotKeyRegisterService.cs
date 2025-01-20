using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Windows.System;

namespace Delete_Newline.Services
{
    [Flags]
    public enum Modifiers
    {
        None = 0x0000,
        MOD_ALT = 0x0001,
        MOD_CONTROL = 0x0002,
        MOD_SHIFT = 0x0004,
        MOD_WIN = 0x0008,
        MOD_NOREPEAT = 0x4000,
    }

    public sealed class HotKeyRegisterService : IHotKeyRegister
    {
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id);

        private readonly IntPtr _hwnd;
        private readonly string _salt = "Delete Newline";

        public HotKeyRegisterService(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        private int HotKeyToHash((VirtualKeyModifiers, VirtualKey) hotKey)
        {
            string hotKeyString = $"{_salt}:{hotKey.Item1}:{hotKey.Item2}";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hotKeyString));
                int hashInt = BitConverter.ToInt32(hashBytes, 0);
                return Math.Abs(hashInt);
            }
        }

        private Modifiers MapVirtualModifiersToWin32(VirtualKeyModifiers virtualModifiers)
        {
            Modifiers win32Modifiers = Modifiers.None;

            if (virtualModifiers.HasFlag(VirtualKeyModifiers.Control))
                win32Modifiers |= Modifiers.MOD_CONTROL;
            if (virtualModifiers.HasFlag(VirtualKeyModifiers.Menu))
                win32Modifiers |= Modifiers.MOD_ALT;
            if (virtualModifiers.HasFlag(VirtualKeyModifiers.Shift))
                win32Modifiers |= Modifiers.MOD_SHIFT;
            if (virtualModifiers.HasFlag(VirtualKeyModifiers.Windows))
                win32Modifiers |= Modifiers.MOD_WIN;

            return win32Modifiers;
        }

        public bool RegisterHotKey((VirtualKeyModifiers, VirtualKey) hotKey)
        {
            int hotKeyId = HotKeyToHash(hotKey);
            Modifiers win32Modifiers = MapVirtualModifiersToWin32(hotKey.Item1);
            bool result = RegisterHotKey(_hwnd, hotKeyId, (uint)win32Modifiers, (uint)hotKey.Item2);

            Debug.WriteLine($"_hwnd : {_hwnd}");
            if (!result)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"RegisterHotKey failed with error code: {errorCode}");
            }
            else
            {
                Debug.WriteLine($"RegisterHotKey succeeded. ID={hotKeyId}");
            }
            return result;
        }

        public void UnregisterHotKey((VirtualKeyModifiers, VirtualKey) hotKey)
        {
            int hotKeyId = HotKeyToHash(hotKey);
            bool result = UnregisterHotKey(_hwnd, hotKeyId);
            if (!result)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"UnregisterHotKey failed with error code: {errorCode}");
            }
            else
            {
                Debug.WriteLine($"UnregisterHotKey succeeded. ID={hotKeyId}");
            }
        }
    }
}
