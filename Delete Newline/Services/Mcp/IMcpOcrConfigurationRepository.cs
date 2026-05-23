using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services.Mcp;

public sealed record McpOcrLanguageInfo(string LanguageTag, string DisplayName);

public sealed record McpOcrLanguageInstallResult(
    bool Success,
    bool Installed,
    bool Applied,
    string LanguageTag,
    string DisplayName,
    int? ExitCode,
    IReadOnlyList<string> AvailableLanguageTags,
    IReadOnlyList<string> InstallableLanguageTags,
    string Message);

public interface IMcpOcrConfigurationRepository
{
    string? LanguageTag { get; }

    HotkeyStructure Hotkey { get; }

    IReadOnlyList<string> AvailableLanguageTags { get; }

    IReadOnlyList<McpOcrLanguageInfo> AvailableLanguages { get; }

    IReadOnlyList<McpOcrLanguageInfo> InstallableLanguages { get; }

    Task SetAsync(string? languageTag, HotkeyStructure? hotkey, CancellationToken cancellationToken);

    Task<McpOcrLanguageInstallResult> InstallAndApplyLanguageAsync(string languageTag, CancellationToken cancellationToken);
}
