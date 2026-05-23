using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Services.Mcp;

public sealed class InMemoryMcpOcrConfigurationRepository : IMcpOcrConfigurationRepository
{
    private readonly List<McpOcrLanguageInfo> _availableLanguages =
    [
        new("en-US", "English (United States)"),
        new("ko-KR", "Korean (Korea)")
    ];

    private readonly List<McpOcrLanguageInfo> _installableLanguages =
    [
        new("fr-FR", "French (France)"),
        new("ja-JP", "Japanese (Japan)")
    ];

    public string? LanguageTag { get; private set; } = "en-US";

    public HotkeyStructure Hotkey { get; private set; } = new()
    {
        Modifiers = VirtualKeyModifiers.None,
        Key = VirtualKey.None
    };

    public IReadOnlyList<string> AvailableLanguageTags => _availableLanguages.Select(language => language.LanguageTag).ToArray();

    public IReadOnlyList<McpOcrLanguageInfo> AvailableLanguages => _availableLanguages.ToArray();

    public IReadOnlyList<McpOcrLanguageInfo> InstallableLanguages => _installableLanguages.ToArray();

    public int SaveCount { get; private set; }

    public int InstallCount { get; private set; }

    public Task SetAsync(string? languageTag, HotkeyStructure? hotkey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            LanguageTag = NormalizeAvailableLanguageTag(languageTag);
        }

        if (hotkey != null)
        {
            Hotkey = new HotkeyStructure
            {
                Modifiers = hotkey.Modifiers,
                Key = hotkey.Key
            };
        }

        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<McpOcrLanguageInstallResult> InstallAndApplyLanguageAsync(string languageTag, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(languageTag))
        {
            throw new ArgumentException("languageTag is required.", nameof(languageTag));
        }

        McpOcrLanguageInfo? availableLanguage = FindLanguage(_availableLanguages, languageTag);
        if (availableLanguage != null)
        {
            LanguageTag = availableLanguage.LanguageTag;
            SaveCount++;
            return Task.FromResult(CreateInstallResult(
                success: true,
                installed: false,
                applied: true,
                availableLanguage,
                exitCode: null,
                message: $"{availableLanguage.LanguageTag} was already available and is now selected for OCR."));
        }

        McpOcrLanguageInfo? installableLanguage = FindLanguage(_installableLanguages, languageTag);
        if (installableLanguage == null)
        {
            throw new ArgumentException($"OCR language '{languageTag}' is not available to install. Call get_ocr_languages and copy a tag from installableLanguageTags.");
        }

        _installableLanguages.Remove(installableLanguage);
        _availableLanguages.Add(installableLanguage);
        _availableLanguages.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.CurrentCultureIgnoreCase));
        LanguageTag = installableLanguage.LanguageTag;
        InstallCount++;
        SaveCount++;

        return Task.FromResult(CreateInstallResult(
            success: true,
            installed: true,
            applied: true,
            installableLanguage,
            exitCode: 0,
            message: $"{installableLanguage.LanguageTag} was installed and selected for OCR."));
    }

    private string NormalizeAvailableLanguageTag(string languageTag)
    {
        McpOcrLanguageInfo? language = FindLanguage(_availableLanguages, languageTag);
        if (language == null)
        {
            string supported = string.Join(", ", AvailableLanguageTags);
            throw new ArgumentException($"Unsupported OCR language '{languageTag}'. Supported language tags: {supported}.");
        }

        return language.LanguageTag;
    }

    private McpOcrLanguageInstallResult CreateInstallResult(
        bool success,
        bool installed,
        bool applied,
        McpOcrLanguageInfo language,
        int? exitCode,
        string message)
    {
        return new McpOcrLanguageInstallResult(
            success,
            installed,
            applied,
            language.LanguageTag,
            language.DisplayName,
            exitCode,
            AvailableLanguageTags,
            InstallableLanguages.Select(item => item.LanguageTag).ToArray(),
            message);
    }

    private static McpOcrLanguageInfo? FindLanguage(IEnumerable<McpOcrLanguageInfo> languages, string languageTag)
    {
        return languages.FirstOrDefault(language => language.LanguageTag.Equals(languageTag.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
