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

        bool topMost_origin = Settings.Default.topMost;

        public Page_Setting()
        {
            InitializeComponent();
        }

        public void InitializationDefaultPage()
        {
            SetTopMost();
            SetHook_KeyBind();
        }

        private void SaveSettings_TopMost()
        {
            if (CheckBox_TopMost.IsChecked is not null)
            {
                Settings.Default.topMost = (bool)CheckBox_TopMost.IsChecked;
            }

            Settings.Default.Save();
        }

        private void SetTopMost()
        {
            if (CheckBox_TopMost.IsChecked != null)
            {
                if (CheckBox_TopMost.IsChecked == true)
                {
                    MainWindow.mainWindow.Topmost = true;
                }
                else
                {
                    MainWindow.mainWindow.Topmost = false;
                }
            }

            Settings.Default.Save();
        }

        private void SaveSettings_KeyBind()
        {
            // None 일경우 (사용자가 아무것도 지정하지 않은경우)
            // 문제를 방지하는 기능이 있음.
            if(key1 != Key.None)
            {
                Settings.Default.bindKey_1 = (int)key1;
            }

            if(key2 != Key.None)
            {
                Settings.Default.bindKey_2 = (int)key2;
            }

            Settings.Default.Save();
        }

        private void SetHook_KeyBind()
        {
            key1 = (Key)Settings.Default.bindKey_1;
            key2 = (Key)Settings.Default.bindKey_2;

            SetKeyBindTextBox(key1, key2);

            var Virtual_Key1 = KeyInterop.VirtualKeyFromKey(key1);
            var Virtual_Key2 = KeyInterop.VirtualKeyFromKey(key2);

            HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
        }

        private void SetKeyBindTextBox(Key key1, Key key2)
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
            SaveSettings_TopMost();
            SaveSettings_KeyBind();
            SetHook_KeyBind();
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

            SetKeyBindTextBox((Key)Settings.Default.bindKey_1, (Key)Settings.Default.bindKey_2);
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

            SetKeyBindTextBox(key1, key2);
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
