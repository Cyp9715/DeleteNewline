using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Helpers;

// Only the test editor uses this adapter. Clipboard and MCP text stays literal.
public static class RegexTestInput
{
    public static string Normalize(string text, TestInputLineEnding lineEnding)
    {
        string separator = lineEnding switch
        {
            TestInputLineEnding.CrLf => "\r\n",
            TestInputLineEnding.Lf => "\n",
            TestInputLineEnding.Cr => "\r",
            _ => throw new ArgumentOutOfRangeException(nameof(lineEnding))
        };

        // Do not use ReplaceLineEndings: Unicode separators may be regex test data.
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", separator);
    }

    public static string ToEditor(string text) => Normalize(text, TestInputLineEnding.Cr);

    public static TestInputLineEnding Detect(string text, bool legacyTextBox = false)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] == '\n') return TestInputLineEnding.Lf;
            if (text[index] != '\r') continue;
            if (index + 1 < text.Length && text[index + 1] == '\n') return TestInputLineEnding.CrLf;
            if (!legacyTextBox) return TestInputLineEnding.Cr;
        }

        // Older versions saved Enter as CR. New editor input defaults to Windows CRLF.
        return TestInputLineEnding.CrLf;
    }
}
