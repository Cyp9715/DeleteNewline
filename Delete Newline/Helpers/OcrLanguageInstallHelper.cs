using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.Globalization;

namespace Delete_Newline.Helpers;

public sealed class OcrLanguageManagementItem
{
    public OcrLanguageManagementItem(string languageTag, Language language, bool isInstalled, bool isCurrent, string installedStatusText)
    {
        LanguageTag = languageTag;
        Language = language;
        IsInstalled = isInstalled;
        IsCurrent = isCurrent;
        InstalledStatusText = installedStatusText;
    }

    public string LanguageTag { get; }

    public Language Language { get; }

    public string DisplayName => Language.DisplayName;

    public bool IsInstalled { get; }

    public bool IsCurrent { get; }

    public string InstalledStatusText { get; }

    public string DisplayText => IsInstalled && !string.IsNullOrWhiteSpace(InstalledStatusText)
        ? $"{DisplayName} {InstalledStatusText}"
        : DisplayName;
}

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

    public static IReadOnlyList<OcrLanguageManagementItem> GetManageableLanguages(
        IEnumerable<Language> installedLanguages,
        string? currentLanguageTag,
        string installedStatusText)
    {
        Language[] installedLanguageArray = installedLanguages.ToArray();
        Dictionary<string, Language> installedLanguagesByTag = installedLanguageArray
            .GroupBy(language => language.LanguageTag, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        static bool TagsMatch(string first, string? second)
        {
            if (string.IsNullOrWhiteSpace(second))
            {
                return false;
            }

            return first.Equals(second, StringComparison.OrdinalIgnoreCase)
                || new Language(first).LanguageTag.Equals(second, StringComparison.OrdinalIgnoreCase);
        }

        IEnumerable<string> unsupportedInstalledTags = installedLanguageArray
            .Select(language => language.LanguageTag)
            .Where(installedTag => !SupportedOcrLanguageTags.Any(supportedTag => TagsMatch(supportedTag, installedTag)));

        return SupportedOcrLanguageTags
            .Concat(unsupportedInstalledTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(languageTag =>
            {
                Language displayLanguage = new(languageTag);
                bool isInstalled = installedLanguagesByTag.TryGetValue(languageTag, out Language? installedLanguage)
                    || installedLanguagesByTag.TryGetValue(displayLanguage.LanguageTag, out installedLanguage);
                Language language = installedLanguage ?? displayLanguage;
                bool isCurrent = isInstalled
                    && (TagsMatch(languageTag, currentLanguageTag) || TagsMatch(language.LanguageTag, currentLanguageTag));

                return new OcrLanguageManagementItem(languageTag, language, isInstalled, isCurrent, installedStatusText);
            })
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
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
        using Process process = StartElevatedPowerShell($"Add-WindowsCapability -Online -Name '{EscapePowerShellSingleQuotedString(capabilityName)}'");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    public static async Task<int> RemoveOcrLanguageCapabilityAsync(string languageTag)
    {
        string capabilityName = GetOcrCapabilityName(languageTag);
        using Process process = StartElevatedPowerShell($"Remove-WindowsCapability -Online -Name '{EscapePowerShellSingleQuotedString(capabilityName)}'");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static Process StartElevatedPowerShell(string command)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
            Verb = "runas",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start elevated PowerShell for OCR language management.");
    }
}
