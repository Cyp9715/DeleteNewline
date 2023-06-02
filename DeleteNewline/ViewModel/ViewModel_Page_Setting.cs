using CommunityToolkit.Mvvm.ComponentModel;

using GlobalHook;
using System;
using System.Windows.Input;

namespace DeleteNewline.ViewModel
{
    class ViewModel_Page_Setting : ObservableObject
    {
        Settings appdata = DeleteNewline.Settings.Default;

        bool _isChecked_checkBox_topMost;
        bool _isChecked_checkBox_notification;
        bool _ischecked_checkBox_deleteMultipleSpace;

        string _text_textBox_keybind = String.Empty;

        public ViewModel_Page_Setting()
        {
            isChecked_checkBox_topMost = appdata.topMost;
            isChecked_checkBox_notification = appdata.notification;
            isChecked_checkBox_deleteMultipleSpace = appdata.deleteMultipleSpace;

            SetUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
            SetHookKeys();
        }

        public bool isChecked_checkBox_topMost
        {
            get => _isChecked_checkBox_topMost;
            set
            {
                SetProperty(ref _isChecked_checkBox_topMost, value);

                MainWindow.mainWindow.Topmost = value;
                appdata.topMost = value;
                appdata.Save();
            }
        }

        // 해당변수가 변경될 때 지정된 값에 따라 HookImplement.execute 의 Action 값을 치환해 줌.
        public bool isChecked_checkBox_notification
        {
            get =>_isChecked_checkBox_notification;
            set
            {
                SetProperty(ref _isChecked_checkBox_notification, value);

                HookImplement.execute = (value == true) ?
                    Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;

                appdata.notification = value;
                appdata.Save();
            }
        }

        public bool isChecked_checkBox_deleteMultipleSpace
        {
            get =>_ischecked_checkBox_deleteMultipleSpace;
            set
            {
                SetProperty(ref _ischecked_checkBox_deleteMultipleSpace, value);

                appdata.deleteMultipleSpace = value;
                appdata.Save();
            }
        }

        public string text_textBox_keybind
        {
            get => _text_textBox_keybind;

            // SetProperty 는 변경되었을 경우만 OnPropertyChanged 를 발생시키기에 수동으로 추가함.
            // (아무것도 입력하지 않고 textBox_bindKey Focus 벗어날 시 문제발생)
            set
            {
                OnPropertyChanging();
                _text_textBox_keybind = value;
                OnPropertyChanged();
            }
        }

        public Key key1 = Key.None;
        public Key key2 = Key.None;

        public void SaveKeyBind()
        {
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

        public void SetHookKeys()
        {
            var Virtual_Key1 = KeyInterop.VirtualKeyFromKey((Key)appdata.bindKey_1);
            var Virtual_Key2 = KeyInterop.VirtualKeyFromKey((Key)appdata.bindKey_2);

            HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
        }

        KeyConverter keyConverter = new KeyConverter();

        public void SetUI_keybind(Key key1, Key key2)
        {
            string key1_text = String.Empty;
            string key2_text = String.Empty;

            if (key2 == Key.None)
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                text_textBox_keybind = key1_text;
            }
            else
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                key2_text = keyConverter.ConvertToString(key2)!;
                text_textBox_keybind = key1_text + " + " + key2_text;
            }
        }
    }
}
