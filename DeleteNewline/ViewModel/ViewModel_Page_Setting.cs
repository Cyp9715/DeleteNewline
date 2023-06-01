using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit;

using GlobalHook;
using System;
using System.Windows.Forms;
using System.Windows.Input;

namespace DeleteNewline.ViewModel
{
    class ViewModel_Page_Setting : ObservableObject
    {
        Settings appdata = DeleteNewline.Settings.Default;

        bool _isChecked_checkBox_topMost;
        bool _isChecked_checkBox_notification;
        bool _ischecked_checkBox_deleteMultipleSpace;

        string _content_textBox_keybind = String.Empty;

        public ViewModel_Page_Setting()
        {
            isChecked_checkBox_topMost = appdata.topMost;
            isChecked_checkBox_notification = appdata.notification;
            isChecked_checkBox_deleteMultipleSpace = appdata.deleteMultipleSpace;

            content_textBox_keybind = GetKeybindText((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
            SetHookImplement();
        }

        public bool isChecked_checkBox_topMost
        {
            get => _isChecked_checkBox_topMost;
            set
            {
                appdata.topMost = value;
                appdata.Save();

                _isChecked_checkBox_topMost = value;
            }
        }

        // 해당변수가 변경될 때 지정된 값에 따라 HookImplement.execute 의 Action 값을 치환해 줌.
        public bool isChecked_checkBox_notification
        {
            get =>_isChecked_checkBox_notification;
            set
            {
                appdata.notification = value!;
                appdata.Save();

                HookImplement.execute = (value == true) ?
                    Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;

                _isChecked_checkBox_notification = value;
            }
        }

        public bool isChecked_checkBox_deleteMultipleSpace
        {
            get =>_ischecked_checkBox_deleteMultipleSpace;
            set
            {
                appdata.deleteMultipleSpace = value;
                appdata.Save();

                _ischecked_checkBox_deleteMultipleSpace = value;
            }
        }

        public string content_textBox_keybind
        {
            get => _content_textBox_keybind;
            set
            {
                _content_textBox_keybind = value;
            }
        }

        public void SaveKeyBindSettings()
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


        KeyConverter keyConverter = new KeyConverter();

        public string GetKeybindText(Key key1, Key key2)
        {
            string key1_text = string.Empty;
            string key2_text = string.Empty;

            string output = string.Empty;

            if (key2 == Key.None)
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                output = key1_text;
            }
            else
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                key2_text = keyConverter.ConvertToString(key2)!;
                output = key1_text + " + " + key2_text;
            }

            return output;
        }

        public EventToCommandBehaviorPage()
        {
            Content = new Button()
            .Behaviors(new EventToCommandBehavior
            {
                EventName = nameof(Button.Clicked),
                Command = new MyCustomCommand()
            });
        }


        public Key key1 = Key.None;
        public Key key2 = Key.None;

        public string key1_text = string.Empty;
        public string key2_text = string.Empty;

        public void SetHookImplement()
        {
            Action setKeybind = () =>
            {
                key1 = (Key)appdata.bindKey_1;
                key2 = (Key)appdata.bindKey_2;

                content_textBox_keybind = GetKeybindText(key1, key2);

                var Virtual_Key1 = KeyInterop.VirtualKeyFromKey(key1);
                var Virtual_Key2 = KeyInterop.VirtualKeyFromKey(key2);

                HookImplement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
            };

            setKeybind();
        }

    }
}
