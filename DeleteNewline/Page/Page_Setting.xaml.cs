﻿using GlobalHook;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeleteNewline
{
    /// <summary>
    /// Page_Setting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Setting
    {
        KeyConverter keyConverter = new KeyConverter();
        Settings appdata = Settings.Default;

        public Page_Setting()
        {
            InitializeComponent();
        }

        public void InitializationDefaultPage()
        {
            SetUI_fromAppdata();
            SetBackendSettings_fromUI();
        }

        private void SetUI_fromAppdata()
        {
            CheckBox_topMost.IsChecked = appdata.topMost;
            CheckBox_notification.IsChecked = appdata.notification;
            CheckBox_deleteMultipleSpace.IsChecked = appdata.deleteMultipleSpace;
            SetUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
        }

        private void SetBackendSettings_fromUI()
        {
            Action setCheckbox = () =>
            {
                MainWindow.mainWindow.Topmost = (bool)CheckBox_topMost.IsChecked!;

                HookImplement.execute = (CheckBox_notification.IsChecked == true) ? 
                    Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;

                // removeMultiSpace Settings 는 appdata 를 그대로 참조함...
            };

            Action setKeybind = () =>
            {
                key1 = (Key)appdata.bindKey_1;
                key2 = (Key)appdata.bindKey_2;

                SetUI_keybind(key1, key2);

                var Virtual_Key1 = KeyInterop.VirtualKeyFromKey(key1);
                var Virtual_Key2 = KeyInterop.VirtualKeyFromKey(key2);

                HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
            };

            setCheckbox();
            setKeybind();
        }

        private void SaveAppdata()
        {
            appdata.topMost = (bool)CheckBox_topMost.IsChecked!;
            appdata.notification = (bool)CheckBox_notification.IsChecked!;
            appdata.deleteMultipleSpace = (bool)CheckBox_deleteMultipleSpace.IsChecked!;

            // None 일경우 (사용자가 아무것도 지정하지 않은경우) 문제 방지 기능이 있음.
            // 갱신되지 않을경우 기본셋팅코드로 리셋.
            if (key1 != Key.None)
            {
                appdata.bindKey_1 = (int)key1;
            }

            if (key2 != Key.None)
            {
                appdata.bindKey_2 = (int)key2;
            }

            Settings.Default.Save();
        }

        private void SetUI_keybind(Key key1, Key key2)
        {
            if (key2 == Key.None)
            {
                key1_text = keyConverter.ConvertToString(key1);
                TextBox_bindKey.Text = key1_text;
            }
            else
            {
                key1_text = keyConverter.ConvertToString(key1);
                key2_text = keyConverter.ConvertToString(key2);
                TextBox_bindKey.Text = key1_text + " + " + key2_text;
            }
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            SaveAppdata();
            SetBackendSettings_fromUI();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            CheckBox_topMost.IsChecked = appdata.topMost;
            CheckBox_notification.IsChecked = appdata.notification;
            CheckBox_deleteMultipleSpace.IsChecked = appdata.deleteMultipleSpace;

            key1 = (Key)appdata.bindKey_1;
            key2 = (Key)appdata.bindKey_2;
            SetUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
        }

        Key key1 = Key.None;
        Key key2 = Key.None;

        string key1_text = string.Empty;
        string key2_text = string.Empty;

        private void TextBox_bindKey_KeyDown(object sender, KeyEventArgs e)
        {
            if(key1 == Key.None)
            {
                key1 = (e.Key == Key.System) ? e.SystemKey : e.Key;

                key1_text = keyConverter.ConvertToString(key1);
            }
            else
            {
                key2 = (e.Key == Key.System) ? e.SystemKey : e.Key;

                key2_text = keyConverter.ConvertToString(key2);
            }

            SetUI_keybind(key1, key2);
        }

        private void TextBox_bindKey_KeyUp(object sender, KeyEventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);
            Keyboard.ClearFocus();
        }

        private void TextBox_bindKey_GotFocus(object sender, RoutedEventArgs e)
        {
            key1_text = string.Empty;
            key2_text = string.Empty;

            key1 = Key.None;
            key2 = Key.None;

            TextBox_bindKey.Text = string.Empty;
        }

    }
}
