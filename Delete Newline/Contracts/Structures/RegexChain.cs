using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Delete_Newline.Contracts.Structures;

public partial class RegexChain : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ChainItem> _chainItems = new ObservableCollection<ChainItem>();

    public RegexChain()
    {
        if(ChainItems.Count == 0)
        {
            AddChainItem();
        }
    }

    public void AddChainItem()
    {
        ChainItems.Add(new ChainItem());
    }
}

public partial class ChainItem : ObservableObject
{
    [ObservableProperty]
    public string? _regexExpression;

    [ObservableProperty]
    public string? _replace;
}
