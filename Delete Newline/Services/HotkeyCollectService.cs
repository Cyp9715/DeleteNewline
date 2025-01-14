using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Delete_Newline.Services
{
    public class HotkeyCollectService
    {
        public ObservableCollection<HotkeyPageStructure> HotkeyConfigs { get; private set; }

        private readonly ILocalSettingsService _localSettingsService;
        private const string HotkeyCollectionSettingsKey = "HotkeyCollection";
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

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
                Hotkey = new HotkeyStructure(),
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
                await _localSettingsService.SaveSettingAsync(HotkeyCollectionSettingsKey, HotkeyConfigs);
            }
            catch (Exception ex)
            {
                // Todo : Error logic.
                System.Diagnostics.Debug.WriteLine($"Hotkeys 저장 중 오류 발생: {ex.Message}");
            }
            finally
            {
                _saveLock.Release();
            }
        }
    }
}
