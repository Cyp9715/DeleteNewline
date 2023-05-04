using DeleteNewline;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows;

namespace GlobalHook
{
    class HookImplement
    { 
        GlobalKeyHook? globalKeyHook;
        Action? action_execute;
        ClipboardManager cbManager = new ClipboardManager();
        IDataObject? idataObj;

        public void InstallGlobalHook()
        {
            globalKeyHook = new GlobalKeyHook();
            // globalKeyHook.OnKeyPressed += GlobalKeyHook_OnKeyPressed;
            globalKeyHook.OnKeyUp += GlobalKeyHook_OnKeyUp;
            globalKeyHook.OnKeyDown += GlobalKeyHook_OnKeyDown;
        }

        VirtualKeycodes key1 = VirtualKeycodes.LeftAlt;
        VirtualKeycodes key2 = VirtualKeycodes.F1;

        bool isPressedKey1 = false; 
        bool isPressedKey2 = false;

        private void GlobalKeyHook_OnKeyDown(object? sender, GlobalKeyEventArgs e)
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

        private void ShortcutSetClipboard()
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

        private void GlobalKeyHook_OnKeyUp(object? sender, GlobalKeyEventArgs e)
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
