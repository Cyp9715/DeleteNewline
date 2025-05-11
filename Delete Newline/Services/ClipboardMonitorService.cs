using System.Diagnostics;

using Microsoft.UI.Dispatching; 
using Windows.ApplicationModel.DataTransfer;

namespace Delete_Newline.Services
{
    public class ClipboardMonitorService
    {
        private readonly IRegexService _regexService;
        private readonly DispatcherQueue? _dispatcherQueue; 


        public ClipboardMonitorService(IRegexService regexService)
        {
            _regexService = regexService ?? throw new ArgumentNullException(nameof(regexService));
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); 
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
            if (dataPackageView.Contains(StandardDataFormats.Text) is false)
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
                if (App.ActiveHotkeyIdForCopy.HasValue is true) 
                {
                    triggeredHotkeyId = App.ActiveHotkeyIdForCopy.Value;
                    App.ActiveHotkeyIdForCopy = null; // Reset
                }

                if (triggeredHotkeyId.HasValue is true)
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
                
                // UI Update: Best practice is to raise an event that the UI layer (MainWindow/ViewModel) subscribes to.
                // For direct update (if _dispatcherQueue is valid and on UI thread, or for logging):
                if (_dispatcherQueue != null)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        Debug.WriteLine($"[ClipboardMonitorService] Clipboard processed (UI thread): [{DateTime.Now:HH:mm:ss}] {cleanedText.Substring(0, Math.Min(cleanedText.Length, 100))}...");
                        // ProcessedTextAvailable?.Invoke(this, new ProcessedTextEventArgs(cleanedText, triggeredHotkeyId));
                    });
                }
                else
                {
                     Debug.WriteLine($"[ClipboardMonitorService] Clipboard processed (non-UI thread context): [{DateTime.Now:HH:mm:ss}] {cleanedText.Substring(0, Math.Min(cleanedText.Length, 100))}...");
                     // ProcessedTextAvailable?.Invoke(this, new ProcessedTextEventArgs(cleanedText, triggeredHotkeyId));
                }
            }
            catch (Exception ex)
            {
                // Catching exceptions from GetTextAsync() or _regexService processing.
                Debug.WriteLine($"[ClipboardMonitorService] Error processing clipboard content. Raw text snippet was '{rawText.Substring(0, Math.Min(rawText.Length,50))}...': {ex.Message}");
            }
        }
    }
} 