using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalHook
{
    class HookImplement
    { 
        GlobalKeyHook? globalKeyHook;
        Action? action_execute;

        public void InstallGlobalHook(Action executeFunc)
        {
            globalKeyHook = new GlobalKeyHook();
            // globalKeyHook.OnKeyPressed += GlobalKeyHook_OnKeyPressed;
            globalKeyHook.OnKeyUp += GlobalKeyHook_OnKeyUp;
            globalKeyHook.OnKeyDown += GlobalKeyHook_OnKeyDown;
            
            action_execute = executeFunc;
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

        private void GlobalKeyHook_OnKeyUp(object? sender, GlobalKeyEventArgs e)
        {
            if (isPressedKey1 == true && isPressedKey2 == true)
            {
                if (action_execute is not null)
                {
                    isPressedKey1 = false;
                    isPressedKey2 = false;
                    action_execute();
                }
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
