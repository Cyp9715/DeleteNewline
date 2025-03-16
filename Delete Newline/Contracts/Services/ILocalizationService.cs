using Delete_Newline.Models;

namespace Delete_Newline.Contracts.Services
{
    public interface ILocalizationService
    {
        List<LanguageItem> Languages { get; }
        void Initialize();
        LanguageItem GetCurrentLanguageItem();
        Task SetLanguage(LanguageItem languageItem);
    }
}