using System.Reflection;
using System.Diagnostics;
using Windows.ApplicationModel;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Helpers;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Services;
using Delete_Newline.Services.Mcp;
using Delete_Newline.Views;

namespace Delete_Newline.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navigationService;
    private readonly IFilePickerService _filePickerService;
    private readonly SettingsFileService _localSettingsService;
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly TopMostService _topMostService;
    private readonly McpAccessService _mcpAccessService;
    private readonly SettingsImportApplyService _settingsImportApplyService;

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

    [ObservableProperty]
    private bool _enableMcpServer;

    [ObservableProperty]
    private double _mcpPort;

    [ObservableProperty]
    private bool _isMcpPortEditable;

    public SettingsViewModel(ILocalizationService localizationService, 
        IThemeSelectorService themeSelectorService,
        INavigationService navigationService,
        IFilePickerService filePickerService,
        InAppNotificationService inAppNotificationService,
        SettingsFileService localSettingsService,
        TopMostService topMostService,
        McpAccessService mcpAccessService,
        SettingsImportApplyService settingsImportApplyService)
    {
        _localizationService = localizationService;
        _themeSelectorService = themeSelectorService;
        _navigationService = navigationService;
        _inAppNotificationService = inAppNotificationService;
        _localSettingsService = localSettingsService;
        _filePickerService = filePickerService;
        _topMostService = topMostService;
        _mcpAccessService = mcpAccessService;
        _settingsImportApplyService = settingsImportApplyService;

        AvailableLanguages = _localizationService.Languages;
        SelectedLanguage = _localizationService.GetCurrentLanguageItem();
        SelectedTheme = _themeSelectorService.Theme.ToString();
        VersionDescription = GetVersionDescription();
        EnableNotification = App.GetService<NotificationService>().GetEnableNotification();
        EnableTopMost = _topMostService.EnableTopMost;
        EnableStartOnTray = _localSettingsService.ReadSetting<bool>(DefaultStartOnTray);
        EnableMcpServer = _mcpAccessService.GetEnableMcp();
        IsMcpPortEditable = !EnableMcpServer;
        McpPort = _mcpAccessService.GetPort();

        // Load initial startup task state
        InitializeStartupTaskStateAsync();

        App.GetService<NotificationService>().EnableNotificationChanged += OnNotificationEnabledChanged!;
        _mcpAccessService.EnableMcpChanged += OnMcpEnabledChanged!;
        _mcpAccessService.McpPortChanged += OnMcpPortChanged!;
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

    private void OnMcpEnabledChanged(object sender, bool isEnabled)
    {
        var dispatcherQueue = App.MainWindow.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            EnableMcpServer = isEnabled;
        }
        else
        {
            dispatcherQueue.TryEnqueue(() => EnableMcpServer = isEnabled);
        }
    }

    private void OnMcpPortChanged(object sender, int port)
    {
        var dispatcherQueue = App.MainWindow.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            McpPort = port;
        }
        else
        {
            dispatcherQueue.TryEnqueue(() => McpPort = port);
        }
    }

    partial void OnEnableMcpServerChanged(bool value)
    {
        IsMcpPortEditable = !value;
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
        if (param is null)
            return;

        if (param.Tag == _localizationService.GetCurrentLanguageItem().Tag)
            return;

        await _localizationService.SetLanguage(param);
        VersionDescription = GetVersionDescription();
        RefreshCurrentUiLanguage();

        _inAppNotificationService.ShowInAppNotification(
            "Notification_LanguageChanged_Title", 
            "Notification_LanguageChanged_Message_Applied",
            InfoBarSeverity.Success);
    }

    [RelayCommand]
    private async Task ToggleNotificationAsync(bool isChecked)
    {
        await App.GetService<NotificationService>().SetEnableNotificationAsync(isChecked);
    }

    [RelayCommand]
    private async Task ToggleMcpServerAsync(bool isChecked)
    {
        try
        {
            if (isChecked && !await TryChangeMcpPortAsync(McpPort))
            {
                EnableMcpServer = _mcpAccessService.GetEnableMcp();
                return;
            }

            await _mcpAccessService.SetEnableMcpAsync(isChecked);
        }
        catch (Exception)
        {
            EnableMcpServer = _mcpAccessService.GetEnableMcp();
            _inAppNotificationService.ShowInAppNotification(
                "Notification_McpServer_Error_Title",
                "Notification_McpServer_Error_Message",
                InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task ChangeMcpPortAsync(double value)
    {
        await TryChangeMcpPortAsync(value);
    }

    private async Task<bool> TryChangeMcpPortAsync(double value)
    {
        if (_mcpAccessService.GetEnableMcp())
        {
            int currentPort = _mcpAccessService.GetPort();
            McpPort = currentPort;
            if (TryGetWholePort(value, out int requestedPort) && requestedPort == currentPort)
            {
                return true;
            }

            _inAppNotificationService.ShowInAppNotification(
                "Notification_McpServer_Error_Title",
                "Notification_McpServer_Error_Message",
                InfoBarSeverity.Error);
            return false;
        }

        if (!TryGetWholePort(value, out int port) || !McpPortPolicy.IsValidPort(port))
        {
            McpPort = _mcpAccessService.GetPort();
            _inAppNotificationService.ShowInAppNotification(
                "Notification_McpServer_Error_Title",
                "Notification_McpServer_Error_Message",
                InfoBarSeverity.Error);
            return false;
        }

        try
        {
            await _mcpAccessService.SetPortAsync(port);
            McpPort = _mcpAccessService.GetPort();
            return true;
        }
        catch (Exception)
        {
            McpPort = _mcpAccessService.GetPort();
            EnableMcpServer = _mcpAccessService.GetEnableMcp();
            _inAppNotificationService.ShowInAppNotification(
                "Notification_McpServer_Error_Title",
                "Notification_McpServer_Error_Message",
                InfoBarSeverity.Error);
            return false;
        }
    }

    private static bool TryGetWholePort(double value, out int port)
    {
        port = default;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return false;
        }

        double rounded = Math.Round(value);
        if (Math.Abs(value - rounded) > double.Epsilon || rounded < int.MinValue || rounded > int.MaxValue)
        {
            return false;
        }

        port = (int)rounded;
        return true;
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
            IReadOnlyDictionary<string, Newtonsoft.Json.Linq.JToken> previousSettings = _localSettingsService.GetAllSettingsSnapshot();
            var success = await _localSettingsService.ImportSettingsAsync(importFilePath);
            if (success)
            {
                try
                {
                    await _settingsImportApplyService.ApplyAsync(_localSettingsService.GetAllSettingsSnapshot());
                    _inAppNotificationService.ShowInAppNotification("Notification_SettingsImported_Success_Title", "Notification_SettingsImported_Success_Message_Applied", InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error applying imported settings: {ex}");
                    await _localSettingsService.ReplaceSettingsAsync(previousSettings);
                    try
                    {
                        await _settingsImportApplyService.ApplyAsync(previousSettings);
                    }
                    catch (Exception rollbackEx)
                    {
                        Debug.WriteLine($"Error rolling back imported settings: {rollbackEx}");
                    }

                    _inAppNotificationService.ShowInAppNotification("Notification_SettingsImport_Error_Title", "Notification_SettingsImport_Error_General_Message", InfoBarSeverity.Error);
                }
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

    private void RefreshCurrentUiLanguage()
    {
        if (App.MainWindow.Content is ShellPage shellPage)
        {
            shellPage.RefreshLocalizedTexts();
        }

        // Rebuild only the current settings page to refresh all x:Uid resources
        // without recreating the whole shell/navigation view.
        _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!, Guid.NewGuid().ToString(), clearNavigation: true);
    }
}
