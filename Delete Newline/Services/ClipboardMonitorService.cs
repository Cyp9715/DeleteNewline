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

                if (!string.IsNullOrEmpty(afterText))
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
            else
            {
                Debug.WriteLine("[ClipboardMonitorService] General clipboard change. No regex processing.");
                afterText = beforeText;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content. Raw text snippet was '{beforeText.Substring(0, Math.Min(beforeText.Length,50))}...': {ex.Message}");
        }
    }

    private void UpdateClipboardContent(string text, int? triggeredHotkeyId, bool wasModified)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        
        try
        {
            Clipboard.SetContent(dataPackage);

            if (triggeredHotkeyId.HasValue && _notificationService.GetEnableNotification())
            {
                if (wasModified)
                {
                    ShowModifiedNotification(triggeredHotkeyId.Value);
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
            Debug.WriteLine($"[ClipboardMonitorService] Error setting clipboard content: {exSetContent.Message}. Text was: {text.Substring(0, Math.Min(text.Length,50))}...");
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
