using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Text;
using Delete_Newline.Helpers;

namespace Delete_Newline.Services;

public sealed class HotkeyCollectSaveService
{
    public ObservableCollection<HotkeyPageStructure> HotkeyConfigs { get; private set; }

    private readonly SettingsService _localSettingsService;
    private readonly HotkeyRegisterService _HotkeyRegisterService;
    private readonly InAppNotificationService _inAppNotificationService;

    private const string HotkeyCollectionSettingsKey = "HotkeyCollection";
    private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1);

    public HotkeyCollectSaveService(SettingsService localSettingsService, HotkeyRegisterService HotkeyRegisterService, InAppNotificationService inAppNotificationService)
    {
        _localSettingsService = localSettingsService;
        _HotkeyRegisterService = HotkeyRegisterService;
        _inAppNotificationService = inAppNotificationService;
        HotkeyConfigs = new ObservableCollection<HotkeyPageStructure>();
    }

    public void Initialize()
    {
        var savedHotkeyConfigs = LoadSavedHotkeyConfigurations();

        if (savedHotkeyConfigs != null && savedHotkeyConfigs.Count > 0) // Ensure there are configs to process
        {
            this.HotkeyConfigs.Clear(); // Clear before re-populating
            ShowFailedRegistrationNotification(RegisterHotkeysAndCollectFailures(savedHotkeyConfigs));
        }
        
        this.HotkeyConfigs.CollectionChanged += OnHotkeyConfigsChanged;
    }

    private List<HotkeyPageStructure>? LoadSavedHotkeyConfigurations()
    {
        return _localSettingsService.ReadSetting<List<HotkeyPageStructure>>(HotkeyCollectionSettingsKey);
    }

    private List<string> RegisterHotkeysAndCollectFailures(IEnumerable<HotkeyPageStructure> savedConfigs)
    {
        List<string> failedHotkeyStrings = new List<string>();

        foreach (var config in savedConfigs)
        {
            config.IsRegistrationFailed = false; // Reset status
            this.HotkeyConfigs.Add(config); // Add to the main collection
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
            messageBuilder.AppendLine("The following hotkeys might already be in use by another application:");

            foreach (var hotkeyStr in failedHotkeyStrings)
            {
                messageBuilder.AppendLine($"- {hotkeyStr}");
            }
            messageBuilder.Append("Problematic hotkeys are highlighted in red in the list.");

            _inAppNotificationService.ShowNotification(
                title: "Some Hotkey Registrations Failed",
                message: messageBuilder.ToString(),
                severity: InfoBarSeverity.Warning
            );
        }
    }

    public void AddHotkeyConfig(HotkeyPageStructure? config = null)
    {
        var newConfig = config ?? new HotkeyPageStructure();
        HotkeyConfigs.Add(newConfig);
        // Subscribe logic is handled by OnHotkeyConfigsChanged if newConfig is added to HotkeyConfigs
    }

    public void RemoveHotkeyConfig(HotkeyPageStructure config)
    {
        if (HotkeyConfigs.Remove(config))
        {
            Unsubscribe(config);

            if (config!.Hotkey!.Modifiers != VirtualKeyModifiers.None && config.Hotkey.Key != VirtualKey.None)
            {
                _HotkeyRegisterService.UnRegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));
            }
        }
    }

    private async void OnHotkeyConfigsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (HotkeyPageStructure config in e.NewItems)
            {
                Subscribe(config);
                // Note: Initial registration for newly added hotkeys might need to be handled here
                // or rely on user navigating to edit the hotkey.
                // For now, it's consistent with how it was before refactoring:
                // new hotkeys are added, saved, and registered upon next app start or when edited.
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (HotkeyPageStructure config in e.OldItems)
            {
                Unsubscribe(config);
                // Unregistration is handled in RemoveHotkeyConfig
            }
        }
        
        // If items are moved (reordered), subscriptions remain.
        // If items are replaced, old ones should be unsubscribed, new ones subscribed.
        // The current Subscribe/Unsubscribe in Add/Remove and Initialize should cover most cases.

        await SaveSettingsAsync();
    }

    private void Subscribe(HotkeyPageStructure config)
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

    private void Unsubscribe(HotkeyPageStructure config)
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
            var configsList = HotkeyConfigs.ToList();
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

    public HotkeyPageStructure? GetHotkeyStructureById(int hotkeyId)
    {
        foreach (var config in HotkeyConfigs)
        {
            if (config.Hotkey != null &&
                config.Hotkey.Modifiers != VirtualKeyModifiers.None &&
                config.Hotkey.Key != VirtualKey.None)
            {
                int currentConfigHotkeyId = _HotkeyRegisterService.HotkeyToHash((config.Hotkey.Modifiers, config.Hotkey.Key));
                if (currentConfigHotkeyId == hotkeyId)
                {
                    return config;
                }
            }
        }
        return null;
    }
}
