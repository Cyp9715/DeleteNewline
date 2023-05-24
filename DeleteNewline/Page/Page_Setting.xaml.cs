using GlobalHook;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeleteNewline.Page
{
    /// <summary>
    /// Page_Setting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Setting
    {
        KeyConverter keyConverter = new KeyConverter();
        DeleteNewline.Settings appdata = Settings.Default;

        public Page_Setting()
        {
            InitializeComponent();
        }

        public void InitializationDefaultPage()
        {
            SetUI_fromAppdata();
            SetSettings_fromUI();
        }

        private void SetUI_fromAppdata()
        {
            CheckBox_TopMost.IsChecked = appdata.topMost;
            CheckBox_Notification.IsChecked = appdata.notification;
            UpdateUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
        }

        private void SetSettings_fromUI()
        {
            Action setCheckbox = () =>
            {
                MainWindow.mainWindow.Topmost = (bool)CheckBox_TopMost.IsChecked!;

                if (CheckBox_Notification.IsChecked == true)
                {
                    HookImplement.startDeleteNewline = HookImplement.StartDeleteNewline_WithNotifier;
                }
                else
                {
                    HookImplement.startDeleteNewline = HookImplement.StartDeleteNewline_WithoutNotifier;
                }
            };

            Action setKeybind = () =>
            {
                key1 = (Key)appdata.bindKey_1;
                key2 = (Key)appdata.bindKey_2;

                UpdateUI_keybind(key1, key2);

                var Virtual_Key1 = KeyInterop.VirtualKeyFromKey(key1);
                var Virtual_Key2 = KeyInterop.VirtualKeyFromKey(key2);

                HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
            };

            setCheckbox();
            setKeybind();
        }

        private void SaveAppdata()
        {
            appdata.topMost = (bool)CheckBox_TopMost.IsChecked!;
            appdata.notification = (bool)CheckBox_Notification.IsChecked!;

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

        private void UpdateUI_keybind(Key key1, Key key2)
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
            SetSettings_fromUI();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            CheckBox_TopMost.IsChecked = appdata.topMost;
            CheckBox_Notification.IsChecked = appdata.notification;

            key1 = (Key)appdata.bindKey_1;
            key2 = (Key)appdata.bindKey_2;
            UpdateUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
        }

        Key key1 = Key.None;
        Key key2 = Key.None;

        string key1_text = string.Empty;
        string key2_text = string.Empty;

        private void TextBox_bindKey_KeyDown(object sender, KeyEventArgs e)
        {
            if(key1 == Key.None)
            {
                if (e.Key == Key.System)
                {
                    key1 = e.SystemKey;
                }
                else
                {
                    key1 = e.Key;
                }

                key1_text = keyConverter.ConvertToString(key1);
            }
            else
            {
                if (e.Key == Key.System)
                {
                    key2 = e.SystemKey;
                }
                else
                {
                    key2 = e.Key;
                }

                key2_text = keyConverter.ConvertToString(key2);
            }

            UpdateUI_keybind(key1, key2);
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
