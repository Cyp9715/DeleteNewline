using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Views;
using Microsoft.UI.Dispatching;

namespace Delete_Newline.Services;

public class InAppNotificationService
{
    private ShellPage? _shellPage;
    private DispatcherQueue? _dispatcherQueue;

    public void Initialize(ShellPage shellPage)
    {
        _shellPage = shellPage;
        _dispatcherQueue = shellPage.DispatcherQueue;
    }

    public void ShowNotification(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        if (_dispatcherQueue == null || _shellPage == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            _shellPage.ShowNotification(title, message, severity);
        });
    }

    public void HideNotification()
    {
        if (_dispatcherQueue == null || _shellPage == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            _shellPage.HideNotification();
        });
    }
} 