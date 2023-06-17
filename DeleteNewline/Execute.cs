using System;
using System.Windows;
using Windows;

namespace DeleteNewline
{
    static class Execute
    {
        static IDataObject? idataObj;

        public static void DeleteNewline_WithNotifier(string regex, string replace)
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            
            bool isTextForm = false;
            bool success = false;
            string replacedText = String.Empty;

            VirtualInput.InputImplement.TypeKeyboard_Copy();

            if (ClipboardManager.GetText(ref idataObj) == true)
            {
                isTextForm = true;

                (success, replacedText) = ClipboardManager.ReplaceText(ref idataObj, regex, replace);                

                int limitLen = 100;

                if (replacedText.Length > limitLen)
                {
                    notifyContent = replacedText.Substring(0, limitLen) + " ...";
                }
                else
                {
                    notifyContent = replacedText;
                }
            }

            if(isTextForm == false)
            {
                notifyHeader = "ERROR";
                notifyContent = "CLIPBOARD FORM IS NOT TEXT";
            }
            else if(success == false)
            {
                notifyHeader = "ERROR";
                notifyContent = replacedText;
            }
            else
            {
                notifyHeader = "SUCCESS";
            }

            Notification.Send(notifyHeader, notifyContent);
        }

        public static void DeleteNewline_WithoutNotifier(string regex, string replace)
        {
            VirtualInput.InputImplement.TypeKeyboard_Copy();

            if (ClipboardManager.GetText(ref idataObj) == true)
            {
                ClipboardManager.ReplaceText(ref idataObj, regex, replace);
            }
        }
    }
}
