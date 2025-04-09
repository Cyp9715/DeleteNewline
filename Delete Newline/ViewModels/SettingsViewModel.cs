using System.Reflection;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Helpers;
using Delete_Newline.Models;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Services;
using System.Diagnostics;

namespace Delete_Newline.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;
    private readonly IFilePickerService _filePickerService;
    private readonly SettingsService _localSettingsService;
    private readonly NotificationService _notificationService;
    private readonly TopMostService _topMostService;

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
    private bool _enableStartOnTray;

    [ObservableProperty]
    private bool _enableTopMost;

    public SettingsViewModel(ILocalizationService localizationService, 
        IThemeSelectorService themeSelectorService,
        IFilePickerService filePickerService,
        NotificationService notificationService,
        SettingsService localSettingsService,
        TopMostService topMostService)
    {
        _localizationService = localizationService;
        _themeSelectorService = themeSelectorService;
        _notificationService = notificationService;
        _localSettingsService = localSettingsService;
        _filePickerService = filePickerService;
        _topMostService = topMostService;

        // get initial settings
        AvailableLanguages = _localizationService.Languages;
        SelectedLanguage = _localizationService.GetCurrentLanguageItem();
        SelectedTheme = _themeSelectorService.Theme.ToString();
        VersionDescription = GetVersionDescription();
        EnableNotification = _notificationService.GetEnableNotification();
        EnableTopMost = _topMostService.EnableTopMost;
        EnableStartOnTray = _localSettingsService.ReadSetting<bool>(DefaultStartOnTray);

        _notificationService.EnableNotificationChanged += OnNotificationEnabledChanged!;
    }

    private void OnNotificationEnabledChanged(object sender, bool isEnabled)
    {
        EnableNotification = isEnabled;
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
        // Problems running immediately after installing the app.
        if (param is not null)
        {
            _notificationService.ShowNotification("Language Change", "The app needs to restart to apply the new language. Restart now?", force: true);
            await _localizationService.SetLanguage(param);
        }
    }

    [RelayCommand]
    private async Task ToggleNotificationAsync(bool isChecked)
    {
        Debug.WriteLine($"ToggleNotificationAsync : {EnableNotification}");
        await _notificationService.SetEnableNotificationAsync(isChecked);
    }

    [RelayCommand]
    private async Task ToggleTopMost(bool isChecked)
    {
        EnableTopMost = isChecked;
        _topMostService.SetWindowTopMost(App.MainWindow, isChecked);
        await _topMostService.SaveTopMostSettingAsync();
    }

    public const string DefaultStartOnTray = "StartOnTray";

    [RelayCommand]
    private async Task ToggleStartOnTray(bool isChecked)
    {
        EnableStartOnTray = isChecked;
        await _localSettingsService.SaveSettingAsync(DefaultStartOnTray, isChecked);
    }

    [RelayCommand]
    public async Task ImportSettingsAsync()
    {
        string? importFilePath = await _filePickerService.PickOpenFileAsync();
        if (!string.IsNullOrEmpty(importFilePath))
        {
            await _localSettingsService.ImportSettingsAsync(importFilePath);
        }
    }


    [RelayCommand]
    public async Task ExportSettingsAsync()
    {
        string? exportFilePath = await _filePickerService.PickSaveFileAsync();
        if (!string.IsNullOrEmpty(exportFilePath))
        {
            await _localSettingsService.ExportSettingsAsync(exportFilePath);
        }
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
