using System;
using System.Collections.Generic;
using Windows;

namespace DeleteNewline
{
    static class Execute
    {
        public static void DeleteNewline_WithNotifier(List<string> regex, List<string> replace)
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            
            bool isTextForm = false;
            bool success = false;
            string replacedText = String.Empty;

            VirtualInput.Implement.TypeKeyboard_Copy();

            if (ClipboardManager.ContainText() == true)
            {
                isTextForm = true;

                // Regex Chain 적용
                (success, replacedText) = ClipboardManager.ReplaceText(regex, replace);
                // 결과를 Clipboard 에 Setting.
                ClipboardManager.SetText(replacedText);


                // Notifier는 최대 100 자만
                int limitLen = 100;

                if (replacedText.Length > limitLen)
                    notifyContent = replacedText.Substring(0, limitLen) + " ...";
                else
                    notifyContent = replacedText;
            }

            if(isTextForm == false)
            {
                notifyHeader = "ERROR";
                notifyContent = "CLIPBOARD FORM IS NOT TEXT";
                Notification.Send(notifyHeader, notifyContent, Notification.SoundType.reminder, 300);
            }
            else if(success == false)
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

        public static void DeleteNewline_WithoutNotifier(List<string> regex, List<string> replace)
        {
            VirtualInput.Implement.TypeKeyboard_Copy();

            if (ClipboardManager.ContainText() == true)
            {
                (bool success, string replacedText) = ClipboardManager.ReplaceText(regex, replace);
                ClipboardManager.SetText(replacedText);
            }
        }
    }
}
