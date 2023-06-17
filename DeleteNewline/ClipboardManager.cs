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
            IDataObject temp_idata;

            lock (lockClipboard)
            {
                temp_idata = Clipboard.GetDataObject();

                if (temp_idata.GetDataPresent(DataFormats.Text) == false)
                {
                    return false;
                }
                else
                {
                    idata = temp_idata;
                    return true;
                }
            }
        }
    }
}
