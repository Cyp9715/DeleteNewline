using System;
using System.Collections.Generic;
using System.Diagnostics;
using DeleteNewline.ViewModel;

namespace GlobalHook
{
    static class HookImplement
    {
        static GlobalKeyHook? globalKeyHook;
        static bool isSetHook = false;
        
        public static Action<string, string>? execute;

        public static void InstallGlobalHook()
        {
            if(globalKeyHook == null) 
            { 
                globalKeyHook = new GlobalKeyHook();
            }

            if(isSetHook == false)
            {
                // globalKeyHook.OnKeyPressed += GlobalKeyHook_OnKeyPressed;
                globalKeyHook.OnKeyUp += GlobalKeyHook_OnKeyUp;
                globalKeyHook.OnKeyDown += GlobalKeyHook_OnKeyDown;
                isSetHook = true;
            }
        }

        public static void UnInstallGlobalHook()
        {
            if(isSetHook == true)
            {
                globalKeyHook.OnKeyUp -= GlobalKeyHook_OnKeyUp;
                globalKeyHook.OnKeyDown -= GlobalKeyHook_OnKeyDown;
                isSetHook = false;
            }
        }

        static VirtualKeycodes key1 = VirtualKeycodes.LeftAlt;
        static VirtualKeycodes key2 = VirtualKeycodes.F1;

        static Dictionary<VirtualKeycodes, uint> pressedKeys = new Dictionary<VirtualKeycodes, uint>();
        static uint pressedCount = 0;

        public static void SetKeys(VirtualKeycodes key1_, VirtualKeycodes key2_)
        {
            key1 = key1_;
            key2 = key2_;
        }

        private static void GlobalKeyHook_OnKeyDown(object? sender, GlobalKeyEventArgs e)
        {
            pressedKeys.Add(e.KeyCode, ++pressedCount);
        }

        private static void GlobalKeyHook_OnKeyUp(object? sender, GlobalKeyEventArgs e)
        {
            bool isExist = pressedKeys.ContainsKey(key1) && pressedKeys.ContainsKey(key2);

            if (isExist)
            {
                bool onlyTwoKeys = pressedKeys.Count == 2;
                bool isSequential = pressedKeys[key1] + 1 == pressedKeys[key2];

                pressedKeys.Remove(e.KeyCode);

                if (onlyTwoKeys && isSequential)
                {
                    string regexExpression = ViewModel_Page_Setting.vm_settings.text_textBox_regexExpression;
                    string regexReplace = ViewModel_Page_Setting.vm_settings.text_textBox_regexReplace;

                    execute(regexExpression, regexReplace);
                }
            }
            else
            {
                pressedKeys.Remove(e.KeyCode);
            }
        }
    }
}
