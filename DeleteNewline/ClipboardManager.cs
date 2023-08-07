using System;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using Windows;

namespace DeleteNewline
{
    /* 
     * ClipboardManager 의 SetText, GetText 부분에서 Exception 이 발생한다면
     * 이는 대부분 VirtualInput 과 상호작용간의 오류 (VirtualInput 은 Ctrl+C 를 통해 Clipboard 에 접근함) 문제임.
     * 때문에 VirtualInput 실행단에 적절히 유휴시간을 둘 필요성이 있으며,
     * 해당 코드에서는 올바르게 데이터를 가져왔는지, 올바르게 지정했는지 이중으로 확인할 필요성이 있음.
     */
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

            try
            {
                output = Clipboard.GetText(TextDataFormat.UnicodeText);

                if(String.IsNullOrEmpty(output))
                {
                    string msgHeader = "ERROR";
                    string msgContent = "STRING DATA FORMAT IS INCORRECT";
                    Notification.Send(msgHeader, msgContent, Notification.SoundType.reminder, 300);
                }
            }
            catch (Exception)
            {
                string msgHeader = "ERROR";
                string msgContent = "FAILED GET CLIPBOARD";
                Notification.Send(msgHeader, msgContent, Notification.SoundType.reminder, 300);
            }

            return output;
        }


        /*
         * 해당함수는 Text를 Clipboard 에 확실히 Set 하도록 보장하지 않는 문제점이 있음.
         * 사용자 반응성을 중시.
         */
        public static void SetText(string text)
        {
            try
            {
                Clipboard.SetDataObject(text);
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
