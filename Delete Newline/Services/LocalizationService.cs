using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;
using Delete_Newline.Contracts.Services;

namespace Delete_Newline.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string DefaultLanguageTag = "en-US";
    private const string LocalizationTagSettingsKey = "Localization";
    private readonly SettingsFileService _localSettingsService;

    private readonly ResourceManager _resourceManager;
    private readonly ResourceContext _resourceContext;

    public List<LanguageItem> Languages { get; } = new();

    private LanguageItem _currentLanguageItem = new(Tag: DefaultLanguageTag, DisplayName: "English");

    public LocalizationService(SettingsFileService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        _resourceManager = new();
        _resourceContext = _resourceManager.CreateResourceContext();
    }

    public void Initialize()
    {
        RegisterLanguageFromResource();

        string? languageTag = _localSettingsService.ReadSetting<string>(LocalizationTagSettingsKey);

        ApplyLanguage(languageTag != null ? GetLanguageItem(languageTag) : DetectSystemLanguage());
    }

    public void ApplyLanguage(LanguageItem languageItem)
    {
        if (Languages.Contains(languageItem) == true)
        {
            _currentLanguageItem = languageItem;
            // Keep both language override APIs aligned so XAML/resource lookups resolve consistently.
            ApplicationLanguages.PrimaryLanguageOverride = languageItem.Tag;
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
        return Languages.FirstOrDefault(item => string.Equals(item.Tag, languageTag, StringComparison.OrdinalIgnoreCase))
                ?? Languages.First(item => item.Tag == DefaultLanguageTag);
    }

    private LanguageItem DetectSystemLanguage()
    {
        // Read Windows preferences rather than the app's potentially saved language override.
        foreach (string systemLanguage in Windows.System.UserProfile.GlobalizationPreferences.Languages)
        {
            LanguageItem? exactMatch = Languages.FirstOrDefault(item =>
                string.Equals(item.Tag, systemLanguage, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null) return exactMatch;

            string languageCode = systemLanguage.Split('-')[0];
            LanguageItem? languageMatch = Languages.FirstOrDefault(item =>
                item.Tag.StartsWith(languageCode + "-", StringComparison.OrdinalIgnoreCase));
            if (languageMatch != null) return languageMatch;
        }

        return GetLanguageItem(DefaultLanguageTag);
    }

    private void RegisterLanguageFromResource()
    {
        Languages.Clear();
        ResourceMap resourceMap = _resourceManager.MainResourceMap.GetSubtree("LanguageList");

        for (uint i = 0; i < resourceMap.ResourceCount; ++i)
        {
            var resource = resourceMap.GetValueByIndex(i);
            Languages.Add(new LanguageItem(resource.Key, resource.Value.ValueAsString));
        }
    }
}
