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

    public OCRViewModel(
        InAppNotificationService notificationService, 
        SettingsService settingsService)
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
        // Unregister previous OCR hotkey if exists
        if (_ocrHotkey.Modifiers != VirtualKeyModifiers.None && _ocrHotkey.Key != VirtualKey.None)
        {
            HotkeyRegister.UnregisterHotkey((_ocrHotkey.Modifiers, _ocrHotkey.Key), HotkeyType.Ocr);
        }

        if(!HotkeyValidator.ValidateHotkey(args, _inAppNotificationService))
        {
            return; // Exit if validation fails
        }

        // Register new OCR hotkey
        if (HotkeyRegister.RegisterHotkey((args.Modifiers, args.Key), HotkeyType.Ocr))
        {
            _ocrHotkey.Modifiers = args.Modifiers;
            _ocrHotkey.Key = args.Key;

            // Save the new hotkey settings
            _ = SaveOcrHotkeyAsync();

        }

        //// Move focus to dummy button to remove focus from TextBox
        //if (_dummyFocusButton != null && _dispatcherQueue != null)
        //{
        //    _dispatcherQueue.TryEnqueue(() =>
        //    {
        //        _dummyFocusButton.Focus(FocusState.Programmatic);
        //    });
        //}
    }
} 