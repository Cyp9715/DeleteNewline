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
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private string _versionDescription;

    [ObservableProperty]
    private List<LanguageItem> _availableLanguages;

    [ObservableProperty]
    private LanguageItem _selectedLanguage;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private bool _enableNotification;

    [ObservableProperty]
    private bool _enableTopMost;

    public SettingsViewModel(ILocalizationService localizationService, IThemeSelectorService themeSelectorService, INotificationService notificationService)
    {
        _localizationService = localizationService;
        _themeSelectorService = themeSelectorService;
        _notificationService = notificationService;

        AvailableLanguages = _localizationService.Languages;
        SelectedLanguage = _localizationService.GetCurrentLanguageItem();
        SelectedTheme = _themeSelectorService.Theme.ToString();
        _versionDescription = GetVersionDescription();
        _enableNotification = _notificationService.GetEnableNotification();
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
        _notificationService.ShowNotification("Language Change", "The app needs to restart to apply the new language. Restart now?", force:true);
        await _localizationService.SetLanguageAsync(param);
    }

    [RelayCommand]
    private async Task ToggleNotificationAsync(bool isChecked)
    {
        EnableNotification = isChecked;
        await _notificationService.SetEnableNotificationAsync(isChecked);
    }

    [RelayCommand]
    private void ToggleTopMost(bool isChecked)
    {
        EnableTopMost = isChecked;
        TopMostHelper.SetWindowTopMost(App.MainWindow, isChecked);
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
