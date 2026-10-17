using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Delete_Newline.Services;

public class ClipboardMonitorService
{
    private const int ClipboardReadAttempts = 6;
    private static readonly TimeSpan ClipboardReadRetryDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan ClipboardReadTimeout = TimeSpan.FromMilliseconds(750);

    private readonly RegexService _regexService;
    private readonly NotificationService _notificationService;
    private readonly RegexCollectSaveService _regexCollectSaveService;
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private bool _isMonitoring;

    public ClipboardMonitorService(RegexService regexService, NotificationService notificationService, RegexCollectSaveService regexCollectSaveService)
    {
        _regexService = regexService ?? throw new ArgumentNullException(nameof(regexService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _regexCollectSaveService = regexCollectSaveService ?? throw new ArgumentNullException(nameof(regexCollectSaveService));
    }

    public void StartMonitoring()
    {
        if (_isMonitoring)
        {
            return;
        }

        try
        {
            Clipboard.ContentChanged += OnClipboardContentChanged;
            _isMonitoring = true;
            Debug.WriteLine("[ClipboardMonitorService] Clipboard monitoring started.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error starting clipboard monitoring: {ex.Message}");
        }
    }

    public void StopMonitoring()
    {
        if (_isMonitoring == false)
        {
            return;
        }

        Clipboard.ContentChanged -= OnClipboardContentChanged;
        _isMonitoring = false;
        Debug.WriteLine("[ClipboardMonitorService] Clipboard monitoring stopped.");
    }

    public void RestartMonitoring()
    {
        StopMonitoring();
        StartMonitoring();
    }

    private async void OnClipboardContentChanged(object? sender, object e)
    {
        if (App.TryConsumeActiveHotkeyIdForCopy(out int triggeredHotkeyId) == false)
        {
            Debug.WriteLine("[ClipboardMonitorService] Clipboard change ignored because no hotkey is pending.");
            return;
        }

        string? beforeText = null;
        bool lockAcquired = false;
        try
        {
            await _processingLock.WaitAsync();
            lockAcquired = true;
            uint clipboardVersion = GetClipboardSequenceNumber();
            beforeText = await TryReadClipboardTextAsync();
            if (beforeText == null)
            {
                Debug.WriteLine($"[ClipboardMonitorService] Clipboard text read timed out for Hotkey ID: {triggeredHotkeyId}.");
                return;
            }

            Debug.WriteLine($"[ClipboardMonitorService] Clipboard change by Hotkey ID: {triggeredHotkeyId}. Applying regex rules.");
            if (GetClipboardSequenceNumber() != clipboardVersion) return;
            string afterText = await _regexService.ApplyRegexRulesAsync(beforeText, triggeredHotkeyId);

            // Do not overwrite a newer copy while a long-running rule was executing.
            if (GetClipboardSequenceNumber() == clipboardVersion)
            {
                if (beforeText != afterText)
                {
                    // Text was modified
                    UpdateClipboardContent(afterText, triggeredHotkeyId, true);
                }
                else
                {
                    // Text was not modified - no matching rules
                    UpdateClipboardContent(afterText, triggeredHotkeyId, false);
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException or TimeoutException)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Regex failed: {ex.Message}");
            _notificationService.ShowSystemNotification(
                "Notification_RegexFailed_Title", "Notification_RegexFailed_Message", force: false, addTag: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content: {ex.Message}");
        }
        finally
        {
            if (lockAcquired)
            {
                _processingLock.Release();
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    private async Task<string?> TryReadClipboardTextAsync()
    {
        for (int attempt = 0; attempt < ClipboardReadAttempts; attempt++)
        {
            try
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Text) == false)
                {
                    Debug.WriteLine("[ClipboardMonitorService] Clipboard content is not text yet.");
                }
                else
                {
                    return await dataPackageView.GetTextAsync().AsTask().WaitAsync(ClipboardReadTimeout);
                }
            }
            catch (TimeoutException)
            {
                Debug.WriteLine($"[ClipboardMonitorService] Timed out while reading clipboard text on attempt {attempt + 1}.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ClipboardMonitorService] Clipboard read attempt {attempt + 1} failed: {ex.Message}");
            }

            await Task.Delay(ClipboardReadRetryDelay);
        }

        return null;
    }

    private void UpdateClipboardContent(string text, int triggeredHotkeyId, bool wasModified)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        
        try
        {
            if (wasModified)
            {
                Clipboard.SetContent(dataPackage);
            }

            if (_notificationService.GetEnableNotification())
            {
                if (wasModified)
                {
                    ShowModifiedNotification(triggeredHotkeyId);
                }
                else
                {
                    _notificationService.ShowSystemNotification(
                        titleKey: "Notification_TextNotModified_Title",
                        messageKey: "Notification_TextNotModified_Message",
                        force: false,
                        addTag: true
                    );
                }
            }
        }
        catch (Exception exSetContent)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error setting clipboard content: {exSetContent.Message}");
        }
    }

    private void ShowModifiedNotification(int hotkeyId)
    {
        RegexPageStructure? hotkeyStructure = _regexCollectSaveService.GetRegexStructureByHotkeyId(hotkeyId);
        if (hotkeyStructure == null)
        {
            return;
        }

        string displayHotkey = HotkeyFormatter.GetDisplayText(hotkeyStructure.Hotkey.Modifiers, hotkeyStructure.Hotkey.Key);
        string messageKey;
        object[] messageArgs;

        if (string.IsNullOrWhiteSpace(hotkeyStructure.HotkeyName))
        {
            messageKey = "Notification_ActionForHotkeyCompleted_Message";
            messageArgs = new object[] { displayHotkey };
        }
        else
        {
            messageKey = "Notification_ActionForHotkeyWithNameCompleted_Message";
            messageArgs = new object[] { hotkeyStructure.HotkeyName, displayHotkey };
        }

        _notificationService.ShowSystemNotification(
            titleKey: "Notification_TextModified_Title",
            messageKey: messageKey,
            force: false,
            addTag: true,
            messageArgs: messageArgs
        );
    }
}
