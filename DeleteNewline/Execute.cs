using System;
using System.Collections.Generic;
using System.Windows;
using Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Windows.Notification;

namespace DeleteNewline
{
    static class Execute
    {
        public static void DeleteNewline_WithNotifier(List<string> regex, List<string> replace)
        {
            var (isTextForm, success, replacedText) = ProcessClipboardText(regex, replace);

            if (!isTextForm)
            {
                Notification.Send("ERROR", "CLIPBOARD FORM IS NOT TEXT", Notification.SoundType.reminder, 300);
            }
            else if (!success)
            {
                Notification.Send("ERROR", "THE REGULAR EXPRESSION FUNCTION DID NOT WORK CORRECTLY.", Notification.SoundType.reminder, 300);
            }
            else
            {
                ClipboardManager.SetText(replacedText);
                string notifyContent = replacedText.Length > 100 ? replacedText.Substring(0, 100) + " ..." : replacedText;
                Notification.Send("SUCCESS", notifyContent, Notification.SoundType.default_, 300);
            }
        }

        public static void DeleteNewline_WithoutNotifier(List<string> regex, List<string> replace)
        {
            var (isTextForm, success, replacedText) = ProcessClipboardText(regex, replace);

            if (!isTextForm)
            {
                Notification.Send("ERROR", "CLIPBOARD FORM IS NOT TEXT", Notification.SoundType.reminder, 300);
            }
            else if (!success)
            {
                Notification.Send("ERROR", "THE REGULAR EXPRESSION FUNCTION DID NOT WORK CORRECTLY.", Notification.SoundType.reminder, 300);
            }
            else
            {
                ClipboardManager.SetText(replacedText);
            }
        }

        private static (bool isTextForm, bool success, string replacedText) ProcessClipboardText(List<string> regex, List<string> replace)
        {
            VirtualInput.Implement.TypeKeyboard_Copy();

            if (!ClipboardManager.ContainText())
            {
                return (false, false, string.Empty);
            }

            var (success, replacedText) = ClipboardManager.ReplaceText(regex, replace);
            return (true, success, replacedText);
        }
    }
}
