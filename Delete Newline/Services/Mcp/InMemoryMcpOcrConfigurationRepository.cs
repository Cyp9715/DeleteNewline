using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Services.Mcp;

public sealed class InMemoryMcpOcrConfigurationRepository : IMcpOcrConfigurationRepository
{
    public string? LanguageTag { get; private set; } = "en-US";

    public HotkeyStructure Hotkey { get; private set; } = new()
    {
        Modifiers = VirtualKeyModifiers.None,
        Key = VirtualKey.None
    };

    public IReadOnlyList<string> AvailableLanguageTags { get; } = ["en-US", "ko-KR"];

    public int SaveCount { get; private set; }

    public Task SetAsync(string? languageTag, HotkeyStructure? hotkey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            LanguageTag = languageTag;
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
}
