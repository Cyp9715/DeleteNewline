using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Windows
{
    static class Notification
    {
        public static void Send(string title, string content, int expirationTime = 0)
        {
            Task task_notify = new Task(() =>
            {
                try
                {
                    // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                    new ToastContentBuilder()
                    .AddText(title)
                    .AddText(content)
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime);
                    });
                } 
                
                catch (ArgumentException e) 
                {
                    new ToastContentBuilder()
                    .AddText("SUCCESS... BUT")
                    .AddText("CONTENT INCLUDE [include invalid character]")
                    .Show(toast =>
                    {
                        toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime);
                    });
                }

            });

            task_notify.Start();
            task_notify.Wait();
        }
    }
}
