using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Delete_Newline.Contracts.Structures;

public partial class RegexChainStructure : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ChainItem> _chainItems = new ObservableCollection<ChainItem>();

    public RegexChainStructure()
    {
    }

    public void AddChainItem()
    {
        ChainItems.Add(new ChainItem());
    }

    public void RemoveChainItem(ChainItem item)
    {
        ChainItems.Remove(item);
    }
}

public partial class ChainItem : ObservableObject
{
    [ObservableProperty]
    public string? _regexExpression;

    [ObservableProperty]
    public string? _replace;
}
