using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Delete_Newline.Contracts.Structures;

public partial class RegexChain : ObservableObject
{
    [ObservableProperty]
    public string _chainName;
    [ObservableProperty]
    public string _chainComment;
    [ObservableProperty]
    public ObservableCollection<string> _regexExpressions = new ObservableCollection<string>();
    [ObservableProperty]
    public ObservableCollection<string> _replaces = new ObservableCollection<string>();

    public RegexChain(string ruleName, string chainComment)
    {
        ChainName = ruleName;
        ChainComment = chainComment;
    }

    public IReadOnlyList<string> GetRegexExpressions() => RegexExpressions.AsReadOnly();
    public IReadOnlyList<string> GetReplaces() => Replaces.AsReadOnly();

    public void AddRule(string regexExpression, string replace)
    {
        RegexExpressions.Add(regexExpression);
        Replaces.Add(replace);
    }

    public void RemoveRule(int index)
    {
        if (index >= 0 && index < RegexExpressions.Count)
        {
            RegexExpressions.RemoveAt(index);
            Replaces.RemoveAt(index);
        }
    }

    // remove oldIndex, move new Index.
    public void MoveRule(int oldIndex, int newIndex)
    {
        if (IsValidIndex(oldIndex) && IsValidIndex(newIndex) && oldIndex != newIndex)
        {
            var regexExpression = RegexExpressions[oldIndex];
            var replace = Replaces[oldIndex];

            RegexExpressions.RemoveAt(oldIndex);
            Replaces.RemoveAt(oldIndex);

            RegexExpressions.Insert(newIndex, regexExpression);
            Replaces.Insert(newIndex, replace);
        }
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < RegexExpressions.Count;
    }
}