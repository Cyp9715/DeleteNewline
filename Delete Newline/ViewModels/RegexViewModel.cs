using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using System.Collections.Specialized;
using Delete_Newline.Helpers.Hotkeys;
using Delete_Newline.Helpers;

namespace Delete_Newline.ViewModels;
public partial class RegexViewModel : ObservableRecipient
{
    private readonly InAppNotificationService _inAppNotificationService;

    [ObservableProperty]
    private RegexPageStructure? _currentRegexConfig;

    [ObservableProperty]
    private string? _displayHotkey;

    [ObservableProperty]
    private string? _regexOutputText;

    public RegexViewModel(InAppNotificationService inAppNotificationService)
    {
        _inAppNotificationService = inAppNotificationService;
    }

    partial void OnCurrentRegexConfigChanged(RegexPageStructure? oldValue, RegexPageStructure? newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= CurrentRegexConfig_PropertyChanged;
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
            newValue.PropertyChanged += CurrentRegexConfig_PropertyChanged;
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

    private void CurrentRegexConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RegexPageStructure.InputText))
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
        if (CurrentRegexConfig?.Hotkey == null)
        {
            DisplayHotkey = string.Empty;
            return;
        }

        DisplayHotkey = HotkeyFormatter.GetDisplayText(CurrentRegexConfig.Hotkey.Modifiers, CurrentRegexConfig.Hotkey.Key);
    }

    // https://github.com/microsoft/microsoft-ui-xaml/issues/1826
    private void UpdateRegexOutput()
    {
        if (CurrentRegexConfig == null || CurrentRegexConfig.RegexChain == null)
        {
            RegexOutputText = string.Empty;
            return;
        }

        string currentText = CurrentRegexConfig.InputText ?? string.Empty;

        foreach (var chainItem in CurrentRegexConfig.RegexChain.ChainItems)
        {
            currentText = RegexService.ProcessRegex(currentText, chainItem.RegexExpression, chainItem.Replace);
        }
        RegexOutputText = currentText;
    }

    [RelayCommand]
    private void AddRegexItem()
    {
        if (CurrentRegexConfig is null || CurrentRegexConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentRegexConfig is null.");
        }

        CurrentRegexConfig.RegexChain.AddChainItem();
    }

    [RelayCommand]
    private void RemoveRegexItem(ChainItem item)
    {
        if (CurrentRegexConfig is null || CurrentRegexConfig.RegexChain is null)
        {
            throw new InvalidOperationException("CurrentRegexConfig is null.");
        }

        // Clear the properties before removing to ensure proper UI update
        item.RegexExpression = null;
        item.Replace = null;

        CurrentRegexConfig.RegexChain.RemoveChainItem(item);
    }

    [RelayCommand]
    public void ProcessKeyInput(KeyboardInputEventArgs args)
    {
        if (CurrentRegexConfig?.Hotkey == null) 
            return;

        if (HotkeyValidator.ValidateHotkey(args, HotkeyType.Regex, _inAppNotificationService) == false)
            return;

        var newHotkey = (args.Modifiers, args.Key);
        var oldHotkey = (CurrentRegexConfig.Hotkey.Modifiers, CurrentRegexConfig.Hotkey.Key);

        // 1. If the new hotkey is the same as the old one, do nothing.
        if (newHotkey == oldHotkey)
        {
            return;
        }

        // 2. Check if the new hotkey is already registered by another item (not the current one).
        //    To do this accurately, we must first temporarily unregister the current item's hotkey.
        HotkeyRegister.UnregisterHotkey(oldHotkey, HotkeyType.Regex);

        if (HotkeyRegister.IsHotkeyRegisteredGlobally(newHotkey))
        {
            // 3. If the hotkey is a duplicate:
            //    - Display a notification to the user.
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_HotkeyRegistrationFailed_Title",
                messageKey: "Notification_HotkeyRegistrationFailed_InUse_Message",
                severity: InfoBarSeverity.Error
            );

            //    - Crucially, re-register the old hotkey to restore the original state.
            if (oldHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey(oldHotkey, HotkeyType.Regex);
            }

            //    - Exit without modifying the model to ensure the UI keeps displaying the old value.
            //      (If the UI doesn't update immediately, you might need to raise a PropertyChanged event manually).
            //      OnPropertyChanged(nameof(DisplayHotkey)); 
            return;
        }

        // 4. If the hotkey is not a duplicate and is available for use:
        //    - Register the new hotkey (if it's not VirtualKey.None).
        bool registrationSuccess = true;
        if (newHotkey.Key != VirtualKey.None)
        {
            registrationSuccess = HotkeyRegister.RegisterHotkey(newHotkey, HotkeyType.Regex);
        }

        if (registrationSuccess)
        {
            //    - If registration is successful, update the model (CurrentRegexConfig) with the new hotkey.
            CurrentRegexConfig.Hotkey.Modifiers = newHotkey.Item1;
            CurrentRegexConfig.Hotkey.Key = newHotkey.Item2;
            CurrentRegexConfig.IsRegistrationFailed = false;
        }
        else
        {
            //    - (Edge case) If registration fails here, roll back by re-registering the old hotkey.
            CurrentRegexConfig.IsRegistrationFailed = true;

            if (oldHotkey.Key != VirtualKey.None)
            {
                HotkeyRegister.RegisterHotkey(oldHotkey, HotkeyType.Regex);
            }
        }
        // The UI updates automatically because changing the Hotkey properties will trigger a PropertyChanged notification.
    }

    [RelayCommand]
    public void GotFocusHotkeyTextBox()
    {
        HotkeyRegister.DisableAllHotkeys();
    }

    [RelayCommand]
    public void LostFocusHotkeyTextBox()
    {
        HotkeyRegister.EnableAllHotkeys();
    }
}

