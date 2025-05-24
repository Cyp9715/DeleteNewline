using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.System;
using Delete_Newline.Views;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Services;

public sealed class OCRService
{
    private readonly HotkeyRegisterService _hotkeyManager;
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly SettingsService _settingsService;

    // Settings keys
    private const string OcrHotkeyModifiersKey = "OCR_HotkeyModifiers";
    private const string OcrHotkeyKeyKey = "OCR_HotkeyKey";
    private const string OcrSingleLineModeKey = "OCR_SingleLineMode";
    private const string OcrLanguageTagKey = "OCR_LanguageTag";

    // OCR settings
    private VirtualKeyModifiers _ocrModifiers = VirtualKeyModifiers.None;
    private VirtualKey _ocrKey = VirtualKey.None;
    private bool _isSingleLineMode;
    private Language? _selectedLanguage;

    public OCRService(HotkeyRegisterService hotkeyManager, InAppNotificationService inAppNotificationService, SettingsService settingsService)
    {
        _hotkeyManager = hotkeyManager;
        _inAppNotificationService = inAppNotificationService;
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        LoadSettings();
        System.Diagnostics.Debug.WriteLine("OCRService initialized - global OCR hotkeys registered");
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
        
        // Load Single Line Mode setting
        _isSingleLineMode = _settingsService.ReadSetting<bool?>(OcrSingleLineModeKey) ?? false;
        
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
                System.Diagnostics.Debug.WriteLine($"Loaded saved OCR language: {savedLanguage.DisplayName} ({savedLanguage.LanguageTag})");
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
                System.Diagnostics.Debug.WriteLine($"Saved OCR language '{savedLanguageTag}' is no longer available");
            }
        }
        
        // Fall back to default language (English or first available)
        var englishLang = availableLanguages.FirstOrDefault(l => l.LanguageTag.StartsWith("en"));
        _selectedLanguage = englishLang ?? availableLanguages.FirstOrDefault();
        
        if (_selectedLanguage != null)
        {
            System.Diagnostics.Debug.WriteLine($"Using default OCR language: {_selectedLanguage.DisplayName} ({_selectedLanguage.LanguageTag})");
        }
    }

    // OCR capture execution method
    public void LaunchOcrCapture()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== Starting OCR Capture from global hotkey ===");
            System.Diagnostics.Debug.WriteLine($"Selected language: {_selectedLanguage?.DisplayName ?? "None"} ({_selectedLanguage?.LanguageTag ?? "None"})");
            System.Diagnostics.Debug.WriteLine($"Single Line Mode: {_isSingleLineMode}");
            
            // Pre-capture desktop screenshot for background
            System.Diagnostics.Debug.WriteLine("Capturing desktop screenshot...");
            var backgroundImage = Delete_Newline.Helpers.ImageHelper.GetFullDesktopScreenshotAsImageSource();
            System.Diagnostics.Debug.WriteLine("Desktop screenshot captured successfully");
            
            // Create OCR window
            var ocrWindow = new OcrCaptureWindow();
            
            // Setup fullscreen capture with preloaded background, selected language, and single line mode
            ocrWindow.SetupFullscreen(backgroundImage, _selectedLanguage, _isSingleLineMode);
            ocrWindow.Activate();
            
            System.Diagnostics.Debug.WriteLine("=== OCR Capture window created and activated ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating OCR window: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    // Public getters for OCRViewModel to access current settings
    public VirtualKeyModifiers OcrModifiers => _ocrModifiers;
    public VirtualKey OcrKey => _ocrKey;
    public bool IsSingleLineMode => _isSingleLineMode;
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
            System.Diagnostics.Debug.WriteLine($"OCR Hotkey saved: {modifiers} + {key}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR hotkey: {ex.Message}");
        }
    }

    public async Task UpdateSingleLineModeAsync(bool singleLineMode)
    {
        _isSingleLineMode = singleLineMode;
        
        try
        {
            await _settingsService.SaveSettingAsync(OcrSingleLineModeKey, singleLineMode);
            System.Diagnostics.Debug.WriteLine($"OCR Single Line Mode saved: {singleLineMode}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR Single Line Mode: {ex.Message}");
        }
    }

    public async Task UpdateLanguageAsync(Language language)
    {
        _selectedLanguage = language;
        
        try
        {
            await _settingsService.SaveSettingAsync(OcrLanguageTagKey, language.LanguageTag);
            System.Diagnostics.Debug.WriteLine($"OCR Language saved: {language.DisplayName} ({language.LanguageTag})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR Language: {ex.Message}");
        }
    }
} 