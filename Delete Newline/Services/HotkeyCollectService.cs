using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Delete_Newline.Services
{
    public class HotkeyCollectService
    {
        private readonly ILocalSettingsService _localSettingsService;
        private const string HotkeyCollectionSettingsKey = "HotkeyCollection";
        public ObservableCollection<HotkeyPageStructure> HotkeyConfigs { get; private set; }

        public HotkeyCollectService(ILocalSettingsService localSettingsService)
        {
            _localSettingsService = localSettingsService;
            HotkeyConfigs = new ObservableCollection<HotkeyPageStructure>();
        }

        public async Task InitializeAsync()
        {
            var savedHotkeyConfigs = await _localSettingsService.ReadSettingAsync<ObservableCollection<HotkeyPageStructure>>(HotkeyCollectionSettingsKey);
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

        public void AddHotkeyConfig(HotkeyPageStructure? config = null)
        {
            var newConfig = config ?? new HotkeyPageStructure
            {
                Hotkey = new HotkeyStructure(_localSettingsService),
                RegexChain = new RegexChainStructure()
            };
            HotkeyConfigs.Add(newConfig);
        }

        public void RemoveHotkeyConfig(HotkeyPageStructure config)
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
            config.PropertyChanged += OnConfigPropertyChanged;
            if (config.RegexChain != null)
            {
                Subscribe((RegexChainStructure)config.RegexChain);
            }
        }

        private void Unsubscribe(HotkeyPageStructure config)
        {
            config.PropertyChanged -= OnConfigPropertyChanged;
            if (config.RegexChain != null)
            {
                Unsubscribe((RegexChainStructure)config.RegexChain);
            }
        }

        private void Subscribe(RegexChainStructure chain)
        {
            chain.PropertyChanged += OnConfigPropertyChanged;
            chain.ChainItems.CollectionChanged += OnChainItemsChanged;
            foreach (var item in chain.ChainItems)
            {
                item.PropertyChanged += OnConfigPropertyChanged;
            }
        }

        private void Unsubscribe(RegexChainStructure chain)
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
