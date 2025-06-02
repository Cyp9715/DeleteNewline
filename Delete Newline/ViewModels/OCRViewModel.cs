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

namespace Delete_Newline.ViewModels;

public partial class OCRViewModel : ObservableRecipient
{
    private readonly HotkeyRegisterService _hotkeyManager;
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly SettingsService _settingsService;
    private Button? _dummyFocusButton;
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

    public OCRViewModel(HotkeyRegisterService hotkeyManager, InAppNotificationService notificationService, SettingsService settingsService)
    {
        _hotkeyManager = hotkeyManager;
        _inAppNotificationService = notificationService;
        _settingsService = settingsService;
        _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _availableLanguages = new ObservableCollection<Language>(OcrEngine.AvailableRecognizerLanguages);
        
        // Initialize settings
        Initialize();
    }

    public void Initialize()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load OCR hotkey
        var savedHotkey = _settingsService.ReadSetting<HotkeyStructure>(OcrHotkeyKey);
        
        if (savedHotkey != null)
        {
            _ocrHotkey = savedHotkey;
            
            // Register the loaded hotkey
            if (_ocrHotkey.Modifiers != VirtualKeyModifiers.None && _ocrHotkey.Key != VirtualKey.None)
            {
                _hotkeyManager.RegisterHotkey((_ocrHotkey.Modifiers, _ocrHotkey.Key), HotkeyType.Ocr);
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
        DisplayHotkey = HotkeyHelper.GetDisplayText(_ocrHotkey.Modifiers, _ocrHotkey.Key);
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

    public void SetDummyFocusButton(Button btn)
    {
        _dummyFocusButton = btn;
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
        // Check for forbidden hotkey combinations using centralized validation
        if (HotkeyHelper.IsShiftAlone(args.Modifiers))
        {
            var (titleKey, messageKey) = HotkeyHelper.GetForbiddenHotkeyError(args.Modifiers, args.Key);
            _inAppNotificationService.ShowInAppNotification(
                titleKey: titleKey,
                messageKey: messageKey,
                severity: InfoBarSeverity.Warning
            );
        }

        // Unregister previous OCR hotkey if exists
        if (_ocrHotkey.Modifiers != VirtualKeyModifiers.None && _ocrHotkey.Key != VirtualKey.None)
        {
            _hotkeyManager.UnregisterHotkey((_ocrHotkey.Modifiers, _ocrHotkey.Key), HotkeyType.Ocr);
        }

        // Check for system hotkey using centralized validation
        if (HotkeyHelper.IsSystemHotkey(args.Modifiers, args.Key))
        {
            var (titleKey, messageKey) = HotkeyHelper.GetForbiddenHotkeyError(args.Modifiers, args.Key);
            _inAppNotificationService.ShowInAppNotification(
                titleKey: titleKey,
                messageKey: messageKey,
                severity: InfoBarSeverity.Error
            );
            return;
        }

        // Check if hotkey is already registered for text processing
        if (_hotkeyManager.IsHotkeyRegistered((args.Modifiers, args.Key)))
        {
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_InvalidHotkey_Title",
                messageKey: "Notification_InvalidHotkey_AlreadyRegistered_Message",
                severity: InfoBarSeverity.Error
            );
            return;
        }

        // Register new OCR hotkey
        if (_hotkeyManager.RegisterHotkey((args.Modifiers, args.Key), HotkeyType.Ocr))
        {
            _ocrHotkey.Modifiers = args.Modifiers;
            _ocrHotkey.Key = args.Key;

            // Save the new hotkey settings
            _ = SaveOcrHotkeyAsync();
        }
        else
        {
            // Reset hotkey to None when registration fails
            _ocrHotkey.Modifiers = VirtualKeyModifiers.None;
            _ocrHotkey.Key = VirtualKey.None;
            
            // Save the reset hotkey settings
            _ = SaveOcrHotkeyAsync();
            
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_HotkeyRegistrationFailed_Title",
                messageKey: "Notification_HotkeyRegistrationFailed_InUse_Message",
                severity: InfoBarSeverity.Error
            );
        }
    }
} 