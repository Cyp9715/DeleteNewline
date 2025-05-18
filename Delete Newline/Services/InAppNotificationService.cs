using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Views;
using Microsoft.UI.Dispatching;
using Delete_Newline.Helpers; // For LocalizationHelper

namespace Delete_Newline.Services;

public class InAppNotificationService
{
    private InfoBar? _infoBar; // Store direct reference to InfoBar
    private DispatcherQueue? _dispatcherQueue;

    public void Initialize(InfoBar infoBar)
    {
        _infoBar = infoBar;
        _dispatcherQueue = infoBar.DispatcherQueue;
    }

    public void ShowInAppNotification(string titleKey, string messageKey, InfoBarSeverity severity = InfoBarSeverity.Informational, params object[]? messageArgs)
    {
        if (_dispatcherQueue == null || _infoBar == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            _infoBar.Title = LocalizationHelper.GetLocalizedString(titleKey);
            _infoBar.Message = LocalizationHelper.GetLocalizedString(messageKey, messageArgs ?? System.Array.Empty<object>());
            _infoBar.Severity = severity;
            _infoBar.IsOpen = true;
        });
    }

    public void HideInAppNotification()
    {
        if (_dispatcherQueue == null || _infoBar == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_infoBar != null) _infoBar.IsOpen = false;
        });
    }
} 