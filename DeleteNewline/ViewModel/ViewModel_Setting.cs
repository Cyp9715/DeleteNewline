using System;
using GlobalHook;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DeleteNewline.ViewModel
{
    public partial class ViewModel_Setting : ObservableValidator
    {
        Settings setting;
        KeyConverter keyConverter;

        public ViewModel_Setting()
        {
            setting = Settings.GetInstance();
            keyConverter = new KeyConverter();

            IsChecked_checkBox_topMost = setting.topMost;
            IsChecked_checkBox_notification = setting.notification;
            SetUI_keybind(setting.bindKey_1, setting.bindKey_2);

            Text_textBox_regexExpression = setting.regexExpression;
            Text_textBox_regexReplace = setting.regexReplace;
            Text_textBox_inputRegex = setting.inputRegex;
            Update_RegexOutput();

            SetNotifier();
            HookImplement.SetHookKeys(setting.bindKey_1, setting.bindKey_2);
            additionalRegex = new ObservableCollection<GenericParameter_OC>();
        }

        [ObservableProperty]
        bool isChecked_checkBox_topMost;
        [ObservableProperty]
        bool isChecked_checkBox_notification;

        [ObservableProperty]
        string text_textBox_keybind;
        [ObservableProperty]
        string text_textBox_regexExpression;
        [ObservableProperty]
        string text_textBox_regexReplace;

        [ObservableProperty]
        string text_textBox_inputRegex = string.Empty;
        [ObservableProperty]
        string text_textBox_outputRegex;


        Key key1 = Key.None;
        Key key2 = Key.None;

        // 키를 누르는것에 따라 UI를 지정함. (여기서 지정되는 Key 는 사용자 반응성을 위한것으로 임시적임)
        [RelayCommand]
        private void TextBox_bindKey_KeyDown(KeyEventArgs e)
        {
            if (key1 == Key.None)
            {
                key1 = (e.Key == Key.System) ? e.SystemKey : e.Key;
            }
            else
            {
                // 만약 key1과 key2가 같은 입력값이라면 key2 에 할당하지 않음 (Press 동작 방지)
                if (e.Key != key1 && e.SystemKey != key1)
                {
                    key2 = (e.Key == Key.System) ? e.SystemKey : e.Key;
                }
            }

            SetUI_keybind(key1, key2);
        }

        // 키를 입력후 뗄 경우 vm_setting.key 임시변수에 값을 지정.
        [RelayCommand]
        private void TextBox_bindKey_KeyUp(object sender)
        {
            // 키를 뗄경우 자동적으로 포커스를 초기화 시킴.
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);
            Keyboard.ClearFocus();
        }

        // 포커스를 다시 얻었을 경우 GlobalHook 를 활성화 하고, 임시변수 값들과 텍스트박스를 초기화함.
        [RelayCommand]
        private void TextBox_bindKey_GotFocus()
        {
            HookImplement.UnInstallGlobalHook();

            key1 = Key.None;
            key2 = Key.None;

            Text_textBox_keybind = string.Empty;
        }

        // 포커스를 잃어버릴경우 appdata 기반으로 키를 설정하고 UI를 재설정함.
        [RelayCommand]
        private void TextBox_bindKey_LostFocus()
        {
            // None 일경우 (사용자가 아무것도 지정하지 않은경우) 문제 방지 기능이 있음.
            // 갱신되지 않을경우 기본셋팅코드로 리셋.
            if (key1 != Key.None)
            {
                setting.bindKey_1 = key1;
            }

            if (key2 != Key.None)
            {
                setting.bindKey_2 = key2;
            }

            // 만약 사용자가 의도적으로 '동일한 키' 입력을 지정하려 한다면(ex : S + S) 기본 키 셋팅값인 LeftAlt + F1 값으로 설정함.
            if (setting.bindKey_1 == setting.bindKey_2)
            {
                setting.bindKey_1 = Key.LeftAlt;
                setting.bindKey_2 = Key.F1;
            }

            Settings.Apply(setting);
            HookImplement.SetHookKeys(setting.bindKey_1, setting.bindKey_2);
            SetUI_keybind(setting.bindKey_1, setting.bindKey_2);

            HookImplement.InstallGlobalHook();
        }

        public void SetUI_keybind(Key key1, Key key2)
        {
            string key1_text = string.Empty;
            string key2_text = string.Empty;

            if (key2 == Key.None)
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                Text_textBox_keybind = key1_text;
            }
            else
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                key2_text = keyConverter.ConvertToString(key2)!;
                Text_textBox_keybind = key1_text + " + " + key2_text;
            }
        }

        public (List<string>, List<string>) GetAdditionalRegexAndReplace()
        {
            List<string> regex_expressions = new List<string>();
            List<string> regex_replaces = new List<string>();

            regex_expressions.Add(Text_textBox_regexExpression);
            regex_replaces.Add(Text_textBox_regexReplace);

            if (additionalRegex != null)
            {
                foreach (var i in additionalRegex)
                {
                    regex_expressions.Add(i.text_textBox_addtionalRegexExpression);
                    regex_replaces.Add(i.text_textBox_additionalRegexReplace);
                }
            }
            return (regex_expressions, regex_replaces);
        }

        [RelayCommand]
        private void button_RegexDefault_Click()
        {
            Text_textBox_regexExpression = @"\r\n|\n";
            Text_textBox_regexReplace = " ";

            Update_RegexOutput();
        }

        [RelayCommand]
        private void TextBox_regexExpression_TextChanged()
        {
            setting.regexExpression = Text_textBox_regexExpression;
            setting.regexReplace = Text_textBox_regexReplace;
            setting.inputRegex = Text_textBox_inputRegex;
            setting.outputRegex = Text_textBox_outputRegex;

            Update_RegexOutput();
            Settings.Apply(setting);
        }

        [RelayCommand]
        private void SetNotifier()
        {
            HookImplement.execute = (IsChecked_checkBox_notification == true) ? 
                Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;
        }

        public void Update_RegexOutput()
        {
            var regexAndReplace = GetAdditionalRegexAndReplace();
            (var success, Text_textBox_outputRegex) = RegexManager.Replace(Text_textBox_inputRegex, regexAndReplace.Item1, regexAndReplace.Item2);
        }

        public class GenericParameter_OC
        {
            public string label_expression { get; set; } = string.Empty;
            public string label_replace { get; set; } = string.Empty;
            public string text_textBox_addtionalRegexExpression { get; set; } = string.Empty;
            public string text_textBox_additionalRegexReplace { get; set; } = string.Empty;

            public int index { get; set; }

            public GenericParameter_OC(string content_expression, string content_replace, int index_)
            {
                label_expression = content_expression;
                label_replace = content_replace;
                index = index_;
            }
        }

        public int gp_count = 1;

        public ObservableCollection<GenericParameter_OC> additionalRegex { get; set; }
    }
}
