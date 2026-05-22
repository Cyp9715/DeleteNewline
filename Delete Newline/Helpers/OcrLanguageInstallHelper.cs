using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Globalization;

namespace Delete_Newline.Helpers;

public static class OcrLanguageInstallHelper
{
    private static readonly Regex LanguageTagPattern = new("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8}){1,3}$", RegexOptions.Compiled);

    private static readonly string[] SupportedOcrLanguageTags =
    [
        "ar-SA",
        "bg-BG",
        "bs-Latn-BA",
        "cs-CZ",
        "da-DK",
        "de-DE",
        "el-GR",
        "en-GB",
        "en-US",
        "es-ES",
        "es-MX",
        "et-EE",
        "fi-FI",
        "fr-CA",
        "fr-FR",
        "hr-HR",
        "hu-HU",
        "it-IT",
        "ja-JP",
        "ko-KR",
        "lt-LT",
        "lv-LV",
        "nb-NO",
        "nl-NL",
        "pl-PL",
        "pt-BR",
        "pt-PT",
        "ro-RO",
        "ru-RU",
        "sk-SK",
        "sl-SI",
        "sr-Cyrl-RS",
        "sr-Latn-RS",
        "sv-SE",
        "tr-TR",
        "uk-UA",
        "zh-CN",
        "zh-HK",
        "zh-TW"
    ];

    public static IReadOnlyList<Language> GetInstallableLanguages(IEnumerable<Language> installedLanguages)
    {
        HashSet<string> installedTags = installedLanguages
            .Select(language => language.LanguageTag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return SupportedOcrLanguageTags
            .Where(languageTag => !installedTags.Contains(languageTag))
            .Select(languageTag => new Language(languageTag))
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static string GetOcrCapabilityName(string languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag) || !LanguageTagPattern.IsMatch(languageTag))
        {
            throw new ArgumentException("Language tag must be a specific Windows language tag such as en-US or ko-KR.", nameof(languageTag));
        }

        return $"Language.OCR~~~{languageTag}~0.0.1.0";
    }

    public static async Task<int> InstallOcrLanguageCapabilityAsync(string languageTag)
    {
        string capabilityName = GetOcrCapabilityName(languageTag);
        using Process process = StartElevatedPowerShell(capabilityName);
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static Process StartElevatedPowerShell(string capabilityName)
    {
        string escapedCapabilityName = capabilityName.Replace("'", "''", StringComparison.Ordinal);
        string installCommand = $"Add-WindowsCapability -Online -Name '{escapedCapabilityName}'";
        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{installCommand}\"",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start elevated PowerShell for OCR language installation.");
    }
}
