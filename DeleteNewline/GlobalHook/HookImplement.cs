using DeleteNewline;
using System;
using System.Windows;
namespace GlobalHook
{
    static class HookImplement
    {
        static GlobalKeyHook? globalKeyHook;
        public static Action? execute;
        static bool isSetHook = false;

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

        private static void GlobalKeyHook_OnKeyUp(object? sender, GlobalKeyEventArgs e)
        {
            if (isPressedKey1 == true && isPressedKey2 == true)
            {
                isPressedKey1 = false;
                isPressedKey2 = false;
                execute();
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
