using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Delete_Newline.Contracts.Structures;

public partial class RegexChain : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ChainItem> _chainItems = new ObservableCollection<ChainItem>();

    public RegexChain()
    {
        ChainItems.Add(new ChainItem());
    }

    public void AddChainItem()
    {
        ChainItems.Add(new ChainItem());
    }
}

public class ChainItem : ObservableObject
{
    public string? RegexExpression { get; set; }
    public string? Replace { get; set; }
}
