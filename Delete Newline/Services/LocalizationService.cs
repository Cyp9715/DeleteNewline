using Microsoft.Windows.ApplicationModel.Resources;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Models;

namespace Delete_Newline.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string LocalizationTagSettingsKey = "Localization";
    private readonly SettingsService _localSettingsService;

    private readonly ResourceManager _resourceManager;
    private readonly ResourceContext _resourceContext;

    public List<LanguageItem> Languages { get; } = new();

    private LanguageItem _currentLanguageItem = new(Tag: "en-US", DisplayName: "English");

    public LocalizationService(SettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        _resourceManager = new();
        _resourceContext = _resourceManager.CreateResourceContext();
    }

    public void Initialize()
    {
        RegisterLanguageFromResource();

        string? languageTag = _localSettingsService.ReadSetting<string>(LocalizationTagSettingsKey);

        if (languageTag != null && GetLanguageItem(languageTag) is LanguageItem languageItem)
        {
            ApplyLanguage(languageItem);
        }
        else
        {
            // Detect system default language when no saved language setting exists
            var systemLanguage = DetectSystemLanguage();
            ApplyLanguage(systemLanguage);
        }
    }

    public void ApplyLanguage(LanguageItem languageItem)
    {
        if (Languages.Contains(languageItem) == true)
        {
            _currentLanguageItem = languageItem;
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = languageItem.Tag;
            _resourceContext.QualifierValues["Language"] = languageItem.Tag;
        }
    }

    public async Task SaveLanguageSettingAsync(LanguageItem languageItem)
    {
        if (Languages.Contains(languageItem) == true)
        {
            await _localSettingsService.SaveSettingAsync(LocalizationTagSettingsKey, languageItem.Tag);
        }
    }

    public async Task SetLanguage(LanguageItem languageItem)
    {
        if (Languages.Contains(languageItem) == true)
        {
            ApplyLanguage(languageItem);
            await SaveLanguageSettingAsync(languageItem);
        }
    }

    public LanguageItem GetCurrentLanguageItem() => _currentLanguageItem;

    private LanguageItem GetLanguageItem(string languageTag)
    {
        return Languages.FirstOrDefault(item => item.Tag == languageTag)
                ?? Languages.First(item => item.Tag == "en-US");
    }

    private LanguageItem DetectSystemLanguage()
    {
        // Detect system default language
        var systemLanguages = Windows.Globalization.ApplicationLanguages.Languages;
        
        foreach (var systemLang in systemLanguages)
        {
            // Find matching language among supported languages
            var matchingLanguage = Languages.FirstOrDefault(lang => lang.Tag == systemLang);
            if (matchingLanguage != null)
            {
                System.Diagnostics.Debug.WriteLine($"System language detected: {matchingLanguage.Tag} ({matchingLanguage.DisplayName})");
                return matchingLanguage;
            }
            
            // Check for partial match with language code only (e.g., ko-KR and ko)
            var languageCode = systemLang.Split('-')[0];
            var partialMatch = Languages.FirstOrDefault(lang => lang.Tag.StartsWith(languageCode + "-"));
            if (partialMatch != null)
            {
                System.Diagnostics.Debug.WriteLine($"System language partially detected: {partialMatch.Tag} ({partialMatch.DisplayName})");
                return partialMatch;
            }
        }
        
        // Return default value if system language is not found
        System.Diagnostics.Debug.WriteLine("System language not found, using default: en-US");
        return _currentLanguageItem;
    }

    private void RegisterLanguageFromResource()
    {
        ResourceMap resourceMap = _resourceManager.MainResourceMap.GetSubtree("LanguageList");

        for (uint i = 0; i < resourceMap.ResourceCount; ++i)
        {
            var resource = resourceMap.GetValueByIndex(i);
            Languages.Add(new LanguageItem(resource.Key, resource.Value.ValueAsString));
        }
    }
}