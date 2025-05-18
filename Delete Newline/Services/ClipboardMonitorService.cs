using System.Diagnostics;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace Delete_Newline.Services;

public class ClipboardMonitorService
{
    private readonly RegexService _regexService;
    private readonly NotificationService _notificationService;
    private readonly HotkeyCollectSaveService _hotkeyCollectSaveService;

    public ClipboardMonitorService(RegexService regexService, NotificationService notificationService, HotkeyCollectSaveService hotkeyCollectSaveService)
    {
        _regexService = regexService ?? throw new ArgumentNullException(nameof(regexService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _hotkeyCollectSaveService = hotkeyCollectSaveService ?? throw new ArgumentNullException(nameof(hotkeyCollectSaveService));
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

    private async void OnClipboardContentChangedInternal(object? sender, object e)
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
            rawText = await dataPackageView.GetTextAsync();
            string cleanedText;

            // Critical section for ActiveHotkeyIdForCopy: Read and immediately reset.
            int? triggeredHotkeyId = null;

            // Assuming App.ActiveHotkeyIdForCopy is a static property in the App class.
            if (App.ActiveHotkeyIdForCopy.HasValue == true) 
            {
                triggeredHotkeyId = App.ActiveHotkeyIdForCopy.Value;
                App.ActiveHotkeyIdForCopy = null; // Reset
            }

            if (triggeredHotkeyId.HasValue == true)
            {
                Debug.WriteLine($"[ClipboardMonitorService] Clipboard change by Hotkey ID: {triggeredHotkeyId.Value}. Applying specific rules.");
                cleanedText = _regexService.ApplyHotkeyRules(rawText, triggeredHotkeyId.Value);
            }
            else
            {
                Debug.WriteLine("[ClipboardMonitorService] General clipboard change. Applying default rules.");
                cleanedText = rawText;
            }

            if (rawText != cleanedText)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(cleanedText);
                
                try
                {
                    Clipboard.SetContent(dataPackage);

                    if (triggeredHotkeyId.HasValue)
                    {
                        if (_notificationService.GetEnableNotification())
                        {
                            HotkeyPageStructure? hotkeyStructure = _hotkeyCollectSaveService.GetHotkeyStructureById(triggeredHotkeyId.Value);
                            string titleKey = "Notification_TextModified_Title";
                            string messageKey = "Notification_HotkeyActionCompleted_Message";
                            object[]? messageArgs = null;

                            if (hotkeyStructure != null)
                            {
                                string displayHotkey = HotkeyDisplayHelper.GetDisplayText(hotkeyStructure);
                                titleKey = string.IsNullOrWhiteSpace(hotkeyStructure.HotkeyName) ? "Notification_HotkeyActivated_Title" : hotkeyStructure.HotkeyName; 
                                // If HotkeyName is a literal and not a key, we can't directly use it as a titleKey.
                                // For simplicity here, if HotkeyName is not empty, we assume it's a literal title or it should be made a resource key itself.
                                // A more robust solution would be to ensure HotkeyName can also be a resource key if needed for localization.
                                // Or, have a generic title key and pass HotkeyName as an argument if it's always a literal.
                                // Let's assume for now if HotkeyName is not empty, it's used as a literal title, otherwise use a default key.
                                if (string.IsNullOrWhiteSpace(hotkeyStructure.HotkeyName))
                                {
                                    titleKey = "Notification_HotkeyActivated_Title";
                                }
                                else
                                {
                                     // This case needs careful consideration: is hotkeyStructure.HotkeyName a literal string or a resource key?
                                     // For now, treating it as a literal title to be passed directly if not a key.
                                     // Or, create a dynamic title like "Hotkey '{0}' Activated" where {0} is HotkeyName if it's always literal.
                                     // Let's go with a general key and pass name as arg for message.
                                    titleKey = "Notification_HotkeyActivated_Title"; // Generic title
                                }

                                messageKey = "Notification_ActionForHotkeyCompleted_Message";
                                messageArgs = new object[] { displayHotkey };
                            }
                            
                            // If titleKey is actually a literal string from hotkeyStructure.HotkeyName and not a resource key,
                            // this call will fail to find a resource. This part needs careful handling based on whether
                            // HotkeyName is intended to be localizable or is always a user-defined literal.
                            // Assuming titleKey will resolve to a valid resource key or we use a generic one.
                            _notificationService.ShowSystemNotification(titleKey, messageKey, force: false, addTag: true, messageArgs: messageArgs);
                        }
                    }
                }
                catch (Exception exSetContent)
                {
                    // This can happen if the clipboard is busy or unavailable.
                    Debug.WriteLine($"[ClipboardMonitorService] Error setting clipboard content: {exSetContent.Message}. Text was: {cleanedText.Substring(0, Math.Min(cleanedText.Length,50))}...");
                }
            }
        }
        catch (Exception ex)
        {
            // Catching exceptions from GetTextAsync() or _regexService processing.
            Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content. Raw text snippet was '{rawText.Substring(0, Math.Min(rawText.Length,50))}...': {ex.Message}");
        }
    }
}
