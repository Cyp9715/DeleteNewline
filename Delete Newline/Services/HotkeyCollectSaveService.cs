using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.System;

namespace Delete_Newline.Services;

public sealed class HotkeyCollectSaveService
{
    public ObservableCollection<HotkeyPageStructure> HotkeyConfigs { get; private set; }

    private readonly SettingsService _localSettingsService;
    private readonly HotkeyRegisterService _HotkeyRegisterService;

    private const string HotkeyCollectionSettingsKey = "HotkeyCollection";
    private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1);

    public HotkeyCollectSaveService(SettingsService localSettingsService, HotkeyRegisterService HotkeyRegisterService)
    {
        _localSettingsService = localSettingsService;
        _HotkeyRegisterService = HotkeyRegisterService;

        HotkeyConfigs = new ObservableCollection<HotkeyPageStructure>();
    }

    public void Initialize()
    {
        var savedHotkeyConfigs = _localSettingsService.ReadSetting<List<HotkeyPageStructure>>(HotkeyCollectionSettingsKey);
        if (savedHotkeyConfigs != null)
        {
            // First, unregister all existing Hotkeys
            foreach (var config in savedHotkeyConfigs)
            {
                if (config.Hotkey != null && 
                    config.Hotkey.Modifiers != VirtualKeyModifiers.None && 
                    config.Hotkey.Key != VirtualKey.None)
                {
                    _HotkeyRegisterService.UnRegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));
                }
            }

            HotkeyConfigs.Clear();
            foreach (var config in savedHotkeyConfigs)
            {
                HotkeyConfigs.Add(config);
                Subscribe(config);

                if(config.Hotkey.Modifiers == VirtualKeyModifiers.None && 
                    config.Hotkey.Key == VirtualKey.None)
                    continue;

                _HotkeyRegisterService.RegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));
            }
        }
        HotkeyConfigs.CollectionChanged += OnHotkeyConfigsChanged;
    }

    public void AddHotkeyConfig(HotkeyPageStructure? config = null)
    {
        var newConfig = config ?? new HotkeyPageStructure();
        HotkeyConfigs.Add(newConfig);
    }

    public void RemoveHotkeyConfig(HotkeyPageStructure config)
    {
        if (HotkeyConfigs.Remove(config))
        {
            Unsubscribe(config);

            // Unregister Hotkey
            if (config!.Hotkey!.Modifiers != VirtualKeyModifiers.None && config.Hotkey.Key != VirtualKey.None)
            {
                _HotkeyRegisterService.UnRegisterHotkey((config.Hotkey.Modifiers, config.Hotkey.Key));
            }
        }
    }

    private async void OnHotkeyConfigsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (HotkeyPageStructure config in e.NewItems)
            {
                Subscribe(config);
            }
        }

        if (e.OldItems != null)
        {
            foreach (HotkeyPageStructure config in e.OldItems)
            {
                Unsubscribe(config);
            }
        }

        await SaveSettingsAsync();
    }

    private void Subscribe(HotkeyPageStructure config)
    {
        // Name, Comment
        config.PropertyChanged += OnConfigPropertyChanged;

        // Hotkey
        if (config.Hotkey != null)
        {
            config.Hotkey.PropertyChanged += OnConfigPropertyChanged;
        }

        //RegexChain
        if (config.RegexChain != null)
        {
            config.RegexChain.ChainItems.CollectionChanged += OnChainItemsChanged;

            // each items.
            foreach (var item in config.RegexChain.ChainItems)
            {
                item.PropertyChanged += OnConfigPropertyChanged;
            }
        }
    }

    private void Unsubscribe(HotkeyPageStructure config)
    {
        config.PropertyChanged -= OnConfigPropertyChanged;

        // Hotkey
        if (config.Hotkey != null)
        {
            config.Hotkey.PropertyChanged -= OnConfigPropertyChanged;
        }

        // RegexChain
        if (config.RegexChain != null)
        {
            config.RegexChain.ChainItems.CollectionChanged -= OnChainItemsChanged;

            // each items.
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
            // Convert ObservableCollection to List for serialization
            var configsList = HotkeyConfigs.ToList();
            await _localSettingsService.SaveSettingAsync(HotkeyCollectionSettingsKey, configsList);
        }
        catch (Exception ex)
        {
            // Todo : Error logic.
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
                // _HotkeyRegisterService is an injected instance of HotkeyRegisterService
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
