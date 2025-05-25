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
    private const string OcrApplyRegexKey = "OCR_ApplyRegex";

    // OCR settings
    private VirtualKeyModifiers _ocrModifiers = VirtualKeyModifiers.None;
    private VirtualKey _ocrKey = VirtualKey.None;
    private bool _applyRegex;
    private Language? _selectedLanguage;

    // OCR → Hotkey matching state
    private string? _lastOcrResult = null;

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
        
        // Load Auto Apply Hotkey setting
        _applyRegex = _settingsService.ReadSetting<bool?>(OcrApplyRegexKey) ?? false;
        
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
            ocrWindow.SetupFullscreen(backgroundImage, _selectedLanguage, _applyRegex);
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
    public bool ApplyRegex => _applyRegex;
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

    public async Task UpdateApplyRegexAsync(bool applyRegex)
    {
        _applyRegex = applyRegex;
        
        try
        {
            await _settingsService.SaveSettingAsync(OcrApplyRegexKey, applyRegex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR Apply Regex: {ex.Message}");
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

    // Called when a hotkey is triggered - checks for OCR → Hotkey matching
    public bool TryApplyOcrToHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        if (!_applyRegex || string.IsNullOrWhiteSpace(_lastOcrResult))
        {
            System.Diagnostics.Debug.WriteLine("TryApplyOcrToHotkey: Conditions not met, returning false");
            return false;
        }

        // Find the matching hotkey configuration
        var matchingHotkey = _hotkeyCollectSaveService.RegexConfigs.FirstOrDefault(h => 
            h.Hotkey?.Modifiers == modifiers && h.Hotkey?.Key == key);

        if (matchingHotkey?.RegexChain == null)
        {
            System.Diagnostics.Debug.WriteLine("TryApplyOcrToHotkey: No matching hotkey configuration found");
            return false;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"Applying regex chain for hotkey: {matchingHotkey.HotkeyName}");
            
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

            _lastOcrResult = null;
            System.Diagnostics.Debug.WriteLine("OCR → Hotkey processing completed successfully");

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in TryApplyOcrToHotkey: {ex.Message}");
            _lastOcrResult = null;
            return false;
        }
    }

    // Show OCR → Hotkey success notification
    public void ShowOcrRegexNotification()
    {
        _notificationService.ShowSystemNotification(
            titleKey: "Notification_OcrHotkeyApplied_Title",
            messageKey: "Notification_OcrHotkeyApplied_Message",
            force: false,
            addTag: true
        );
    }

    // Called when OCR is completed - sets up the matching window
    public void NotifyOcrCompleted(string ocrResult)
    {
        System.Diagnostics.Debug.WriteLine($"=== NotifyOcrCompleted called ===");
        System.Diagnostics.Debug.WriteLine($"ApplyRegex: {_applyRegex}");
        System.Diagnostics.Debug.WriteLine($"OCR Result: '{ocrResult}'");
        
        if (!_applyRegex)
        {
            System.Diagnostics.Debug.WriteLine("❌ OCR → Hotkey feature is DISABLED. Please enable 'Apply Regular Expressions' in OCR page settings!");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(ocrResult))
        {
            System.Diagnostics.Debug.WriteLine("❌ OCR result is empty or whitespace");
            return;
        }

        _lastOcrResult = ocrResult;
        System.Diagnostics.Debug.WriteLine($"✅ OCR state set: _lastOcrResult='{_lastOcrResult}'");
    }
} 