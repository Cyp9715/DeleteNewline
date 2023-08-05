using System.Windows.Controls;

namespace DeleteNewline.ViewModel
{
    class ViewModel_Page_InputText
    {
        public void AddAlertMessage(ref TextBox textBox)
        {
            textBox.AppendText("\r\n");
            textBox.AppendText(" ========================================================\r\n");
            textBox.AppendText(" ================== Only text form can be entered =================\r\n");
            textBox.AppendText(" ========================================================\r\n");
            textBox.ScrollToEnd();
        }
    }
}
