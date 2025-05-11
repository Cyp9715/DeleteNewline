using Delete_Newline.Services; // For IRegexService, assuming App.ActiveHotkeyIdForCopy is accessible via App class
using Microsoft.UI.Dispatching; 
using System;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;

namespace Delete_Newline.Services // Namespace should match your project structure
{
    public class ClipboardMonitorService
    {
        private readonly IRegexService _regexService;
        private readonly DispatcherQueue? _dispatcherQueue; // Nullable if not always on UI thread

        // It's better to get ActiveHotkeyIdForCopy directly from App class if it's static there.
        // Passing App instance or specific properties around can complicate things.

        public ClipboardMonitorService(IRegexService regexService)
        {
            _regexService = regexService ?? throw new ArgumentNullException(nameof(regexService));
            
            // Attempt to get DispatcherQueue for the current thread.
            // This is crucial if this service is created on the main UI thread.
            // If created on a background thread and UI updates are needed from here,
            // the DispatcherQueue from App.MainWindow.DispatcherQueue should be injected.
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); 
            if (_dispatcherQueue == null)
            {
                Debug.WriteLine("[ClipboardMonitorService WARNING] DispatcherQueue.GetForCurrentThread() returned null. UI updates from here might fail or need explicit dispatching to main thread.");
            }
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
                // Potentially re-throw or handle more gracefully depending on requirements
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
                return;

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
                    App.ActiveHotkeyIdForCopy = null; // Reset it
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

        // Optional: Define EventArgs and Event for UI updates
        // public class ProcessedTextEventArgs : EventArgs
        // {
        // public string ProcessedText { get; }
        // public int? HotkeyId { get; }
        // public ProcessedTextEventArgs(string text, int? hotkeyId) { ProcessedText = text; HotkeyId = hotkeyId; }
        // }
        // public event EventHandler<ProcessedTextEventArgs> ProcessedTextAvailable;
    }
} 