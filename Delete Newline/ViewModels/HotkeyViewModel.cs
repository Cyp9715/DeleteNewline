using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Windows.System;

namespace Delete_Newline.ViewModels;
public partial class HotkeyViewModel : ObservableRecipient
{
    [ObservableProperty]
    private HotkeyPageStructure? _currentHotkeyConfig;

    [ObservableProperty]
    private string? _displayHotkey;

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

        CurrentHotkeyConfig.RegexChain.RemoveChainItem(item);
    }

    [RelayCommand]
    public void HandleKeyboardAccelerator(KeyboardAcceleratorEventArgs args)
    {
        var HotkeyManager = App.GetService<HotkeyRegisterService>();

        if (CurrentHotkeyConfig!.Hotkey!.Modifiers != VirtualKeyModifiers.None &&
            CurrentHotkeyConfig!.Hotkey!.Key != VirtualKey.None)
        {
            HotkeyManager.UnRegisterHotkey((CurrentHotkeyConfig.Hotkey.Modifiers, CurrentHotkeyConfig.Hotkey.Key));
        }

        if (HotkeyManager.RegisterHotkey((args.Modifiers, args.Key)))
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
            
            // DisplayHotkey will be updated via property changed event
        }
        else
        {
            // Reset Hotkey to None when registration fails
            CurrentHotkeyConfig.Hotkey.Modifiers = VirtualKeyModifiers.None;
            CurrentHotkeyConfig.Hotkey.Key = VirtualKey.None;
            
            // Show error notification
            App.GetService<NotificationService>().ShowNotification(
                "Hotkey Registration Failed",
                "This Hotkey combination is already in use by another application.",
                force: true
            );
        }
    }
}

