using Delete_Newline.Contracts.Structures;
using Delete_Newline.ViewModels;
using Microsoft.UI.Dispatching;

namespace Delete_Newline.Services.Mcp;

public sealed class OcrMcpConfigurationRepository : IMcpOcrConfigurationRepository
{
    private readonly OCRViewModel _ocrViewModel;

    public OcrMcpConfigurationRepository(OCRViewModel ocrViewModel)
    {
        _ocrViewModel = ocrViewModel;
    }

    public string? LanguageTag => _ocrViewModel.CurrentLanguageTag;

    public HotkeyStructure Hotkey => _ocrViewModel.CurrentHotkey;

    public IReadOnlyList<string> AvailableLanguageTags => _ocrViewModel.AvailableLanguageTags;

    public Task SetAsync(string? languageTag, HotkeyStructure? hotkey, CancellationToken cancellationToken)
    {
        return RunOnUiThreadAsync(() => _ocrViewModel.SetOcrSettingsAsync(languageTag, hotkey), cancellationToken);
    }

    private static Task RunOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DispatcherQueue dispatcherQueue = App.MainWindow.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool queued = dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await action();
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        if (!queued)
        {
            completion.TrySetException(new InvalidOperationException("Failed to queue MCP OCR update on the UI thread."));
        }

        cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        return completion.Task;
    }
}
