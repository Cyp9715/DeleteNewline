using DeleteNewline.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace DeleteNewline
{
    public partial class Page_InputText
    {
        ViewModel_InputText? vm_input;
        ViewModel_Setting? vm_settings;

        public Page_InputText(ViewModel_InputText? vm_input_, ViewModel_Setting? vm_settings_)
        {
            InitializeComponent();
            vm_input = vm_input_;
            vm_settings = vm_settings_;

            DataContext = vm_input_;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (ClipboardManager.ContainText() == true)
            {
                originalText = ClipboardManager.GetText();
                TextBox_Main.AppendText(originalText);

                string regexExpression = vm_settings.text_textBox_regexExpression;
                string regexReplace = vm_settings.text_textBox_regexReplace;

                (bool success, string replacedText) = ClipboardManager.ReplaceText(regexExpression, regexReplace);
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
                    string regexExpression = vm_settings.text_textBox_regexExpression;
                    string regexReplace = vm_settings.text_textBox_regexReplace;

                    (bool success, string replacedText) = ClipboardManager.ReplaceText(regexExpression, regexReplace);
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
    }
}
