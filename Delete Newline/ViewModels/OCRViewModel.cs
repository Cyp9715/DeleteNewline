using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Windows.Globalization;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Delete_Newline.ViewModels;

public partial class OCRViewModel : ObservableRecipient
{
    private readonly HotkeyRegisterService _hotkeyManager;
    private readonly InAppNotificationService _inAppNotificationService;
    private readonly SettingsService _settingsService;
    private readonly OCRService _ocrService;
    private Button? _dummyFocusButton;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private Language? _selectedLanguage;

    [ObservableProperty]
    private string? _displayHotkey;

    // OCR dedicated hotkey settings (synchronized with OCRService)
    private HotkeyStructure _ocrHotkey = new HotkeyStructure { Modifiers = VirtualKeyModifiers.None, Key = VirtualKey.None };

    public OCRViewModel(HotkeyRegisterService hotkeyManager, InAppNotificationService notificationService, SettingsService settingsService, OCRService ocrService)
    {
        _hotkeyManager = hotkeyManager;
        _inAppNotificationService = notificationService;
        _settingsService = settingsService;
        _ocrService = ocrService;
        
        try
        {
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        }
        catch
        {
            // If not on UI thread, set to null
            _dispatcherQueue = null;
        }
        
        // Initialize settings from OCRService
        Initialize();
    }

    public void Initialize()
    {
        // Synchronize with OCRService settings
        SyncWithOCRService();
    }

    private void SyncWithOCRService()
    {
        // Get current settings from OCRService
        _ocrHotkey = _ocrService.OcrHotkey;
        SelectedLanguage = _ocrService.SelectedLanguage;
        
        // Update display hotkey
        UpdateDisplayHotkey();
    }

    private void UpdateDisplayHotkey()
    {
        DisplayHotkey = HotkeyHelper.GetDisplayText(_ocrHotkey.Modifiers, _ocrHotkey.Key);
    }

    private async Task SaveOcrHotkeyAsync()
    {
        await _ocrService.UpdateHotkeyAsync(_ocrHotkey);
        // Update display after saving
        UpdateDisplayHotkey();
    }

    private async Task SaveOcrLanguageAsync()
    {
        if (SelectedLanguage != null)
        {
            await _ocrService.UpdateLanguageAsync(SelectedLanguage);
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
    public void ProcessKeyInput(KeyboardInputEventArgs args)
    {
        // Ignore if not in hotkey registration mode
        if (!_hotkeyManager.IsRegisteringHotkey())
        {
            return;
        }

        // Check for forbidden hotkey combinations using centralized validation
        if (HotkeyHelper.IsShiftAlone(args.Modifiers))
        {
            var (titleKey, messageKey) = HotkeyHelper.GetForbiddenHotkeyError(args.Modifiers, args.Key);
            _inAppNotificationService.ShowInAppNotification(
                titleKey: titleKey,
                messageKey: messageKey,
                severity: InfoBarSeverity.Error
            );
            return;
        }

        // Unregister previous OCR hotkey if exists
        if (_ocrHotkey.Modifiers != VirtualKeyModifiers.None && _ocrHotkey.Key != VirtualKey.None)
        {
            _hotkeyManager.UnregisterOcrHotkey();
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
        if (_hotkeyManager.RegisterOcrHotkey(args.Modifiers, args.Key))
        {
            _ocrHotkey.Modifiers = args.Modifiers;
            _ocrHotkey.Key = args.Key;

            // Save the new hotkey settings
            _ = SaveOcrHotkeyAsync();

            // Move focus to dummy button to remove focus from TextBox
            if (_dummyFocusButton != null && _dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        _dummyFocusButton.Focus(FocusState.Programmatic);
                    }
                    catch
                    {
                        // Ignore focus move failure
                    }
                    finally
                    {
                        _hotkeyManager.EndHotkeyRegistration();
                    }
                });
            }
            else
            {
                _hotkeyManager.EndHotkeyRegistration();
            }
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

    public void StartHotkeyRegistration()
    {
        _hotkeyManager.StartHotkeyRegistration();
    }

    public void EndHotkeyRegistration()
    {
        _hotkeyManager.EndHotkeyRegistration();
    }
} 