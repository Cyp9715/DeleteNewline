using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Windows.System;

namespace Delete_Newline.Services;

public sealed class HotKeyCollectSaveService
{
    public ObservableCollection<HotKeyPageStructure> HotKeyConfigs { get; private set; }

    private readonly SettingsService _localSettingsService;
    private readonly HotKeyRegisterService _hotKeyRegisterService;

    private const string HotKeyCollectionSettingsKey = "HotKeyCollection";
    private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1);

    public HotKeyCollectSaveService(SettingsService localSettingsService, HotKeyRegisterService hotKeyRegisterService)
    {
        _localSettingsService = localSettingsService;
        _hotKeyRegisterService = hotKeyRegisterService;

        HotKeyConfigs = new ObservableCollection<HotKeyPageStructure>();
    }

    public void Initialize()
    {
        var savedHotKeyConfigs = _localSettingsService.ReadSetting<List<HotKeyPageStructure>>(HotKeyCollectionSettingsKey);
        if (savedHotKeyConfigs != null)
        {
            // First, unregister all existing hotkeys
            foreach (var config in savedHotKeyConfigs)
            {
                if (config.HotKey != null && 
                    config.HotKey.Modifiers != VirtualKeyModifiers.None && 
                    config.HotKey.Key != VirtualKey.None)
                {
                    _hotKeyRegisterService.UnRegisterHotKey((config.HotKey.Modifiers, config.HotKey.Key));
                }
            }

            HotKeyConfigs.Clear();
            foreach (var config in savedHotKeyConfigs)
            {
                HotKeyConfigs.Add(config);
                Subscribe(config);

                if(config.HotKey.Modifiers == VirtualKeyModifiers.None && 
                    config.HotKey.Key == VirtualKey.None)
                    continue;

                _hotKeyRegisterService.RegisterHotKey((config.HotKey.Modifiers, config.HotKey.Key));
            }
        }
        HotKeyConfigs.CollectionChanged += OnHotKeyConfigsChanged;
    }

    public void AddHotKeyConfig(HotKeyPageStructure? config = null)
    {
        var newConfig = config ?? new HotKeyPageStructure();
        HotKeyConfigs.Add(newConfig);
    }

    public void RemoveHotKeyConfig(HotKeyPageStructure config)
    {
        if (HotKeyConfigs.Remove(config))
        {
            Unsubscribe(config);

            // Unregister HotKey
            if (config!.HotKey!.Modifiers != VirtualKeyModifiers.None && config.HotKey.Key != VirtualKey.None)
            {
                _hotKeyRegisterService.UnRegisterHotKey((config.HotKey.Modifiers, config.HotKey.Key));
            }
        }
    }

    private async void OnHotKeyConfigsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (HotKeyPageStructure config in e.NewItems)
            {
                Subscribe(config);
            }
        }

        if (e.OldItems != null)
        {
            foreach (HotKeyPageStructure config in e.OldItems)
            {
                Unsubscribe(config);
            }
        }

        await SaveSettingsAsync();
    }

    private void Subscribe(HotKeyPageStructure config)
    {
        // Name, Comment
        config.PropertyChanged += OnConfigPropertyChanged;

        // HotKey
        if (config.HotKey != null)
        {
            config.HotKey.PropertyChanged += OnConfigPropertyChanged;
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

    private void Unsubscribe(HotKeyPageStructure config)
    {
        config.PropertyChanged -= OnConfigPropertyChanged;

        // HotKey
        if (config.HotKey != null)
        {
            config.HotKey.PropertyChanged -= OnConfigPropertyChanged;
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
            var configsList = HotKeyConfigs.ToList();
            await _localSettingsService.SaveSettingAsync(HotKeyCollectionSettingsKey, configsList);
        }
        catch (Exception ex)
        {
            // Todo : Error logic.
            System.Diagnostics.Debug.WriteLine($"Error saving HotKeys: {ex.Message}");
        }
        finally
        {
            _saveLock.Release();
        }
    }
}
