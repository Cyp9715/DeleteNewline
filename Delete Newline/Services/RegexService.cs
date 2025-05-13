using System.Diagnostics;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services;

public class RegexService
{
    private readonly HotkeyCollectSaveService _hotkeyCollectSaveService;

    public RegexService(HotkeyCollectSaveService hotkeyCollectSaveService)
    {
        _hotkeyCollectSaveService = hotkeyCollectSaveService ?? throw new ArgumentNullException(nameof(hotkeyCollectSaveService));
    }


    // 단축키로 특정 규칙 적용 시
    public string ApplyHotkeyRules(string inputText, int hotkeyId)
    {
        Debug.WriteLine($"[RegexService] Attempting to apply rules for Hotkey ID: {hotkeyId}");
        HotkeyPageStructure? hotkeyStructure = _hotkeyCollectSaveService.GetHotkeyStructureById(hotkeyId);
        RegexChainStructure? regexChain = hotkeyStructure?.RegexChain;

        if (regexChain != null && regexChain.ChainItems != null && regexChain.ChainItems.Any())
        {
            string processedText = inputText;
            bool rulesApplied = false;

            try
            {
                foreach (var chainItem in regexChain.ChainItems)
                {
                    if (!string.IsNullOrEmpty(chainItem.RegexExpression))
                    {
                        Debug.WriteLine($"[RegexService] Applying rule: '{chainItem.RegexExpression}' -> '{chainItem.Replace ?? string.Empty}' for Hotkey ID: {hotkeyId}");
                        processedText = Regex.Replace(processedText, chainItem.RegexExpression, Regex.Unescape(chainItem.Replace ?? string.Empty));
                        rulesApplied = true;
                    }
                }

                if (rulesApplied)
                {
                    Debug.WriteLine($"[RegexService] Successfully applied rules for Hotkey ID: {hotkeyId}.");
                    return processedText;
                }
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine($"[RegexService] Regex processing error for Hotkey ID: {hotkeyId}: {ex.Message}. Input text: {inputText.Substring(0, Math.Min(inputText.Length, 50))}...");
                return $"{inputText} [Regex Error: Invalid Pattern in Chain]";
            }
        }
        
        Debug.WriteLine($"[RegexService] No valid rules found or applied for Hotkey ID: {hotkeyId}. Using original text.");
        return inputText;
    }
}