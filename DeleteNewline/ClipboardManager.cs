using System.Windows;

namespace DeleteNewline
{
    class ClipboardManager
    {
        RegexManager regexManager = new RegexManager();

        public (bool, string) applyRegex(ref IDataObject idata, string regex, string replace)
        {
            string clipboardText = (string)idata.GetData(DataFormats.Text);
            return regexManager.Replace(clipboardText, regex, replace);
        }

        public bool GetClipboardText(ref IDataObject idata)
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
