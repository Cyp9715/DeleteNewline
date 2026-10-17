using System.Diagnostics;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;

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
        return RegexTextProcessor.ProcessRegex(
            inputText,
            regexPattern,
            replacement,
            LocalizationHelper.GetLocalizedString("RegexService_InvalidRegex_Message"));
    }

    public Task<string> ApplyRegexRulesAsync(string inputText, int hotkeyId)
    {
        Debug.WriteLine($"[RegexService] Attempting to apply rules for Hotkey ID: {hotkeyId}");
        RegexPageStructure? regexStructure = _regexCollectSaveService.GetRegexStructureByHotkeyId(hotkeyId);
        RegexChainStructure? regexChain = regexStructure?.RegexChain;

        if (regexChain?.ChainItems is { Count: > 0 })
        {
            // Snapshot on the UI thread before processing so editing a profile cannot change an active operation.
            ChainItem[] rules = regexChain.ChainItems.Select(item => new ChainItem
            {
                RegexExpression = item.RegexExpression,
                Replace = item.Replace
            }).ToArray();
            return Task.Run(() => RegexTextProcessor.Transform(inputText, rules));
        }
        
        Debug.WriteLine($"[RegexService] No valid rules found or applied for Hotkey ID: {hotkeyId}. Using original text.");
        return Task.FromResult(inputText);
    }
}
