using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;
using Microsoft.UI.Xaml.Controls;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Models;

namespace Delete_Newline.Services
{
    public class RegexService : IRegexService
    {
        private readonly List<(string Pattern, string Replacement)> _defaultRules;
        private readonly HotkeyCollectSaveService _hotkeyCollectSaveService;

        public RegexService(HotkeyCollectSaveService hotkeyCollectSaveService)
        {
            _hotkeyCollectSaveService = hotkeyCollectSaveService ?? throw new ArgumentNullException(nameof(hotkeyCollectSaveService));

            _defaultRules = new List<(string Pattern, string Replacement)>
            {
                (@"\r?\n+", "\r\n")
            };
        }

        // 일반 클립보드 변경 시 (기본 규칙 적용)
        public string ProcessText(string inputText)
        {
            Debug.WriteLine("[RegexService] Applying default rules.");
            return ApplyRules(inputText, _defaultRules);
        }

        // 단축키로 특정 규칙 적용 시
        public string ProcessText(string inputText, int hotkeyId)
        {
            Debug.WriteLine($"[RegexService] Attempting to apply rules for Hotkey ID: {hotkeyId}");
            RegexChainStructure? regexChain = _hotkeyCollectSaveService.GetRegexChainByHotkeyId(hotkeyId);

            if (regexChain != null && regexChain.ChainItems != null && regexChain.ChainItems.Any())
            {
                var rulesToApply = new List<(string Pattern, string Replacement)>();
                foreach (var chainItem in regexChain.ChainItems)
                {
                    if (!string.IsNullOrEmpty(chainItem.RegexExpression))
                    {
                        rulesToApply.Add((chainItem.RegexExpression, chainItem.Replace ?? string.Empty));
                    }
                }

                if (rulesToApply.Any())
                {
                    Debug.WriteLine($"[RegexService] Found {rulesToApply.Count} rules for Hotkey ID: {hotkeyId}. Applying sequentially...");
                    return ApplyRulesSequentially(inputText, rulesToApply);
                }
                else
                {
                    Debug.WriteLine($"[RegexService] No valid patterns in RegexChain for Hotkey ID: {hotkeyId}. Falling back to default rules.");
                    return ApplyRules(inputText, _defaultRules);
                }
            }
            else
            {
                Debug.WriteLine($"[RegexService] No RegexChain found or chain is empty for Hotkey ID: {hotkeyId}. Falling back to default rules.");
                return ApplyRules(inputText, _defaultRules);
            }
        }

        private string ApplyRulesSequentially(string text, List<(string Pattern, string Replacement)> rules)
        {
            if (string.IsNullOrEmpty(text) || rules == null || !rules.Any())
            {
                return text;
            }

            string processedText = text;

            try
            {
                foreach (var rule in rules)
                {
                    if (!string.IsNullOrEmpty(rule.Pattern))
                    {
                        processedText = Regex.Replace(processedText, rule.Pattern, Regex.Unescape(rule.Replacement ?? string.Empty));
                    }
                }
            }
            catch (ArgumentException ex) 
            {
                Debug.WriteLine($"[RegexService] Regex processing error: {ex.Message}. Input text: {text.Substring(0, Math.Min(text.Length, 50))}...");
                return $"{text} [Regex Error: Invalid Pattern in Chain]";
            }
            
            return processedText;
        }

        private string ApplyRules(string text, List<(string Pattern, string Replacement)> rules)
        {
            if (string.IsNullOrEmpty(text) || rules == null || rules.Count == 0)
            {
                return text;
            }

            string processedText = text;

            try
            {
                foreach (var rule in rules)
                {
                    if (string.IsNullOrEmpty(rule.Pattern) is false)
                    {
                        processedText = Regex.Replace(processedText, rule.Pattern, Regex.Unescape(rule.Replacement ?? string.Empty));
                    }
                }
            }
            catch (ArgumentException ex) 
            {
                Debug.WriteLine($"[RegexService] Regex processing error in default rules: {ex.Message}.");
                return $"{text} [Regex Error: Invalid Default Pattern]"; 
            }
            
            return processedText;
        }
    }
} 