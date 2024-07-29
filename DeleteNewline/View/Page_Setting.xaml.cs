using DeleteNewline.ViewModel;
using GlobalHook;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeleteNewline
{
    public partial class Page_Setting
    {
        Settings appdata;
        ViewModel_Setting vm_setting;

        public Page_Setting(ViewModel_Setting vm_setting_) 
        {
            InitializeComponent();
            appdata = Settings.GetInstance();
            vm_setting = vm_setting_;
            DataContext = vm_setting_;
        }

        Key key1 = Key.None;
        Key key2 = Key.None;

        // 키를 누르는것에 따라 UI를 지정함. (여기서 지정되는 Key 는 사용자 반응성을 위한것으로 임시적임)
        private void TextBox_bindKey_KeyDown(object sender, KeyEventArgs e)
        {
            if(key1 == Key.None)
            {
                key1 = (e.Key == Key.System) ? e.SystemKey : e.Key;
            }
            else
            {
                // 만약 key1과 key2가 같은 입력값이라면 key2 에 할당하지 않음 (Press 동작 방지)
                if(e.Key != key1 && e.SystemKey != key1)
                {
                    key2 = (e.Key == Key.System) ? e.SystemKey : e.Key;
                }
            }

            vm_setting.SetUI_keybind(key1, key2);
        }

        // 키를 입력후 뗄 경우 vm_setting.key 임시변수에 값을 지정.
        private void TextBox_bindKey_KeyUp(object sender, KeyEventArgs e)
        {
            vm_setting.key1 = key1;
            vm_setting.key2 = key2;

            // 키를 뗄경우 자동적으로 포커스를 초기화 시킴.
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope((TextBox)sender), null);
            Keyboard.ClearFocus();
        }

        // 포커스를 다시 얻었을 경우 GlobalHook 를 활성화 하고, 임시변수 값들과 텍스트박스를 초기화함.
        private void TextBox_bindKey_GotFocus(object sender, RoutedEventArgs e)
        {
            HookImplement.UnInstallGlobalHook();

            key1 = Key.None;
            key2 = Key.None;

            TextBox_bindKey.Text = string.Empty;
        }

        // 포커스를 잃어버릴경우 appdata 기반으로 키를 설정하고 UI를 재설정함.
        private void TextBox_bindKey_LostFocus(object sender, RoutedEventArgs e)
        {
            vm_setting.SaveKeyBind();
            HookImplement.SetHookKeys(appdata);

            vm_setting.SetUI_keybind((Key)appdata.bindKey_1, (Key)appdata.bindKey_2);

            HookImplement.InstallGlobalHook();
        }

        private void button_RegexDefault_Click(object sender, RoutedEventArgs e)
        {
            vm_setting.text_textBox_regexExpression = @"\r\n|\n";
            vm_setting.text_textBox_regexReplace = " ";

            vm_setting.UpdateTextBox_regexOutput();
        }

        private void TextBox_regexExpression_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm_setting.UpdateTextBox_regexOutput(textBox_regexExpression: TextBox_regexExpression);
        }

        private void TextBox_regexReplace_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm_setting.UpdateTextBox_regexOutput(textBox_regexReplace: TextBox_regexReplace);
        }

        private void TextBox_regexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            vm_setting.UpdateTextBox_regexOutput(textBox_regexInput: TextBox_regexInput);
        }

        private void Button_addRegex_Click(object sender, RoutedEventArgs e)
        {
            vm_setting.additionalRegex.Add(new ViewModel_Setting.GenericParameter_OC("Regex " + vm_setting.gp_count, "Replace " + vm_setting.gp_count, vm_setting.gp_count++));
        }

        private void Button_deleteRegex_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ViewModel_Setting.GenericParameter_OC gp_oc)
            {
                // machineFunction의 index 및 다른 속성에 접근할 수 있다.
                int index = gp_oc.index;

                // 이 정보를 사용하여 작업을 수행한다.
                vm_setting.additionalRegex.Remove(vm_setting.additionalRegex.Where(i => i.index == index).Single());
            }
        }
    }
}
