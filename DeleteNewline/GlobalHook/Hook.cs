using System;
using System.Windows.Input;
using System.Collections.Generic;
using DeleteNewline.ViewModel;
using DeleteNewline;
using Windows;

namespace GlobalHook
{
    static class Hook
    {
        static GlobalKeyHook? globalKeyHook;
        static bool isSetHook = false;
        public static Action<List<string>, List<string>>? execute;
        private const int NotificationDuration = 300;

        public static void Install()
        {
            globalKeyHook ??= new GlobalKeyHook();

            if(isSetHook == false)
            {
                // globalKeyHook.OnKeyPressed += GlobalKeyHook_OnKeyPressed;
                globalKeyHook.OnKeyUp += GlobalKeyHook_OnKeyUp;
                globalKeyHook.OnKeyDown += GlobalKeyHook_OnKeyDown;
                isSetHook = true;
            }
        }

        public static void UnInstall()
        {
            if(isSetHook == true)
            {
                globalKeyHook.OnKeyUp -= GlobalKeyHook_OnKeyUp;
                globalKeyHook.OnKeyDown -= GlobalKeyHook_OnKeyDown;
                isSetHook = false;
            }
        }

        static readonly VirtualKeycodes default_key1 = VirtualKeycodes.LeftAlt;
        static readonly VirtualKeycodes default_key2 = VirtualKeycodes.F1;

        public static VirtualKeycodes key1 { get; private set; } = default_key1;
        public static VirtualKeycodes key2 { get; private set; } = default_key2;

        static Dictionary<VirtualKeycodes, uint> pressedKeys = new Dictionary<VirtualKeycodes, uint>();
        static uint pressedCount = 0;

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

        public static void SetKeys(Key bindKey_1, Key bindKey_2)
        {
            var virtual_key1 = KeyInterop.VirtualKeyFromKey(bindKey_1);
            var virtual_key2 = KeyInterop.VirtualKeyFromKey(bindKey_2);

            if (Enum.IsDefined(typeof(VirtualKeycodes), virtual_key1) ||
                Enum.IsDefined(typeof(VirtualKeycodes), virtual_key2))
            {
                virtual_key1 = (int)default_key1;
                virtual_key2 = (int)default_key2;
            }

            key1 = (VirtualKeycodes)virtual_key1;
            key2 = (VirtualKeycodes)virtual_key2;
        }
    }
}