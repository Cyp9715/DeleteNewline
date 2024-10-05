using System.Reflection;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Helpers;

using Delete_Newline.Models;

namespace Delete_Newline.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private LanguageItem _selectedLanguage;

    [ObservableProperty]
    private bool _isLocalizationChanged;

    [ObservableProperty]
    private string _versionDescription;

    [ObservableProperty]
    private List<LanguageItem> _availableLanguages;

    public SettingsViewModel(ILocalizationService localizationService, IThemeSelectorService themeSelectorService)
    {
        _localizationService = localizationService;
        _themeSelectorService = themeSelectorService;

        AvailableLanguages = _localizationService.Languages;
        SelectedLanguage = _localizationService.GetCurrentLanguageItem();
        SelectedTheme = _themeSelectorService.Theme.ToString();
        _versionDescription = GetVersionDescription();
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(string param)
    {
        if (Enum.TryParse<ElementTheme>(param, out var theme))
        {
            await _themeSelectorService.SetThemeAsync(theme);
        }
    }

    [RelayCommand] // need restart
    private async Task SwitchLanguageAsync(LanguageItem param)
    {
        await _localizationService.SetLanguageAsync(param);
        IsLocalizationChanged = true;
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} {version.Major}.{version.Minor}.{version.Build}";
    }

}
