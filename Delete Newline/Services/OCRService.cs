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
    private readonly HotkeyCollectSaveService _hotkeyCollectSaveService;
    private readonly RegexService _regexService;
    private readonly NotificationService _notificationService;

    // Settings keys
    private const string OcrHotkeyModifiersKey = "OCR_HotkeyModifiers";
    private const string OcrHotkeyKeyKey = "OCR_HotkeyKey";
    private const string OcrLanguageTagKey = "OCR_LanguageTag";
    private const string OcrAutoApplyHotkeyKey = "OCR_AutoApplyHotkey";

    // OCR settings
    private VirtualKeyModifiers _ocrModifiers = VirtualKeyModifiers.None;
    private VirtualKey _ocrKey = VirtualKey.None;
    private bool _autoApplyHotkey;
    private Language? _selectedLanguage;

    // OCR → Hotkey matching state
    private bool _ocrJustCompleted = false;
    private string? _lastOcrResult = null;

    public OCRService(HotkeyRegisterService hotkeyManager, InAppNotificationService inAppNotificationService, SettingsService settingsService, HotkeyCollectSaveService hotkeyCollectSaveService, RegexService regexService, NotificationService notificationService)
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
        
        // Load Auto Apply Hotkey setting
        _autoApplyHotkey = _settingsService.ReadSetting<bool?>(OcrAutoApplyHotkeyKey) ?? false;
        
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
            
            // Setup fullscreen capture with preloaded background, selected language, and auto apply hotkey setting
            ocrWindow.SetupFullscreen(backgroundImage, _selectedLanguage, _autoApplyHotkey);
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
    public bool AutoApplyHotkey => _autoApplyHotkey;
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

    public async Task UpdateAutoApplyHotkeyAsync(bool autoApplyHotkey)
    {
        _autoApplyHotkey = autoApplyHotkey;
        
        try
        {
            await _settingsService.SaveSettingAsync(OcrAutoApplyHotkeyKey, autoApplyHotkey);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR Auto Apply Hotkey: {ex.Message}");
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

    // Called when OCR is completed - sets up the matching window
    public void NotifyOcrCompleted(string ocrResult)
    {
        if (!_autoApplyHotkey || string.IsNullOrWhiteSpace(ocrResult))
        {
            return;
        }

        _ocrJustCompleted = true;
        _lastOcrResult = ocrResult;
    }

    // Called when user input is detected (typing, mouse clicks) - disables matching
    public void NotifyUserInputDetected()
    {
        if (_ocrJustCompleted)
        {
            _ocrJustCompleted = false;
            _lastOcrResult = null;
        }
    }

    // Called when a hotkey is triggered - checks for OCR → Hotkey matching
    public bool TryApplyOcrToHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        // Check if this is the OCR hotkey itself - should not be processed as regex hotkey
        if (_ocrModifiers == modifiers && _ocrKey == key)
        {
            _ocrJustCompleted = false;
            _lastOcrResult = null;
            return false;
        }

        if (!_autoApplyHotkey || !_ocrJustCompleted || string.IsNullOrWhiteSpace(_lastOcrResult))
        {
            return false;
        }

        // Find the matching hotkey configuration
        var matchingHotkey = _hotkeyCollectSaveService.HotkeyConfigs.FirstOrDefault(h => 
            h.Hotkey?.Modifiers == modifiers && h.Hotkey?.Key == key);

        if (matchingHotkey?.RegexChain == null)
        {
            return false;
        }

        try
        {
            // Apply the regex chain to the OCR result
            string processedText = _lastOcrResult;
            foreach (var chainItem in matchingHotkey.RegexChain.ChainItems)
            {
                processedText = RegexService.ProcessRegex(processedText, chainItem.RegexExpression, chainItem.Replace);
            }

            // Set the processed text to clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(processedText);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            // Clear the matching state
            _ocrJustCompleted = false;
            _lastOcrResult = null;

            return true;
        }
        catch (Exception)
        {
            _ocrJustCompleted = false;
            _lastOcrResult = null;
            return false;
        }
    }

    // Show OCR → Hotkey success notification
    public void ShowOcrHotkeyNotification()
    {
        _notificationService.ShowSystemNotification(
            titleKey: "Notification_OcrHotkeyApplied_Title",
            messageKey: "Notification_OcrHotkeyApplied_Message",
            force: false,
            addTag: true
        );
    }
} 