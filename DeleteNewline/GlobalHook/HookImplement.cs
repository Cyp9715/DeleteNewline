using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using DeleteNewline;
using DeleteNewline.ViewModel;
using Windows;

namespace GlobalHook
{
    static class HookImplement
    {
        static GlobalKeyHook? globalKeyHook;
        static bool isSetHook = false;
        public static Action<List<string>, List<string>>? execute;
        private const int NotificationDuration = 300;

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

        // 혹시라도 해당 로직부분에서 단 하나라도 제대로 인식되지 않을경우 프로그램 작동에 문제가 생길 여지가 있음.
        private static void GlobalKeyHook_OnKeyDown(object? sender, GlobalKeyEventArgs e)
        {
            try
            {
                pressedKeys.Add(e.KeyCode, ++pressedCount);
            }
            catch (System.ArgumentException)
            {
                string msgHeader = "ERROR";
                string msgContent = "FAILED. GLOBALHOOK DETECT";
                Notification.Send(msgHeader, msgContent, Notification.SoundType.reminder, NotificationDuration);
            }
        }

        private static void GlobalKeyHook_OnKeyUp(object? sender, GlobalKeyEventArgs e)
        {
            bool isExist = pressedKeys.ContainsKey(key1) && pressedKeys.ContainsKey(key2);

            if (isExist)
            {
                // 이 부분에서 문제가 생길 수 있음.
                bool onlyTwoKeys = pressedKeys.Count == 2;
                bool isSequential = pressedKeys[key1] + 1 == pressedKeys[key2];

                // VirtualInput 으로 인한 연속호출 방지.
                pressedKeys.Clear();
                if (onlyTwoKeys && isSequential)
                {
                    ViewModel_Setting vm_setting = App.GetService<ViewModel_Setting>();

                    (List<string>, List<string>) regexAndReplace = vm_setting.GetAdditionalRegexAndReplace();

                    execute(regexAndReplace.Item1, regexAndReplace.Item2);
                }
            }
            else
            {
                pressedKeys.Remove(e.KeyCode);
            }
        }

        public static void SetHookKeys(Settings setting)
        {
            var Virtual_Key1 = KeyInterop.VirtualKeyFromKey((Key)setting.bindKey_1);
            var Virtual_Key2 = KeyInterop.VirtualKeyFromKey((Key)setting.bindKey_2);

            HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
        }
    }
}