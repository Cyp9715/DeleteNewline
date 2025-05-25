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

            Debug.WriteLine("[ClipboardMonitorService] General clipboard change. Applying default rules.");
            cleanedText = rawText;

            if (rawText != cleanedText && !string.IsNullOrEmpty(cleanedText))
            {
                UpdateClipboardContent(cleanedText);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content. Raw text snippet was '{rawText.Substring(0, Math.Min(rawText.Length,50))}...': {ex.Message}");
        }
    }

    private void UpdateClipboardContent(string cleanedText)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(cleanedText);
        
        try
        {
            Clipboard.SetContent(dataPackage);
        }
        catch (Exception exSetContent)
        {
            Debug.WriteLine($"[ClipboardMonitorService] Error setting clipboard content: {exSetContent.Message}. Text was: {cleanedText.Substring(0, Math.Min(cleanedText.Length,50))}...");
        }
    }
}
