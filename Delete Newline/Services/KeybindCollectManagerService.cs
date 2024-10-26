using Delete_Newline.Contracts.Services;
using Delete_Newline.Contracts.Structures;
using System.Collections.ObjectModel;

namespace Delete_Newline.Services;

public class KeybindCollectManagerService
{
    public ObservableCollection<RegexChain> RegexChains { get; }

    public KeybindCollectManagerService()
    {
        RegexChains = new ObservableCollection<RegexChain>();
        InitializeAsync();
    }

    public Task InitializeAsync()
    {
        RegexChains.Add(new RegexChain("Chain 1"));
        return Task.CompletedTask;
    }

    public ObservableCollection<RegexChain> GetRegexChains()
    {
        return RegexChains;
    }

    public void AddRegexChain(RegexChain? regexChain = null)
    {
        if(regexChain is not null)
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

    // remove oldIndex, move new Index.
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
}