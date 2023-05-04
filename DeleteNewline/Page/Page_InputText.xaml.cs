
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeleteNewline
{
    /// <summary>
    /// Page_InputText.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_InputText
    {
        ClipboardManager cbManager = new ClipboardManager();
        IDataObject? idataObj;

        public Page_InputText()
        {
            InitializeComponent();
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (cbManager.GetClipboardData_Text(ref idataObj) == true)
            {
                originalText = (string)idataObj.GetData(DataFormats.Text);
                TextBox_Main.AppendText(originalText);
                Clipboard.SetDataObject(cbManager.DeleteClipboardNewline(ref idataObj).ToString());
            }
            else
            {
                AddAlertMessageInTextBox();
                return;
            }
        }

        private void AddAlertMessageInTextBox()
        {
            TextBox_Main.AppendText("\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.AppendText(" ================== Only text form can be entered =================\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.ScrollToEnd();
        }

        private void TextBox_Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
                if (cbManager.GetClipboardData_Text(ref idataObj) == true)
                {
                    Clipboard.SetDataObject(cbManager.DeleteClipboardNewline(ref idataObj).ToString());
                }
                else
                {
                    AddAlertMessageInTextBox();
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
