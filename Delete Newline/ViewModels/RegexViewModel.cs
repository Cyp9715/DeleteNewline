using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using System.Collections.Specialized;

namespace Delete_Newline.ViewModels;
public partial class RegexViewModel : ObservableRecipient
{
    private readonly RegexCollectSaveService _regexCollectSaveService;
    private readonly InAppNotificationService _inAppNotificationService;
    private Button? _dummyFocusButton;
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private RegexPageStructure? _currentRegexConfig;

    [ObservableProperty]
    private string? _displayHotkey;

    [ObservableProperty]
    private string? _regexOutputText;

    public RegexViewModel(
        RegexCollectSaveService regexCollectSaveService,
        InAppNotificationService inAppNotificationService)
    {
        _regexCollectSaveService = regexCollectSaveService;
        _inAppNotificationService = inAppNotificationService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    public void SetDummyFocusButton(Button btn)
    {
        _dummyFocusButton = btn;
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
        if (CurrentRegexConfig != null)
        {
            HotkeyRegister.UnregisterHotkey((CurrentRegexConfig.Hotkey.Modifiers, CurrentRegexConfig.Hotkey.Key));
        }

        if(!HotkeyValidator.ValidateHotkey(args, _inAppNotificationService))
        {
            return; // Exit if validation fails
        }

        // Register new hotkey
        if (HotkeyRegister.RegisterHotkey((args.Modifiers, args.Key)))
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

            CurrentRegexConfig!.Hotkey.Modifiers = tempModifiers;
            CurrentRegexConfig.Hotkey.Key = args.Key;
            CurrentRegexConfig.IsRegistrationFailed = false;

            //// Move focus to dummy button to remove focus from TextBox
            //if (_dummyFocusButton != null && _dispatcherQueue != null)
            //{
            //    _dispatcherQueue.TryEnqueue(() =>
            //    {
            //        _dummyFocusButton.Focus(FocusState.Programmatic);
            //    });
            //}
        }
        else
        {
            // Reset hotkey to None when registration fails
            CurrentRegexConfig!.Hotkey.Modifiers = VirtualKeyModifiers.None;
            CurrentRegexConfig.Hotkey.Key = VirtualKey.None;
            CurrentRegexConfig.IsRegistrationFailed = true;
            
            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_HotkeyRegistrationFailed_Title",
                messageKey: "Notification_HotkeyRegistrationFailed_InUse_Message",
                severity: InfoBarSeverity.Error
            );
        }
    }
}

