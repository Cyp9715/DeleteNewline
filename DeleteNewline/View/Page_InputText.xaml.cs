using DeleteNewline.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace DeleteNewline
{
    public partial class Page_InputText
    {
        public static Page_InputText? instance;
        ViewModel_Page_InputText vm_Input;

        public Page_InputText()
        {
            InitializeComponent();
            instance = this;
            vm_Input = new ViewModel_Page_InputText();
            DataContext = vm_Input;
        }

        IDataObject? idataObj;

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (ClipboardManager.GetText(ref idataObj) == true)
            {
                originalText = (string)idataObj.GetData(DataFormats.Text);
                TextBox_Main.AppendText(originalText);

                string regexExpression = ViewModel_Page_Setting.vm_settings.text_textBox_regexExpression;
                string regexReplace = ViewModel_Page_Setting.vm_settings.text_textBox_regexReplace;

                ClipboardManager.ReplaceText(ref idataObj, regexExpression, regexReplace);
            }
            else
            {
                vm_Input.AddAlertMessage(ref TextBox_Main);
                return;
            }
        }

        private void TextBox_Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
                if (ClipboardManager.GetText(ref idataObj) == true)
                {
                    string regexExpression = ViewModel_Page_Setting.vm_settings.text_textBox_regexExpression;
                    string regexReplace = ViewModel_Page_Setting.vm_settings.text_textBox_regexReplace;

                    ClipboardManager.ReplaceText(ref idataObj, regexExpression, regexReplace);
                }
                else
                {
                    vm_Input.AddAlertMessage(ref TextBox_Main);
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
