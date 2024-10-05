using Delete_Newline.Models;

namespace Delete_Newline.Contracts.Services
{
    public interface ILocalizationService
    {
        List<LanguageItem> Languages { get; }

        LanguageItem GetCurrentLanguageItem();
        Task InitializeAsync();
        Task SetLanguageAsync(LanguageItem languageItem);
    }
}