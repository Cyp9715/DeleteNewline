using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;


namespace Delete_Newline.Services;

public class KeybindCollectManagerService
{
    private readonly ILocalSettingsService _localSettingsService;
    private const string KeybindCollectionSettingsKey = "KeybindCollection";
    public ObservableCollection<RegexChain> RegexChains { get; private set; }

    public KeybindCollectManagerService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        RegexChains = new ObservableCollection<RegexChain>();
        RegexChains.CollectionChanged += RegexChains_CollectionChanged;
    }

    public async Task InitializeAsync()
    {
        var savedChains = await _localSettingsService.ReadSettingAsync<ObservableCollection<RegexChain>>(KeybindCollectionSettingsKey);
        if (savedChains != null)
        {
            RegexChains = savedChains;
            RegexChains.CollectionChanged += RegexChains_CollectionChanged;
        }
    }

    public ObservableCollection<RegexChain> GetRegexChains()
    {
        return RegexChains;
    }

    public void AddRegexChain(RegexChain? regexChain = null)
    {
        if (regexChain is not null)
        {
            RegexChains.Add(regexChain);
        }
        else
        {
            RegexChains.Add(new RegexChain("New Chain"));
        }
    }

    public void RemoveRegexChain(RegexChain chain)
    {
        if (RegexChains.Contains(chain))
        {
            RegexChains.Remove(chain);
        }
    }

    public void SwapChain(int index1, int index2)
    {
        if (IsValidIndex(index1) && IsValidIndex(index2) && index1 != index2)
        {
            SwapElements(RegexChains, index1, index2);
        }
    }

    public void MoveChain(int oldIndex, int newIndex)
    {
        if (IsValidIndex(oldIndex) && IsValidIndex(newIndex) && oldIndex != newIndex)
        {
            var regexExpression = RegexChains[oldIndex];
            RegexChains.RemoveAt(oldIndex);
            RegexChains.Insert(newIndex, regexExpression);
        }
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

    private async void RegexChains_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        await _localSettingsService.SaveSettingAsync(KeybindCollectionSettingsKey, RegexChains);
    }
}
