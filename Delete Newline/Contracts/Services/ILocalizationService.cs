namespace Delete_Newline.Contracts.Services;

public record LanguageItem(string Tag, string DisplayName);

public interface ILocalizationService
{
    List<LanguageItem> Languages { get; }
    void Initialize();
    LanguageItem GetCurrentLanguageItem();
    Task SetLanguage(LanguageItem languageItem);
}