using System.Text.RegularExpressions;
using System.Windows;

namespace DeleteNewline
{
    class ClipboardManager
    {
        public string DeleteClipboardNewline(ref IDataObject idata, bool deleteMultipleSpace = false)
        {
            string clipboardText = (string)idata.GetData(DataFormats.Text);
            
            clipboardText = Regex.Replace(clipboardText, @"\r\n", "");

            if (deleteMultipleSpace == true)
            {
                clipboardText = Regex.Replace(clipboardText, @"\s+", " ");
            }

            return clipboardText;
        }

        public bool GetClipboardData_Text(ref IDataObject idata)
        {
            idata = Clipboard.GetDataObject();

            if (idata.GetDataPresent(DataFormats.Text) == false)
            {
                return false;
            }

            return true;
        }
    }
}
