using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Windows
{
    static class Notification
    {
        public static void Send(string title, string content, int expirationTime = 30)
        {
            Task task_notify = new Task(() =>
            {
                // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                new ToastContentBuilder()
                .AddText(title)
                .AddText(content)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(expirationTime);
                });
            });

            task_notify.Start();
            task_notify.Wait();
        }
    }
}
