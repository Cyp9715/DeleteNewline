using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.System;
using Delete_Newline.Views;
using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public sealed class OCRService
{
    private readonly HotkeyRegisterService _hotkeyManager;
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly SettingsService _settingsService;
    private readonly RegexCollectSaveService _hotkeyCollectSaveService;
    private readonly RegexService _regexService;
    private readonly NotificationService _notificationService;

    // Settings keys
    private const string OcrHotkeyModifiersKey = "OCR_HotkeyModifiers";
    private const string OcrHotkeyKeyKey = "OCR_HotkeyKey";
    private const string OcrLanguageTagKey = "OCR_LanguageTag";

    // OCR settings
    private VirtualKeyModifiers _ocrModifiers = VirtualKeyModifiers.None;
    private VirtualKey _ocrKey = VirtualKey.None;
    private Language? _selectedLanguage;

    public OCRService(HotkeyRegisterService hotkeyManager, InAppNotificationService inAppNotificationService, SettingsService settingsService, RegexCollectSaveService hotkeyCollectSaveService, RegexService regexService, NotificationService notificationService)
    {
        _hotkeyManager = hotkeyManager;
        _inAppNotificationService = inAppNotificationService;
        _settingsService = settingsService;
        _hotkeyCollectSaveService = hotkeyCollectSaveService;
        _regexService = regexService;
        _notificationService = notificationService;
    }

    public void Initialize()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Load OCR hotkey
        var savedModifiers = _settingsService.ReadSetting<int?>(OcrHotkeyModifiersKey);
        var savedKey = _settingsService.ReadSetting<int?>(OcrHotkeyKeyKey);
        
        if (savedModifiers.HasValue && savedKey.HasValue)
        {
            _ocrModifiers = (VirtualKeyModifiers)savedModifiers.Value;
            _ocrKey = (VirtualKey)savedKey.Value;
            
            // Register the loaded hotkey
            if (_ocrModifiers != VirtualKeyModifiers.None && _ocrKey != VirtualKey.None)
            {
                _hotkeyManager.RegisterOcrHotkey(_ocrModifiers, _ocrKey);
            }
        }
        
        // Load OCR Language setting
        LoadSavedLanguage();
    }

    private void LoadSavedLanguage()
    {
        var savedLanguageTag = _settingsService.ReadSetting<string>(OcrLanguageTagKey);
        var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
        
        if (!string.IsNullOrEmpty(savedLanguageTag))
        {
            // Try to find the saved language in available languages
            var savedLanguage = availableLanguages.FirstOrDefault(l => l.LanguageTag == savedLanguageTag);
            
            if (savedLanguage != null)
            {
                _selectedLanguage = savedLanguage;
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
        var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        _selectedLanguage = englishLang ?? availableLanguages.FirstOrDefault();
    }

    // OCR capture execution method
    public void LaunchOcrCapture()
    {
        try
        {
            // Pre-capture desktop screenshot for background
            var backgroundImage = Delete_Newline.Helpers.ImageHelper.GetFullDesktopScreenshotAsImageSource();
            
            // Create OCR window
            var ocrWindow = new OcrCaptureWindow();
            
            // Setup fullscreen capture with preloaded background and selected language
            ocrWindow.SetupFullscreen(backgroundImage, _selectedLanguage, false);
            ocrWindow.Activate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating OCR window: {ex.Message}");
        }
    }

    // Public getters for OCRViewModel to access current settings
    public VirtualKeyModifiers OcrModifiers => _ocrModifiers;
    public VirtualKey OcrKey => _ocrKey;
    public Language? SelectedLanguage => _selectedLanguage;

    // Methods for OCRViewModel to update settings
    public async Task UpdateHotkeyAsync(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        _ocrModifiers = modifiers;
        _ocrKey = key;
        
        try
        {
            await _settingsService.SaveSettingAsync(OcrHotkeyModifiersKey, (int)modifiers);
            await _settingsService.SaveSettingAsync(OcrHotkeyKeyKey, (int)key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR hotkey: {ex.Message}");
        }
    }

    public async Task UpdateLanguageAsync(Language language)
    {
        _selectedLanguage = language;
        
        try
        {
            await _settingsService.SaveSettingAsync(OcrLanguageTagKey, language.LanguageTag);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR Language: {ex.Message}");
        }
    }
} 