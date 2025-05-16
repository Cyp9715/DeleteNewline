using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

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

    [ObservableProperty]
    private string? _regexOutputText;

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
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= CurrentHotkeyConfig_PropertyChanged;
            if (oldValue.Hotkey != null)
            {
                oldValue.Hotkey.PropertyChanged -= OnHotkeyPropertyChanged;
            }
            if (oldValue.RegexChain != null)
            {
                oldValue.RegexChain.ChainItems.CollectionChanged -= RegexChain_CollectionChanged;
                foreach (var item in oldValue.RegexChain.ChainItems)
                {
                    item.PropertyChanged -= ChainItem_PropertyChanged;
                }
            }
        }

        if (newValue != null)
        {
            newValue.PropertyChanged += CurrentHotkeyConfig_PropertyChanged;
            if (newValue.Hotkey != null)
            {
                newValue.Hotkey.PropertyChanged += OnHotkeyPropertyChanged;
            }
            if (newValue.RegexChain != null)
            {
                foreach (var item in newValue.RegexChain.ChainItems)
                {
                    item.PropertyChanged += ChainItem_PropertyChanged;
                }
                newValue.RegexChain.ChainItems.CollectionChanged += RegexChain_CollectionChanged;
            }
        }
        UpdateDisplayHotkey();
        UpdateRegexOutput();
    }

    private void CurrentHotkeyConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HotkeyPageStructure.InputText))
        {
            UpdateRegexOutput();
        }
    }

    private void RegexChain_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ChainItem item in e.OldItems)
            {
                item.PropertyChanged -= ChainItem_PropertyChanged;
            }
        }
        if (e.NewItems != null)
        {
            foreach (ChainItem item in e.NewItems)
            {
                item.PropertyChanged += ChainItem_PropertyChanged;
            }
        }
        UpdateRegexOutput();
    }

    private void ChainItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChainItem.RegexExpression) || e.PropertyName == nameof(ChainItem.Replace))
        {
            UpdateRegexOutput();
        }
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

    private void UpdateRegexOutput()
    {
        if (CurrentHotkeyConfig == null || CurrentHotkeyConfig.RegexChain == null)
        {
            RegexOutputText = string.Empty;
            return;
        }

        string currentText = CurrentHotkeyConfig.InputText ?? string.Empty;

        foreach (var chainItem in CurrentHotkeyConfig.RegexChain.ChainItems)
        {
            if (string.IsNullOrEmpty(chainItem.RegexExpression) == false)
            {
                string replacement = chainItem.Replace ?? string.Empty;
                currentText = Regex.Replace(currentText, chainItem.RegexExpression, replacement);
            }
        }
        RegexOutputText = currentText;
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
            CurrentHotkeyConfig.IsRegistrationFailed = false;
            
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
            CurrentHotkeyConfig.IsRegistrationFailed = true;
            
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

