using System;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using Windows;

namespace DeleteNewline
{
    static class ClipboardManager
    {
        static RegexManager regexManager = new RegexManager();

        public static (bool, string) ReplaceText(string regex, string replace)
        {
            bool success = false;
            string replacedText = string.Empty;

            string clipboardText = GetText_unicode();
            (success, replacedText) = regexManager.Replace(clipboardText, regex, replace);
            Clipboard.SetDataObject(replacedText);
            
            return (success, clipboardText);
        }

        public static string GetText_unicode()
        {
            string output = string.Empty;

            // ClipBoard 를 다른 프로그램에서 이미 사용중일 경우 재시도.
            Thread thread = new Thread(() =>
            {
                int maxTryCount = 5;

                for(int i = 1; i <= maxTryCount; ++i)
                {
                    try
                    {
                        output = Clipboard.GetText(TextDataFormat.UnicodeText);
                        break;
                    }
                    catch(Exception)
                    {
                        Thread.Sleep(75);

                        if(i == maxTryCount)
                        {
                            string warningMsg = "FAILED Clipboard.GetText()";
                            Notification.Send(warningMsg, String.Empty, Notification.SoundType.reminder);
                        }
                    }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return output;
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
