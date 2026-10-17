using System.Diagnostics;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public static class RegexTextProcessor
{
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan ChainTimeout = TimeSpan.FromSeconds(2);

    public static string ProcessRegex(string inputText, string? regexPattern, string? replacement, string invalidRegexMessage)
    {
        try
        {
            return Replace(inputText, regexPattern, replacement);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            Debug.WriteLine($"Regex failed: {ex.Message}");
            return invalidRegexMessage;
        }
    }

    public static string ApplyRegexChain(string inputText, IEnumerable<ChainItem> chainItems, string invalidRegexMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Transform(inputText, chainItems, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException or TimeoutException)
        {
            Debug.WriteLine($"Regex chain failed: {ex.Message}");
            return invalidRegexMessage;
        }
    }

    // Strict API for clipboard and MCP callers: errors must never become replacement text.
    public static string Transform(string inputText, IEnumerable<ChainItem> chainItems, CancellationToken cancellationToken = default)
    {
        long started = Stopwatch.GetTimestamp();
        string currentText = inputText;
        foreach (ChainItem chainItem in chainItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Stopwatch.GetElapsedTime(started) > ChainTimeout)
            {
                throw new TimeoutException("The regex chain exceeded its two-second time limit.");
            }
            currentText = Replace(currentText, chainItem.RegexExpression, chainItem.Replace);
        }
        return currentText;
    }

    private static string Replace(string inputText, string? pattern, string? replacement) =>
        string.IsNullOrEmpty(pattern)
            ? inputText
            : Regex.Replace(inputText, pattern, Regex.Unescape(replacement ?? string.Empty), RegexOptions.Multiline, MatchTimeout);

    public static void ValidateRules(IEnumerable<ChainItem> chainItems)
    {
        foreach (ChainItem item in chainItems)
        {
            if (string.IsNullOrEmpty(item.RegexExpression)) continue;
            _ = new Regex(item.RegexExpression, RegexOptions.Multiline, MatchTimeout);
            _ = Regex.Unescape(item.Replace ?? string.Empty);
        }
    }
}
