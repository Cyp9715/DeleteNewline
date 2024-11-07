using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.ViewModels;
using System.Collections.ObjectModel;

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
        KeybindConfigs.CollectionChanged += KeybindConfigs_CollectionChanged;
    }

    public async Task InitializeAsync()
    {
        var savedChains = await _localSettingsService.ReadSettingAsync<ObservableCollection<KeybindPageConfiguration>>(KeybindCollectionSettingsKey);
        if (savedChains != null)
        {
            KeybindConfigs = savedChains;
            KeybindConfigs.CollectionChanged += KeybindConfigs_CollectionChanged;
        }
    }

    public ObservableCollection<KeybindPageConfiguration> GetKeybindConfigs()
    {
        return KeybindConfigs;
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
                RegexChain = new RegexChain("New chain", "")
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
        // Code used to prevent unnecessary SaveSettingAsync calls caused by rapid Remove and Add operations during Drag&Drop.
        if (KeybindCollectViewModel.isDragEnded is true)
        {
            await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, KeybindConfigs);
        }
        else
        {
            KeybindCollectViewModel.isDragEnded = true;
        }
    }
}
