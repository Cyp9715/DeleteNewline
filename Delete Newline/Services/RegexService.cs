using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Delete_Newline.Core.Contracts.Services; // Assuming ISettingsService might be used later
using Delete_Newline.Models; // Assuming Settings models might be used

namespace Delete_Newline.Services
{
    public class RegexService : IRegexService
    {
        // Default rules for general clipboard changes (via OnClipboardContentChanged)
        private readonly List<(string Pattern, string Replacement)> _defaultRules;

        // Hotkey-specific rules. Key: Hotkey ID, Value: List of regex rules for that hotkey.
        // TODO: This should be loaded from HotkeyCollectSaveService
        private readonly Dictionary<int, List<(string Pattern, string Replacement)>> _hotkeySpecificRules;

        public RegexService(/* HotkeyCollectSaveService hotkeyCollectSaveService */)
        {
            // Initialize default rules (as before)
            _defaultRules = new List<(string Pattern, string Replacement)>
            {
                (@"\r?\n+", "\r\n") // Example: Normalize newlines
            };

            // TODO: Replace this with actual loading logic from HotkeyCollectSaveService
            _hotkeySpecificRules = new Dictionary<int, List<(string Pattern, string Replacement)>>
            {
                // Example: Hotkey ID 1 might have specific rules
                { 1, new List<(string Pattern, string Replacement)> { (@"\s+", " ") /* Replace multiple spaces with single */ } },
                // Example: Hotkey ID 2 might remove all digits
                { 2, new List<(string Pattern, string Replacement)> { (@"\d", "") } }
            };
        }

        // Process with default rules
        public string ProcessText(string inputText)
        {
            return ApplyRules(inputText, _defaultRules);
        }

        // Process with hotkey-specific rules
        public string ProcessText(string inputText, int hotkeyId)
        {
            if (_hotkeySpecificRules.TryGetValue(hotkeyId, out var specificRules))
            {
                Debug.WriteLine($"Applying specific rules for hotkey ID: {hotkeyId}");
                return ApplyRules(inputText, specificRules);
            }
            else
            {
                // If no specific rules for this hotkey, maybe apply default or do nothing?
                // For now, let's return the original text or indicate no rules found.
                Debug.WriteLine($"No specific rules found for hotkey ID: {hotkeyId}. Applying default rules instead.");
                return ApplyRules(inputText, _defaultRules); // Or return inputText; if no action is desired
            }
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
                    processedText = Regex.Replace(processedText, rule.Pattern, Regex.Unescape(rule.Replacement));
                }
            }
            catch (ArgumentException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Regex processing error: {ex.Message}");
                return $"{text} [Regex Error: Invalid Pattern]";
            }
            
            return processedText;
        }
    }
} 