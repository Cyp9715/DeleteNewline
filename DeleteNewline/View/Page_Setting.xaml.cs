using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeleteNewline.ViewModel;
using GlobalHook;
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
        Settings appdata = DeleteNewline.Settings.Default;
        ViewModel_Page_Setting vm_setting;
        
        Key key1 = Key.None;
        Key key2 = Key.None;

        string key1_text = String.Empty;
        string key2_text = String.Empty;

        public Page_Setting()
        {
            InitializeComponent();
            vm_setting = new ViewModel_Page_Setting();
            this.DataContext = vm_setting;
        }

        private void SetKeybindUI(Key key1, Key key2)
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

        // 키를 누르는것에 따라 UI를 지정함. (여기서 지정되는 UI_Key 는 사용자 반응성을 위한것으로 임시적임)
        private void TextBox_bindKey_KeyDown(object sender, KeyEventArgs e)
        {
            if(key1 == Key.None)
            {
                key1 = (e.Key == Key.System) ? e.SystemKey : e.Key;
            }
            else
            {
                key2 = (e.Key == Key.System) ? e.SystemKey : e.Key;
            }

            SetKeybindUI(key1, key2);
        }

        // 키를 해제할경우 해당 키값을 셋팅에 이관.
        private void TextBox_bindKey_KeyUp(object sender, KeyEventArgs e)
        {
            vm_setting.key1 = key1;
            vm_setting.key2 = key2;
            vm_setting.key1_text = key1_text;
            vm_setting.key2_text = key2_text;

            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);
            Keyboard.ClearFocus();
        }

        // 포커스를 다시 얻었을 경우 각종 키 임시변수 값들과 텍스트박스를 초기화함.
        private void TextBox_bindKey_GotFocus(object sender, RoutedEventArgs e)
        {
            key1_text = string.Empty;
            key2_text = string.Empty;

            key1 = Key.None;
            key2 = Key.None;

            TextBox_bindKey.Text = string.Empty;
        }

        // 포커스를 잃어버릴경우 appdata 기반으로 키를 설정하고 UI를 재지정함.
        private void TextBox_bindKey_LostFocus(object sender, RoutedEventArgs e)
        {
            vm_setting.SaveKeyBindSettings();
            vm_setting.SetHookImplement();

            SetKeybindUI((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
        }
    }
}
