using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Delete_Newline.Helpers;

namespace Delete_Newline.Services;

public class InAppNotificationService
{
    private const int AutoHideDelayMs = 3500;
    private const int FadeOutDurationMs = 500;

    private InfoBar? _infoBar;
    private DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _autoHideTimer;
    private Storyboard? _fadeOutStoryboard;

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
            StopAutoHideTimer();
            StopFadeOutAnimation();

            _infoBar.Opacity = 1;
            _infoBar.Title = LocalizationHelper.GetLocalizedString(titleKey);
            _infoBar.Message = LocalizationHelper.GetLocalizedString(messageKey, messageArgs ?? System.Array.Empty<object>());
            _infoBar.Severity = severity;
            _infoBar.IsOpen = true;

            StartAutoHideTimer();
        });
    }

    public void HideInAppNotification()
    {
        if (_dispatcherQueue == null || _infoBar == null) return;

        _dispatcherQueue.TryEnqueue(() =>
        {
            StopAutoHideTimer();
            StopFadeOutAnimation();
            _infoBar.Opacity = 1;
            _infoBar.IsOpen = false;
        });
    }

    private void StartAutoHideTimer()
    {
        if (_dispatcherQueue == null || _infoBar == null) return;

        _autoHideTimer ??= _dispatcherQueue.CreateTimer();
        _autoHideTimer.Stop();
        _autoHideTimer.Interval = TimeSpan.FromMilliseconds(AutoHideDelayMs);
        _autoHideTimer.Tick -= OnAutoHideTimerTick;
        _autoHideTimer.Tick += OnAutoHideTimerTick;
        _autoHideTimer.Start();
    }

    private void StopAutoHideTimer()
    {
        if (_autoHideTimer == null) return;
        _autoHideTimer.Stop();
        _autoHideTimer.Tick -= OnAutoHideTimerTick;
    }

    private void OnAutoHideTimerTick(DispatcherQueueTimer sender, object args)
    {
        StopAutoHideTimer();
        BeginFadeOut();
    }

    private void BeginFadeOut()
    {
        if (_infoBar == null || _dispatcherQueue == null || !_infoBar.IsOpen) return;

        StopFadeOutAnimation();

        var fadeAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(FadeOutDurationMs)),
            EnableDependentAnimation = true
        };

        _fadeOutStoryboard = new Storyboard();
        _fadeOutStoryboard.Children.Add(fadeAnimation);
        Storyboard.SetTarget(fadeAnimation, _infoBar);
        Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
        _fadeOutStoryboard.Completed += OnFadeOutCompleted;
        _fadeOutStoryboard.Begin();
    }

    private void StopFadeOutAnimation()
    {
        if (_fadeOutStoryboard == null) return;
        _fadeOutStoryboard.Completed -= OnFadeOutCompleted;
        _fadeOutStoryboard.Stop();
        _fadeOutStoryboard = null;
    }

    private void OnFadeOutCompleted(object? sender, object e)
    {
        if (_infoBar != null)
        {
            _infoBar.IsOpen = false;
            _infoBar.Opacity = 1;
        }

        StopFadeOutAnimation();
    }
} 
