using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Delete_Newline.ViewModels;
public partial class HotkeyViewModel : ObservableRecipient
{
    private readonly HotkeyRegisterService _hotkeyManager;
    private readonly InAppNotificationService _notificationService;
    private TextBox? _hotkeyTextBox;
    private Button? _dummyFocusButton;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private HotkeyPageStructure? _currentHotkeyConfig;

    [ObservableProperty]
    private string? _displayHotkey;

    public HotkeyViewModel(HotkeyRegisterService hotkeyManager, InAppNotificationService notificationService)
    {
        _hotkeyManager = hotkeyManager;
        _notificationService = notificationService;
        try
        {
            _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        }
        catch
        {
            // If not on UI thread, set to null
            _dispatcherQueue = null;
        }
    }

    public void SetHotkeyTextBox(TextBox textBox)
    {
        _hotkeyTextBox = textBox;
    }

    public void SetDummyFocusButton(Button btn)
    {
        _dummyFocusButton = btn;
    }

    partial void OnCurrentHotkeyConfigChanged(HotkeyPageStructure? oldValue, HotkeyPageStructure? newValue)
    {
        if (oldValue?.Hotkey != null)
        {
            oldValue.Hotkey.PropertyChanged -= OnHotkeyPropertyChanged;
        }

        if (newValue?.Hotkey != null)
        {
            newValue.Hotkey.PropertyChanged += OnHotkeyPropertyChanged;
        }
        UpdateDisplayHotkey();
    }

    private void OnHotkeyPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateDisplayHotkey();
    }

    private void UpdateDisplayHotkey()
    {
        if (CurrentHotkeyConfig?.Hotkey == null)
        {
            DisplayHotkey = string.Empty;
            return;
        }

        DisplayHotkey = HotkeyDisplayHelper.GetDisplayText(CurrentHotkeyConfig);
    }

    [RelayCommand]
    private void AddRegexItem()
    {
        if (CurrentHotkeyConfig is null || CurrentHotkeyConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentHotkeyConfig is null.");
        }

        CurrentHotkeyConfig.RegexChain.AddChainItem();
    }

    [RelayCommand]
    private void RemoveRegexItem(ChainItem item)
    {
        if (CurrentHotkeyConfig is null || CurrentHotkeyConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentHotkeyConfig is null.");
        }

        // Clear the properties before removing to ensure proper UI update
        item.RegexExpression = null;
        item.Replace = null;
        
        CurrentHotkeyConfig.RegexChain.RemoveChainItem(item);
    }

    [RelayCommand]
    public void HandleKeyboardAccelerator(KeyboardAcceleratorEventArgs args)
    {
        // Ignore if not in hotkey registration mode
        if (!_hotkeyManager.IsRegisteringHotkey())
        {
            return;
        }

        // Unregister previous hotkey if exists
        if (CurrentHotkeyConfig!.Hotkey!.Modifiers != VirtualKeyModifiers.None &&
            CurrentHotkeyConfig!.Hotkey!.Key != VirtualKey.None)
        {
            _hotkeyManager.UnRegisterHotkey((CurrentHotkeyConfig.Hotkey.Modifiers, CurrentHotkeyConfig.Hotkey.Key));
        }

        // Check for system hotkey
        if (_hotkeyManager.IsSystemHotkey((args.Modifiers, args.Key)))
        {
            _notificationService.ShowNotification(
                "Invalid Hotkey",
                "System hotkeys (Ctrl+C, Ctrl+V, etc.) cannot be registered.",
                InfoBarSeverity.Error
            );
            return;
        }

        // Check if hotkey is already registered
        if (_hotkeyManager.IsHotkeyRegistered((args.Modifiers, args.Key)))
        {
            _notificationService.ShowNotification(
                "Invalid Hotkey",
                "This hotkey combination is already registered.",
                InfoBarSeverity.Error
            );
            return;
        }

        // Register new hotkey
        if (_hotkeyManager.RegisterHotkey((args.Modifiers, args.Key)))
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

            CurrentHotkeyConfig.Hotkey.Modifiers = tempModifiers;
            CurrentHotkeyConfig.Hotkey.Key = args.Key;
            
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
            CurrentHotkeyConfig.Hotkey.Modifiers = VirtualKeyModifiers.None;
            CurrentHotkeyConfig.Hotkey.Key = VirtualKey.None;
            
            _notificationService.ShowNotification(
                "Hotkey Registration Failed",
                "This hotkey combination is already in use by another application.",
                InfoBarSeverity.Error
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

