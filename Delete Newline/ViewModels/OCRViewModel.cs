using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Delete_Newline.Views;
using Windows.Globalization;
using Windows.Media.Ocr;
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

    // Settings keys (kept for compatibility, but OCRService handles the actual persistence)
    private const string OcrHotkeyModifiersKey = "OCR_HotkeyModifiers";
    private const string OcrHotkeyKeyKey = "OCR_HotkeyKey";
    private const string OcrSingleLineModeKey = "OCR_SingleLineMode";
    private const string OcrLanguageTagKey = "OCR_LanguageTag";

    [ObservableProperty]
    private string? _displayHotkey;
    
    [ObservableProperty]
    private Language? _selectedLanguage;

    [ObservableProperty]
    private bool _isSingleLineMode;

    // OCR dedicated hotkey settings (synchronized with OCRService)
    private VirtualKeyModifiers _ocrModifiers = VirtualKeyModifiers.None;
    private VirtualKey _ocrKey = VirtualKey.None;

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
        
        // Initialize display with default values
        DisplayHotkey = "Click to set hotkey";
    }

    public void Initialize()
    {
        // Synchronize with OCRService settings
        SyncWithOCRService();
        System.Diagnostics.Debug.WriteLine("OCRViewModel synchronized with OCRService");
    }

    private void SyncWithOCRService()
    {
        // Get current settings from OCRService
        _ocrModifiers = _ocrService.OcrModifiers;
        _ocrKey = _ocrService.OcrKey;
        IsSingleLineMode = _ocrService.IsSingleLineMode;
        SelectedLanguage = _ocrService.SelectedLanguage;
        
        UpdateDisplayHotkey();
    }

    private async Task SaveOcrHotkeyAsync()
    {
        await _ocrService.UpdateHotkeyAsync(_ocrModifiers, _ocrKey);
    }

    private async Task SaveSingleLineModeAsync()
    {
        await _ocrService.UpdateSingleLineModeAsync(IsSingleLineMode);
    }

    private async Task SaveOcrLanguageAsync()
    {
        if (SelectedLanguage != null)
        {
            await _ocrService.UpdateLanguageAsync(SelectedLanguage);
        }
    }

    // Handle Single Line Mode changes
    partial void OnIsSingleLineModeChanged(bool value)
    {
        _ = SaveSingleLineModeAsync();
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

    private void UpdateDisplayHotkey()
    {
        if (_ocrModifiers == VirtualKeyModifiers.None && _ocrKey == VirtualKey.None)
        {
            DisplayHotkey = "Click to set hotkey";
            return;
        }

        // Create temporary HotkeyStructure to use existing FormatHotkey method
        var tempHotkey = new HotkeyStructure
        {
            Modifiers = _ocrModifiers,
            Key = _ocrKey
        };
        
        DisplayHotkey = HotkeyDisplayHelper.FormatHotkey(tempHotkey);
    }

    [RelayCommand]
    public void ProcessKeyInput(KeyboardInputEventArgs args)
    {
        // Ignore if not in hotkey registration mode
        if (!_hotkeyManager.IsRegisteringHotkey())
        {
            return;
        }

        // Prevent using Shift key alone as it conflicts with many system shortcuts
        if (args.Modifiers == VirtualKeyModifiers.Shift)
        {
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_InvalidHotkey_ShiftAlone_NotAllowed_Title",
                messageKey: "Notification_InvalidHotkey_ShiftAlone_NotAllowed_Message",
                severity: InfoBarSeverity.Error
            );
            return;
        }

        // Unregister previous OCR hotkey if exists
        if (_ocrModifiers != VirtualKeyModifiers.None && _ocrKey != VirtualKey.None)
        {
            _hotkeyManager.UnregisterOcrHotkey();
        }

        // Check for system hotkey
        if (_hotkeyManager.IsSystemHotkey((args.Modifiers, args.Key)))
        {
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_InvalidHotkey_Title",
                messageKey: "Notification_InvalidHotkey_SystemKey_Message",
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
            VirtualKeyModifiers tempModifiers = VirtualKeyModifiers.None;

            if (args.Modifiers.HasFlag(VirtualKeyModifiers.Control))
                tempModifiers |= VirtualKeyModifiers.Control;
            if (args.Modifiers.HasFlag(VirtualKeyModifiers.Menu))
                tempModifiers |= VirtualKeyModifiers.Menu;
            if (args.Modifiers.HasFlag(VirtualKeyModifiers.Shift))
                tempModifiers |= VirtualKeyModifiers.Shift;
            if (args.Modifiers.HasFlag(VirtualKeyModifiers.Windows))
                tempModifiers |= VirtualKeyModifiers.Windows;

            _ocrModifiers = tempModifiers;
            _ocrKey = args.Key;

            UpdateDisplayHotkey();

            // Save the new hotkey settings
            _ = SaveOcrHotkeyAsync();

            // Show success notification
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_HotkeySaved_Title",
                messageKey: "Notification_HotkeySaved_Message",
                severity: InfoBarSeverity.Success
            );
            
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
            _ocrModifiers = VirtualKeyModifiers.None;
            _ocrKey = VirtualKey.None;
            UpdateDisplayHotkey();
            
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
    
    // OCR capture execution method
    public void LaunchOcrCapture()
    {
        _ocrService.LaunchOcrCapture();
    }
} 