using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Windows
{
    static class Notification
    {
        public struct SoundType
        {
            private const string nameSpace = "ms-winsoundevent:Notification.";

            public const string default_ = $"{nameSpace}Default";
            public const string im = $"{nameSpace}IM";
            public const string reminder = $"{nameSpace}Reminder";
            public const string sms = $"{nameSpace}SMS";
            public const string alarm = $"{nameSpace}Looping.Alarm";
            public const string alarm2 = $"{nameSpace}Looping.Alarm2";
            public const string alarm3 = $"{nameSpace}Looping.Alarm3";
            public const string alarm4 = $"{nameSpace}Looping.Alarm4";
            public const string alarm5 = $"{nameSpace}Looping.Alarm5";
            public const string alarm6 = $"{nameSpace}Looping.Alarm6";
            public const string alarm7 = $"{nameSpace}Looping.Alarm7";
            public const string alarm8 = $"{nameSpace}Looping.Alarm8";
            public const string alarm9 = $"{nameSpace}Looping.Alarm9";
            public const string alarm10 = $"{nameSpace}Looping.Alarm10";
            public const string call = $"{nameSpace}Looping.Call";
            public const string call2 = $"{nameSpace}Looping.Call2";
            public const string call3 = $"{nameSpace}Looping.Call3";
            public const string call4 = $"{nameSpace}Looping.Call4";
            public const string call5 = $"{nameSpace}Looping.Call5";
            public const string call6 = $"{nameSpace}Looping.Call6";
            public const string call7 = $"{nameSpace}Looping.Call7";
            public const string call8 = $"{nameSpace}Looping.Call8";
            public const string call9 = $"{nameSpace}Looping.Call9";
            public const string call10 = $"{nameSpace}Looping.Call10";
        }

        private static string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }

        public static void Send(string title, string content, string soundType, int expirationTime = 5)
        {
            string removeHexDecimalText = ReplaceHexadecimalSymbols(content);

            Task task_notify = new Task(() =>
            {
                try
                {
                    // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                    new ToastContentBuilder()
                    .AddText(title)
                    .AddText(removeHexDecimalText)
                    .SetBackgroundActivation()
                    .AddAudio(new Uri(soundType))
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime);
                    });
                } 
                catch (Exception)
                {
                    new ToastContentBuilder()
                    .AddText("SUCCESS... BUT")
                    .AddText("INCLUDE INVALID CHARACTER.")
                    .SetBackgroundActivation()
                    .AddAudio(new Uri(SoundType.reminder))
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime);
                    });
                }
            });

            // 동시 task 방지.
            task_notify.Start();
            task_notify.Wait();
        }
    }
}
