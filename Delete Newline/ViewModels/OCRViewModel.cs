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
    [NotifyCanExecuteChangedFor(nameof(InstallOcrLanguageCommand))]
    private bool _isInstallingLanguage;

    // Allow only one OCR capture window/session at a time.
    private int _ocrSessionActive;
    private bool _suppressLanguageAutoSave;
    private OcrCaptureWindow? _activeOcrWindow;

    // OCR dedicated hotkey settings
    private HotkeyStructure _ocrHotkey = new HotkeyStructure { Modifiers = VirtualKeyModifiers.None, Key = VirtualKey.None };

    public IReadOnlyList<string> AvailableLanguageTags => AvailableLanguages.Select(language => language.LanguageTag).ToArray();

    public IReadOnlyList<McpOcrLanguageInfo> AvailableLanguageInfos => AvailableLanguages
        .Select(ToMcpOcrLanguageInfo)
        .ToArray();

    public IReadOnlyList<McpOcrLanguageInfo> InstallableLanguageInfos => InstallableLanguages
        .Select(ToMcpOcrLanguageInfo)
        .ToArray();

    public string? CurrentLanguageTag => SelectedLanguage?.LanguageTag;

    public HotkeyStructure CurrentHotkey => new()
    {
        Modifiers = _ocrHotkey.Modifiers,
        Key = _ocrHotkey.Key
    };

    public async Task SetOcrSettingsAsync(string? languageTag, HotkeyStructure? hotkey)
    {
        if (!string.IsNullOrWhiteSpace(languageTag))
        {
            Language? language = AvailableLanguages.FirstOrDefault(item => item.LanguageTag.Equals(languageTag, StringComparison.OrdinalIgnoreCase));
            if (language == null)
            {
                string supported = string.Join(", ", AvailableLanguages.Select(item => item.LanguageTag));
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
        AvailableLanguages.Clear();
        foreach (Language language in OcrEngine.AvailableRecognizerLanguages.OrderBy(language => language.DisplayName))
        {
            AvailableLanguages.Add(language);
        }

        RefreshInstallableOcrLanguages();
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
        if (value != null && !_suppressLanguageAutoSave)
        {
            _ = SaveOcrLanguageAsync();
        }
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

            // Setup fullscreen capture with preloaded background and selected language
            ocrWindow.SetupFullscreen(backgroundImage, SelectedLanguage, AvailableLanguages.ToArray(), OnCaptureLanguageChanged);
            ocrWindow.Activate();
        }
        catch (Exception ex)
        {
            if (_activeOcrWindow != null)
            {
                _activeOcrWindow.Closed -= OcrWindow_Closed;
                _activeOcrWindow = null;
            }
            Interlocked.Exchange(ref _ocrSessionActive, 0);

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
        }

        _activeOcrWindow = null;
        Interlocked.Exchange(ref _ocrSessionActive, 0);
    }

    private void OnCaptureLanguageChanged(Language language)
    {
        SelectedLanguage = language;
    }

    private bool CanInstallOcrLanguage() => SelectedInstallLanguage != null && !IsInstallingLanguage;

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

        Language? languageToInstall = FindLanguageByTag(InstallableLanguages, languageTag);
        if (languageToInstall == null)
        {
            string installableTags = string.Join(", ", InstallableLanguages.Select(language => language.LanguageTag));
            throw new ArgumentException($"OCR language '{languageTag}' is not available to install. Call get_ocr_languages and copy a tag from installableLanguageTags. Installable language tags: {installableTags}.");
        }

        try
        {
            int exitCode = await OcrLanguageInstallHelper.InstallOcrLanguageCapabilityAsync(languageToInstall.LanguageTag);
            RefreshAvailableOcrLanguages();

            Language? installedLanguage = FindLanguageByTag(AvailableLanguages, languageToInstall.LanguageTag);
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
        IsInstallingLanguage = true;

        try
        {
            await InstallAndApplyOcrLanguageAsync(languageTag);
        }
        finally
        {
            IsInstallingLanguage = false;
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
            InstallableLanguages.Select(item => item.LanguageTag).ToArray(),
            message);
    }

    private static McpOcrLanguageInfo ToMcpOcrLanguageInfo(Language language)
    {
        return new McpOcrLanguageInfo(language.LanguageTag, language.DisplayName);
    }

    private static Language? FindLanguageByTag(IEnumerable<Language> languages, string languageTag)
    {
        return languages.FirstOrDefault(language => language.LanguageTag.Equals(languageTag.Trim(), StringComparison.OrdinalIgnoreCase));
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
