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

        ClipboardManager clipboardManager = new ClipboardManager();
        IDataObject? idataObj;

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (clipboardManager.GetClipboardText(ref idataObj) == true)
            {
                originalText = (string)idataObj.GetData(DataFormats.Text);
                TextBox_Main.AppendText(originalText);

                (var success, var replaceText) = clipboardManager.applyRegex(ref idataObj,
                        ViewModel_Page_Setting.page_settings.text_textBox_regexExpression, 
                        ViewModel_Page_Setting.page_settings.text_textBox_regexReplace);
                Clipboard.SetDataObject(replaceText);
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
                if (clipboardManager.GetClipboardText(ref idataObj) == true)
                {
                    (var success, var replaceText) = clipboardManager.applyRegex(ref idataObj,
                            ViewModel_Page_Setting.page_settings.text_textBox_regexExpression,
                            ViewModel_Page_Setting.page_settings.text_textBox_regexReplace);

                    Clipboard.SetDataObject(replaceText);
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
