using Delete_Newline.Contracts.Services;
using Delete_Newline.Helpers;
using Delete_Newline.ViewModels;
using Delete_Newline.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace Delete_Newline.Services.Mcp;

public sealed class AppMcpSettingsRepository : IMcpSettingsRepository
{
    private const string ThemeSettingsKey = "AppBackgroundRequestedTheme";
    private const string LocalizationSettingsKey = "Localization";
    private const string NotificationSettingsKey = "Notification";
    private const string StartupTaskSettingsKey = "StartupTask";

    private readonly McpAccessService _mcpAccessService;
    private readonly SettingsFileService _settingsFileService;
    private readonly NotificationService _notificationService;
    private readonly TopMostService _topMostService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;

    public AppMcpSettingsRepository(
        McpAccessService mcpAccessService,
        SettingsFileService settingsFileService,
        NotificationService notificationService,
        TopMostService topMostService,
        IThemeSelectorService themeSelectorService,
        ILocalizationService localizationService)
    {
        _mcpAccessService = mcpAccessService;
        _settingsFileService = settingsFileService;
        _notificationService = notificationService;
        _topMostService = topMostService;
        _themeSelectorService = themeSelectorService;
        _localizationService = localizationService;
    }

    public bool GetBoolean(string key)
    {
        object? value = GetValue(key);
        return value is bool boolValue && boolValue;
    }

    public object? GetValue(string key)
    {
        return NormalizeKey(key) switch
        {
            McpAccessService.McpEnabledSettingsKey => _mcpAccessService.GetEnableMcp(),
            McpAccessService.McpPortSettingsKey => _mcpAccessService.GetPort(),
            NotificationSettingsKey => _notificationService.GetEnableNotification(),
            SettingsViewModel.DefaultStartOnTray => _settingsFileService.ReadSetting<bool>(SettingsViewModel.DefaultStartOnTray),
            TopMostService.DefaultTopMostKey => _topMostService.EnableTopMost,
            ThemeSettingsKey => _themeSelectorService.Theme.ToString(),
            LocalizationSettingsKey => _localizationService.GetCurrentLanguageItem().Tag,
            _ => null
        };
    }

    public IReadOnlyDictionary<string, object?> GetAllSettings()
    {
        return new Dictionary<string, object?>
        {
            [McpAccessService.McpEnabledSettingsKey] = _mcpAccessService.GetEnableMcp(),
            [McpAccessService.McpPortSettingsKey] = _mcpAccessService.GetPort(),
            ["McpEndpoint"] = _mcpAccessService.EndpointUrl,
            [NotificationSettingsKey] = _notificationService.GetEnableNotification(),
            [SettingsViewModel.DefaultStartOnTray] = _settingsFileService.ReadSetting<bool>(SettingsViewModel.DefaultStartOnTray),
            [TopMostService.DefaultTopMostKey] = _topMostService.EnableTopMost,
            ["Theme"] = _themeSelectorService.Theme.ToString(),
            ["Language"] = _localizationService.GetCurrentLanguageItem().Tag,
            ["AvailableLanguages"] = _localizationService.Languages.Select(language => new
            {
                tag = language.Tag,
                displayName = language.DisplayName
            }).ToArray(),
            ["StartupTaskSupported"] = RuntimeHelper.IsMSIX
        };
    }

    public Task SetBooleanAsync(string key, bool value, CancellationToken cancellationToken)
    {
        return NormalizeKey(key) == McpAccessService.McpEnabledSettingsKey
            ? SetMcpEnabledAsync(value, deferStop: false, cancellationToken)
            : SetValueAsync(key, value, cancellationToken);
    }

    public async Task SetMcpEnabledAsync(bool value, bool deferStop, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _mcpAccessService.SetEnableMcpAsync(value, deferStop: deferStop);
    }

    public async Task SetValueAsync(string key, object value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (NormalizeKey(key))
        {
            case McpAccessService.McpEnabledSettingsKey:
                await SetMcpEnabledAsync(RequireBool(key, value), deferStop: false, cancellationToken);
                break;

            case McpAccessService.McpPortSettingsKey:
                await _mcpAccessService.SetPortAsync(McpPortPolicy.RequirePortSettingValue(key, value));
                break;

            case NotificationSettingsKey:
                await _notificationService.SetEnableNotificationAsync(RequireBool(key, value));
                break;

            case SettingsViewModel.DefaultStartOnTray:
                bool startOnTray = RequireBool(key, value);
                await _settingsFileService.SaveSettingAsync(SettingsViewModel.DefaultStartOnTray, startOnTray);
                await RunOnUiThreadAsync(() => TryUpdateSettingsViewModel(vm => vm.EnableStartOnTray = startOnTray));
                break;

            case TopMostService.DefaultTopMostKey:
                await SetTopMostAsync(RequireBool(key, value));
                break;

            case ThemeSettingsKey:
                await SetThemeAsync(RequireString(key, value));
                break;

            case LocalizationSettingsKey:
                await SetLanguageAsync(RequireString(key, value));
                break;

            case StartupTaskSettingsKey:
                await SetStartupTaskAsync(RequireBool(key, value));
                break;

            default:
                await _settingsFileService.SaveSettingAsync(key, value);
                break;
        }
    }

    private async Task SetTopMostAsync(bool value)
    {
        await RunOnUiThreadAsync(() =>
        {
            _topMostService.SetWindowTopMost(App.MainWindow, value);
            TryUpdateSettingsViewModel(vm => vm.EnableTopMost = value);
        });
        await _topMostService.SaveTopMostSettingAsync();
    }

