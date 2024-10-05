using Microsoft.Windows.ApplicationModel.Resources;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Models;

namespace Delete_Newline.Services;

public class LocalizationService : ILocalizationService
{
    private const string LocalizationTagSettingsKey = "AppBackgroundRequestedLocalization";
    private readonly ILocalSettingsService _localSettingsService;

    private readonly ResourceManager _resourceManager;
    private readonly ResourceContext _resourceContext;

    public List<LanguageItem> Languages { get; } = new();

    private LanguageItem _currentLanguageItem = new(Tag: "en-US", DisplayName: "English");

    public LocalizationService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        _resourceManager = new();
        _resourceContext = _resourceManager.CreateResourceContext();
    }

    public async Task InitializeAsync()
    {
        RegisterLanguageFromResource();

        string languageTag = await GetLanguageTagFromSettingsAsync();

        if (languageTag is not null && GetLanguageItem(languageTag) is LanguageItem languageItem)
        {
            await SetLanguageAsync(languageItem);
        }
        else
        {
            await SetLanguageAsync(_currentLanguageItem);
        }
    }

    public async Task SetLanguageAsync(LanguageItem languageItem)
    {
        if (Languages.Contains(languageItem) is true)
        {
            _currentLanguageItem = languageItem;

            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = languageItem.Tag;
            _resourceContext.QualifierValues["Language"] = languageItem.Tag;

            await _localSettingsService.SaveSettingAsync(LocalizationTagSettingsKey, languageItem.Tag);
        }
    }

    public LanguageItem GetCurrentLanguageItem() => _currentLanguageItem;

    private LanguageItem GetLanguageItem(string languageTag)
    {
        return Languages.FirstOrDefault(item => item.Tag == languageTag)
                ?? Languages.First(item => item.Tag == "en-US");
    }

    private async Task<string> GetLanguageTagFromSettingsAsync()
    {
        return await _localSettingsService.ReadSettingAsync<string>(LocalizationTagSettingsKey);
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