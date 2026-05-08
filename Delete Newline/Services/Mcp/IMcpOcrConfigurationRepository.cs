using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services.Mcp;

public interface IMcpOcrConfigurationRepository
{
    string? LanguageTag { get; }

    HotkeyStructure Hotkey { get; }

    IReadOnlyList<string> AvailableLanguageTags { get; }

    Task SetAsync(string? languageTag, HotkeyStructure? hotkey, CancellationToken cancellationToken);
}
