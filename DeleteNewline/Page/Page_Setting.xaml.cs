using GlobalHook;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows;
using Windows.System;

namespace DeleteNewline.Page
{
    /// <summary>
    /// Page_Setting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Setting
    {
        KeyConverter keyConverter = new KeyConverter();

        public Page_Setting()
        {
            InitializeComponent();
        }

        public void InitializationDefaultPage()
        {
            InitUI();
            ApplySettings();
        }

        private void InitUI()
        {
            CheckBox_TopMost.IsChecked = Settings.Default.topMost;
            CheckBox_Notification.IsChecked = Settings.Default.notification;
            UpdateUI_Keybind((Key)Settings.Default.bindKey_1, (Key)Settings.Default.bindKey_2);
        }

        private void ApplySettings()
        {
            Apply_CheckBox();
            Apply_Keybind();
        }

        private void SaveSettings()
        {
            Settings.Default.topMost = (bool)CheckBox_TopMost.IsChecked!;
            Settings.Default.notification = (bool)CheckBox_Notification.IsChecked!;

            // None 일경우 (사용자가 아무것도 지정하지 않은경우) 문제 방지 기능이 있음.
            if (key1 != Key.None)
            {
                Settings.Default.bindKey_1 = (int)key1;
            }

            if (key2 != Key.None)
            {
                Settings.Default.bindKey_2 = (int)key2;
            }

            Settings.Default.Save();
        }

        private void Apply_CheckBox()
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
        }

        private void Apply_Keybind()
        {
            key1 = (Key)Settings.Default.bindKey_1;
            key2 = (Key)Settings.Default.bindKey_2;

            UpdateUI_Keybind(key1, key2);

            var Virtual_Key1 = KeyInterop.VirtualKeyFromKey(key1);
            var Virtual_Key2 = KeyInterop.VirtualKeyFromKey(key2);

            HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
        }

        private void UpdateUI_Keybind(Key key1, Key key2)
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
            SaveSettings();
            ApplySettings();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if(Settings.Default.topMost == true) 
            {
                CheckBox_TopMost.IsChecked = true;
            }
            else
            {
                CheckBox_TopMost.IsChecked = false;
            }

            if(Settings.Default.notification == true)
            {
                CheckBox_Notification.IsChecked = true;
            }
            else
            {
                CheckBox_Notification.IsChecked = false;
            }

            key1 = (Key)Settings.Default.bindKey_1;
            key2 = (Key)Settings.Default.bindKey_2;
            UpdateUI_Keybind((Key)Settings.Default.bindKey_1, (Key)Settings.Default.bindKey_2);
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

            UpdateUI_Keybind(key1, key2);
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
