using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
// using System.Threading; // No longer needed directly in this file if STA thread and its sleep are gone
using static Delete_Newline.Helpers.NativeMethods; // User added this, will use it.

namespace Delete_Newline.Helpers
{
    internal static class NativeMethods
    {
        internal const uint INPUT_KEYBOARD = 1;

        // dwFlags for KEYBDINPUT
        internal const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        internal const int KEYEVENTF_KEYUP = 0x0002;
        internal const int KEYEVENTF_SCANCODE = 0x0008;
        // internal const int KEYEVENTF_UNICODE = 0x0004; // Not used for Ctrl+C

        // Virtual Key Codes
        internal const short VK_LCONTROL = 0xA2;
        internal const short VK_RCONTROL = 0xA3;
        internal const short VK_LSHIFT = 0xA0;
        internal const short VK_RSHIFT = 0xA1;
        internal const short VK_LMENU = 0xA4; // Left Alt
        internal const short VK_RMENU = 0xA5; // Right Alt
        internal const short VK_C = 0x43;

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            internal uint type;
            internal InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            // Other members (mi, hi) are not strictly needed for this keyboard-only helper
            // but kept for struct size if an array of INPUTs were to be mixed.
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal short wVk;
            internal short wScan;
            internal int dwFlags; // Changed from uint to int
            internal int time;    // Changed from uint to int
            internal IntPtr dwExtraInfo;
        }

        // Dummy structs for size matching if mi/hi were used in InputUnion
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT { public int dx, dy, mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // Overload for single input, to match old code's SendInput(1, ref ki, ...) style if needed,
        // but using an array of 1 is also fine with the above.
        // For direct use:
        // [DllImport("user32.dll", SetLastError = true)]
        // internal static extern uint SendInput(uint nInputs, ref INPUT pInput, int cbSize);

        [DllImport("user32.dll")]
        internal static extern short MapVirtualKey(short uCode, uint uMapType); // Changed first param to short
        // uMapType: 0 (MAPVK_VK_TO_VSC) or 2 (MAPVK_VSC_TO_VK_EX) for extended, etc.
        // For Ctrl+C, uMapType = 0 is typically used.
        internal const uint MAPVK_VK_TO_VSC = 0x00;
    }

    public static class VirtualInputHelper
    {
        private static void SendKeyInput(short virtualKey, bool press, bool isExtended)
        {
            INPUT input = new INPUT { type = INPUT_KEYBOARD };
            input.U.ki.wVk = virtualKey;
            input.U.ki.wScan = MapVirtualKey(virtualKey, MAPVK_VK_TO_VSC);
            
            int flags = 0;
            if (input.U.ki.wScan > 0)
            {
                flags |= KEYEVENTF_SCANCODE;
            }
            if (!press)
            {
                flags |= KEYEVENTF_KEYUP;
            }
            if (isExtended)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }
            input.U.ki.dwFlags = flags;
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;

            INPUT[] inputs = new INPUT[] { input };
            if (SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                System.Diagnostics.Debug.WriteLine($"SendKeyInput for VK {virtualKey:X} failed with error code: {Marshal.GetLastWin32Error()}");
            }
        }

        private static void ResetStuckKeys()
        {
            System.Diagnostics.Debug.WriteLine("Resetting potentially stuck modifier keys...");
            SendKeyInput(VK_LCONTROL, false, false);
            SendKeyInput(VK_LSHIFT,   false, false);
            SendKeyInput(VK_LMENU,    false, false);
            SendKeyInput(VK_RCONTROL, false, true);
            SendKeyInput(VK_RSHIFT,   false, true);
            SendKeyInput(VK_RMENU,    false, true);
            System.Diagnostics.Debug.WriteLine("Finished resetting modifier keys.");
        }

        public static void SendCtrlC()
        {
            System.Diagnostics.Debug.WriteLine("Executing SendCtrlC...");

            ResetStuckKeys(); 

            SendKeyInput(VK_LCONTROL, true,  false); // Press Left Control
            SendKeyInput(VK_C,        true,  false); // Press C
            SendKeyInput(VK_C,        false, false); // Release C
            SendKeyInput(VK_LCONTROL, false, false); // Release Left Control

            System.Diagnostics.Debug.WriteLine("SendCtrlC sequence finished.");
        }
    }
} 