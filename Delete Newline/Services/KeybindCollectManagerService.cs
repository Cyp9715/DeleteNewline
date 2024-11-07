using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.ViewModels;
using System.Collections.ObjectModel;

namespace Delete_Newline.Services;

public class KeybindCollectManagerService
{
    private readonly ILocalSettingsService _localSettingsService;
    private const string KeybindCollectionSettingsKey = "KeybindCollection";
    public ObservableCollection<KeybindInfo> KeybindInfos { get; private set; }

    public KeybindCollectManagerService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        KeybindInfos = new ObservableCollection<KeybindInfo>();
        KeybindInfos.CollectionChanged += KeybindInfos_CollectionChanged;
    }

    public async Task InitializeAsync()
    {
        var savedChains = await _localSettingsService.ReadSettingAsync<ObservableCollection<KeybindInfo>>(KeybindCollectionSettingsKey);
        if (savedChains != null)
        {
            KeybindInfos = savedChains;
            KeybindInfos.CollectionChanged += KeybindInfos_CollectionChanged;
        }
    }

    public ObservableCollection<KeybindInfo> GetKeybindInfos()
    {
        return KeybindInfos;
    }

    public void AddKeybindInfo(KeybindInfo? keybindInfo = null)
    {
        if (keybindInfo is not null)
        {
            KeybindInfos.Add(keybindInfo);
        }
        else
        {
            KeybindInfos.Add(new KeybindInfo
            {
                Keybind = new Keybind(),
                RegexChain = new RegexChain("New chain", "")
            });
        }
    }

    public void RemoveKeybindInfo(KeybindInfo keybindInfo)
    {
        if (KeybindInfos.Contains(keybindInfo))
        {
            KeybindInfos.Remove(keybindInfo);
        }
    }

    private async void KeybindInfos_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Code used to prevent unnecessary SaveSettingAsync calls caused by rapid Remove and Add operations during Drag&Drop.
        if (KeybindCollectViewModel.isDragEnded is true)
        {
            await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, KeybindInfos);
        }
        else
        {
            KeybindCollectViewModel.isDragEnded = true;
        }
    }
}
