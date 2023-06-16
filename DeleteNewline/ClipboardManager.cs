using System.Windows;

namespace DeleteNewline
{
    class ClipboardManager
    {
        RegexManager regexManager = new RegexManager();

        public string DeleteClipboardNewline(ref IDataObject idata, string regex, string replace)
        {
            string clipboardText = (string)idata.GetData(DataFormats.Text);
            return regexManager.Replace(clipboardText, regex, replace);
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
