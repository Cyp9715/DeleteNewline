using Microsoft.Toolkit.Uwp.Notifications;
using System.Threading.Tasks;

namespace WindowNotification
{
    static class Notification
    {
        public static void Send(string title, string content)
        {
            Task task_notify = new Task(() =>
            {
                // Requires Microsoft.Toolkit.Uwp.Notifications NuGet package version 7.0 or greater
                new ToastContentBuilder()
                    .AddText(title)
                    .AddText(content)
                    .Show();
            });

            task_notify.Start();
            task_notify.Wait();
        }
    }
}
