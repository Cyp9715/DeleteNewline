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
    private DateTime _ocrCompletedTime = DateTime.MinValue;
    private readonly TimeSpan _ocrHotkeyMatchingWindow = TimeSpan.FromSeconds(3); // 3초 내에 매칭

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
            
            // Pre-capture desktop screenshot for background
            System.Diagnostics.Debug.WriteLine("Capturing desktop screenshot...");
            var backgroundImage = Delete_Newline.Helpers.ImageHelper.GetFullDesktopScreenshotAsImageSource();
            System.Diagnostics.Debug.WriteLine("Desktop screenshot captured successfully");
            
            // Create OCR window
            var ocrWindow = new OcrCaptureWindow();
            
            // Setup fullscreen capture with preloaded background, selected language, and auto apply hotkey setting
            ocrWindow.SetupFullscreen(backgroundImage, _selectedLanguage, _autoApplyHotkey);
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
            System.Diagnostics.Debug.WriteLine($"OCR Hotkey saved: {modifiers} + {key}");
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
            System.Diagnostics.Debug.WriteLine($"OCR Auto Apply Hotkey saved: {autoApplyHotkey}");
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
            System.Diagnostics.Debug.WriteLine($"OCR Language saved: {language.DisplayName} ({language.LanguageTag})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save OCR Language: {ex.Message}");
        }
    }

    // Called when OCR is completed - sets up the matching window
    public void NotifyOcrCompleted(string ocrResult)
    {
        System.Diagnostics.Debug.WriteLine($"=== NotifyOcrCompleted called ===");
        System.Diagnostics.Debug.WriteLine($"AutoApplyHotkey setting: {_autoApplyHotkey}");
        System.Diagnostics.Debug.WriteLine($"OCR Result: '{ocrResult}'");
        System.Diagnostics.Debug.WriteLine($"OCR Result length: {ocrResult?.Length ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Is null or whitespace: {string.IsNullOrWhiteSpace(ocrResult)}");

        if (!_autoApplyHotkey)
        {
            System.Diagnostics.Debug.WriteLine("=== AutoApplyHotkey disabled - skipping ===");
            return;
        }

        if (string.IsNullOrWhiteSpace(ocrResult))
        {
            System.Diagnostics.Debug.WriteLine("=== OCR result is empty - skipping ===");
            return;
        }

        _ocrJustCompleted = true;
        _lastOcrResult = ocrResult;
        _ocrCompletedTime = DateTime.UtcNow;

        System.Diagnostics.Debug.WriteLine($"=== OCR → Hotkey matching window activated ===");
        System.Diagnostics.Debug.WriteLine($"Stored OCR Result: '{_lastOcrResult}'");
        System.Diagnostics.Debug.WriteLine($"Completion time: {_ocrCompletedTime:HH:mm:ss.fff}");
        System.Diagnostics.Debug.WriteLine($"Matching window: {_ocrHotkeyMatchingWindow.TotalSeconds} seconds");
        System.Diagnostics.Debug.WriteLine($"Window expires at: {(_ocrCompletedTime + _ocrHotkeyMatchingWindow):HH:mm:ss.fff}");
    }

    // Called when user input is detected (typing, mouse clicks) - disables matching
    public void NotifyUserInputDetected()
    {
        if (_ocrJustCompleted)
        {
            System.Diagnostics.Debug.WriteLine("=== User input detected - OCR → Hotkey matching disabled ===");
            _ocrJustCompleted = false;
            _lastOcrResult = null;
        }
    }

    // Called when a hotkey is triggered - checks for OCR → Hotkey matching
    public bool TryApplyOcrToHotkey(VirtualKeyModifiers modifiers, VirtualKey key)
    {
        System.Diagnostics.Debug.WriteLine($"=== TryApplyOcrToHotkey called ===");
        System.Diagnostics.Debug.WriteLine($"Hotkey: {modifiers} + {key}");
        System.Diagnostics.Debug.WriteLine($"Current time: {DateTime.UtcNow:HH:mm:ss.fff}");
        System.Diagnostics.Debug.WriteLine($"AutoApplyHotkey: {_autoApplyHotkey}");
        System.Diagnostics.Debug.WriteLine($"OCR just completed: {_ocrJustCompleted}");
        System.Diagnostics.Debug.WriteLine($"Last OCR result: '{_lastOcrResult ?? "null"}'");
        System.Diagnostics.Debug.WriteLine($"OCR completed time: {_ocrCompletedTime:HH:mm:ss.fff}");

        if (!_autoApplyHotkey)
        {
            System.Diagnostics.Debug.WriteLine("=== AutoApplyHotkey disabled - not matching ===");
            return false;
        }

        if (!_ocrJustCompleted)
        {
            System.Diagnostics.Debug.WriteLine("=== OCR not recently completed - not matching ===");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_lastOcrResult))
        {
            System.Diagnostics.Debug.WriteLine("=== No OCR result stored - not matching ===");
            return false;
        }

        // Check if we're still within the matching window
        var timeSinceOcr = DateTime.UtcNow - _ocrCompletedTime;
        System.Diagnostics.Debug.WriteLine($"Time since OCR: {timeSinceOcr.TotalSeconds:F3} seconds");
        System.Diagnostics.Debug.WriteLine($"Matching window: {_ocrHotkeyMatchingWindow.TotalSeconds} seconds");
        
        if (timeSinceOcr > _ocrHotkeyMatchingWindow)
        {
            System.Diagnostics.Debug.WriteLine("=== OCR → Hotkey matching window expired ===");
            _ocrJustCompleted = false;
            _lastOcrResult = null;
            return false;
        }

        // Find the matching hotkey configuration
        var matchingHotkey = _hotkeyCollectSaveService.HotkeyConfigs.FirstOrDefault(h => 
            h.Hotkey?.Modifiers == modifiers && h.Hotkey?.Key == key);

        if (matchingHotkey?.RegexChain == null)
        {
            System.Diagnostics.Debug.WriteLine("=== No matching hotkey configuration found ===");
            return false;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"=== Applying OCR → Hotkey matching ===");
            System.Diagnostics.Debug.WriteLine($"Original OCR text: {_lastOcrResult}");
            System.Diagnostics.Debug.WriteLine($"Hotkey: {matchingHotkey.HotkeyName ?? "Unnamed"} ({modifiers} + {key})");

            // Apply the regex chain to the OCR result
            string processedText = _lastOcrResult;
            foreach (var chainItem in matchingHotkey.RegexChain.ChainItems)
            {
                processedText = RegexService.ProcessRegex(processedText, chainItem.RegexExpression, chainItem.Replace);
            }

            System.Diagnostics.Debug.WriteLine($"Processed text: {processedText}");

            // Set the processed text to clipboard
            var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dataPackage.SetText(processedText);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

            // Clear the matching state
            _ocrJustCompleted = false;
            _lastOcrResult = null;

            // Show success notification (as Windows system notification, like regular hotkeys)
            _notificationService.ShowSystemNotification(
                titleKey: "Notification_OcrHotkeyApplied_Title",
                messageKey: "Notification_OcrHotkeyApplied_Message",
                force: false,
                addTag: true
            );

            System.Diagnostics.Debug.WriteLine("=== OCR → Hotkey matching completed successfully ===");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error applying OCR → Hotkey matching: {ex.Message}");
            _ocrJustCompleted = false;
            _lastOcrResult = null;
            return false;
        }
    }
} 