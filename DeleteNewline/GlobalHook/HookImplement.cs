using DeleteNewline;
using System;
using System.Windows;
using Windows;

namespace GlobalHook
{
    static class HookImplement
    {
        static GlobalKeyHook? globalKeyHook;
        static ClipboardManager cbManager = new ClipboardManager();
        static IDataObject? idataObj;
        static bool isSet = false;

        public static void InstallGlobalHook()
        {
            if(globalKeyHook == null) 
            { 
            globalKeyHook = new GlobalKeyHook();
            }

            if(isSet == false)
            {
                // globalKeyHook.OnKeyPressed += GlobalKeyHook_OnKeyPressed;
                globalKeyHook.OnKeyUp += GlobalKeyHook_OnKeyUp;
                globalKeyHook.OnKeyDown += GlobalKeyHook_OnKeyDown;
                isSet = true;
            }
        }

        public static void UnInstallGlobalHook()
        {
            if(isSet == true)
            {
                globalKeyHook.OnKeyUp -= GlobalKeyHook_OnKeyUp;
                globalKeyHook.OnKeyDown -= GlobalKeyHook_OnKeyDown;
                isSet = false;
            }
        }

        static VirtualKeycodes key1 = VirtualKeycodes.LeftAlt;
        static VirtualKeycodes key2 = VirtualKeycodes.F1;

        static bool isPressedKey1 = false;
        static bool isPressedKey2 = false;

        public static void SetKeys(VirtualKeycodes key1_, VirtualKeycodes key2_)
        {
            key1 = key1_;
            key2 = key2_;
        }

        private static void GlobalKeyHook_OnKeyDown(object? sender, GlobalKeyEventArgs e)
        {
            if(e.KeyCode == key1)
            {
                isPressedKey1 = true;
            }

            if(e.KeyCode == key2)
            {
                isPressedKey2 = true;
            }
        }

        private static void ShortcutSetClipboard()
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            int limitLen = 100;

            VirtualInput.InputImplement.PressKeyboard_Copy();

            if (cbManager.GetClipboardData_Text(ref idataObj) == true)
            {
                notifyHeader = "SUCCESS";

                string deletedText = cbManager.DeleteClipboardNewline(ref idataObj).ToString();
                Clipboard.SetDataObject(deletedText);

                if (deletedText.Length > limitLen)
                {
                    notifyContent = deletedText.Substring(0, limitLen) + " ...";
                }
                else
                {
                    notifyContent = deletedText;
                }
            }
            else
            {
                notifyHeader = "ERROR";
                notifyContent = "CLIPBOARD FORM IS NOT TEXT";
            }

            Notification.Send(notifyHeader, notifyContent);
        }

        private static void GlobalKeyHook_OnKeyUp(object? sender, GlobalKeyEventArgs e)
        {
            if (isPressedKey1 == true && isPressedKey2 == true)
            {
                isPressedKey1 = false;
                isPressedKey2 = false;
                ShortcutSetClipboard();
            }

            if (e.KeyCode == key1)
            {
                isPressedKey1 = false;
            }

            if (e.KeyCode == key2)
            {
                isPressedKey2 = false;
            }
        }
    }
}
