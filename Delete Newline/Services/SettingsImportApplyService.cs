using System.Collections.ObjectModel;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services.Mcp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Delete_Newline.Services;

public sealed class SettingsImportApplyService
{
    private const string RegexCollectionSettingsKey = "RegexCollection";
    private const string OcrHotkeySettingsKey = "OCR_Hotkey";
    private const string OcrLanguageTagSettingsKey = "OCR_LanguageTag";

    private static readonly string[] AppSettingKeysToApply =
    [
        "Notification",
        "StartOnTray",
        "TopMost",
        "AppBackgroundRequestedTheme",
        "Localization",
        "StartupTask"
    ];

    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();

    private readonly IMcpSettingsRepository _settingsRepository;
    private readonly IMcpRegexConfigurationRepository _regexRepository;
    private readonly IMcpOcrConfigurationRepository _ocrRepository;

    public SettingsImportApplyService(
        IMcpSettingsRepository settingsRepository,
        IMcpRegexConfigurationRepository regexRepository,
        IMcpOcrConfigurationRepository ocrRepository)
    {
        _settingsRepository = settingsRepository;
        _regexRepository = regexRepository;
        _ocrRepository = ocrRepository;
    }

    public async Task ApplyAsync(IReadOnlyDictionary<string, JToken> importedSettings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ApplyMcpSettingsAsync(importedSettings, cancellationToken);
        await ApplyAppSettingsAsync(importedSettings, cancellationToken);
        await ApplyRegexSettingsAsync(importedSettings, cancellationToken);
        await ApplyOcrSettingsAsync(importedSettings, cancellationToken);
    }

    private async Task ApplyMcpSettingsAsync(IReadOnlyDictionary<string, JToken> importedSettings, CancellationToken cancellationToken)
    {
        bool hasImportedPort = TryReadValue(importedSettings, McpPortPolicy.PortSettingsKey, out int importedPort);
        bool hasImportedEnabled = TryReadValue(importedSettings, McpPortPolicy.EnabledSettingsKey, out bool importedEnabled);

        if (!hasImportedPort && !hasImportedEnabled)
        {
            return;
        }

        if (hasImportedPort)
        {
            McpPortPolicy.ValidatePort(importedPort);
        }

        bool wasEnabled = _settingsRepository.GetBoolean(McpPortPolicy.EnabledSettingsKey);
        if (hasImportedPort && wasEnabled)
        {
            await _settingsRepository.SetMcpEnabledAsync(false, deferStop: false, cancellationToken);
        }

        if (hasImportedPort)
        {
            await _settingsRepository.SetValueAsync(McpPortPolicy.PortSettingsKey, importedPort, cancellationToken);
        }

        if (hasImportedEnabled)
        {
            await _settingsRepository.SetMcpEnabledAsync(importedEnabled, deferStop: false, cancellationToken);
        }
        else if (hasImportedPort && wasEnabled)
        {
            await _settingsRepository.SetMcpEnabledAsync(true, deferStop: false, cancellationToken);
        }
    }

    private async Task ApplyAppSettingsAsync(IReadOnlyDictionary<string, JToken> importedSettings, CancellationToken cancellationToken)
    {
        foreach (string key in AppSettingKeysToApply)
        {
            if (!importedSettings.TryGetValue(key, out JToken? token))
            {
                continue;
            }

            object? value = token.ToObject<object?>(Serializer);
            if (value != null)
            {
                await _settingsRepository.SetValueAsync(key, value, cancellationToken);
            }
        }
    }

    private async Task ApplyRegexSettingsAsync(IReadOnlyDictionary<string, JToken> importedSettings, CancellationToken cancellationToken)
    {
        if (!importedSettings.TryGetValue(RegexCollectionSettingsKey, out JToken? token))
        {
            return;
        }

        List<RegexPageStructure> regexConfigs = token.ToObject<List<RegexPageStructure>>(Serializer) ?? [];
        regexConfigs = regexConfigs.Select(NormalizeRegexConfig).ToList();
        await _regexRepository.ReplaceAllAsync(regexConfigs, cancellationToken);
    }

    private Task ApplyOcrSettingsAsync(IReadOnlyDictionary<string, JToken> importedSettings, CancellationToken cancellationToken)
    {
        string? languageTag = TryReadValue(importedSettings, OcrLanguageTagSettingsKey, out string? importedLanguageTag)
            ? importedLanguageTag
            : null;
        HotkeyStructure? hotkey = TryReadValue(importedSettings, OcrHotkeySettingsKey, out HotkeyStructure? importedHotkey)
            ? importedHotkey
            : null;

        return languageTag != null || hotkey != null
            ? _ocrRepository.SetAsync(languageTag, hotkey, cancellationToken)
            : Task.CompletedTask;
    }

    private static bool TryReadValue<T>(IReadOnlyDictionary<string, JToken> settings, string key, out T? value)
    {
        if (settings.TryGetValue(key, out JToken? token) && token.Type != JTokenType.Null)
        {
            value = token.ToObject<T>(Serializer);
            return true;
        }

        value = default;
        return false;
    }

    private static RegexPageStructure NormalizeRegexConfig(RegexPageStructure? config)
    {
        config ??= new RegexPageStructure();
        config.HotkeyName ??= "New Hotkey";
        config.HotkeyComment ??= "Comment";
        config.InputText ??= string.Empty;
        config.Hotkey ??= new HotkeyStructure();
        config.RegexChain ??= new RegexChainStructure();

        ObservableCollection<ChainItem> normalizedItems = [];
        foreach (ChainItem? item in config.RegexChain.ChainItems ?? [])
        {
            if (item == null)
            {
                continue;
            }

            item.RegexExpression ??= string.Empty;
            item.Replace ??= string.Empty;
            normalizedItems.Add(item);
        }

        config.RegexChain.ChainItems = normalizedItems;
        return config;
    }
}
