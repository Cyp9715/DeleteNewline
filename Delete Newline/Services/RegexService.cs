using System.Diagnostics;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public class RegexService
{
    private readonly RegexCollectSaveService _regexCollectSaveService;

    public RegexService(RegexCollectSaveService regexCollectSaveService)
    {
        _regexCollectSaveService = regexCollectSaveService ?? throw new ArgumentNullException(nameof(regexCollectSaveService));
    }

    public static string ProcessRegex(string inputText, string? regexPattern, string? replacement)
    {
        if (string.IsNullOrEmpty(regexPattern))
        {
            return inputText;
        }
        return Regex.Replace(inputText, regexPattern, Regex.Unescape(replacement ?? string.Empty), RegexOptions.Multiline);
    }

    public string ApplyRegexRules(string inputText, int hotkeyId)
    {
        Debug.WriteLine($"[RegexService] Attempting to apply rules for Hotkey ID: {hotkeyId}");
        RegexPageStructure? regexStructure = _regexCollectSaveService.GetRegexStructureByHotkeyId(hotkeyId);
        RegexChainStructure? regexChain = regexStructure?.RegexChain;

        if (regexChain != null && regexChain.ChainItems != null && regexChain.ChainItems.Any())
        {
            string processedText = inputText;
            bool rulesApplied = false;

            foreach (var chainItem in regexChain.ChainItems)
            {
                processedText = ProcessRegex(processedText, chainItem.RegexExpression, chainItem.Replace);

                if (processedText != inputText || (chainItem.RegexExpression != null && chainItem.RegexExpression != string.Empty) )
                {
                    if(!rulesApplied && chainItem.RegexExpression != null && chainItem.RegexExpression != string.Empty)
                    {
                        Debug.WriteLine($"[RegexService] Applying rule: '{chainItem.RegexExpression}' → '{chainItem.Replace ?? string.Empty}' for Hotkey ID: {hotkeyId}");
                        rulesApplied = true;
                    } else if (rulesApplied && chainItem.RegexExpression != null && chainItem.RegexExpression != string.Empty)
                    {
                         Debug.WriteLine($"[RegexService] Applying next rule: '{chainItem.RegexExpression}' → '{chainItem.Replace ?? string.Empty}' for Hotkey ID: {hotkeyId}");
                    }
                }
            }

            if (rulesApplied)
            {
                Debug.WriteLine($"[RegexService] Successfully applied rules for Hotkey ID: {hotkeyId}.");
                return processedText;
            }
        }
        
        Debug.WriteLine($"[RegexService] No valid rules found or applied for Hotkey ID: {hotkeyId}. Using original text.");
        return inputText;
    }
}