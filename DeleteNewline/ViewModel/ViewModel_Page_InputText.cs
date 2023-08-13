using System.Windows.Controls;

namespace DeleteNewline.ViewModel
{
    class ViewModel_Page_InputText
    {
        public void AddAlertMessage(ref TextBox textBox)
        {
            string errMsg = "\r\n" +
                  " ========================================================\r\n" +
                  " ================== Only text form can be entered =================\r\n" +
                  " ========================================================\r\n";

            textBox.AppendText(errMsg);
            textBox.ScrollToEnd();
        }
    }
}
