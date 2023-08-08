using System;
using Windows;

namespace DeleteNewline
{
    static class Execute
    {
        public static void DeleteNewline_WithNotifier(string regex, string replace)
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            
            bool isTextForm = false;
            bool isReplaced = false;
            string replacedText = String.Empty;

            VirtualInput.Implement.TypeKeyboard_Copy();

            if (ClipboardManager.ContainText() == true)
            {
                isTextForm = true;

                (isReplaced, replacedText) = ClipboardManager.ReplaceText(regex, replace);
                ClipboardManager.SetText(replacedText);

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
                Notification.Send(notifyHeader, notifyContent, Notification.SoundType.reminder, 300);
            }
            else if(isReplaced == false)
            {
                notifyHeader = "ERROR";
                notifyContent = "THE REGULAR EXPRESSION FUNCTION DID NOT WORK CORRECTLY.";
                Notification.Send(notifyHeader, notifyContent, Notification.SoundType.reminder, 300);
            }
            else
            {
                notifyHeader = "SUCCESS";
                Notification.Send(notifyHeader, notifyContent, Notification.SoundType.default_);
            }
        }

        public static void DeleteNewline_WithoutNotifier(string regex, string replace)
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;

            bool isTextForm = false;
            bool isReplaced = false;

            VirtualInput.Implement.TypeKeyboard_Copy();

            if (ClipboardManager.ContainText() == true)
            {
                isTextForm = true;

                (isReplaced, string replacedText) = ClipboardManager.ReplaceText(regex, replace);
                ClipboardManager.SetText(replacedText);
            }

            // WithoutNotifier 여도 ERROR 메세지는 알림.
            if(isTextForm == false)
            {
                notifyHeader = "ERROR";
                notifyContent = "CLIPBOARD FORM IS NOT TEXT";
                Notification.Send(notifyHeader, notifyContent, Notification.SoundType.reminder, 300);
            }
            else if(isReplaced == false)
            {
                notifyHeader = "ERROR";
                notifyContent = "THE REGULAR EXPRESSION FUNCTION DID NOT WORK CORRECTLY.";
                Notification.Send(notifyHeader, notifyContent, Notification.SoundType.reminder, 300);
            }
        }
    }
}
