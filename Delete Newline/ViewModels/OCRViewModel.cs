using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading;
using Delete_Newline.Views;
using Delete_Newline.Helpers.Hotkeys;
using Delete_Newline.Services.Mcp;

namespace Delete_Newline.ViewModels;

public partial class OCRViewModel : ObservableRecipient
{
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly SettingsFileService _settingsService;

    // Settings keys
    public const string OcrHotkeySettingsKey = "OCR_Hotkey";
    public const string OcrLanguageTagSettingsKey = "OCR_LanguageTag";

    [ObservableProperty]
    private Language? _selectedLanguage;

    [ObservableProperty]
    private string? _displayHotkey;

    [ObservableProperty]
    private ObservableCollection<Language> _availableLanguages;

    [ObservableProperty]
    private ObservableCollection<Language> _installableLanguages;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallOcrLanguageCommand))]
    private Language? _selectedInstallLanguage;

    [ObservableProperty]
    private ObservableCollection<OcrLanguageManagementItem> _manageableLanguages;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallManagedOcrLanguageCommand))]
    private OcrLanguageManagementItem? _selectedManageLanguage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallOcrLanguageCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallManagedOcrLanguageCommand))]
    private bool _isManagingLanguage;

    // Allow only one OCR capture window/session at a time.
    private int _ocrSessionActive;
    private bool _suppressLanguageAutoSave;
    private OcrCaptureWindow? _activeOcrWindow;

    // OCR dedicated hotkey settings
    private HotkeyStructure _ocrHotkey = new HotkeyStructure { Modifiers = VirtualKeyModifiers.None, Key = VirtualKey.None };

    public IReadOnlyList<McpOcrLanguageInfo> AvailableLanguageInfos => ManageableLanguages
        .Where(language => language.IsInstalled)
        .Select(language => new McpOcrLanguageInfo(language.LanguageTag, language.DisplayName))
        .ToArray();

    public IReadOnlyList<string> AvailableLanguageTags => AvailableLanguageInfos
        .Select(language => language.LanguageTag)
        .ToArray();

    public IReadOnlyList<McpOcrLanguageInfo> InstallableLanguageInfos => ManageableLanguages
        .Where(language => !language.IsInstalled)
        .Select(language => new McpOcrLanguageInfo(language.LanguageTag, language.DisplayName))
        .ToArray();

    public string? CurrentLanguageTag => SelectedLanguage == null
        ? null
        : ManageableLanguages.FirstOrDefault(language => language.IsInstalled && language.IsCurrent)?.LanguageTag ?? SelectedLanguage.LanguageTag;

    public Visibility InstallManagedLanguageButtonVisibility => SelectedManageLanguage is { IsInstalled: false }
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility DeleteManagedLanguageButtonVisibility => SelectedManageLanguage is { IsInstalled: true, IsCurrent: false }
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility CurrentManagedLanguageButtonVisibility => SelectedManageLanguage is { IsInstalled: true, IsCurrent: true }
        ? Visibility.Visible
        : Visibility.Collapsed;

    public bool CanDeleteManagedLanguage => SelectedManageLanguage is { IsInstalled: true, IsCurrent: false } && !IsManagingLanguage;

    public HotkeyStructure CurrentHotkey => new()
    {
        Modifiers = _ocrHotkey.Modifiers,
        Key = _ocrHotkey.Key
    };

    public async Task SetOcrSettingsAsync(string? languageTag, HotkeyStructure? hotkey)
    {
        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            Language? language = FindLanguageByTag(AvailableLanguages, languageTag);
            if (language == null)
            {
                string supported = string.Join(", ", AvailableLanguageTags);
                throw new ArgumentException($"Unsupported OCR language '{languageTag}'. Supported language tags: {supported}.");
            }

            _suppressLanguageAutoSave = true;
            try
            {
                SelectedLanguage = language;
            }
            finally
            {
                _suppressLanguageAutoSave = false;
            }

            await SaveOcrLanguageAsync();
        }

        if (hotkey != null)
        {
            await SetOcrHotkeyAsync(hotkey);
        }
    }

    private async Task SetOcrHotkeyAsync(HotkeyStructure hotkey)
    {
        var newHotkey = (hotkey.Modifiers, hotkey.Key);
        var oldHotkey = (_ocrHotkey.Modifiers, _ocrHotkey.Key);

        if (newHotkey == oldHotkey)
        {
            await SaveOcrHotkeyAsync();
            return;
        }

        if (oldHotkey.Key != VirtualKey.None)
        {
            HotkeyRegister.UnregisterHotkey(oldHotkey, HotkeyType.Ocr);
        }

        try
        {
            if (HotkeyRegister.IsHotkeyRegisteredGlobally(newHotkey))
            {
                throw new InvalidOperationException($"Hotkey '{hotkey}' is already registered.");
            }

            if (newHotkey.Key != VirtualKey.None && HotkeyRegister.RegisterHotkey(newHotkey, HotkeyType.Ocr) == false)
            {
                throw new InvalidOperationException($"Failed to register OCR hotkey '{hotkey}'.");
            }

            _ocrHotkey.Modifiers = newHotkey.Modifiers;
            _ocrHotkey.Key = newHotkey.Key;
            await SaveOcrHotkeyAsync();
        }
        catch
        {
            if (oldHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey(oldHotkey, HotkeyType.Ocr);
            }

            UpdateDisplayHotkey();
            throw;
        }
    }

    public OCRViewModel(
        InAppNotificationService notificationService,
        SettingsFileService settingsService)
    {
        _inAppNotificationService = notificationService;
        _settingsService = settingsService;
        _availableLanguages = [];
        _installableLanguages = [];
        _manageableLanguages = [];
        RefreshAvailableOcrLanguages();
    }

    public void Initialize()
    {
        // Load OCR hotkey
        var savedHotkey = _settingsService.ReadSetting<HotkeyStructure>(OcrHotkeySettingsKey);

        if (savedHotkey != null)
        {
            _ocrHotkey = savedHotkey;

            // Register the loaded hotkey
            if (_ocrHotkey.Modifiers != VirtualKeyModifiers.None && _ocrHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey((_ocrHotkey.Modifiers, _ocrHotkey.Key), HotkeyType.Ocr);
            }
        }

        // Load OCR Language setting
        LoadSavedLanguage();

        // Update display hotkey
        UpdateDisplayHotkey();
    }

    private void RefreshAvailableOcrLanguages()
    {
        string? selectedLanguageTag = SelectedLanguage?.LanguageTag;

        AvailableLanguages.Clear();
        foreach (Language language in OcrEngine.AvailableRecognizerLanguages.OrderBy(language => language.DisplayName))
        {
            AvailableLanguages.Add(language);
        }

        RestoreSelectedLanguageAfterRefresh(selectedLanguageTag);
        RefreshInstallableOcrLanguages();
        RefreshManageableOcrLanguages();
    }

    private void RestoreSelectedLanguageAfterRefresh(string? selectedLanguageTag)
    {
        if (string.IsNullOrWhiteSpace(selectedLanguageTag))
        {
            return;
        }

        Language? restoredLanguage = FindLanguageByTag(AvailableLanguages, selectedLanguageTag);
        if (restoredLanguage == null || ReferenceEquals(SelectedLanguage, restoredLanguage))
        {
            return;
        }

        _suppressLanguageAutoSave = true;
        try
        {
            SelectedLanguage = restoredLanguage;
        }
        finally
        {
            _suppressLanguageAutoSave = false;
        }
    }

    private void RefreshInstallableOcrLanguages()
    {
        string? previousSelection = SelectedInstallLanguage?.LanguageTag;
        IReadOnlyList<Language> installableLanguages = OcrLanguageInstallHelper.GetInstallableLanguages(AvailableLanguages);

        InstallableLanguages.Clear();
        foreach (Language language in installableLanguages)
        {
            InstallableLanguages.Add(language);
        }

        SelectedInstallLanguage = InstallableLanguages.FirstOrDefault(language =>
            language.LanguageTag.Equals(previousSelection, StringComparison.OrdinalIgnoreCase)) ?? InstallableLanguages.FirstOrDefault();
    }

    private void RefreshManageableOcrLanguages()
    {
        string? previousSelection = SelectedManageLanguage?.LanguageTag;
        string installedStatusText = GetLocalizedResourceString("OCRPage_Text_LanguageInstalledSuffix.Text", "(installed)");
        IReadOnlyList<OcrLanguageManagementItem> manageableLanguages = OcrLanguageInstallHelper.GetManageableLanguages(
            AvailableLanguages,
            SelectedLanguage?.LanguageTag,
            installedStatusText);

        ManageableLanguages.Clear();
        foreach (OcrLanguageManagementItem language in manageableLanguages)
        {
            ManageableLanguages.Add(language);
        }

        SelectedManageLanguage = ManageableLanguages.FirstOrDefault(language =>
                language.LanguageTag.Equals(previousSelection, StringComparison.OrdinalIgnoreCase))
            ?? ManageableLanguages.FirstOrDefault(language => !language.IsInstalled)
            ?? ManageableLanguages.FirstOrDefault();
    }

    private void LoadSavedLanguage()
    {
        var savedLanguageTag = _settingsService.ReadSetting<string>(OcrLanguageTagSettingsKey);

        if (!string.IsNullOrEmpty(savedLanguageTag))
        {
            // Try to find the saved language in available languages
            var savedLanguage = AvailableLanguages.FirstOrDefault(l => l.LanguageTag == savedLanguageTag);

            if (savedLanguage != null)
            {
                SelectedLanguage = savedLanguage;
                return;
            }
            else
            {
                // Saved language is no longer available, show error notification
                _inAppNotificationService.ShowInAppNotification(
                    titleKey: "Notification_OcrLanguageNotFound_Title",
                    messageKey: "Notification_OcrLanguageNotFound_Message",
                    severity: InfoBarSeverity.Warning
                );
            }
        }

        // Fall back to default language (English or first available)
        var englishLang = AvailableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        SelectedLanguage = englishLang ?? AvailableLanguages.FirstOrDefault();
    }

    private void UpdateDisplayHotkey()
    {
        DisplayHotkey = HotkeyFormatter.GetDisplayText(_ocrHotkey.Modifiers, _ocrHotkey.Key);
    }

    private async Task SaveOcrHotkeyAsync()
    {
        try
        {
            await _settingsService.SaveSettingAsync(OcrHotkeySettingsKey, _ocrHotkey);
            // Update display after saving
            UpdateDisplayHotkey();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR hotkey: {ex.Message}");
        }
    }

    private async Task SaveOcrLanguageAsync()
    {
        if (SelectedLanguage != null)
        {
            try
            {
                await _settingsService.SaveSettingAsync(OcrLanguageTagSettingsKey, SelectedLanguage.LanguageTag);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save OCR Language: {ex.Message}");
            }
        }
    }

    // Handle OCR Language changes
    partial void OnSelectedLanguageChanged(Language? value)
    {
        if (value != null)
        {
            if (!_suppressLanguageAutoSave)
            {
                _ = SaveOcrLanguageAsync();
            }

            RefreshManageableOcrLanguages();
        }
    }

    partial void OnSelectedManageLanguageChanged(OcrLanguageManagementItem? value)
    {
        NotifyManagedLanguageActionStateChanged();
    }

    partial void OnIsManagingLanguageChanged(bool value)
    {
        NotifyManagedLanguageActionStateChanged();
    }

    private void NotifyManagedLanguageActionStateChanged()
    {
        OnPropertyChanged(nameof(InstallManagedLanguageButtonVisibility));
        OnPropertyChanged(nameof(DeleteManagedLanguageButtonVisibility));
        OnPropertyChanged(nameof(CurrentManagedLanguageButtonVisibility));
        OnPropertyChanged(nameof(CanDeleteManagedLanguage));
    }

    [RelayCommand]
    public void LaunchOcr()
    {
        if (Interlocked.CompareExchange(ref _ocrSessionActive, 1, 0) == 1)
        {
            System.Diagnostics.Debug.WriteLine("OCR launch skipped: capture session already active.");
            return;
        }

        try
        {
            // Pre-capture desktop screenshot for background. Keep the preview sharp; the capture UI adds only a dark filter.
            var backgroundImage = ImageHelper.GetFullDesktopScreenshotAsImageSource();

            // Create OCR window
            var ocrWindow = new OcrCaptureWindow();
            _activeOcrWindow = ocrWindow;
            ocrWindow.Closed += OcrWindow_Closed;
            ocrWindow.OcrProcessingCompleted += OcrWindow_OcrProcessingCompleted;

            // Setup fullscreen capture with preloaded background and selected language
            ocrWindow.SetupFullscreen(backgroundImage, SelectedLanguage, AvailableLanguages.ToArray(), OnCaptureLanguageChanged);
            ocrWindow.Activate();
        }
        catch (Exception ex)
        {
            if (_activeOcrWindow != null)
            {
                OcrCaptureWindow failedWindow = _activeOcrWindow;
                failedWindow.Closed -= OcrWindow_Closed;
                failedWindow.OcrProcessingCompleted -= OcrWindow_OcrProcessingCompleted;
                _activeOcrWindow = null;

                try
                {
                    failedWindow.Close();
                }
                catch (Exception closeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to close OCR window after launch error: {closeEx.Message}");
                }
            }
            ReleaseOcrSession();

            System.Diagnostics.Debug.WriteLine($"Error creating OCR window: {ex.Message}");
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_OcrLaunchFailed_Title",
                messageKey: "Notification_OcrLaunchFailed_Message",
                severity: InfoBarSeverity.Error
            );
        }
    }

    private void OcrWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is OcrCaptureWindow closedWindow)
        {
            closedWindow.Closed -= OcrWindow_Closed;
            if (!closedWindow.IsOcrProcessing)
            {
                closedWindow.OcrProcessingCompleted -= OcrWindow_OcrProcessingCompleted;
                ReleaseOcrSession();
                return;
            }
        }

        _activeOcrWindow = null;
    }

    private void OcrWindow_OcrProcessingCompleted(object? sender, EventArgs args)
    {
        if (sender is OcrCaptureWindow completedWindow)
        {
            completedWindow.OcrProcessingCompleted -= OcrWindow_OcrProcessingCompleted;
            completedWindow.Closed -= OcrWindow_Closed;
        }

        ReleaseOcrSession();
    }

    private void ReleaseOcrSession()
    {
        _activeOcrWindow = null;
        Interlocked.Exchange(ref _ocrSessionActive, 0);
    }

    private void OnCaptureLanguageChanged(Language language)
    {
        SelectedLanguage = language;
    }

    private bool CanInstallOcrLanguage() => SelectedInstallLanguage != null && !IsManagingLanguage;

    private bool CanInstallManagedOcrLanguage() => SelectedManageLanguage is { IsInstalled: false } && !IsManagingLanguage;

    public async Task<McpOcrLanguageInstallResult> InstallAndApplyOcrLanguageAsync(string languageTag, bool showNotification = true)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            throw new ArgumentException("languageTag is required.", nameof(languageTag));
        }

        Language? availableLanguage = FindLanguageByTag(AvailableLanguages, languageTag);
        if (availableLanguage != null)
        {
            SelectedLanguage = availableLanguage;
            await SaveOcrLanguageAsync();
            return CreateOcrLanguageInstallResult(
                success: true,
                installed: false,
                applied: true,
                availableLanguage,
                exitCode: null,
                message: $"{availableLanguage.LanguageTag} was already available and is now selected for OCR.");
        }

        OcrLanguageManagementItem? managedLanguageToInstall = ManageableLanguages.FirstOrDefault(language =>
            !language.IsInstalled && LanguageTagsMatch(language.LanguageTag, languageTag));
        Language? languageToInstall = managedLanguageToInstall?.Language ?? FindLanguageByTag(InstallableLanguages, languageTag);
        string capabilityLanguageTag = managedLanguageToInstall?.LanguageTag ?? languageTag.Trim();
        if (languageToInstall == null)
        {
            string installableTags = string.Join(", ", InstallableLanguageInfos.Select(language => language.LanguageTag));
            throw new ArgumentException($"OCR language '{languageTag}' is not available to install. Call get_ocr_languages and copy a tag from installableLanguageTags. Installable language tags: {installableTags}.");
        }

        try
        {
            int exitCode = await OcrLanguageInstallHelper.InstallOcrLanguageCapabilityAsync(capabilityLanguageTag);
            RefreshAvailableOcrLanguages();

            Language? installedLanguage = FindLanguageByTag(AvailableLanguages, capabilityLanguageTag)
                ?? FindLanguageByTag(AvailableLanguages, languageToInstall.LanguageTag);
            if (exitCode == 0 && installedLanguage != null)
            {
                SelectedLanguage = installedLanguage;
                await SaveOcrLanguageAsync();
                if (showNotification)
                {
                    _inAppNotificationService.ShowInAppNotification(
                        titleKey: "Notification_OcrLanguageInstallSuccess_Title",
                        messageKey: "Notification_OcrLanguageInstallSuccess_Message",
                        severity: InfoBarSeverity.Success,
                        messageArgs: new object[] { installedLanguage.DisplayName });
                }

                return CreateOcrLanguageInstallResult(
                    success: true,
                    installed: true,
                    applied: true,
                    installedLanguage,
                    exitCode,
                    message: $"{installedLanguage.LanguageTag} was installed and selected for OCR.");
            }

            string failureMessage = $"OCR language '{languageToInstall.LanguageTag}' did not become available after installation. Exit code: {exitCode}.";
            if (showNotification)
            {
                _inAppNotificationService.ShowInAppNotification(
                    titleKey: "Notification_OcrLanguageInstallFailed_Title",
                    messageKey: "Notification_OcrLanguageInstallFailed_Message",
                    severity: InfoBarSeverity.Error,
                    messageArgs: new object[] { languageToInstall.DisplayName });
            }

            return CreateOcrLanguageInstallResult(
                success: false,
                installed: false,
                applied: false,
                languageToInstall,
                exitCode,
                failureMessage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to install OCR language '{languageToInstall.LanguageTag}': {ex.Message}");
            if (showNotification)
            {
                _inAppNotificationService.ShowInAppNotification(
                    titleKey: "Notification_OcrLanguageInstallFailed_Title",
                    messageKey: "Notification_OcrLanguageInstallFailed_Message",
                    severity: InfoBarSeverity.Error,
                    messageArgs: new object[] { languageToInstall.DisplayName });
            }

            return CreateOcrLanguageInstallResult(
                success: false,
                installed: false,
                applied: false,
                languageToInstall,
                exitCode: null,
                message: ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstallOcrLanguage))]
    private async Task InstallOcrLanguageAsync()
    {
        if (SelectedInstallLanguage == null)
        {
            return;
        }

        string languageTag = SelectedInstallLanguage.LanguageTag;
        IsManagingLanguage = true;

        try
        {
            await InstallAndApplyOcrLanguageAsync(languageTag);
        }
        finally
        {
            IsManagingLanguage = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstallManagedOcrLanguage))]
    private async Task InstallManagedOcrLanguageAsync()
    {
        if (SelectedManageLanguage is not { IsInstalled: false } languageToInstall)
        {
            return;
        }

        string languageTag = languageToInstall.LanguageTag;
        IsManagingLanguage = true;

        try
        {
            await InstallAndApplyOcrLanguageAsync(languageTag);
        }
        finally
        {
            IsManagingLanguage = false;
        }
    }

    public async Task<McpOcrLanguageDeleteResult> DeleteManagedOcrLanguageAsync(string languageTag, bool showNotification = true)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            throw new ArgumentException("languageTag is required.", nameof(languageTag));
        }

        string requestedLanguageTag = languageTag.Trim();
        if (SelectedLanguage != null && LanguageTagsMatch(SelectedLanguage.LanguageTag, requestedLanguageTag))
        {
            throw new InvalidOperationException("Do not delete the current OCR language. Select another OCR language first with set_ocr_settings.");
        }

        OcrLanguageManagementItem? managedLanguageToDelete = ManageableLanguages.FirstOrDefault(language =>
            language.IsInstalled && LanguageTagsMatch(language.LanguageTag, requestedLanguageTag));
        Language? languageToDelete = managedLanguageToDelete?.Language ?? FindLanguageByTag(AvailableLanguages, requestedLanguageTag);
        string capabilityLanguageTag = managedLanguageToDelete?.LanguageTag ?? requestedLanguageTag;
        if (languageToDelete == null)
        {
            RefreshAvailableOcrLanguages();
            return CreateOcrLanguageDeleteResult(
                success: true,
                deleted: false,
                languageTag: capabilityLanguageTag,
                displayName: capabilityLanguageTag,
                exitCode: null,
                message: $"OCR language '{capabilityLanguageTag}' was not installed.");
        }

        IsManagingLanguage = true;
        try
        {
            int exitCode = await OcrLanguageInstallHelper.RemoveOcrLanguageCapabilityAsync(capabilityLanguageTag);
            RefreshAvailableOcrLanguages();

            bool deleted = exitCode == 0 && FindLanguageByTag(AvailableLanguages, capabilityLanguageTag) == null;
            if (deleted)
            {
                if (showNotification)
                {
                    _inAppNotificationService.ShowInAppNotification(
                        titleKey: "Notification_OcrLanguageDeleteSuccess_Title",
                        messageKey: "Notification_OcrLanguageDeleteSuccess_Message",
                        severity: InfoBarSeverity.Success,
                        messageArgs: new object[] { languageToDelete.DisplayName });
                }

                return CreateOcrLanguageDeleteResult(
                    success: true,
                    deleted: true,
                    languageTag: capabilityLanguageTag,
                    displayName: languageToDelete.DisplayName,
                    exitCode,
                    message: $"{capabilityLanguageTag} was deleted from Windows OCR languages.");
            }

            if (showNotification)
            {
                _inAppNotificationService.ShowInAppNotification(
                    titleKey: "Notification_OcrLanguageDeleteFailed_Title",
                    messageKey: "Notification_OcrLanguageDeleteFailed_Message",
                    severity: InfoBarSeverity.Error,
                    messageArgs: new object[] { languageToDelete.DisplayName });
            }

            return CreateOcrLanguageDeleteResult(
                success: false,
                deleted: false,
                languageTag: capabilityLanguageTag,
                displayName: languageToDelete.DisplayName,
                exitCode,
                message: $"OCR language '{capabilityLanguageTag}' did not disappear after deletion. Exit code: {exitCode}.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete OCR language '{languageToDelete.LanguageTag}': {ex.Message}");
            if (showNotification)
            {
                _inAppNotificationService.ShowInAppNotification(
                    titleKey: "Notification_OcrLanguageDeleteFailed_Title",
                    messageKey: "Notification_OcrLanguageDeleteFailed_Message",
                    severity: InfoBarSeverity.Error,
                    messageArgs: new object[] { languageToDelete.DisplayName });
            }

            return CreateOcrLanguageDeleteResult(
                success: false,
                deleted: false,
                languageTag: capabilityLanguageTag,
                displayName: languageToDelete.DisplayName,
                exitCode: null,
                message: ex.Message);
        }
        finally
        {
            IsManagingLanguage = false;
        }
    }

    private McpOcrLanguageInstallResult CreateOcrLanguageInstallResult(
        bool success,
        bool installed,
        bool applied,
        Language language,
        int? exitCode,
        string message)
    {
        return new McpOcrLanguageInstallResult(
            success,
            installed,
            applied,
            language.LanguageTag,
            language.DisplayName,
            exitCode,
            AvailableLanguageTags,
            InstallableLanguageInfos.Select(item => item.LanguageTag).ToArray(),
            message);
    }

    private McpOcrLanguageDeleteResult CreateOcrLanguageDeleteResult(
        bool success,
        bool deleted,
        string languageTag,
        string displayName,
        int? exitCode,
        string message)
    {
        return new McpOcrLanguageDeleteResult(
            success,
            deleted,
            languageTag,
            displayName,
            exitCode,
            AvailableLanguageTags,
            InstallableLanguageInfos.Select(item => item.LanguageTag).ToArray(),
            message);
    }

    private static McpOcrLanguageInfo ToMcpOcrLanguageInfo(Language language)
    {
        return new McpOcrLanguageInfo(language.LanguageTag, language.DisplayName);
    }

    private static Language? FindLanguageByTag(IEnumerable<Language> languages, string languageTag)
    {
        return languages.FirstOrDefault(language => LanguageTagsMatch(language.LanguageTag, languageTag));
    }

    private static bool LanguageTagsMatch(string first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
        {
            return false;
        }

        string trimmedSecond = second.Trim();
        if (first.Equals(trimmedSecond, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            return new Language(first).LanguageTag.Equals(trimmedSecond, StringComparison.OrdinalIgnoreCase)
                || first.Equals(new Language(trimmedSecond).LanguageTag, StringComparison.OrdinalIgnoreCase)
                || new Language(first).LanguageTag.Equals(new Language(trimmedSecond).LanguageTag, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string GetLocalizedResourceString(string resourceKey, string fallback)
    {
        try
        {
            Microsoft.Windows.ApplicationModel.Resources.ResourceManager resourceManager = new();
            Microsoft.Windows.ApplicationModel.Resources.ResourceContext resourceContext = resourceManager.CreateResourceContext();
            string languageOverride = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
            if (!string.IsNullOrWhiteSpace(languageOverride))
            {
                resourceContext.QualifierValues["Language"] = languageOverride;
            }

            return resourceManager.MainResourceMap
                .GetSubtree("Resources")
                .GetValue(resourceKey, resourceContext)
                .ValueAsString ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    [RelayCommand]
    public void ProcessKeyInput(KeyboardInputEventArgs args)
    {
        if(HotkeyValidator.ValidateHotkey(args, HotkeyType.Ocr, _inAppNotificationService) == false)
            return;

        var newHotkey = (args.Modifiers, args.Key);
        var oldHotkey = (_ocrHotkey.Modifiers, _ocrHotkey.Key);

        // 1. If the new hotkey is the same as the old one, do nothing.
        if (newHotkey == oldHotkey)
        {
            return;
        }

        // 2. First, unregister the old hotkey.
        if (oldHotkey.Key != VirtualKey.None)
        {
            HotkeyRegister.UnregisterHotkey(oldHotkey, HotkeyType.Ocr);
        }

        // 3. Check if the new hotkey is already registered globally.
        if (HotkeyRegister.IsHotkeyRegisteredGlobally(newHotkey))
        {
            // 3-1. If it's a duplicate, show a notification and re-register the old hotkey (rollback).
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_HotkeyRegistrationFailed_Title",
                messageKey: "Notification_HotkeyRegistrationFailed_InUse_Message",
                severity: InfoBarSeverity.Error
            );

            if (oldHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey(oldHotkey, HotkeyType.Ocr);
            }

            // Force a UI update to revert to the previous hotkey display.
            UpdateDisplayHotkey();
            return;
        }

        // 4. If the hotkey is available, register the new hotkey.
        bool registrationSuccess = true;
        if (newHotkey.Key != VirtualKey.None)
        {
            registrationSuccess = HotkeyRegister.RegisterHotkey(newHotkey, HotkeyType.Ocr);
        }

        if (registrationSuccess)
        {
            // 4-1. If registration succeeds, update the model and save.
            _ocrHotkey.Modifiers = newHotkey.Item1;
            _ocrHotkey.Key = newHotkey.Item2;
            _ = SaveOcrHotkeyAsync(); // UpdateDisplayHotkey() is called inside this method.
        }
        else
        {
            // 4-2. If registration fails, roll back.
            if (oldHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey(oldHotkey, HotkeyType.Ocr);
            }
            UpdateDisplayHotkey();
        }
    }


    [RelayCommand]
    public void GotFocusHotkeyTextBox()
    {
        HotkeyRegister.DisableAllHotkeys();
    }

    [RelayCommand]
    public void LostFocusHotkeyTextBox()
    {
        HotkeyRegister.EnableAllHotkeys();
    }
}
