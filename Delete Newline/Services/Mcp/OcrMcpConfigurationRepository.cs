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

    public IReadOnlyList<McpOcrLanguageInfo> AvailableLanguages => _ocrViewModel.AvailableLanguageInfos;

    public IReadOnlyList<McpOcrLanguageInfo> InstallableLanguages => _ocrViewModel.InstallableLanguageInfos;

    public Task SetAsync(string? languageTag, HotkeyStructure? hotkey, CancellationToken cancellationToken)
    {
        return RunOnUiThreadAsync(() => _ocrViewModel.SetOcrSettingsAsync(languageTag, hotkey), cancellationToken);
    }

    public Task<McpOcrLanguageInstallResult> InstallAndApplyLanguageAsync(string languageTag, CancellationToken cancellationToken)
    {
        return RunOnUiThreadAsync(() => _ocrViewModel.InstallAndApplyOcrLanguageAsync(languageTag, showNotification: false), cancellationToken);
    }

    private static Task RunOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        return RunOnUiThreadAsync(async () =>
        {
            await action();
            return true;
        }, cancellationToken);
    }

    private static Task<T> RunOnUiThreadAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DispatcherQueue dispatcherQueue = App.MainWindow.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        cancellationRegistration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        bool queued = dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                T result = await action();
                completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                cancellationRegistration.Dispose();
            }
        });

        if (!queued)
        {
            cancellationRegistration.Dispose();
            completion.TrySetException(new InvalidOperationException("Failed to queue MCP OCR update on the UI thread."));
        }

        return completion.Task;
    }
}
