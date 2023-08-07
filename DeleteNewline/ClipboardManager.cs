using System;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using Windows;

namespace DeleteNewline
{
    static class ClipboardManager
    {
        public static (bool, string) ReplaceText(string regex, string replace)
        {
            bool success = false;
            string replacedText = string.Empty;

            string clipboardText = GetText();
            (success, replacedText) = RegexManager.Replace(clipboardText, regex, replace);
            
            return (success, replacedText);
        }

        public static string GetText()
        {
            string output = string.Empty;
            int maxTryCount = 5;

            for (int i = 1; i <= maxTryCount; ++i)
            {
                try
                {
                    output = Clipboard.GetText(TextDataFormat.UnicodeText);
                    Thread.Sleep(100);
                    break;
                }
                catch (Exception)
                {
                    if (i == maxTryCount)
                    {
                        string msgHeader = "ERROR";
                        string msgContent = "FAILED GET CLIPBOARD";
                        Notification.Send(msgHeader, msgContent, Notification.SoundType.reminder, 300);
                    }
                }
            }

            return output;
        }


        // SetDataObject 는 기본적으로 100ms 간격으로 10번 시도함.
        public static void SetText(string text)
        {
            try
            {
                Clipboard.SetDataObject(text);
                Thread.Sleep(100);
            }
            catch (Exception)
            {
                string msgHeader = "ERROR";
                string msgContent = "FAILED SET CLIPBOARD";
                Notification.Send(msgHeader, msgContent, Notification.SoundType.reminder, 300);
            }
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
