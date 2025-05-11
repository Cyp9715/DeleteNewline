using System.Collections.Generic;

namespace Delete_Newline.Services
{
    public interface IRegexService
    {
        string ProcessText(string inputText);
        string ProcessText(string inputText, int hotkeyId);
        // In the future, you might want methods to load/manage regex rules, e.g.:
        // List<(string Pattern, string Replacement)> GetRegexRules();
        // void SetRegexRules(List<(string Pattern, string Replacement)> rules);
    }
} 