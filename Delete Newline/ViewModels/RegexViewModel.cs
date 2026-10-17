using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Delete_Newline.Contracts.Services;
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
    private readonly RegexCollectSaveService _regexCollectSaveService;
    private readonly INavigationService _navigationService;
    private CancellationTokenSource? _previewCancellation;
    private readonly HashSet<ChainItem> _subscribedItems = [];
    private bool _synchronizingTestInput;

    [ObservableProperty]
    private RegexPageStructure? _currentRegexConfig;

    [ObservableProperty]
    private string? _displayHotkey;

    [ObservableProperty]
    private string? _regexOutputText;

    private int _testInputLineEndingIndex;

    public int TestInputLineEndingIndex
    {
        get => _testInputLineEndingIndex;
        set
        {
            if (!Enum.IsDefined((TestInputLineEnding)value)) return;
            if (SetProperty(ref _testInputLineEndingIndex, value)) OnTestInputLineEndingIndexChanged(value);
        }
    }

    // Keep TextBox's CR-only representation out of the saved sample and regex engine.
    public string TestInputText => RegexTestInput.ToEditor(CurrentRegexConfig?.InputText ?? string.Empty);

    public RegexViewModel(
        InAppNotificationService inAppNotificationService,
        RegexCollectSaveService regexCollectSaveService,
        INavigationService navigationService)
    {
        _inAppNotificationService = inAppNotificationService;
        _regexCollectSaveService = regexCollectSaveService;
        _navigationService = navigationService;
        _regexCollectSaveService.RegexConfigs.CollectionChanged += RegexConfigs_CollectionChanged;
    }

    private void RegexConfigs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateCurrentRegexConfigIfMissing();
    }

    private void InvalidateCurrentRegexConfigIfMissing()
    {
        if (CurrentRegexConfig == null || _regexCollectSaveService.RegexConfigs.Contains(CurrentRegexConfig))
        {
            return;
        }

        CurrentRegexConfig = null;
        _navigationService.RemoveHistoryEntries(typeof(RegexViewModel).FullName!);
        if (_navigationService.IsCurrentPage(typeof(RegexViewModel).FullName!))
        {
            _navigationService.NavigateTo(typeof(RegexCollectViewModel).FullName!, clearNavigation: true);
        }
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
            foreach (ChainItem item in _subscribedItems)
            {
                item.PropertyChanged -= ChainItem_PropertyChanged;
            }
            _subscribedItems.Clear();
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
                    _subscribedItems.Add(item);
                }
                newValue.RegexChain.ChainItems.CollectionChanged += RegexChain_CollectionChanged;
            }
        }
        SynchronizeTestInput();
        UpdateDisplayHotkey();
        UpdateRegexOutput();
    }

    private void CurrentRegexConfig_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_synchronizingTestInput) return;

        if (e.PropertyName == nameof(RegexPageStructure.TestInputLineEnding))
        {
            SynchronizeTestInput();
        }
        if (e.PropertyName == nameof(RegexPageStructure.InputText))
        {
            SynchronizeTestInput();
            UpdateRegexOutput();
        }
    }

    private void SynchronizeTestInput()
    {
        if (CurrentRegexConfig is not { } config)
        {
            OnPropertyChanged(nameof(TestInputText));
            return;
        }

        _synchronizingTestInput = true;
        try
        {
            string input = config.InputText ?? string.Empty;
            bool legacy = config.TestInputLineEnding == null;
            TestInputLineEnding lineEnding = config.TestInputLineEnding is { } saved && Enum.IsDefined(saved)
                ? saved
                : RegexTestInput.Detect(input, legacyTextBox: true);
            TestInputLineEndingIndex = (int)lineEnding;
            config.TestInputLineEnding = lineEnding;

            // Repair CR-only samples saved by the old TextBox binding once. Explicit CR
            // samples (including those supplied by MCP) retain their selected format.
            if (legacy && input.Contains('\r') && !input.Contains('\n'))
            {
                config.InputText = RegexTestInput.Normalize(input, lineEnding);
            }
        }
        finally
        {
            _synchronizingTestInput = false;
        }
        OnPropertyChanged(nameof(TestInputText));
    }

    public void UpdateTestInput(string editorText)
    {
        if (CurrentRegexConfig is not { } config || editorText == TestInputText) return;
        config.InputText = RegexTestInput.Normalize(editorText, (TestInputLineEnding)TestInputLineEndingIndex);
    }

    private void OnTestInputLineEndingIndexChanged(int value)
    {
        if (_synchronizingTestInput || !Enum.IsDefined((TestInputLineEnding)value) || CurrentRegexConfig is not { } config) return;

        _synchronizingTestInput = true;
        try
        {
            config.TestInputLineEnding = (TestInputLineEnding)value;
            config.InputText = RegexTestInput.Normalize(config.InputText ?? string.Empty, (TestInputLineEnding)value);
        }
        finally
        {
            _synchronizingTestInput = false;
        }
        OnPropertyChanged(nameof(TestInputText));
        UpdateRegexOutput();
    }

    private void RegexChain_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Clear() raises Reset without OldItems; reconcile subscriptions to avoid retaining deleted rules.
        foreach (ChainItem item in _subscribedItems)
        {
            item.PropertyChanged -= ChainItem_PropertyChanged;
        }
        _subscribedItems.Clear();
        if (CurrentRegexConfig?.RegexChain != null)
        {
            foreach (ChainItem item in CurrentRegexConfig.RegexChain.ChainItems)
            {
                item.PropertyChanged += ChainItem_PropertyChanged;
                _subscribedItems.Add(item);
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

    private void UpdateRegexOutput()
    {
        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _previewCancellation = null;
        if (CurrentRegexConfig == null || CurrentRegexConfig.RegexChain == null)
        {
            RegexOutputText = string.Empty;
            return;
        }

        string input = CurrentRegexConfig.InputText ?? string.Empty;
        ChainItem[] rules = CurrentRegexConfig.RegexChain.ChainItems.Select(item => new ChainItem
        {
            RegexExpression = item.RegexExpression,
            Replace = item.Replace
        }).ToArray();
        _previewCancellation = new CancellationTokenSource();
        _ = UpdateRegexOutputAsync(input, rules, _previewCancellation.Token);
    }

    private async Task UpdateRegexOutputAsync(string input, ChainItem[] rules, CancellationToken cancellationToken)
    {
        string invalidMessage = LocalizationHelper.GetLocalizedString("RegexService_InvalidRegex_Message");
        try
        {
            await Task.Delay(150, cancellationToken);
            string result = await Task.Run(() => RegexTextProcessor.ApplyRegexChain(input, rules, invalidMessage, cancellationToken), cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
            {
                RegexOutputText = result;
            }
        }
        catch (OperationCanceledException)
        {
            // A newer edit owns the preview.
        }
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

