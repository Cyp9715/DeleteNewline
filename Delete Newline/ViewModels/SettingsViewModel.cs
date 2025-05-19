using System.Reflection;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls; // For InfoBarSeverity
using System; // Required for StartupTask
using System.Threading.Tasks; // Required for Task

using Delete_Newline.Contracts.Services;
using Delete_Newline.Helpers;
using Delete_Newline.Models;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Services;

namespace Delete_Newline.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;
    private readonly IFilePickerService _filePickerService;
    private readonly SettingsService _localSettingsService;
    private readonly InAppNotificationService _inAppNotificationService;
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

    [ObservableProperty]
    private bool _enableStartupTask;

    public SettingsViewModel(ILocalizationService localizationService, 
        IThemeSelectorService themeSelectorService,
        IFilePickerService filePickerService,
        NotificationService notificationService,
        InAppNotificationService inAppNotificationService,
        SettingsService localSettingsService,
        TopMostService topMostService)
    {
        _localizationService = localizationService;
        _themeSelectorService = themeSelectorService;
        _inAppNotificationService = inAppNotificationService;
        _localSettingsService = localSettingsService;
        _filePickerService = filePickerService;
        _topMostService = topMostService;

        AvailableLanguages = _localizationService.Languages;
        SelectedLanguage = _localizationService.GetCurrentLanguageItem();
        SelectedTheme = _themeSelectorService.Theme.ToString();
        VersionDescription = GetVersionDescription();
        EnableNotification = App.GetService<NotificationService>().GetEnableNotification();
        EnableTopMost = _topMostService.EnableTopMost;
        EnableStartOnTray = _localSettingsService.ReadSetting<bool>(DefaultStartOnTray);

        // Load initial startup task state
        InitializeStartupTaskStateAsync();

        App.GetService<NotificationService>().EnableNotificationChanged += OnNotificationEnabledChanged!;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        UpdateSelectedTheme();
    }

    public void UpdateSelectedTheme()
    {
        SelectedTheme = _themeSelectorService.Theme.ToString();
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

    [RelayCommand]
    private async Task SwitchLanguageAsync(LanguageItem param)
    {
        if (param is not null)
        {
            _inAppNotificationService.ShowInAppNotification(
                "Notification_LanguageChanged_Title", 
                "Notification_LanguageChanged_Message_RestartRequired",
                InfoBarSeverity.Warning);
            await _localizationService.SetLanguage(param);
        }
    }

    [RelayCommand]
    private async Task ToggleNotificationAsync(bool isChecked)
    {
        await App.GetService<NotificationService>().SetEnableNotificationAsync(isChecked);
    }

    [RelayCommand]
    private async Task ToggleTopMost(bool isChecked)
    {
        _topMostService.SetWindowTopMost(App.MainWindow, isChecked);
        await _topMostService.SaveTopMostSettingAsync();
    }

    public const string DefaultStartOnTray = "StartOnTray";

    [RelayCommand]
    private async Task ToggleStartOnTray(bool isChecked)
    {
        await _localSettingsService.SaveSettingAsync(DefaultStartOnTray, isChecked);
    }

    private async void InitializeStartupTaskStateAsync()
    {
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                StartupTask startupTask = await StartupTask.GetAsync("DeleteNewlineStartupTask");
                EnableStartupTask = startupTask.State == StartupTaskState.Enabled || startupTask.State == StartupTaskState.EnabledByPolicy;
                System.Diagnostics.Debug.WriteLine($"Initial startup task state: {startupTask.State}, IsEnabled: {EnableStartupTask}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting startup task: {ex.Message}");
                EnableStartupTask = false; 
            }
        }
        else
        {
            EnableStartupTask = false; 
            System.Diagnostics.Debug.WriteLine("Startup task API not available (not an MSIX package).");
        }
    }

    [RelayCommand]
    private async Task ToggleStartupTaskAsync(bool isEnabled)
    {
        if (!RuntimeHelper.IsMSIX)
        {
            _inAppNotificationService.ShowInAppNotification(
                "Notification_StartupTask_NotSupported_Title", 
                "Notification_StartupTask_NotSupported_Message",
                InfoBarSeverity.Warning);
            if (EnableStartupTask) EnableStartupTask = false;
            return;
        }

        try
        {
            StartupTask startupTask = await StartupTask.GetAsync("DeleteNewlineStartupTask");

            if (isEnabled)
            {
                StartupTaskState newState = await startupTask.RequestEnableAsync();
                bool actualStateIsNowEnabled = newState == StartupTaskState.Enabled || newState == StartupTaskState.EnabledByPolicy;

                if (EnableStartupTask != actualStateIsNowEnabled)
                {
                    EnableStartupTask = actualStateIsNowEnabled;
                }

                if (actualStateIsNowEnabled)
                {
                    System.Diagnostics.Debug.WriteLine($"Startup task enabled successfully. State: {newState}");
                }
                else
                {
                    // Provide more specific feedback based on the newState ONLY on failure
                    switch (newState)
                    {
                        case StartupTaskState.DisabledByUser:
                            _inAppNotificationService.ShowInAppNotification(
                                "Notification_StartupTask_DisabledByUser_Title",
                                "Notification_StartupTask_DisabledByUser_Message",
                                InfoBarSeverity.Warning);
                            break;
                        case StartupTaskState.DisabledByPolicy:
                            _inAppNotificationService.ShowInAppNotification(
                                "Notification_StartupTask_DisabledByPolicy_Title",
                                "Notification_StartupTask_DisabledByPolicy_Message",
                                InfoBarSeverity.Warning);
                            break;
                        default: // General failure (includes StartupTaskState.Disabled)
                            _inAppNotificationService.ShowInAppNotification(
                                "Notification_StartupTask_EnableFailed_Title",
                                "Notification_StartupTask_EnableFailed_Message",
                                InfoBarSeverity.Warning);
                            break;
                    }
                }
            }
            else // isEnabled is false (attempting to disable)
            {
                startupTask.Disable();
                if(EnableStartupTask) EnableStartupTask = false; 
                System.Diagnostics.Debug.WriteLine("Startup task disabled successfully via ToggleStartupTaskAsync.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error toggling startup task: {ex.Message}");
             _inAppNotificationService.ShowInAppNotification(
                "Notification_StartupTask_Error_Title", 
                "Notification_StartupTask_Error_Message", 
                InfoBarSeverity.Error);
            InitializeStartupTaskStateAsync(); // Re-sync UI on error
        }
    }

    [RelayCommand]
    public async Task ImportSettingsAsync()
    {
        string? importFilePath = await _filePickerService.PickOpenFileAsync();
        if (!string.IsNullOrEmpty(importFilePath))
        {
            var success = await _localSettingsService.ImportSettingsAsync(importFilePath);
            if (success)
            {
                _inAppNotificationService.ShowInAppNotification("Notification_SettingsImported_Success_Title", "Notification_SettingsImported_Success_Message_RestartRequired", InfoBarSeverity.Success);
            }
            else
            {
                _inAppNotificationService.ShowInAppNotification("Notification_SettingsImport_Error_Title", "Notification_SettingsImport_Error_General_Message", InfoBarSeverity.Error);
            }
        }
    }

    [RelayCommand]
    public async Task ExportSettingsAsync()
    {
        string? exportFilePath = await _filePickerService.PickSaveFileAsync();
        if (!string.IsNullOrEmpty(exportFilePath))
        {
            var success = await _localSettingsService.ExportSettingsAsync(exportFilePath);
            if (success)
            {
                _inAppNotificationService.ShowInAppNotification("Notification_SettingsExported_Success_Title", "Notification_SettingsExported_Success_Message", InfoBarSeverity.Success);
            }
            else
            {
                _inAppNotificationService.ShowInAppNotification("Notification_SettingsExport_Error_Title", "Notification_SettingsExport_Error_General_Message", InfoBarSeverity.Error);
            }
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

        return $"{LocalizationHelper.GetLocalizedString("AppDisplayName")} {version.Major}.{version.Minor}.{version.Build}";
    }
}
