using System.Diagnostics;

using Windows.ApplicationModel.DataTransfer;

namespace Delete_Newline.Services;

public class ClipboardMonitorService
{
    private readonly RegexService _regexService;

    public ClipboardMonitorService(RegexService regexService)
    {
        _regexService = regexService ?? throw new ArgumentNullException(nameof(regexService));
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
                cleanedText = _regexService.ProcessText(rawText, triggeredHotkeyId.Value);
            }
            else
            {
                Debug.WriteLine("[ClipboardMonitorService] General clipboard change. Applying default rules.");
                cleanedText = _regexService.ProcessText(rawText);
            }

            if (rawText != cleanedText)
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(cleanedText);
                
                try
                {
                    Clipboard.SetContent(dataPackage);
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
