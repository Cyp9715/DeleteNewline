using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Windows
{
    static class Notification
    {
        private static string ReplaceHexadecimalSymbols(string txt)
        {
            string r = "[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(txt, r, "", RegexOptions.Compiled);
        }

        public static void Send(string title, string content, int expirationTime = 5)
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
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime);
                    });


                } 
                catch (ArgumentException) 
                {
                    new ToastContentBuilder()
                    .AddText("SUCCESS... BUT")
                    .AddText("INCLUDE INVALID CHARACTER.")
                    .SetBackgroundActivation()
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