    private async Task SetThemeAsync(string themeValue)
    {
        if (!Enum.TryParse(themeValue, ignoreCase: true, out ElementTheme theme))
        {
            throw new ArgumentException($"Unsupported theme '{themeValue}'. Use Default, Light, or Dark.");
        }

        await RunOnUiThreadAsync(async () =>
        {
            await _themeSelectorService.SetThemeAsync(theme);
            TryUpdateSettingsViewModel(vm => vm.SelectedTheme = theme.ToString());
        });
    }

    private async Task SetLanguageAsync(string languageValue)
    {
        LanguageItem? languageItem = _localizationService.Languages.FirstOrDefault(language =>
            language.Tag.Equals(languageValue, StringComparison.OrdinalIgnoreCase) ||
            language.DisplayName.Equals(languageValue, StringComparison.OrdinalIgnoreCase));

        if (languageItem == null)
        {
            string supported = string.Join(", ", _localizationService.Languages.Select(language => language.Tag));
            throw new ArgumentException($"Unsupported language '{languageValue}'. Supported language tags: {supported}.");
        }

        await RunOnUiThreadAsync(async () =>
        {
            await _localizationService.SetLanguage(languageItem);
            TryUpdateSettingsViewModel(vm => vm.SelectedLanguage = languageItem);

            if (App.MainWindow.Content is ShellPage shellPage)
            {
                shellPage.RefreshLocalizedTexts();
                if (shellPage.ViewModel.NavigationService.Frame?.Content is SettingsPage)
                {
                    shellPage.ViewModel.NavigationService.NavigateTo(typeof(SettingsViewModel).FullName!, Guid.NewGuid().ToString(), clearNavigation: true);
                }
            }
        });
    }

    private static async Task SetStartupTaskAsync(bool value)
    {
        if (!RuntimeHelper.IsMSIX)
        {
            throw new InvalidOperationException("StartupTask can only be changed when Delete Newline is running as an MSIX package.");
        }

        StartupTask startupTask = await StartupTask.GetAsync("DeleteNewlineStartupTask");
        if (value)
        {
            StartupTaskState newState = await startupTask.RequestEnableAsync();
            if (newState != StartupTaskState.Enabled && newState != StartupTaskState.EnabledByPolicy)
            {
                throw new InvalidOperationException($"StartupTask was not enabled. Current state: {newState}.");
            }
        }
        else
        {
            startupTask.Disable();
        }

        await RunOnUiThreadAsync(() => TryUpdateSettingsViewModel(vm => vm.EnableStartupTask = value));
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
        return RunOnUiThreadAsync(() =>
        {
            action();
            return Task.CompletedTask;
        });
    }

    private static Task RunOnUiThreadAsync(Func<Task> action)
    {
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
            completion.TrySetException(new InvalidOperationException("Failed to queue MCP setting update on the UI thread."));
        }

        return completion.Task;
    }

    private static void TryUpdateSettingsViewModel(Action<SettingsViewModel> update)
    {
        try
        {
            update(App.GetService<SettingsViewModel>());
        }
        catch
        {
            // Settings UI may not be constructed yet; the saved value will be picked up when it opens.
        }
    }

    private static string NormalizeKey(string key)
    {
        if (key.Equals(McpAccessService.McpEnabledSettingsKey, StringComparison.OrdinalIgnoreCase))
        {
            return McpAccessService.McpEnabledSettingsKey;
        }

        if (key.Equals(McpAccessService.McpPortSettingsKey, StringComparison.OrdinalIgnoreCase) || key.Equals("Port", StringComparison.OrdinalIgnoreCase))
        {
            return McpAccessService.McpPortSettingsKey;
        }

        if (key.Equals("notification", StringComparison.OrdinalIgnoreCase) || key.Equals("EnableNotification", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationSettingsKey;
        }

        if (key.Equals(SettingsViewModel.DefaultStartOnTray, StringComparison.OrdinalIgnoreCase) || key.Equals("EnableStartOnTray", StringComparison.OrdinalIgnoreCase))
        {
            return SettingsViewModel.DefaultStartOnTray;
        }

        if (key.Equals(TopMostService.DefaultTopMostKey, StringComparison.OrdinalIgnoreCase) || key.Equals("EnableTopMost", StringComparison.OrdinalIgnoreCase))
        {
            return TopMostService.DefaultTopMostKey;
        }

        if (key.Equals("Theme", StringComparison.OrdinalIgnoreCase) || key.Equals(ThemeSettingsKey, StringComparison.OrdinalIgnoreCase))
        {
            return ThemeSettingsKey;
        }

        if (key.Equals("Language", StringComparison.OrdinalIgnoreCase) || key.Equals("LanguageTag", StringComparison.OrdinalIgnoreCase) || key.Equals(LocalizationSettingsKey, StringComparison.OrdinalIgnoreCase))
        {
            return LocalizationSettingsKey;
        }

        if (key.Equals(StartupTaskSettingsKey, StringComparison.OrdinalIgnoreCase) || key.Equals("EnableStartupTask", StringComparison.OrdinalIgnoreCase))
        {
            return StartupTaskSettingsKey;
        }

        return key;
    }

    private static bool RequireBool(string key, object value)
    {
        return value switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out bool parsed) => parsed,
            _ => throw new ArgumentException($"Setting '{key}' requires a boolean value.")
        };
    }

    private static string RequireString(string key, object value)
    {
        return value as string ?? throw new ArgumentException($"Setting '{key}' requires a string value.");
    }
}
