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

        ClipboardManager cbManager = new ClipboardManager();
        IDataObject? idataObj;

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (cbManager.GetClipboardData_Text(ref idataObj) == true)
            {
                originalText = (string)idataObj.GetData(DataFormats.Text);
                TextBox_Main.AppendText(originalText);
                Clipboard.SetDataObject(cbManager.DeleteClipboardNewline(ref idataObj).ToString(),
                    Settings.Default.deleteMultipleSpace);
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
                if (cbManager.GetClipboardData_Text(ref idataObj) == true)
                {
                    Clipboard.SetDataObject(cbManager.DeleteClipboardNewline(ref idataObj).ToString(), 
                        Settings.Default.deleteMultipleSpace);
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
