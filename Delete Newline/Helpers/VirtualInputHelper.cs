using System;
using System.Runtime.InteropServices;
using System.Threading;
using static Delete_Newline.Helpers.NativeMethods;

namespace Delete_Newline.Helpers
{
    internal static class NativeMethods
    {
        internal const uint INPUT_KEYBOARD = 1;

        // dwFlags for KEYBDINPUT
        internal const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        internal const int KEYEVENTF_KEYUP = 0x0002;

        // Virtual Key Codes
        internal const short VK_LCONTROL = 0xA2;
        internal const short VK_RCONTROL = 0xA3;
        internal const short VK_LSHIFT = 0xA0;
        internal const short VK_RSHIFT = 0xA1;
        internal const short VK_LMENU = 0xA4; // Left Alt
        internal const short VK_RMENU = 0xA5; // Right Alt
        internal const short VK_LWIN = 0x5B;
        internal const short VK_RWIN = 0x5C;
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
            // Keep full union shape to preserve native INPUT struct size.
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
            internal int dwFlags;
            internal int time;
            internal IntPtr dwExtraInfo;
        }

        // Dummy structs for size matching if mi/hi were used in InputUnion
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT { public int dx, dy, mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }


        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    }

    public static class VirtualInputHelper
    {
        private static readonly int InputSize = Marshal.SizeOf<INPUT>();
        private static int _isSendingCtrlC;

        private static INPUT CreateKeyboardInput(short virtualKey, bool isKeyUp, bool isExtended = false)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD
            };

            input.U.ki.wVk = virtualKey;
            input.U.ki.wScan = 0;

            int flags = 0;
            if (isKeyUp)
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

            return input;
        }

        private static INPUT[] CreateModifierReleaseInputs()
        {
            return new INPUT[]
            {
                CreateKeyboardInput(VK_LCONTROL, true),
                CreateKeyboardInput(VK_RCONTROL, true, isExtended: true),
                CreateKeyboardInput(VK_LSHIFT, true),
                CreateKeyboardInput(VK_RSHIFT, true),
                CreateKeyboardInput(VK_LMENU, true),
                CreateKeyboardInput(VK_RMENU, true, isExtended: true),
                CreateKeyboardInput(VK_LWIN, true, isExtended: true),
                CreateKeyboardInput(VK_RWIN, true, isExtended: true),
            };
        }

        private static bool SendInputs(INPUT[] inputs, string actionName)
        {
            uint sent = SendInput((uint)inputs.Length, inputs, InputSize);
            if (sent != inputs.Length)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{actionName} SendInput mismatch: expected={inputs.Length}, actual={sent}, error={Marshal.GetLastWin32Error()}");
                return false;
            }

            return true;
        }

        private static void ForceReleaseModifierKeys()
        {
            _ = SendInputs(CreateModifierReleaseInputs(), "ForceReleaseModifierKeys");
        }

        public static bool SendCtrlC()
        {
            if (Interlocked.Exchange(ref _isSendingCtrlC, 1) == 1)
            {
                System.Diagnostics.Debug.WriteLine("SendCtrlC skipped: operation already in progress.");
                return false;
            }

            try
            {
                INPUT[] releaseInputs = CreateModifierReleaseInputs();
                INPUT[] ctrlCInputs =
                {
                    CreateKeyboardInput(VK_LCONTROL, false),
                    CreateKeyboardInput(VK_C, false),
                    CreateKeyboardInput(VK_C, true),
                    CreateKeyboardInput(VK_LCONTROL, true),
                };

                // WM_HOTKEY can be raised while user modifiers are still physically down (e.g. Alt+F1).
                // Release modifiers and send Ctrl+C as one contiguous input batch.
                INPUT[] inputs = new INPUT[releaseInputs.Length + ctrlCInputs.Length];
                Array.Copy(releaseInputs, 0, inputs, 0, releaseInputs.Length);
                Array.Copy(ctrlCInputs, 0, inputs, releaseInputs.Length, ctrlCInputs.Length);

                bool success = SendInputs(inputs, "SendCtrlC");
                if (!success)
                {
                    ForceReleaseModifierKeys();
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SendCtrlC failed with exception: {ex.Message}");
                ForceReleaseModifierKeys();
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref _isSendingCtrlC, 0);
            }
        }
    }
} 
