using GlobalHook;
using System.Linq;
using System.Windows;
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
            keyConverter = new KeyConverter();
            setting = Settings.GetInstance();

            ImportSettingToUI();
        }

        [ObservableProperty] bool isTopMost;
        [ObservableProperty] bool isNotificationEnabled;

        [ObservableProperty] string keybindText = string.Empty;
        [ObservableProperty] string regexExpression = string.Empty;
        [ObservableProperty] string regexReplace = string.Empty;

        [ObservableProperty] string inputTestRegex = string.Empty;
        [ObservableProperty] string outputTestRegex = string.Empty;

        [ObservableProperty] ObservableCollection<AdditionalRegexConfig> additionalRegex = new ObservableCollection<AdditionalRegexConfig>();

        public void ImportSettingToUI()
        {
            IsTopMost = setting.topMost;
            SetTopMost();
            IsNotificationEnabled = setting.notification;
            SetNotifier();

            // 가상 키 코드를 WPF Key 타입으로 변환
            SetUI_keybind(KeyInterop.KeyFromVirtualKey((int)Hook.key1),
                          KeyInterop.KeyFromVirtualKey((int)Hook.key2));

            RegexExpression = setting.regexExpression;
            RegexReplace = setting.regexReplace;
            InputTestRegex = setting.inputRegex;
            Update_RegexOutput();

        }

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
                // 만약 key1과 key2가 같은 입력값이라면 key2에 할당하지 않음 (Press 동작 방지)
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

        // 포커스를 다시 얻었을 경우 GlobalHook를 활성화 하고, 임시변수 값들과 텍스트박스를 초기화함.
        [RelayCommand]
        private void TextBox_bindKey_GotFocus()
        {
            Hook.UnInstall();

            key1 = Key.None;
            key2 = Key.None;

            KeybindText = string.Empty;
        }

        // 포커스를 잃어버릴경우 저장된 setting에 키를 설정하고 UI를 재설정함.
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

            Hook.SetKeys(setting.bindKey_1, setting.bindKey_2);
            SetUI_keybind(setting.bindKey_1, setting.bindKey_2);

            // GlobalHook 재활성화
            Hook.Install();

            Settings.ApplyLoadedSettings(setting);
        }

        public void SetUI_keybind(Key key1, Key key2)
        {
            string key1_text;
            string key2_text;

            if (key2 == Key.None)
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                KeybindText = key1_text;
            }
            else
            {
                key1_text = keyConverter.ConvertToString(key1)!;
                key2_text = keyConverter.ConvertToString(key2)!;
                KeybindText = key1_text + " + " + key2_text;
            }
        }


        // 구조 개선 필요.
        public (List<string>, List<string>) GetAdditionalRegexAndReplace()
        {
            List<string> regex_expressions = new List<string>();
            List<string> regex_replaces = new List<string>();

            regex_expressions.Add(RegexExpression);
            regex_replaces.Add(RegexReplace);

            if (AdditionalRegex != null)
            {
                foreach (var i in AdditionalRegex)
                {
                    regex_expressions.Add(i.RegexExpression_additional);
                    regex_replaces.Add(i.RegexReplace_additional);
                }
            }
            return (regex_expressions, regex_replaces);
        }

        [RelayCommand]
        private void TextBox_regexExpression_TextChanged(AdditionalRegexConfig? config = null)
        {
            setting.regexExpression = RegexExpression;
            setting.regexReplace = RegexReplace;
            setting.inputRegex = InputTestRegex;
            setting.outputRegex = OutputTestRegex;

            if (config != null)
            {
                // AdditionalRegex 컬렉션에서 config를 찾아 업데이트
                var itemToUpdate = AdditionalRegex.FirstOrDefault(x => x.Index == config.Index);
                if (itemToUpdate != null)
                {
                    itemToUpdate.RegexExpression_additional = config.RegexExpression_additional;
                    itemToUpdate.RegexReplace_additional = config.RegexReplace_additional;
                }
            }

            Update_RegexOutput();
            Settings.ApplyLoadedSettings(setting);
        }

        [RelayCommand]
        private void SetTopMost()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.Topmost = IsTopMost == true ? true : false;
        }

        [RelayCommand]
        private void SetNotifier()
        {
            Hook.execute = (IsNotificationEnabled == true) ? 
                Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;

            setting.notification = IsNotificationEnabled;
            Settings.ApplyLoadedSettings(setting);
        }


        // Text_textBox_inputRegex 및 지정된 Regex Expression, Regex Replace를 적용한 뒤
        // textBox_outputRegex에 출력합니다.
        public void Update_RegexOutput()
        {
            var regexAndReplace = GetAdditionalRegexAndReplace();
            (_, OutputTestRegex) = RegexManager.Replace(InputTestRegex, regexAndReplace.Item1, regexAndReplace.Item2);
        }

        public partial class AdditionalRegexConfig : ObservableObject
        {
            [ObservableProperty] string label_expression;
            [ObservableProperty] string label_replace;
            [ObservableProperty] string regexExpression_additional = string.Empty;
            [ObservableProperty] string regexReplace_additional = string.Empty;
            [ObservableProperty] int index;

            public AdditionalRegexConfig(string content_expression, string content_replace, int index_)
            {
                label_expression = content_expression;
                label_replace = content_replace;
                index = index_;
            }
        }

        // 이러한 Index는 Regex 개수 제한을 위해 부여하려 한 것이나, 굳이 의미가 있나 싶음.
        // 검증해 보진 않았으나 Regex Chain 10,000 개 까지는 인텔 샐러론 Windows PC 환경에서 제대로 구동될것임.
        // 구현할바에 풀어두는게 나을것 같아서 주석과 함께 풀어둠.
        // 하드하게 사용하는 경우 int32 Range를 넘어서서 StackOverFlow 와 중복 제거 오류가 발생할 여지도 있음.
        public int indexCounter_AR = 1;

        [RelayCommand]
        private void Button_addRegex_Click()
        {
            AdditionalRegex.Add(new AdditionalRegexConfig("Regex Expression", "Replace", indexCounter_AR++));
        }

        [RelayCommand]
        private void Button_deleteRegex_Click(AdditionalRegexConfig additionalRegexConfig)
        {
            if (additionalRegexConfig != null)
            {
                var itemToRemove = AdditionalRegex.FirstOrDefault(i => i.Index == additionalRegexConfig.Index);

                if (itemToRemove != null)
                    AdditionalRegex.Remove(itemToRemove);

                TextBox_regexExpression_TextChanged();
            }
        }
    }
}
