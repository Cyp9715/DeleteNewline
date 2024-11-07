using Windows.System;

namespace Delete_Newline.Contracts.Structures;

public class KeybindInfo
{
    public required Keybind Keybind;
    public required RegexChain RegexChain;
}

public class Keybind
{
    public VirtualKey Key1 { get; set; } = VirtualKey.None;
    public VirtualKey Key2 { get; set; } = VirtualKey.None;

    public Keybind()
    {

    }
}

public class RegexChain
{
    public string ChainName { get; set; }
    public string ChainComment { get; set; }
    public List<string> RegexExpressions { get; set; } = new List<string>();
    public List<string> Replaces { get; set; } = new List<string>();

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

    public void SwapRules(int index1, int index2)
    {
        if (IsValidIndex(index1) && IsValidIndex(index2) && index1 != index2)
        {
            SwapElements(RegexExpressions, index1, index2);
            SwapElements(Replaces, index1, index2);
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

    private void SwapElements<T>(List<T> list, int index1, int index2)
    {
        T temp = list[index1];
        list[index1] = list[index2];
        list[index2] = temp;
    }
}
