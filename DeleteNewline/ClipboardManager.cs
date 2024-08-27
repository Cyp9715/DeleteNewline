using System;
using System.Collections.Generic;
using System.Windows;
using Windows;

namespace DeleteNewline
{
    /* 
     * ClipboardManager 의 SetText, GetText 부분에서 Exception 이 발생한다면
     * 이는 대부분 VirtualInput 과 상호작용 오류 (VirtualInput.TypeKeyboard_Copy() 는 Ctrl+C 를 통해 Clipboard 에 접근함) 문제임.
     * 때문에 VirtualInput 실행단을 Thread 로 분리한뒤, Thread 내부에서 적절히 유휴시간을 둘 필요성 + Join() 을 통해 동기화할 필요가 있음.
     * 
     * 이상적으론 해당 코드의 SetText 가 온전히 Text 를 설정할 수 있다는 보장이 없기 때문에 이를 확인하는 절차가 필요하나,
     * 또다시 클립보드에 접근하여 오히려 안정성을 떨어뜨리는 문제가 있음.
     * SetText() 를 검증하려 GetText() 를 사용하는 등으로 구현 하였는데, 그러한 구현은 COM 객체의 동시접근으로 인한 에러 횟수를 크게 늘림.
     * 그렇다고 Thread.Sleep() 등을 사용하면 사용자 반응성이 크게 저하되는 문제가 존재함.
     */
    static class ClipboardManager
    {
        public static (bool, string) ReplaceText(List<string> regex, List<string> replace)
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
            }
            catch (Exception)
            {
                Notification.Send("ERROR", "FAILED GET CLIPBOARD", Notification.SoundType.reminder, 300);
            }

            return output;
        }


        public static void SetText(string text)
        {
            try
            {
                Clipboard.SetDataObject(text);
            }
            catch (Exception)
            {
                Notification.Send("ERROR", "FAILED SET CLIPBOARD", Notification.SoundType.reminder, 300);
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
