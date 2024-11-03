using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Text.Json;


namespace Delete_Newline.Services;

public class KeybindCollectManagerService
{
    private readonly ILocalSettingsService _localSettingsService;
    private const string KeybindCollectionSettingsKey = "KeybindCollection";
    public ObservableCollection<RegexChain> RegexChains { get; private set; }

    public KeybindCollectManagerService(ILocalSettingsService localSettingsService)
    {
        RegexChains = new ObservableCollection<RegexChain>();

        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        RegexChains = await _localSettingsService.ReadSettingAsync<ObservableCollection<RegexChain>>(KeybindCollectionSettingsKey) ?? RegexChains;
    }

    public ObservableCollection<RegexChain> GetRegexChains()
    {
        return RegexChains;
    }

    public async Task AddRegexChain(RegexChain? regexChain = null)
    {
        if(regexChain is not null)
        {
            RegexChains.Add(regexChain);
        }
        else
        {
            RegexChains.Add(new RegexChain("New Chain"));
        }

        await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, RegexChains);
    }

    public async Task RemoveRegexChain(RegexChain chain)
    {
        if (RegexChains.Contains(chain))
        {
            RegexChains.Remove(chain);
        }

        await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, RegexChains);
    }

    public async Task SwapChain(int index1, int index2)
    {
        if (IsValidIndex(index1) && IsValidIndex(index2) && index1 != index2)
        {
            SwapElements(RegexChains, index1, index2);
        }

        await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, RegexChains);
    }

    // remove oldIndex, move new Index.
    public async Task MoveChain(int oldIndex, int newIndex)
    {
        if (IsValidIndex(oldIndex) && IsValidIndex(newIndex) && oldIndex != newIndex)
        {
            var regexExpression = RegexChains[oldIndex];

            RegexChains.RemoveAt(oldIndex);
            RegexChains.Insert(newIndex, regexExpression);
        }

        await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, RegexChains);
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < RegexChains.Count;
    }

    private void SwapElements<T>(ObservableCollection<T> list, int index1, int index2)
    {
        T temp = list[index1];
        list[index1] = list[index2];
        list[index2] = temp;
    }
}