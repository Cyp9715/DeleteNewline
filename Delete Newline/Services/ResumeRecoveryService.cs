using Delete_Newline.Helpers.Hotkeys;
using Microsoft.UI.Dispatching;

namespace Delete_Newline.Services;

public sealed class ResumeRecoveryService
{
    private static readonly TimeSpan ResumeRecoveryDelay = TimeSpan.FromSeconds(2);

    private readonly ClipboardMonitorService _clipboardMonitorService;
    private readonly TrayIconService _trayIconService;
    private readonly TopMostService _topMostService;
    private DispatcherQueueTimer? _resumeRecoveryTimer;

    public ResumeRecoveryService(
        ClipboardMonitorService clipboardMonitorService,
        TrayIconService trayIconService,
        TopMostService topMostService)
    {
        _clipboardMonitorService = clipboardMonitorService;
        _trayIconService = trayIconService;
        _topMostService = topMostService;
    }

    public void ScheduleResumeRecovery()
    {
        var dispatcherQueue = App.MainWindow.DispatcherQueue;
        if (dispatcherQueue == null)
        {
            return;
        }

        dispatcherQueue.TryEnqueue(() =>
        {
            _resumeRecoveryTimer ??= dispatcherQueue.CreateTimer();
            _resumeRecoveryTimer.Stop();
            _resumeRecoveryTimer.IsRepeating = false;
            _resumeRecoveryTimer.Interval = ResumeRecoveryDelay;
            _resumeRecoveryTimer.Tick -= OnResumeRecoveryTimerTick;
            _resumeRecoveryTimer.Tick += OnResumeRecoveryTimerTick;
            _resumeRecoveryTimer.Start();
        });
    }

    private void OnResumeRecoveryTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnResumeRecoveryTimerTick;
        RecoverFromResume();
    }

    private void RecoverFromResume()
    {
        App.ClearActiveHotkeyIdForCopy();

        SafeInvoke("clipboard monitor restart", _clipboardMonitorService.RestartMonitoring);
        SafeInvoke("hotkey refresh", HotkeyRegister.RefreshAllHotkeys);
        SafeInvoke("tray icon refresh", _trayIconService.RefreshTrayIcon);
        SafeInvoke("top-most reapply", _topMostService.ReapplyWindowTopMost);
    }

    private static void SafeInvoke(string operationName, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ResumeRecoveryService] Failed during {operationName}: {ex.Message}");
        }
    }
}
