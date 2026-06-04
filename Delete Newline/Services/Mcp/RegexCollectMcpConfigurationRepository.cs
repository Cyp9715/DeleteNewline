using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers.Hotkeys;
using Microsoft.UI.Dispatching;
using Windows.System;

namespace Delete_Newline.Services.Mcp;

public sealed class RegexCollectMcpConfigurationRepository : IMcpRegexConfigurationRepository
{
    private readonly RegexCollectSaveService _regexCollectSaveService;

    public RegexCollectMcpConfigurationRepository(RegexCollectSaveService regexCollectSaveService)
    {
        _regexCollectSaveService = regexCollectSaveService;
    }

    public IReadOnlyList<RegexPageStructure> RegexConfigs => _regexCollectSaveService.RegexConfigs.ToList();

    public async Task UpsertAsync(int? index, RegexPageStructure config, CancellationToken cancellationToken)
    {
        await RunOnUiThreadAsync(() =>
        {
            if (index.HasValue && index.Value >= 0 && index.Value < _regexCollectSaveService.RegexConfigs.Count)
            {
                RegexPageStructure existing = _regexCollectSaveService.RegexConfigs[index.Value];
                ApplyToExistingConfig(existing, config);
            }
            else
            {
                RegisterHotkeyIfNeeded(config);
                _regexCollectSaveService.AddRegexConfig(config);
            }
        }, cancellationToken);

        await _regexCollectSaveService.SaveSettingsAsync();
    }

    public async Task ReplaceAllAsync(IReadOnlyList<RegexPageStructure> configs, CancellationToken cancellationToken)
    {
        await RunOnUiThreadAsync(() => _regexCollectSaveService.ReplaceRegexConfigs(configs), cancellationToken);
        await _regexCollectSaveService.SaveSettingsAsync();
    }

    public async Task<bool> DeleteAsync(int index, CancellationToken cancellationToken)
    {
        bool removed = false;
        await RunOnUiThreadAsync(() =>
        {
            if (index >= 0 && index < _regexCollectSaveService.RegexConfigs.Count)
            {
                RegexPageStructure existing = _regexCollectSaveService.RegexConfigs[index];
                _regexCollectSaveService.RemoveRegexConfig(existing);
                removed = true;
            }
        }, cancellationToken);

        if (removed)
        {
            await _regexCollectSaveService.SaveSettingsAsync();
        }

        return removed;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _regexCollectSaveService.SaveSettingsAsync();
    }

    private static void ApplyToExistingConfig(RegexPageStructure existing, RegexPageStructure source)
    {
        var oldHotkey = (existing.Hotkey.Modifiers, existing.Hotkey.Key);
        var newHotkey = (source.Hotkey.Modifiers, source.Hotkey.Key);
        bool hotkeyChanged = oldHotkey != newHotkey;
        bool newHotkeyRegistered = false;

        if (hotkeyChanged && oldHotkey.Key != VirtualKey.None)
        {
            HotkeyRegister.UnregisterHotkey(oldHotkey, HotkeyType.Regex);
        }

        try
        {
            if (hotkeyChanged)
            {
                newHotkeyRegistered = RegisterHotkeyIfNeeded(source);
            }

            existing.HotkeyName = source.HotkeyName;
            existing.HotkeyComment = source.HotkeyComment;
            existing.InputText = source.InputText;
            existing.Hotkey.Modifiers = source.Hotkey.Modifiers;
            existing.Hotkey.Key = source.Hotkey.Key;

            existing.RegexChain.ChainItems.Clear();
            foreach (ChainItem item in source.RegexChain.ChainItems)
            {
                existing.RegexChain.ChainItems.Add(new ChainItem
                {
                    RegexExpression = item.RegexExpression,
                    Replace = item.Replace
                });
            }

            existing.IsRegistrationFailed = false;
        }
        catch
        {
            if (newHotkeyRegistered)
            {
                HotkeyRegister.UnregisterHotkey(newHotkey, HotkeyType.Regex);
            }

            if (hotkeyChanged && oldHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey(oldHotkey, HotkeyType.Regex);
            }

            throw;
        }
    }

    private static bool RegisterHotkeyIfNeeded(RegexPageStructure config)
    {
        var hotkey = (config.Hotkey.Modifiers, config.Hotkey.Key);
        if (hotkey.Modifiers == VirtualKeyModifiers.None || hotkey.Key == VirtualKey.None)
        {
            return false;
        }

        if (HotkeyRegister.IsHotkeyRegisteredGlobally(hotkey))
        {
            throw new InvalidOperationException($"Hotkey '{config.Hotkey}' is already registered.");
        }

        if (HotkeyRegister.RegisterHotkey(hotkey, HotkeyType.Regex) == false)
        {
            config.IsRegistrationFailed = true;
            throw new InvalidOperationException($"Failed to register regex hotkey '{config.Hotkey}'.");
        }

        config.IsRegistrationFailed = false;
        return true;
    }

    private static Task RunOnUiThreadAsync(Action action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = App.MainWindow.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        cancellationRegistration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        _ = completion.Task.ContinueWith(
            _ => cancellationRegistration.Dispose(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        bool queued = dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                action();
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        if (!queued)
        {
            completion.TrySetException(new InvalidOperationException("Failed to queue MCP regex update on the UI thread."));
        }

        return completion.Task;
    }
}
