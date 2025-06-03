using System.Diagnostics;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Delete_Newline.Services;

public class ClipboardMonitorService
{
    private readonly RegexService _regexService;
    private readonly NotificationService _notificationService;
    private readonly RegexCollectSaveService _regexCollectSaveService;

    public ClipboardMonitorService(RegexService regexService, NotificationService notificationService, RegexCollectSaveService regexCollectSaveService)
    {
        _regexService = regexService ?? throw new ArgumentNullException(nameof(regexService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _regexCollectSaveService = regexCollectSaveService ?? throw new ArgumentNullException(nameof(regexCollectSaveService));
    }

    public void StartMonitoring()
    {
        try
        {
            Clipboard.ContentChanged += OnClipboardContentChanged;
            Debug.WriteLine("[ClipboardMonitorService] Clipboard monitoring started.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error starting clipboard monitoring: {ex.Message}");
        }
    }

    public void StopMonitoring()
    {
        Clipboard.ContentChanged -= OnClipboardContentChanged;
        Debug.WriteLine("[ClipboardMonitorService] Clipboard monitoring stopped.");
    }

    // This logic is only used in Regex logic.
    private void OnClipboardContentChanged(object? sender, object e)
    {
        // Check if there's content and if it's text.
        DataPackageView dataPackageView = Clipboard.GetContent();
        if (dataPackageView.Contains(StandardDataFormats.Text) == false)
        {
            Debug.WriteLine("[ClipboardMonitorService] is not text");
            return;
        }

        string beforeText = string.Empty;
        try
        {
            beforeText = dataPackageView.GetTextAsync().GetAwaiter().GetResult();
            string afterText;

            // Check if a hotkey triggered this clipboard change
            int? triggeredHotkeyId = App.ActiveHotkeyIdForCopy;
            if (triggeredHotkeyId.HasValue)
            {
                App.ActiveHotkeyIdForCopy = null; // Reset immediately
                Debug.WriteLine($"[ClipboardMonitorService] Clipboard change by Hotkey ID: {triggeredHotkeyId.Value}. Applying regex rules.");
                afterText = _regexService.ApplyRegexRules(beforeText, triggeredHotkeyId.Value);
            }
            else
            {
                Debug.WriteLine("[ClipboardMonitorService] General clipboard change. No regex processing.");
                afterText = beforeText;
            }

            // If it is updated
            if (beforeText != afterText && !string.IsNullOrEmpty(afterText))
            {
                UpdateClipboardContent(afterText, triggeredHotkeyId);
            }
            else if (triggeredHotkeyId.HasValue)
            {
                // Hotkey was triggered but no text changes were made - don't show notification
                Debug.WriteLine($"[ClipboardMonitorService] Hotkey ID: {triggeredHotkeyId.Value} triggered but no regex rules matched or changed the text.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content. Raw text snippet was '{beforeText.Substring(0, Math.Min(beforeText.Length,50))}...': {ex.Message}");
        }
    }

    private void UpdateClipboardContent(string text, int? triggeredHotkeyId)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        
        try
        {
            Clipboard.SetContent(dataPackage);

            if (triggeredHotkeyId.HasValue && _notificationService.GetEnableNotification())
            {
                ShowHotkeyNotification(triggeredHotkeyId.Value);
            }
        }
        catch (Exception exSetContent)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error setting clipboard content: {exSetContent.Message}. Text was: {text.Substring(0, Math.Min(text.Length,50))}...");
        }
    }

    private void ShowHotkeyNotification(int hotkeyId)
    {
        RegexPageStructure? hotkeyStructure = _regexCollectSaveService.GetRegexStructureByHotkeyId(hotkeyId);
        if (hotkeyStructure == null)
        {
            return;
        }

        string displayHotkey = HotkeyHelper.GetDisplayText(hotkeyStructure.Hotkey.Modifiers, hotkeyStructure.Hotkey.Key);
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
