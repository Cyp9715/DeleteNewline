using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using System.Text;
using Delete_Newline.Helpers;
using Delete_Newline.Helpers.Hotkeys;

namespace Delete_Newline.Services;

public sealed class RegexCollectSaveService
{
    public ObservableCollection<RegexPageStructure> RegexConfigs { get; private set; }

    private readonly SettingsFileService _localSettingsService;
    private readonly InAppNotificationService _inAppNotificationService;

    private const string RegexCollectionSettingsKey = DefaultRegexProfile.CollectionSettingsKey;
    private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1);
    private bool _suppressAutoSave;
    private CancellationTokenSource? _pendingSave;
    private readonly HashSet<ChainItem> _subscribedItems = [];

    public RegexCollectSaveService(SettingsFileService localSettingsService, 
                                    InAppNotificationService inAppNotificationService)
    {
        _localSettingsService = localSettingsService;
        _inAppNotificationService = inAppNotificationService;
        RegexConfigs = new ObservableCollection<RegexPageStructure>();
    }

    public async Task InitializeAsync()
    {
        await DefaultRegexProfile.EnsureCreatedAsync(_localSettingsService,
            LocalizationHelper.GetLocalizedString("RegexDefaultProfile_Name"),
            LocalizationHelper.GetLocalizedString("RegexDefaultProfile_Comment"));
        var savedRegexConfigs = _localSettingsService.ReadSetting<List<RegexPageStructure>>(RegexCollectionSettingsKey);

        if (savedRegexConfigs != null && savedRegexConfigs.Count > 0) // Ensure there are configs to process
        {
            this.RegexConfigs.Clear(); // Clear before re-populating
            ShowFailedRegistrationNotification(RegisterRegexConfigsAndCollectFailures(savedRegexConfigs));
        }
        
        this.RegexConfigs.CollectionChanged += OnRegexConfigsChanged;
    }

    private List<string> RegisterRegexConfigsAndCollectFailures(IEnumerable<RegexPageStructure> savedConfigs)
    {
        List<string> failedHotkeyStrings = new List<string>();

        foreach (var config in savedConfigs)
        {
            config.IsRegistrationFailed = false; // Reset status
            this.RegexConfigs.Add(config); // Add to the main collection
            Subscribe(config);

            if (config.Hotkey.Modifiers == VirtualKeyModifiers.None ||
                config.Hotkey.Key == VirtualKey.None)
            {
                continue; // Skip empty/invalid hotkeys
            }

            if (HotkeyRegister.RegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key)) == false)
            {
                config.IsRegistrationFailed = true;
                string hotkeyString = config.Hotkey.ToString(); // Or use a helper for formatted string if available
                failedHotkeyStrings.Add(hotkeyString);
            }
        }
        return failedHotkeyStrings;
    }

    private void ShowFailedRegistrationNotification(List<string> failedHotkeyStrings)
    {
        if (failedHotkeyStrings.Count > 0)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(LocalizationHelper.GetLocalizedString("Notification_HotkeyRegistrationFailed_Message_Prefix"));

            foreach (var hotkeyStr in failedHotkeyStrings)
            {
                messageBuilder.AppendLine($"- {hotkeyStr}");
            }
            messageBuilder.Append(LocalizationHelper.GetLocalizedString("Notification_HotkeyRegistrationFailed_Message_Suffix"));

            _inAppNotificationService.ShowInAppNotification(
                titleKey: "Notification_HotkeyRegistrationFailed_Title",
                messageKey: "Notification_HotkeyRegistrationFailed_Message_Body",
                severity: InfoBarSeverity.Warning,
                messageArgs: new object[] { messageBuilder.ToString() }
            );
        }
    }

    public void AddRegexConfig(RegexPageStructure? config = null)
    {
        var newConfig = config ?? new RegexPageStructure();
        RegexConfigs.Add(newConfig);
    }

    public void RemoveRegexConfig(RegexPageStructure config)
    {
        RemoveRegexConfigCore(config);
    }

    public async Task RemoveRegexConfigsAsync(IEnumerable<RegexPageStructure> configs)
    {
        RegexPageStructure[] configsToRemove = configs
            .Where(config => config is not null && RegexConfigs.Contains(config))
            .Distinct()
            .ToArray();

        if (configsToRemove.Length == 0)
        {
            return;
        }

        _suppressAutoSave = true;
        try
        {
            foreach (RegexPageStructure config in configsToRemove)
            {
                RemoveRegexConfigCore(config);
            }
        }
        finally
        {
            _suppressAutoSave = false;
        }

        await SaveSettingsAsync();
    }

    private bool RemoveRegexConfigCore(RegexPageStructure config)
    {
        if (!RegexConfigs.Remove(config))
        {
            return false;
        }

        Unsubscribe(config);
        UnregisterHotkey(config);
        return true;
    }

    private static void UnregisterHotkey(RegexPageStructure config)
    {
        if (config.Hotkey.Modifiers != VirtualKeyModifiers.None && config.Hotkey.Key != VirtualKey.None)
        {
            HotkeyRegister.UnregisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));
        }
    }

    public void ReplaceRegexConfigs(IReadOnlyList<RegexPageStructure> configs)
    {
        _suppressAutoSave = true;
        try
        {
            foreach (RegexPageStructure existing in RegexConfigs.ToList())
            {
                Unsubscribe(existing);
                if (existing.Hotkey.Modifiers != VirtualKeyModifiers.None && existing.Hotkey.Key != VirtualKey.None)
                {
                    HotkeyRegister.UnregisterHotkey((existing.Hotkey.Modifiers, existing.Hotkey.Key));
                }
            }

            RegexConfigs.Clear();

            List<string> failedHotkeyStrings = [];
            foreach (RegexPageStructure config in configs)
            {
                config.IsRegistrationFailed = false;
                RegexConfigs.Add(config);
                Subscribe(config);

                if (config.Hotkey.Modifiers == VirtualKeyModifiers.None || config.Hotkey.Key == VirtualKey.None)
                {
                    continue;
                }

                if (HotkeyRegister.RegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key)) == false)
                {
                    config.IsRegistrationFailed = true;
                    failedHotkeyStrings.Add(config.Hotkey.ToString());
                }
            }

            ShowFailedRegistrationNotification(failedHotkeyStrings);
        }
        finally
        {
            _suppressAutoSave = false;
        }
    }

    private void OnRegexConfigsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_suppressAutoSave)
        {
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (RegexPageStructure config in e.NewItems)
            {
                Subscribe(config);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (RegexPageStructure config in e.OldItems)
            {
                Unsubscribe(config);
            }
        }
        

        ScheduleSave();
    }

    private void Subscribe(RegexPageStructure config)
    {
        config.PropertyChanged += OnConfigPropertyChanged;

        if (config.Hotkey != null)
        {
            config.Hotkey.PropertyChanged += OnConfigPropertyChanged;
        }

        if (config.RegexChain != null)
        {
            config.RegexChain.ChainItems.CollectionChanged += OnChainItemsChanged;

            foreach (var item in config.RegexChain.ChainItems)
            {
                if (_subscribedItems.Add(item))
                {
                    item.PropertyChanged += OnConfigPropertyChanged;
                }
            }
        }
    }

    private void Unsubscribe(RegexPageStructure config)
    {
        config.PropertyChanged -= OnConfigPropertyChanged;

        if (config.Hotkey != null)
        {
            config.Hotkey.PropertyChanged -= OnConfigPropertyChanged;
        }

        if (config.RegexChain != null)
        {
            config.RegexChain.ChainItems.CollectionChanged -= OnChainItemsChanged;

            foreach (var item in config.RegexChain.ChainItems)
            {
                item.PropertyChanged -= OnConfigPropertyChanged;
                _subscribedItems.Remove(item);
            }
        }
    }

    private void OnChainItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (ChainItem item in _subscribedItems)
            {
                item.PropertyChanged -= OnConfigPropertyChanged;
            }
            _subscribedItems.Clear();
            foreach (ChainItem item in RegexConfigs.SelectMany(config => config.RegexChain.ChainItems))
            {
                if (_subscribedItems.Add(item))
                {
                    item.PropertyChanged += OnConfigPropertyChanged;
                }
            }
        }
        if (e.NewItems != null)
        {
            foreach (ChainItem item in e.NewItems)
            {
                if (_subscribedItems.Add(item))
                {
                    item.PropertyChanged += OnConfigPropertyChanged;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (ChainItem item in e.OldItems)
            {
                item.PropertyChanged -= OnConfigPropertyChanged;
                _subscribedItems.Remove(item);
            }
        }

        ScheduleSave();
    }

    private void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        if (_suppressAutoSave)
        {
            return;
        }
        _pendingSave?.Cancel();
        _pendingSave?.Dispose();
        _pendingSave = new CancellationTokenSource();
        _ = SaveAfterEditAsync(_pendingSave.Token);
    }

    private async Task SaveAfterEditAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(200, cancellationToken);
            await SaveSettingsAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving profiles: {ex.Message}");
            _inAppNotificationService.ShowInAppNotification(
                "Notification_SettingsSaveFailed_Title", "Notification_SettingsSaveFailed_Message", InfoBarSeverity.Error);
        }
    }

    public async Task SaveSettingsAsync()
    {
        _pendingSave?.Cancel();
        _pendingSave?.Dispose();
        _pendingSave = null;
        await _saveLock.WaitAsync();
        try
        {
            var configsList = RegexConfigs.ToList();
            await _localSettingsService.SaveSettingAsync(RegexCollectionSettingsKey, configsList);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    public RegexPageStructure? GetRegexStructureByHotkeyId(int hotkeyId)
    {
        foreach (var config in RegexConfigs)
        {
            if (config.Hotkey != null &&
                config.Hotkey.Modifiers != VirtualKeyModifiers.None &&
                config.Hotkey.Key != VirtualKey.None)
            {
                int currentConfigHotkeyId = HotkeyHasher.GenerateHotkeyHash((config.Hotkey.Modifiers, config.Hotkey.Key));
                if (currentConfigHotkeyId == hotkeyId)
                {
                    return config;
                }
            }
        }
        return null;
    }
}
