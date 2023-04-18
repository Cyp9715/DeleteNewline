using System;
using System.Collections.Generic;
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
            // globalKeyHook.OnKeyUp += GlobalKeyHook_OnKeyUp;
            globalKeyHook.OnKeyDown += GlobalKeyHook_OnKeyDown;
            
            action_execute = executeFunc;
        }

        bool key_ctrl = false;
        bool key_space = false;

        private void GlobalKeyHook_OnKeyDown(object? sender, GlobalKeyEventArgs e)
        {
            if(e.Control == GlobalHook.ModifierKeySide.Left)
            {
                key_ctrl = true;
            }

            if(e.KeyCode == VirtualKeycodes.Space)
            {
                key_space = true;
            }

            if (key_ctrl == true && key_space == true)
            {
                if(action_execute is not null)
                {
                    action_execute();
                }

                key_ctrl = false;
                key_space = false;
            }
        }
    }
}
