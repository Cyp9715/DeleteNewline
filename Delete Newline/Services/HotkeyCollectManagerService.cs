using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Delete_Newline.Services
{
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
            var savedHotkeyConfigs = await _localSettingsService.ReadSettingAsync<ObservableCollection<HotkeyPageConfiguration>>(HotkeyCollectionSettingsKey);
            if (savedHotkeyConfigs != null)
            {
                HotkeyConfigs = savedHotkeyConfigs;
                foreach (var config in HotkeyConfigs)
                {
                    Subscribe(config);
                }
            }
            HotkeyConfigs.CollectionChanged += OnHotkeyConfigsChanged;
        }

        public void AddHotkeyConfig(HotkeyPageConfiguration? config = null)
        {
            var newConfig = config ?? new HotkeyPageConfiguration
            {
                Hotkey = new Hotkey(),
                RegexChain = new RegexChain()
            };
            HotkeyConfigs.Add(newConfig);
        }

        public void RemoveHotkeyConfig(HotkeyPageConfiguration config)
        {
            if (HotkeyConfigs.Remove(config))
            {
                Unsubscribe(config);
            }
        }

        private async void OnHotkeyConfigsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (HotkeyPageConfiguration config in e.NewItems)
                {
                    Subscribe(config);
                }
            }

            if (e.OldItems != null)
            {
                foreach (HotkeyPageConfiguration config in e.OldItems)
                {
                    Unsubscribe(config);
                }
            }

            await SaveSettingsAsync();
        }

        private void Subscribe(HotkeyPageConfiguration config)
        {
            config.PropertyChanged += OnConfigPropertyChanged;
            if (config.RegexChain != null)
            {
                Subscribe(config.RegexChain);
            }
        }

        private void Unsubscribe(HotkeyPageConfiguration config)
        {
            config.PropertyChanged -= OnConfigPropertyChanged;
            if (config.RegexChain != null)
            {
                Unsubscribe(config.RegexChain);
            }
        }

        private void Subscribe(RegexChain chain)
        {
            chain.PropertyChanged += OnConfigPropertyChanged;
            chain.ChainItems.CollectionChanged += OnChainItemsChanged;
            foreach (var item in chain.ChainItems)
            {
                item.PropertyChanged += OnConfigPropertyChanged;
            }
        }

        private void Unsubscribe(RegexChain chain)
        {
            chain.PropertyChanged -= OnConfigPropertyChanged;
            chain.ChainItems.CollectionChanged -= OnChainItemsChanged;
            foreach (var item in chain.ChainItems)
            {
                item.PropertyChanged -= OnConfigPropertyChanged;
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

        private async Task SaveSettingsAsync()
        {
            await _localSettingsService.SaveSettingAsync(HotkeyCollectionSettingsKey, HotkeyConfigs);
        }
    }
}
