using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Delete_Newline.Services;

public class HotkeyCollectManagerService
{
    private readonly ILocalSettingsService _localSettingsService;
    private const string HotkeyCollectionSettingsKey = "HotkeyCollection";
    public ObservableCollection<HotkeyPageConfiguration> HotkeyConfigs { get; private set; }

    public HotkeyCollectManagerService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        HotkeyConfigs = new ObservableCollection<HotkeyPageConfiguration>();
    }

    public async Task InitializeAsync()
    {
        var savedHotkeyConfig = await _localSettingsService.ReadSettingAsync<ObservableCollection<HotkeyPageConfiguration>>(HotkeyCollectionSettingsKey);
        if (savedHotkeyConfig != null)
        {
            HotkeyConfigs = savedHotkeyConfig;

            // Subscribe to PropertyChanged events for initial items
            foreach (HotkeyPageConfiguration HotkeyConfig in HotkeyConfigs)
            {
                SubscribeToHotkeyConfig(HotkeyConfig);
            }
        }

        HotkeyConfigs.CollectionChanged += HotkeyConfigs_CollectionChanged;
    }

    public void AddHotkeyConfig(HotkeyPageConfiguration? HotkeyConfig = null)
    {
        if (HotkeyConfig is not null)
        {
            HotkeyConfigs.Add(HotkeyConfig);
        }
        else
        {
            HotkeyConfigs.Add(new HotkeyPageConfiguration
            {
                Hotkey = new Hotkey(),
                RegexChain = new RegexChain()
            });
        }
    }

    public void RemoveHotkeyConfig(HotkeyPageConfiguration HotkeyConfig)
    {
        if (HotkeyConfigs.Contains(HotkeyConfig))
        {
            HotkeyConfigs.Remove(HotkeyConfig);
        }
    }

    private async void HotkeyConfigs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Subscription and Unsubscribe processing of newly added and deleted Hotkey within HotkeyConfigs.
        if (e.NewItems != null)
        {
            foreach (HotkeyPageConfiguration HotkeyConfig in e.NewItems)
            {
                SubscribeToHotkeyConfig(HotkeyConfig);
            }
        }

        if (e.OldItems != null)
        {
            foreach (HotkeyPageConfiguration oldItem in e.OldItems)
            {
                UnsubscribeFromHotkeyConfig(oldItem);
            }
        }

        await SaveSettingsAsync();
    }
    

    private void SubscribeToHotkeyConfig(HotkeyPageConfiguration HotkeyConfig)
    {
        HotkeyConfig.PropertyChanged += HotkeyConfig_PropertyChanged;

        if (HotkeyConfig.RegexChain != null)
        {
            SubscribeToRegexChain(HotkeyConfig.RegexChain);
        }
    }

    private void UnsubscribeFromHotkeyConfig(HotkeyPageConfiguration config)
    {
        config.PropertyChanged -= HotkeyConfig_PropertyChanged;

        if (config.RegexChain != null)
        {
            UnsubscribeFromRegexChain(config.RegexChain);
        }
    }

    private void SubscribeToRegexChain(RegexChain chain)
    {
        chain.PropertyChanged += RegexChain_PropertyChanged;
        chain.ChainItems.CollectionChanged += ChainItems_CollectionChanged;

        foreach (var item in chain.ChainItems)
        {
            item.PropertyChanged += ChainItem_PropertyChanged;
        }
    }

    private void UnsubscribeFromRegexChain(RegexChain chain)
    {
        chain.PropertyChanged -= RegexChain_PropertyChanged;
        chain.ChainItems.CollectionChanged -= ChainItems_CollectionChanged;

        foreach (var item in chain.ChainItems)
        {
            item.PropertyChanged -= ChainItem_PropertyChanged;
        }
    }

    private async void ChainItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ChainItem newItem in e.NewItems)
            {
                newItem.PropertyChanged += ChainItem_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ChainItem oldItem in e.OldItems)
            {
                oldItem.PropertyChanged -= ChainItem_PropertyChanged;
            }
        }

        await SaveSettingsAsync();
    }

    private async void HotkeyConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await SaveSettingsAsync();
    }

    private async void ChainItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await SaveSettingsAsync();
    }

    private async void RegexChain_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        await SaveSettingsAsync();
    }

    private async Task SaveSettingsAsync()
    {
        await _localSettingsService.SaveSettingAsync(HotkeyCollectionSettingsKey, HotkeyConfigs);
    }
}
