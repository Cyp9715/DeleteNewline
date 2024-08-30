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
    // label, replace 는 default 값으로 지정.
    public partial class AdditionalRegexConfig : ObservableObject
    {
        [ObservableProperty] string label_expression = "Regex Expression";
        [ObservableProperty] string label_replace = "Regex Replace";
        [ObservableProperty] string textBox_regexExpression = string.Empty;
        [ObservableProperty] string textBox_regexReplace = string.Empty;
    }

    public partial class ViewModel_Setting : ObservableValidator
    {
        Settings setting;

        [ObservableProperty] bool isTopMost;
        [ObservableProperty] bool isNotificationEnabled;

        [ObservableProperty] string keybindText = string.Empty;
        [ObservableProperty] string regexExpression = string.Empty;
        [ObservableProperty] string regexReplace = string.Empty;

        [ObservableProperty] string inputTestRegex = string.Empty;
        [ObservableProperty] string outputTestRegex = string.Empty;

        [ObservableProperty] ObservableCollection<AdditionalRegexConfig> additionalRegex = new ObservableCollection<AdditionalRegexConfig>();

        public ViewModel_Setting()
        {
            setting = Settings.GetInstance();
            ApplySettingFileToUI();
        }

        public void ApplySettingFileToUI()
        {
            IsTopMost = setting.topMost;
            IsNotificationEnabled = setting.notification;
            RegexExpression = setting.regexExpression;
            RegexReplace = setting.regexReplace;
            InputTestRegex = setting.inputTestRegex;

            ApplyTopMost();
            ApplyNotifier();

            // 가상 키 코드를 WPF Key 타입으로 변환
            SetKeybindUI(KeyInterop.KeyFromVirtualKey((int)Hook.key1),
                          KeyInterop.KeyFromVirtualKey((int)Hook.key2));

            if (setting.AdditionalRegexes != null)
            {
                foreach (var regex in setting.AdditionalRegexes)
                {
                    AdditionalRegex.Add(new AdditionalRegexConfig()
                    {
                        TextBox_regexExpression = regex.RegexExpression,
                        TextBox_regexReplace = regex.RegexReplace
                    });
                }
            }

            UpdateRegexOutput();
        }

        public void ReadyToSaveSetting()
        {
            setting.topMost = IsTopMost;
            setting.notification = IsNotificationEnabled;

            setting.regexExpression = RegexExpression;
            setting.regexReplace = RegexReplace;
            setting.inputTestRegex = InputTestRegex;
            setting.outputTestRegex = OutputTestRegex;

            setting.AdditionalRegexes = AdditionalRegex.Select(arc => new AdditionalRegex
            {
                RegexExpression = arc.TextBox_regexExpression,
                RegexReplace = arc.TextBox_regexReplace
            }).ToList();
        }

        Key key1 = Key.None;
        Key key2 = Key.None;

        // 키를 누르는것에 따라 UI를 지정함. (여기서 지정되는 Key 는 사용자 반응성을 위한것으로 임시적임)
        [RelayCommand]
        private void CaptureKeybindOnKeyDown(KeyEventArgs e)
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

            SetKeybindUI(key1, key2);
        }

        // 키를 입력후 뗄 경우 vm_setting.key 임시변수에 값을 지정.
        [RelayCommand]
        private void ClearKeybindFocusOnKeyUp(object sender)
        {
            // 키를 뗄경우 자동적으로 포커스를 초기화 시킴.
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);
            Keyboard.ClearFocus();
        }

        // 포커스를 다시 얻었을 경우 GlobalHook를 활성화 하고, 임시변수 값들과 텍스트박스를 초기화함.
        [RelayCommand]
        private void ResetKeybindOnFocus()
        {
            Hook.UnInstall();

            key1 = Key.None;
            key2 = Key.None;

            KeybindText = string.Empty;
        }

        // 포커스를 잃어버릴경우 저장된 setting에 키를 설정하고 UI를 재설정함.
        [RelayCommand]
        private void SaveAndApplyKeybindOnLostFocus()
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
            SetKeybindUI(setting.bindKey_1, setting.bindKey_2);

            // GlobalHook 재활성화
            Hook.Install();
        }

        KeyConverter keyConverter = new KeyConverter();

        public void SetKeybindUI(Key key1, Key key2)
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

        public (List<string>, List<string>) GetAllRegexAndReplace()
        {
            List<string> regex_expressions = new List<string>();
            List<string> regex_replaces = new List<string>();

            regex_expressions.Add(RegexExpression);
            regex_replaces.Add(RegexReplace);

            if (AdditionalRegex != null)
            {
                foreach (var i in AdditionalRegex)
                {
                    regex_expressions.Add(i.TextBox_regexExpression);
                    regex_replaces.Add(i.TextBox_regexReplace);
                }
            }
            return (regex_expressions, regex_replaces);
        }

        [RelayCommand]
        private void ApplyTopMost()
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.Topmost = IsTopMost;
        }

        [RelayCommand]
        private void ApplyNotifier()
        {
            Hook.execute = (IsNotificationEnabled == true) ? 
                Execute.DeleteNewline_WithNotifier : Execute.DeleteNewline_WithoutNotifier;
        }

        [RelayCommand]
        public void UpdateRegexOutput()
        {
            var regexAndReplace = GetAllRegexAndReplace();
            (_, OutputTestRegex) = RegexManager.Replace(InputTestRegex, regexAndReplace.Item1, regexAndReplace.Item2);
        }

        [RelayCommand]
        private void AddAdditionalRegex()
        {
            AdditionalRegex.Add(new AdditionalRegexConfig());
        }

        [RelayCommand]
        private void DeleteAdditionalRegex(AdditionalRegexConfig additionalRegexConfig)
        {
            if (additionalRegexConfig != null)
            {
                AdditionalRegex.Remove(additionalRegexConfig);
                UpdateRegexOutput();
            }
        }
    }
}
