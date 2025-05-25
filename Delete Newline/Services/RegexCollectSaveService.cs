using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using System.Text;
using Delete_Newline.Helpers;

namespace Delete_Newline.Services;

public sealed class RegexCollectSaveService
{
    public ObservableCollection<RegexPageStructure> RegexConfigs { get; private set; }

    private readonly SettingsService _localSettingsService;
    private readonly HotkeyRegisterService _HotkeyRegisterService;
    private readonly InAppNotificationService _inAppNotificationService;

    private const string HotkeyCollectionSettingsKey = "HotkeyCollection";
    private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1);

    public RegexCollectSaveService(SettingsService localSettingsService, 
        HotkeyRegisterService HotkeyRegisterService, 
        InAppNotificationService inAppNotificationService)
    {
        _localSettingsService = localSettingsService;
        _HotkeyRegisterService = HotkeyRegisterService;
        _inAppNotificationService = inAppNotificationService;
        RegexConfigs = new ObservableCollection<RegexPageStructure>();
    }

    public void Initialize()
    {
        var savedRegexConfigs = LoadSavedHotkeyConfigurations();

        if (savedRegexConfigs != null && savedRegexConfigs.Count > 0) // Ensure there are configs to process
        {
            this.RegexConfigs.Clear(); // Clear before re-populating
            ShowFailedRegistrationNotification(RegisterRegexConfigsAndCollectFailures(savedRegexConfigs));
        }
        
        this.RegexConfigs.CollectionChanged += OnRegexConfigsChanged;
    }

    private List<RegexPageStructure>? LoadSavedHotkeyConfigurations()
    {
        return _localSettingsService.ReadSetting<List<RegexPageStructure>>(HotkeyCollectionSettingsKey);
    }

    private List<string> RegisterRegexConfigsAndCollectFailures(IEnumerable<RegexPageStructure> savedConfigs)
    {
        List<string> failedHotkeyStrings = new List<string>();

        foreach (var config in savedConfigs)
        {
            config.IsRegistrationFailed = false; // Reset status
            this.RegexConfigs.Add(config); // Add to the main collection
            Subscribe(config);

            if (config.Hotkey.Modifiers == VirtualKeyModifiers.None &&
                config.Hotkey.Key == VirtualKey.None)
            {
                continue; // Skip empty/invalid hotkeys
            }

            bool registrationSuccess = _HotkeyRegisterService.RegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));

            if (!registrationSuccess)
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
        if (RegexConfigs.Remove(config))
        {
            Unsubscribe(config);

            if (config!.Hotkey!.Modifiers != VirtualKeyModifiers.None && config.Hotkey.Key != VirtualKey.None)
            {
                _HotkeyRegisterService.UnRegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));
            }
        }
    }

    private async void OnRegexConfigsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
        

        await SaveSettingsAsync();
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
                item.PropertyChanged += OnConfigPropertyChanged;
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
            }
        }
    }

    private async void OnChainItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ChainItem item in e.NewItems)
            {
                item.PropertyChanged += OnConfigPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ChainItem item in e.OldItems)
            {
                item.PropertyChanged -= OnConfigPropertyChanged;
            }
        }

        await SaveSettingsAsync();
    }

    private async void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await SaveSettingsAsync();
    }

    public async Task SaveSettingsAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var configsList = RegexConfigs.ToList();
            await _localSettingsService.SaveSettingAsync(HotkeyCollectionSettingsKey, configsList);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving Hotkeys: {ex.Message}");
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
                int currentConfigHotkeyId = HotkeyHelper.GenerateHotkeyHash(config.Hotkey.Modifiers, config.Hotkey.Key);
                if (currentConfigHotkeyId == hotkeyId)
                {
                    return config;
                }
            }
        }
        return null;
    }
}
