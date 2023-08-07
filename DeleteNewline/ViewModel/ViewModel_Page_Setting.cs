using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Input;
using GlobalHook;
using System.Windows.Controls;

namespace DeleteNewline.ViewModel
{
    class ViewModel_Page_Setting : ObservableObject
    {
        public static ViewModel_Page_Setting? vm_settings;

        Settings appdata = DeleteNewline.Settings.Default;
        readonly string setting_space = "`space`";

        public ViewModel_Page_Setting()
        {
            if(vm_settings == null)
            {
                vm_settings = this;

                isChecked_checkBox_topMost = appdata.topMost;
                isChecked_checkBox_notification = appdata.notification;
                SetUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);
                
                text_textBox_regexExpression = appdata.regexExpression == setting_space ? " " : appdata.regexExpression;
                text_textBox_regexReplace = appdata.regexReplace == setting_space ? " " : appdata.regexReplace;
                text_textBox_inputRegex = appdata.regexInput;
                UpdateTextBox_regexOutput();

                SetHookKeys();
            }
        }

        bool _isChecked_checkBox_topMost;
        bool _isChecked_checkBox_notification;

        string _text_textBox_keybind = String.Empty;
        string _text_textBox_regexExpression = String.Empty;
        string _text_textBox_regexReplace = String.Empty;

        string _text_textBox_inputRegex = String.Empty;
        string _text_textBox_outputRegex = String.Empty;

        public bool isChecked_checkBox_topMost
        {
            get => _isChecked_checkBox_topMost;
            set
            {
                SetProperty(ref _isChecked_checkBox_topMost, value);

                MainWindow.mainWindow.Topmost = value;
                appdata.topMost = value;
            }
        }

        // 해당변수가 변경될 때 지정된 값에 따라 HookImplement.execute 의 Action 값을 치환해 줌.
        public bool isChecked_checkBox_notification
        {
            get =>_isChecked_checkBox_notification;
            set
            {
                SetProperty(ref _isChecked_checkBox_notification, value);

                Implement.execute = (value == true) ?
                    Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;

                appdata.notification = value;
            }
        }

        public string text_textBox_keybind
        {
            get => _text_textBox_keybind;

            // SetProperty 는 변경되었을 경우만 OnPropertyChanged 를 발생시키기에 수동으로 추가함.
            // 아무것도 입력하지 않고 textBox_bindKey Focus 벗어날 시 문제발생
            set
            {
                OnPropertyChanging("text_textBox_keybind");
                _text_textBox_keybind = value;
                OnPropertyChanged("text_textBox_keybind");
            }
        }

        public string text_textBox_regexExpression
        {
            get => _text_textBox_regexExpression;

            set
            {
                SetProperty(ref _text_textBox_regexExpression, value);
                appdata.regexExpression = value == " " ? setting_space : value;
            }
        }

        public string text_textBox_regexReplace
        {
            get => _text_textBox_regexReplace;

            set
            {
                SetProperty(ref _text_textBox_regexReplace, value);
                appdata.regexReplace = value == " " ? setting_space : value;
            }
        }

        public string text_textBox_inputRegex
        {
            get => _text_textBox_inputRegex;

            set
            {
                SetProperty(ref _text_textBox_inputRegex, value);
                appdata.regexInput = value;
            }
        }

        public string text_textBox_outputRegex
        {
            get => _text_textBox_outputRegex;
        
            set
            {
                SetProperty(ref _text_textBox_outputRegex, value);
            }
        }

        public void UpdateTextBox_regexOutput(TextBox? textBox_regexExpression = null, TextBox? textBox_regexReplace = null, TextBox ? textBox_regexInput = null)
        {
            // realtime update
            if (textBox_regexInput != null)
            {
                text_textBox_inputRegex = textBox_regexInput.Text;
            }

            if (textBox_regexExpression != null)
            {
                text_textBox_regexExpression = textBox_regexExpression.Text;
            }

            if (textBox_regexReplace != null)
            {
                text_textBox_regexReplace = textBox_regexReplace.Text;
            }

            OnPropertyChanging("text_textBox_outputRegexExample");
            (var success, text_textBox_outputRegex) = RegexManager.Replace(text_textBox_inputRegex, text_textBox_regexExpression, text_textBox_regexReplace);
            OnPropertyChanged("text_textBox_outputRegexExample");
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

            // 만약 사용자가 의도적으로 '동일한 키' 입력을 지정하려 한다면(ex : S + S) 기본 키 셋팅값인 LeftAlt + F1 값으로 설정함.
            if(appdata.bindKey_1 == appdata.bindKey_2)
            {
                appdata.bindKey_1 = (int)Key.LeftAlt;
                appdata.bindKey_2 = (int)Key.F1;
            }

            appdata.Save();
        }

        public void SetHookKeys()
        {
            var Virtual_Key1 = KeyInterop.VirtualKeyFromKey((Key)appdata.bindKey_1);
            var Virtual_Key2 = KeyInterop.VirtualKeyFromKey((Key)appdata.bindKey_2);

            Implement.SetKeys((VirtualKeycodes)Virtual_Key1, (VirtualKeycodes)Virtual_Key2);
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
