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
            Clipboard.ContentChanged += OnClipboardContentChangedInternal;
            Debug.WriteLine("[ClipboardMonitorService] Clipboard monitoring started.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error starting clipboard monitoring: {ex.Message}");
        }
    }

    public void StopMonitoring()
    {
        Clipboard.ContentChanged -= OnClipboardContentChangedInternal;
        Debug.WriteLine("[ClipboardMonitorService] Clipboard monitoring stopped.");
    }

    private void OnClipboardContentChangedInternal(object? sender, object e)
    {
        // Check if there's content and if it's text.
        DataPackageView dataPackageView = Clipboard.GetContent();
        if (dataPackageView.Contains(StandardDataFormats.Text) == false)
        {
            Debug.WriteLine("[ClipboardMonitorService] is not text");
            return;
        }

        string rawText = string.Empty;
        try
        {
            rawText = dataPackageView.GetTextAsync().GetAwaiter().GetResult();
            string cleanedText;

            // Check if a hotkey triggered this clipboard change
            int? triggeredHotkeyId = App.ActiveHotkeyIdForCopy;
            if (triggeredHotkeyId.HasValue)
            {
                App.ActiveHotkeyIdForCopy = null; // Reset immediately
                Debug.WriteLine($"[ClipboardMonitorService] Clipboard change by Hotkey ID: {triggeredHotkeyId.Value}. Applying regex rules.");
                cleanedText = _regexService.ApplyRegexRules(rawText, triggeredHotkeyId.Value);
            }
            else
            {
                Debug.WriteLine("[ClipboardMonitorService] General clipboard change. No regex processing.");
                cleanedText = rawText;
            }

            if (rawText != cleanedText && !string.IsNullOrEmpty(cleanedText))
            {
                UpdateClipboardContent(cleanedText, triggeredHotkeyId);
            }
            else if (triggeredHotkeyId.HasValue)
            {
                // Hotkey was triggered but no text changes were made - don't show notification
                Debug.WriteLine($"[ClipboardMonitorService] Hotkey ID: {triggeredHotkeyId.Value} triggered but no regex rules matched or changed the text.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content. Raw text snippet was '{rawText.Substring(0, Math.Min(rawText.Length,50))}...': {ex.Message}");
        }
    }

    private void UpdateClipboardContent(string cleanedText, int? triggeredHotkeyId)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(cleanedText);
        
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
            Debug.WriteLine($"[ClipboardMonitorService] Error setting clipboard content: {exSetContent.Message}. Text was: {cleanedText.Substring(0, Math.Min(cleanedText.Length,50))}...");
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
