using DeleteNewline.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace DeleteNewline
{
    public partial class Page_InputText
    {
        ViewModel_InputText? vm_input;
        ViewModel_Setting? vm_setting;

        public Page_InputText(ViewModel_InputText? vm_input_)
        {
            InitializeComponent();
            vm_input = vm_input_;

            DataContext = vm_input_;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (ClipboardManager.ContainText() == true)
            {
                originalText = ClipboardManager.GetText();
                TextBox_Main.AppendText(originalText);

                var regexAndReplace = vm_setting.GetAdditionalRegexAndReplace();

                (bool success, string replacedText) = ClipboardManager.ReplaceText(regexAndReplace.Item1, regexAndReplace.Item2);
                ClipboardManager.SetText(replacedText);
            }
            else
            {
                vm_input.AddAlertMessage(ref TextBox_Main);
                return;
            }
        }

        private void TextBox_Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
                if (ClipboardManager.ContainText() == true)
                {
                    var regexAndReplace = vm_setting.GetAdditionalRegexAndReplace();

                    (bool success, string replacedText) = ClipboardManager.ReplaceText(regexAndReplace.Item1, regexAndReplace.Item2);
                    ClipboardManager.SetText(replacedText);
                }
                else
                {
                    vm_input.AddAlertMessage(ref TextBox_Main);
                    return;
                }
            }
        }

        private void MenuItem_Clear_Click(object sender, RoutedEventArgs e)
        {
            TextBox_Main.Clear();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            vm_setting = App.GetService<ViewModel_Setting>();
        }
    }
}
