using System.Diagnostics;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public static class RegexTextProcessor
{
    public static string ProcessRegex(string inputText, string? regexPattern, string? replacement, string invalidRegexMessage)
    {
        string result = invalidRegexMessage;

        try
        {
            if (string.IsNullOrEmpty(regexPattern))
            {
                return inputText;
            }

            result = Regex.Replace(inputText, regexPattern, Regex.Unescape(replacement ?? string.Empty), RegexOptions.Multiline);
        }
        catch (RegexParseException)
        {
            Debug.WriteLine("If you didn't input an invalid Regex, this is an intended error.");
        }

        return result;
    }

    public static string ApplyRegexChain(string inputText, IEnumerable<ChainItem> chainItems, string invalidRegexMessage)
    {
        string currentText = inputText;
        foreach (ChainItem chainItem in chainItems)
        {
            currentText = ProcessRegex(currentText, chainItem.RegexExpression, chainItem.Replace, invalidRegexMessage);
        }

        return currentText;
    }
}
