using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Delete_Newline.Views;
using Delete_Newline.Helpers.Hotkeys;

namespace Delete_Newline.ViewModels;

public partial class OCRViewModel : ObservableRecipient
{
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly SettingsFileService _settingsService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

    // Settings keys
    private const string OcrHotkeyKey = "OCR_Hotkey";
    private const string OcrLanguageTagKey = "OCR_LanguageTag";

    [ObservableProperty]
    private Language? _selectedLanguage;

    [ObservableProperty]
    private string? _displayHotkey;

    [ObservableProperty]
    private ObservableCollection<Language> _availableLanguages;

    // OCR dedicated hotkey settings
    private HotkeyStructure _ocrHotkey = new HotkeyStructure { Modifiers = VirtualKeyModifiers.None, Key = VirtualKey.None };

    public OCRViewModel(
        InAppNotificationService notificationService,
        SettingsFileService settingsService)
    {
        _inAppNotificationService = notificationService;
        _settingsService = settingsService;
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _availableLanguages = new ObservableCollection<Language>(OcrEngine.AvailableRecognizerLanguages);
    }

    public void Initialize()
    {
        // Load OCR hotkey
        var savedHotkey = _settingsService.ReadSetting<HotkeyStructure>(OcrHotkeyKey);

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

    private void LoadSavedLanguage()
    {
        var savedLanguageTag = _settingsService.ReadSetting<string>(OcrLanguageTagKey);

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
            await _settingsService.SaveSettingAsync(OcrHotkeyKey, _ocrHotkey);
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
                await _settingsService.SaveSettingAsync(OcrLanguageTagKey, SelectedLanguage.LanguageTag);
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
            _ = SaveOcrLanguageAsync();
        }
    }

    [RelayCommand]
    public void LaunchOcr()
    {
        try
        {
            // Pre-capture desktop screenshot for background
            var backgroundImage = ImageHelper.GetFullDesktopScreenshotAsImageSource();

            // Create OCR window
            var ocrWindow = new OcrCaptureWindow();

            // Setup fullscreen capture with preloaded background and selected language
            ocrWindow.SetupFullscreen(backgroundImage, SelectedLanguage, false);
            ocrWindow.Activate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating OCR window: {ex.Message}");
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_OcrLaunchFailed_Title",
                messageKey: "Notification_OcrLaunchFailed_Message",
                severity: InfoBarSeverity.Error
            );
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