using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Delete_Newline.Services;

public class KeybindCollectManagerService
{
    private readonly ILocalSettingsService _localSettingsService;
    private const string KeybindCollectionSettingsKey = "KeybindCollection";
    public ObservableCollection<KeybindPageConfiguration> KeybindConfigs { get; private set; }

    public KeybindCollectManagerService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        KeybindConfigs = new ObservableCollection<KeybindPageConfiguration>();
    }

    public async Task InitializeAsync()
    {
        var savedChains = await _localSettingsService.ReadSettingAsync<ObservableCollection<KeybindPageConfiguration>>(KeybindCollectionSettingsKey);
        if (savedChains != null)
        {
            KeybindConfigs.Clear();
            foreach (var chain in savedChains)
            {
                KeybindConfigs.Add(chain);
            }

            // 초기 항목들의 PropertyChanged 이벤트 구독
            foreach (var item in KeybindConfigs)
            {
                SubscribeToKeybindConfig(item);
            }
        }

        KeybindConfigs.CollectionChanged += KeybindConfigs_CollectionChanged;
    }

    public void AddKeybindConfig(KeybindPageConfiguration? keybindConfig = null)
    {
        if (keybindConfig is not null)
        {
            KeybindConfigs.Add(keybindConfig);
        }
        else
        {
            KeybindConfigs.Add(new KeybindPageConfiguration
            {
                Keybind = new Keybind(),
                RegexChain = new RegexChain()
            });
        }
    }

    public void RemoveKeybindConfig(KeybindPageConfiguration keybindConfig)
    {
        if (KeybindConfigs.Contains(keybindConfig))
        {
            KeybindConfigs.Remove(keybindConfig);
        }
    }

    private async void KeybindConfigs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (KeybindPageConfiguration newItem in e.NewItems)
            {
                SubscribeToKeybindConfig(newItem);
            }
        }

        if (e.OldItems != null)
        {
            foreach (KeybindPageConfiguration oldItem in e.OldItems)
            {
                UnsubscribeFromKeybindConfig(oldItem);
            }
        }

        await SaveSettingsAsync();
    }
    

    private void SubscribeToKeybindConfig(KeybindPageConfiguration config)
    {
        config.PropertyChanged += KeybindConfig_PropertyChanged;

        if (config.RegexChain != null)
        {
            SubscribeToRegexChain(config.RegexChain);
        }
    }

    private void UnsubscribeFromKeybindConfig(KeybindPageConfiguration config)
    {
        config.PropertyChanged -= KeybindConfig_PropertyChanged;

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

    private async void KeybindConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, KeybindConfigs);
    }
}
