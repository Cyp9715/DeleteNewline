﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using DeleteNewline;
using DeleteNewline.ViewModel;
using Windows;
using Windows.Devices.Printers;

namespace GlobalHook
{
    static class Implement
    {
        static GlobalKeyHook? globalKeyHook;
        static bool isSetHook = false;
        public static Action<string, string>? execute;
        static ViewModel_Setting vm_settings = App.GetService<ViewModel_Setting>();
 
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
                Notification.Send(msgHeader, msgContent, Notification.SoundType.reminder, 300);
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
                    string regexExpression = vm_settings.text_textBox_regexExpression;   
                    string regexReplace = vm_settings.text_textBox_regexReplace;
                        
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