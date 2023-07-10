using System.Windows;
using System.Windows.Navigation;

namespace DeleteNewline
{
    static class ClipboardManager
    {
        static RegexManager regexManager = new RegexManager();
        static private object lockClipboard = new object();

        public static (bool, string) ReplaceText(string regex, string replace)
        {
            lock(lockClipboard)
            {
                string clipboardText = Clipboard.GetText(TextDataFormat.UnicodeText);
                (bool success, clipboardText) = regexManager.Replace(clipboardText, regex, replace);
                Clipboard.SetDataObject(clipboardText);

                return (success, clipboardText);
            }
        }

        public static string GetText()
        {
            return Clipboard.GetText(TextDataFormat.UnicodeText);
        }

        public static bool ContainText()
        {
            if(Clipboard.ContainsText() == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
