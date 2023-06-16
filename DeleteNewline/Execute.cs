using System;
using System.Windows;
using Windows;

namespace DeleteNewline
{
    static class Execute
    {
        static ClipboardManager clipboardManager = new ClipboardManager();
        static IDataObject? idataObj;

        public static void DeleteNewline_WithNotifier(string regex, string replace)
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            
            bool isTextForm = false;
            bool isCorrectRegex = false;

            string replacedText = String.Empty;

            VirtualInput.InputImplement.TypeKeyboard_Copy();

            if (clipboardManager.GetClipboardText(ref idataObj) == true)
            {
                isTextForm = true;

                (isCorrectRegex, replacedText) = clipboardManager.applyRegex(ref idataObj, regex, replace);
                Clipboard.SetDataObject(replacedText);
                

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
            else if(isCorrectRegex == false)
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

            if (clipboardManager.GetClipboardText(ref idataObj) == true)
            {
                (var success, var replacedText) = clipboardManager.applyRegex(ref idataObj, regex, replace);
                Clipboard.SetDataObject(replacedText);
            }
        }
    }
}
