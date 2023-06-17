using System.Windows;

namespace DeleteNewline
{
    static class ClipboardManager
    {
        static RegexManager regexManager = new RegexManager();
        static private object lockClipboard = new object();

        public static (bool, string) ReplaceText(ref IDataObject idata, string regex, string replace)
        {
            lock(lockClipboard)
            {
                string clipboardText = (string)idata.GetData(DataFormats.Text);
                (bool success, clipboardText) = regexManager.Replace(clipboardText, regex, replace);
                Clipboard.SetDataObject(clipboardText);

                return (success, clipboardText);
            }
        }

        public static bool GetText(ref IDataObject idata)
        {
            lock (lockClipboard)
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
}
