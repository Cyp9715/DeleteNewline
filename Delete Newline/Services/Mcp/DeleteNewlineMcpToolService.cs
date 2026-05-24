using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Windows.Globalization;
using Windows.System;

namespace Delete_Newline.Services.Mcp;

public sealed class DeleteNewlineMcpToolService
{
    private sealed record OcrLanguageActionDto(
        string LanguageTag,
        string DisplayName,
        bool IsInstalled,
        bool IsCurrent,
        bool CanSelect,
        string Action,
        string RecommendedTool);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IMcpRegexConfigurationRepository _regexRepository;
    private readonly IMcpSettingsRepository _settingsRepository;
    private readonly IMcpOcrConfigurationRepository _ocrRepository;

    public DeleteNewlineMcpToolService(
        IMcpRegexConfigurationRepository regexRepository,
        IMcpSettingsRepository settingsRepository,
        IMcpOcrConfigurationRepository ocrRepository)
    {
        _regexRepository = regexRepository;
        _settingsRepository = settingsRepository;
        _ocrRepository = ocrRepository;
    }

    public IReadOnlyList<McpToolDescriptor> ListTools()
    {
        return
        [
            new McpToolDescriptor
            {
                Name = "get_regex_profiles",
                Title = "Get regex profiles",
                Description = "Return the configured Delete Newline regex profiles, hotkeys, test input text, and regex chains.",
                InputSchema = Schema("""
                { "type": "object", "properties": {}, "additionalProperties": false }
                """),
                ReadOnly = true,
                Destructive = false
            },
            new McpToolDescriptor
            {
                Name = "upsert_regex_profile",
                Title = "Create or update a regex profile",
                Description = "Create a new regex profile or replace an existing zero-based profile. Supports profile name/comment, hotkey, test input text, and a full regex chain. For editing one rule in the middle, prefer insert_regex_chain_item, update_regex_chain_item, or delete_regex_chain_item so the model does not need to delete/recreate and renumber the whole chain. Changes are saved immediately and hotkeys are re-registered immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "index": { "type": "integer", "minimum": 0, "description": "Existing zero-based profile index to replace. Omit to append a new profile." },
                    "name": { "type": "string", "description": "Profile display name. Required for new profiles." },
                    "comment": { "type": "string", "description": "Profile description/comment." },
                    "inputText": { "type": "string", "description": "Saved test input text shown on the Regex page." },
                    "input": { "type": "string", "description": "Alias for inputText." },
                    "hotkey": {
                      "description": "Hotkey string like 'Control+Shift+Q', 'Alt+Number1', 'Alt+1', 'Ctrl+Backspace', 'Ctrl+Left Arrow', 'Ctrl+Page Up', or 'Ctrl+Backtick', or object with modifiers/key. For keyboard number-row keys use Number0..Number9 (digit text 0..9 is also accepted). Common aliases such as Backspace, Return, Del, Spacebar, Page Up/Page Down, Left/Right/Up/Down Arrow, Backtick, Semicolon, Slash, Backslash, Minus, Equals, Comma, Period, LeftBracket, RightBracket, and Quote are accepted; symbol aliases such as `, ;, /, \\, -, =, ,, ., [, ], and ' are also accepted. Do not send JSON numbers for hotkey keys; Windows virtual-key value 1 means LeftButton, not the keyboard 1 key.",
                      "oneOf": [
                        { "type": "string", "description": "Preferred concise form. Examples: Control+Shift+Q, Alt+Number1, Alt+1." },
                        {
                          "type": "object",
                          "properties": {
                            "modifiers": {
                              "description": "Modifier names as a '+' string such as Control+Shift or an array such as [Control, Shift]. Alt is accepted as an alias for Menu.",
                              "oneOf": [
                                { "type": "string" },
                                { "type": "array", "items": { "type": "string" } }
                              ]
                            },
                            "key": { "type": "string", "description": "Human-friendly key name or Windows.System.VirtualKey name such as Q, F1, Enter, Escape, Space, Number1, Backspace, Spacebar, Return, Del, Page Up, Page Down, Left Arrow, Backtick, Semicolon, Slash, Backslash, Minus, Equals, Comma, Period, LeftBracket, RightBracket, or Quote. Symbol aliases like `, ;, /, \\, -, =, ,, ., [, ], and ' are accepted. For keyboard number-row keys use Number0..Number9; digit strings like '1' are accepted and normalized to Number1. Do not send JSON numbers for keys." }
                          },
                          "required": ["key"],
                          "additionalProperties": false
                        }
                      ]
                    },
                    "chain": {
                      "type": "array",
                      "description": "Full ordered regex chain. Use insert/update/delete_regex_chain_item for one-rule edits instead of rebuilding this array.",
                      "items": {
                        "type": "object",
                        "properties": {
                          "regex": { "type": "string", "description": "Delete Newline/.NET regular expression pattern. Test with test_regex_chain before saving complex patterns." },
                          "replace": { "type": "string", "description": "Replacement text. Delete Newline unescapes sequences such as \\n before Regex.Replace, matching the Regex page/runtime behavior." }
                        },
                        "additionalProperties": false
                      }
                    }
                  },
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "insert_regex_chain_item",
                Title = "Insert one regex rule into a profile chain",
                Description = "Insert a single regex rule at profileIndex/chainIndex. Use this instead of deleting and recreating a whole profile when adding a rule in the middle: the existing rule at chainIndex and every later rule shifts right automatically. chainIndex is zero-based and may equal the current chain length to append. Recommended workflow: call get_regex_profiles, optionally call test_regex_chain with sample text, then call this tool. Changes are saved immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "profileIndex": { "type": "integer", "minimum": 0, "description": "Zero-based profile index returned by get_regex_profiles." },
                    "chainIndex": { "type": "integer", "minimum": 0, "description": "Zero-based insertion position within that profile's chain. 0 inserts first; current chain length appends last." },
                    "regex": { "type": "string", "description": "Rule pattern using Delete Newline/.NET regex syntax." },
                    "replace": { "type": "string", "description": "Replacement text. Omit or use empty string to delete matches." }
                  },
                  "required": ["profileIndex", "chainIndex", "regex"],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "update_regex_chain_item",
                Title = "Update one regex rule in a profile chain",
                Description = "Update exactly one existing regex rule at profileIndex/chainIndex without rebuilding the rest of the profile. Provide regex, replace, or both; omitted fields keep their current value. Use get_regex_profiles first to locate the profile/rule and test_regex_chain before saving risky regex changes. Changes are saved immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "profileIndex": { "type": "integer", "minimum": 0, "description": "Zero-based profile index returned by get_regex_profiles." },
                    "chainIndex": { "type": "integer", "minimum": 0, "description": "Zero-based existing rule index within the profile's chain." },
                    "regex": { "type": "string", "description": "New regex pattern. Omit to keep the existing pattern." },
                    "replace": { "type": "string", "description": "New replacement text. Omit to keep the existing replacement." }
                  },
                  "required": ["profileIndex", "chainIndex"],
                  "anyOf": [
                    { "required": ["regex"] },
                    { "required": ["replace"] }
                  ],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "delete_regex_chain_item",
                Title = "Delete one regex rule from a profile chain",
                Description = "Delete exactly one regex rule at profileIndex/chainIndex without rebuilding the profile. Later rules shift left automatically. Use get_regex_profiles first to verify the target. Changes are saved immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "profileIndex": { "type": "integer", "minimum": 0, "description": "Zero-based profile index returned by get_regex_profiles." },
                    "chainIndex": { "type": "integer", "minimum": 0, "description": "Zero-based existing rule index within the profile's chain to delete." }
                  },
                  "required": ["profileIndex", "chainIndex"],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "delete_regex_profile",
                Title = "Delete a regex profile",
                Description = "Delete a whole regex profile by zero-based profile index and unregister its hotkey immediately. To delete only one rule inside a profile, use delete_regex_chain_item instead.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "index": { "type": "integer", "minimum": 0, "description": "Zero-based profile index returned by get_regex_profiles." }
                  },
                  "required": ["index"],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "test_regex_chain",
                Title = "Test a regex chain",
                Description = "Apply either an existing profile's chain or an inline chain to sample input text and return the output without changing saved settings. This uses the same shared regex processing code as Delete Newline's Regex page/runtime, including RegexOptions.Multiline and replacement unescaping, so use it as a dry run before saving regex changes.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "input": { "type": "string", "description": "Sample input text to transform." },
                    "profileIndex": { "type": "integer", "minimum": 0, "description": "Existing zero-based profile index to test. Preferred over the legacy index alias." },
                    "index": { "type": "integer", "minimum": 0, "description": "Backward-compatible alias for profileIndex." },
                    "chain": {
                      "type": "array",
                      "description": "Inline ordered chain to test without saving. If provided, it is used instead of profileIndex/index.",
                      "items": {
                        "type": "object",
                        "properties": {
                          "regex": { "type": "string", "description": "Delete Newline/.NET regular expression pattern." },
                          "replace": { "type": "string", "description": "Replacement text using Delete Newline runtime semantics; e.g. \\n becomes a newline before Regex.Replace." }
                        },
                        "additionalProperties": false
                      }
                    }
                  },
                  "required": ["input"],
                  "additionalProperties": false
                }
                """),
                ReadOnly = true,
                Destructive = false
            },
            new McpToolDescriptor
            {
                Name = "get_ocr_settings",
                Title = "Get OCR settings",
                Description = "Return Delete Newline OCR page settings, including OCR language, available language tags, and OCR hotkey.",
                InputSchema = Schema("""
                { "type": "object", "properties": {}, "additionalProperties": false }
                """),
                ReadOnly = true,
                Destructive = false
            },
            new McpToolDescriptor
            {
                Name = "set_ocr_settings",
                Title = "Set OCR settings",
                Description = "Set OCR language and/or OCR hotkey. Changes are saved immediately and the OCR hotkey is re-registered immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "languageTag": { "type": "string", "description": "OCR recognizer language tag, e.g. en-US or ko-KR. If the desired language is not installed yet, call get_ocr_languages first and then install_ocr_language with a tag from installableLanguageTags." },
                    "language": { "type": "string", "description": "Alias for languageTag." },
                    "hotkey": {
                      "description": "Hotkey string like 'Control+Menu+O', 'Alt+Number1', 'Alt+1', 'Ctrl+Backspace', 'Ctrl+Left Arrow', 'Ctrl+Page Up', or 'Ctrl+Backtick', or object with modifiers/key. For keyboard number-row keys use Number0..Number9 (digit text 0..9 is also accepted). Common aliases such as Backspace, Return, Del, Spacebar, Page Up/Page Down, Left/Right/Up/Down Arrow, Backtick, Semicolon, Slash, Backslash, Minus, Equals, Comma, Period, LeftBracket, RightBracket, and Quote are accepted; symbol aliases such as `, ;, /, \\, -, =, ,, ., [, ], and ' are also accepted. Do not send JSON numbers for hotkey keys; Windows virtual-key value 1 means LeftButton, not the keyboard 1 key.",
                      "oneOf": [
                        { "type": "string", "description": "Preferred concise form. Examples: Control+Menu+O, Alt+Number1, Alt+1." },
                        {
                          "type": "object",
                          "properties": {
                            "modifiers": {
                              "description": "Modifier names as a '+' string such as Control+Menu or an array such as [Control, Menu]. Alt is accepted as an alias for Menu.",
                              "oneOf": [
                                { "type": "string" },
                                { "type": "array", "items": { "type": "string" } }
                              ]
                            },
                            "key": { "type": "string", "description": "Human-friendly key name or Windows.System.VirtualKey name such as O, F1, Enter, Escape, Space, Number1, Backspace, Spacebar, Return, Del, Page Up, Page Down, Left Arrow, Backtick, Semicolon, Slash, Backslash, Minus, Equals, Comma, Period, LeftBracket, RightBracket, or Quote. Symbol aliases like `, ;, /, \\, -, =, ,, ., [, ], and ' are accepted. For keyboard number-row keys use Number0..Number9; digit strings like '1' are accepted and normalized to Number1. Do not send JSON numbers for keys." }
                          },
                          "required": ["key"],
                          "additionalProperties": false
                        }
                      ]
                    }
                  },
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "get_ocr_languages",
                Title = "Get OCR languages",
                Description = "Return the current OCR language and a simple language action table for small/local models. Read manageableLanguages, copy one exact languageTag, then call the recommendedTool. availableLanguageTags are installed and selectable now. installableLanguageTags can be downloaded with install_ocr_language. deletableLanguageTags are installed, not current, and safe to delete with delete_ocr_language. Do not delete the current OCR language.",
                InputSchema = Schema("""
                { "type": "object", "properties": {}, "additionalProperties": false }
                """),
                ReadOnly = true,
                Destructive = false
            },
            new McpToolDescriptor
            {
                Name = "install_ocr_language",
                Title = "Download and apply an OCR language",
                Description = "Download/install one missing Windows OCR language, refresh Delete Newline OCR languages, select that language, and save it immediately. Step 1: call get_ocr_languages. Step 2: choose an item where action is 'install' or copy an exact languageTag from installableLanguageTags. Step 3: call this tool with exactly { \"languageTag\": \"ko-KR\" }. If the language action is 'set', call set_ocr_settings instead. This may show a Windows elevation prompt.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "languageTag": { "type": "string", "description": "Exact Windows OCR language tag to install and apply, for example ko-KR, ja-JP, en-US, zh-CN, fr-FR, or de-DE. Prefer copying a tag from get_ocr_languages.installableLanguageTags or from manageableLanguages where action is 'install'." },
                    "language": { "type": "string", "description": "Alias for languageTag." },
                    "tag": { "type": "string", "description": "Alias for languageTag." }
                  },
                  "anyOf": [
                    { "required": ["languageTag"] },
                    { "required": ["language"] },
                    { "required": ["tag"] }
                  ],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "delete_ocr_language",
                Title = "Delete an OCR language",
                Description = "Delete one installed Windows OCR language that is not the current OCR language. Step 1: call get_ocr_languages. Step 2: copy an exact languageTag from deletableLanguageTags or from manageableLanguages where action is 'delete'. Step 3: call this tool with exactly { \"languageTag\": \"ko-KR\" }. Do not delete the current OCR language; select another OCR language first with set_ocr_settings. This may show a Windows elevation prompt.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "languageTag": { "type": "string", "description": "Exact installed Windows OCR language tag to delete, for example ko-KR, ja-JP, en-US, zh-CN, fr-FR, or de-DE. Copy a tag from get_ocr_languages.deletableLanguageTags or from manageableLanguages where action is 'delete'. Do not use the current OCR language tag." },
                    "language": { "type": "string", "description": "Alias for languageTag." },
                    "tag": { "type": "string", "description": "Alias for languageTag." }
                  },
                  "anyOf": [
                    { "required": ["languageTag"] },
                    { "required": ["language"] },
                    { "required": ["tag"] }
                  ],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            },
            new McpToolDescriptor
            {
                Name = "get_app_settings",
                Title = "Get app settings",
                Description = "Return MCP-visible Delete Newline Settings page values, including MCP enabled state, MCP port, endpoint, notification, start-on-tray, top-most, theme, language, and startup-task support.",
                InputSchema = Schema("""
                { "type": "object", "properties": {}, "additionalProperties": false }
                """),
                ReadOnly = true,
                Destructive = false
            },
            new McpToolDescriptor
            {
                Name = "set_app_setting",
                Title = "Set an app setting",
                Description = "Set a Delete Newline Settings page value and persist it immediately. Supported keys include McpEnabled, McpPort, Notification, StartOnTray, TopMost, Theme, Language, and StartupTask. McpPort accepts only an integer from 1 to 65535 and can be changed only while MCP is disabled.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "key": { "type": "string" },
                    "value": { "oneOf": [ { "type": "boolean" }, { "type": "integer" }, { "type": "number" }, { "type": "string" } ] }
                  },
                  "required": ["key", "value"],
                  "additionalProperties": false
                }
                """),
                ReadOnly = false,
                Destructive = true
            }
        ];
    }

    public async Task<McpToolResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken)
    {
        try
        {
            return toolName switch
            {
                "get_regex_profiles" => GetRegexProfiles(),
                "upsert_regex_profile" => await UpsertRegexProfileAsync(arguments, cancellationToken),
                "insert_regex_chain_item" => await InsertRegexChainItemAsync(arguments, cancellationToken),
                "update_regex_chain_item" => await UpdateRegexChainItemAsync(arguments, cancellationToken),
                "delete_regex_chain_item" => await DeleteRegexChainItemAsync(arguments, cancellationToken),
                "delete_regex_profile" => await DeleteRegexProfileAsync(arguments, cancellationToken),
                "test_regex_chain" => TestRegexChain(arguments),
                "get_ocr_settings" => GetOcrSettings(),
                "set_ocr_settings" => await SetOcrSettingsAsync(arguments, cancellationToken),
                "get_ocr_languages" => GetOcrLanguages(),
                "install_ocr_language" => await InstallOcrLanguageAsync(arguments, cancellationToken),
                "delete_ocr_language" => await DeleteOcrLanguageAsync(arguments, cancellationToken),
                "get_app_settings" => GetAppSettings(),
                "set_app_setting" => await SetAppSettingAsync(arguments, cancellationToken),
                _ => McpToolResult.Error($"Unknown tool: {toolName}")
            };
        }
        catch (JsonException ex)
        {
            return McpToolResult.Error($"Invalid JSON arguments: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return McpToolResult.Error(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return McpToolResult.Error(ex.Message);
        }
        catch (RegexMatchTimeoutException ex)
        {
            return McpToolResult.Error($"Regex timed out: {ex.Message}");
        }
        catch (Exception ex)
        {
            return McpToolResult.Error($"Tool execution failed: {ex.Message}");
        }
    }

    private McpToolResult GetRegexProfiles()
    {
        var profiles = _regexRepository.RegexConfigs.Select(ToProfileDto).ToArray();
        return JsonSuccess(profiles);
    }

    private async Task<McpToolResult> UpsertRegexProfileAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        int? index = TryGetInt32(arguments, "index", out int parsedIndex) ? parsedIndex : null;
        RegexPageStructure? existing = index.HasValue && index.Value >= 0 && index.Value < _regexRepository.RegexConfigs.Count
            ? _regexRepository.RegexConfigs[index.Value]
            : null;

        string name = GetString(arguments, "name") ?? existing?.HotkeyName ?? throw new ArgumentException("name is required for new regex profiles.");
        string comment = GetString(arguments, "comment") ?? existing?.HotkeyComment ?? string.Empty;
        string inputText = GetString(arguments, "inputText") ?? GetString(arguments, "input") ?? existing?.InputText ?? string.Empty;
        HotkeyStructure hotkey = GetProperty(arguments, "hotkey") is JsonElement hotkeyElement
            ? ParseHotkey(hotkeyElement)
            : CloneHotkey(existing?.Hotkey);

        RegexPageStructure config = new()
        {
            HotkeyName = name,
            HotkeyComment = comment,
            Hotkey = hotkey,
            InputText = inputText,
            RegexChain = new RegexChainStructure()
        };

        JsonElement? chainElement = GetProperty(arguments, "chain");
        if (chainElement.HasValue)
        {
            config.RegexChain.ChainItems = new ObservableCollection<ChainItem>(ParseChain(chainElement.Value));
        }
        else if (existing != null)
        {
            config.RegexChain.ChainItems = new ObservableCollection<ChainItem>(existing.RegexChain.ChainItems.Select(CloneChainItem));
        }
        else
        {
            throw new ArgumentException("chain is required for new regex profiles.");
        }

        await _regexRepository.UpsertAsync(index, config, cancellationToken);
        int savedIndex = index.HasValue && index.Value >= 0 && index.Value < _regexRepository.RegexConfigs.Count
            ? index.Value
            : _regexRepository.RegexConfigs.Count - 1;

        RegexPageStructure savedProfile = savedIndex >= 0 && savedIndex < _regexRepository.RegexConfigs.Count
            ? _regexRepository.RegexConfigs[savedIndex]
            : config;

        return JsonSuccess(new
        {
            saved = true,
            index = savedIndex,
            profile = ToProfileDto(savedProfile, savedIndex)
        });
    }

    private async Task<McpToolResult> InsertRegexChainItemAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        int profileIndex = GetRequiredInt32(arguments, "profileIndex");
        int chainIndex = GetRequiredInt32(arguments, "chainIndex");
        string regex = GetString(arguments, "regex") ?? throw new ArgumentException("regex is required.");
        string replace = GetString(arguments, "replace") ?? string.Empty;

        RegexPageStructure config = CloneRegexProfile(GetRegexProfileOrThrow(profileIndex));
        ObservableCollection<ChainItem> chainItems = config.RegexChain.ChainItems;
        if (chainIndex < 0 || chainIndex > chainItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(chainIndex), $"chainIndex {chainIndex} is outside the valid insertion range 0..{chainItems.Count} for profileIndex {profileIndex}.");
        }

        ChainItem insertedItem = new()
        {
            RegexExpression = regex,
            Replace = replace
        };
        chainItems.Insert(chainIndex, insertedItem);

        await _regexRepository.UpsertAsync(profileIndex, config, cancellationToken);
        RegexPageStructure savedProfile = GetSavedRegexProfileOrFallback(profileIndex, config);
        return JsonSuccess(new
        {
            saved = true,
            profileIndex,
            chainIndex,
            insertedItem = ToChainItemDto(insertedItem),
            profile = ToProfileDto(savedProfile, profileIndex)
        });
    }

    private async Task<McpToolResult> UpdateRegexChainItemAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        int profileIndex = GetRequiredInt32(arguments, "profileIndex");
        int chainIndex = GetRequiredInt32(arguments, "chainIndex");
        bool hasRegex = TryGetString(arguments, "regex", out string regex);
        bool hasReplace = TryGetString(arguments, "replace", out string replace);
        if (!hasRegex && !hasReplace)
        {
            throw new ArgumentException("Provide regex and/or replace.");
        }

        RegexPageStructure config = CloneRegexProfile(GetRegexProfileOrThrow(profileIndex));
        ObservableCollection<ChainItem> chainItems = config.RegexChain.ChainItems;
        EnsureExistingChainIndex(profileIndex, chainIndex, chainItems.Count);

        ChainItem existingItem = chainItems[chainIndex];
        ChainItem updatedItem = new()
        {
            RegexExpression = hasRegex ? regex : existingItem.RegexExpression,
            Replace = hasReplace ? replace : existingItem.Replace
        };
        chainItems[chainIndex] = updatedItem;

        await _regexRepository.UpsertAsync(profileIndex, config, cancellationToken);
        RegexPageStructure savedProfile = GetSavedRegexProfileOrFallback(profileIndex, config);
        return JsonSuccess(new
        {
            saved = true,
            profileIndex,
            chainIndex,
            updatedItem = ToChainItemDto(updatedItem),
            profile = ToProfileDto(savedProfile, profileIndex)
        });
    }

    private async Task<McpToolResult> DeleteRegexChainItemAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        int profileIndex = GetRequiredInt32(arguments, "profileIndex");
        int chainIndex = GetRequiredInt32(arguments, "chainIndex");

        RegexPageStructure config = CloneRegexProfile(GetRegexProfileOrThrow(profileIndex));
        ObservableCollection<ChainItem> chainItems = config.RegexChain.ChainItems;
        EnsureExistingChainIndex(profileIndex, chainIndex, chainItems.Count);

        ChainItem removedItem = chainItems[chainIndex];
        chainItems.RemoveAt(chainIndex);

        await _regexRepository.UpsertAsync(profileIndex, config, cancellationToken);
        RegexPageStructure savedProfile = GetSavedRegexProfileOrFallback(profileIndex, config);
        return JsonSuccess(new
        {
            saved = true,
            profileIndex,
            chainIndex,
            deletedItem = ToChainItemDto(removedItem),
            profile = ToProfileDto(savedProfile, profileIndex)
        });
    }

    private async Task<McpToolResult> DeleteRegexProfileAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!TryGetInt32(arguments, "index", out int index))
        {
            throw new ArgumentException("index is required.");
        }

        bool removed = await _regexRepository.DeleteAsync(index, cancellationToken);
        return removed
            ? JsonSuccess(new { deleted = true, index })
            : McpToolResult.Error($"Regex profile index {index} does not exist.");
    }

    private McpToolResult TestRegexChain(JsonElement arguments)
    {
        string input = GetString(arguments, "input") ?? string.Empty;
        IReadOnlyList<ChainItem> chain;

        JsonElement? inlineChain = GetProperty(arguments, "chain");
        if (inlineChain.HasValue)
        {
            chain = ParseChain(inlineChain.Value).ToArray();
        }
        else if (TryGetInt32(arguments, "profileIndex", out int profileIndex) && profileIndex >= 0 && profileIndex < _regexRepository.RegexConfigs.Count)
        {
            chain = _regexRepository.RegexConfigs[profileIndex].RegexChain.ChainItems.ToArray();
        }
        else if (TryGetInt32(arguments, "index", out int index) && index >= 0 && index < _regexRepository.RegexConfigs.Count)
        {
            chain = _regexRepository.RegexConfigs[index].RegexChain.ChainItems.ToArray();
        }
        else
        {
            throw new ArgumentException("Provide either chain or a valid profileIndex.");
        }

        string output = RegexTextProcessor.ApplyRegexChain(input, chain, "Invalid regex");
        return JsonSuccess(new { input, output });
    }

    private McpToolResult GetOcrSettings()
    {
        return JsonSuccess(new
        {
            languageTag = _ocrRepository.LanguageTag,
            availableLanguageTags = _ocrRepository.AvailableLanguageTags,
            installableLanguageTags = _ocrRepository.InstallableLanguages.Select(language => language.LanguageTag).ToArray(),
            hotkey = ToHotkeyDto(_ocrRepository.Hotkey)
        });
    }

    private McpToolResult GetOcrLanguages()
    {
        OcrLanguageActionDto[] manageableLanguages = BuildManageableLanguageDtos();
        string[] deletableLanguageTags = manageableLanguages
            .Where(item => item.Action == "delete")
            .Select(item => item.LanguageTag)
            .ToArray();

        return JsonSuccess(new
        {
            languageTag = _ocrRepository.LanguageTag,
            availableLanguageTags = _ocrRepository.AvailableLanguageTags,
            availableLanguages = _ocrRepository.AvailableLanguages.Select(ToOcrLanguageDto).ToArray(),
            installableLanguageTags = _ocrRepository.InstallableLanguages.Select(language => language.LanguageTag).ToArray(),
            installableLanguages = _ocrRepository.InstallableLanguages.Select(ToOcrLanguageDto).ToArray(),
            deletableLanguageTags,
            manageableLanguages,
            recommendedWorkflow = "Call get_ocr_languages first. To download and apply a missing OCR language, copy an exact languageTag from installableLanguageTags and call install_ocr_language. To delete an installed OCR language that is not current, copy an exact languageTag from deletableLanguageTags and call delete_ocr_language. Do not delete the current OCR language. To switch to an installed language, copy an exact languageTag from availableLanguageTags and call set_ocr_settings."
        });
    }

    private async Task<McpToolResult> InstallOcrLanguageAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string languageTag = GetString(arguments, "languageTag") ?? GetString(arguments, "language") ?? GetString(arguments, "tag") ?? throw new ArgumentException("languageTag is required. Call get_ocr_languages and copy a tag from installableLanguageTags.");

        McpOcrLanguageInstallResult result = await _ocrRepository.InstallAndApplyLanguageAsync(languageTag, cancellationToken);
        return result.Success
            ? JsonSuccess(result)
            : McpToolResult.Error(result.Message);
    }

    private async Task<McpToolResult> DeleteOcrLanguageAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string languageTag = GetString(arguments, "languageTag") ?? GetString(arguments, "language") ?? GetString(arguments, "tag") ?? throw new ArgumentException("languageTag is required. Call get_ocr_languages and copy a tag from deletableLanguageTags.");

        McpOcrLanguageDeleteResult result = await _ocrRepository.DeleteLanguageAsync(languageTag, cancellationToken);
        return result.Success
            ? JsonSuccess(result)
            : McpToolResult.Error(result.Message);
    }

    private async Task<McpToolResult> SetOcrSettingsAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string? languageTag = GetString(arguments, "languageTag") ?? GetString(arguments, "language");
        HotkeyStructure? hotkey = GetProperty(arguments, "hotkey") is JsonElement hotkeyElement
            ? ParseHotkey(hotkeyElement)
            : null;

        if (string.IsNullOrWhiteSpace(languageTag) && hotkey == null)
        {
            throw new ArgumentException("Provide languageTag and/or hotkey.");
        }

        await _ocrRepository.SetAsync(languageTag, hotkey, cancellationToken);
        return JsonSuccess(new
        {
            saved = true,
            languageTag = _ocrRepository.LanguageTag,
            hotkey = ToHotkeyDto(_ocrRepository.Hotkey)
        });
    }

    private McpToolResult GetAppSettings()
    {
        return JsonSuccess(_settingsRepository.GetAllSettings());
    }

    private async Task<McpToolResult> SetAppSettingAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        string key = GetString(arguments, "key") ?? throw new ArgumentException("key is required.");
        JsonElement valueElement = GetProperty(arguments, "value") ?? throw new ArgumentException("value is required.");

        if (McpPortPolicy.IsEnabledKey(key))
        {
            bool enabled = valueElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new ArgumentException($"{McpPortPolicy.EnabledSettingsKey} requires a boolean value.")
            };

            await _settingsRepository.SetMcpEnabledAsync(enabled, deferStop: enabled == false, cancellationToken);
            return JsonSuccess(new
            {
                saved = true,
                key = McpPortPolicy.EnabledSettingsKey,
                value = _settingsRepository.GetValue(McpPortPolicy.EnabledSettingsKey) ?? enabled,
                settings = _settingsRepository.GetAllSettings()
            });
        }

        if (McpPortPolicy.IsPortKey(key))
        {
            int port = McpPortPolicy.ParseJsonPortValue(valueElement);
            bool isMcpEnabled = _settingsRepository.GetBoolean(McpPortPolicy.EnabledSettingsKey);
            int? currentPort = McpPortPolicy.TryReadPort(_settingsRepository.GetValue(McpPortPolicy.PortSettingsKey));
            McpPortPolicy.EnsureCanChangePort(isMcpEnabled, currentPort, port);

            await _settingsRepository.SetValueAsync(McpPortPolicy.PortSettingsKey, port, cancellationToken);
            return JsonSuccess(new
            {
                saved = true,
                key = McpPortPolicy.PortSettingsKey,
                value = _settingsRepository.GetValue(McpPortPolicy.PortSettingsKey) ?? port,
                settings = _settingsRepository.GetAllSettings()
            });
        }

        object value = ParseSettingValue(valueElement);
        if (!IsSupportedAppSettingKey(key))
        {
            throw new ArgumentException($"Unsupported setting key '{key}'. Supported keys: McpEnabled, McpPort, Notification, StartOnTray, TopMost, Theme, Language, StartupTask.");
        }

        await _settingsRepository.SetValueAsync(key, value, cancellationToken);
        return JsonSuccess(new
        {
            saved = true,
            key,
            value = _settingsRepository.GetValue(key) ?? value,
            settings = _settingsRepository.GetAllSettings()
        });
    }

    private static IEnumerable<ChainItem> ParseChain(JsonElement chainElement)
    {
        if (chainElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("chain must be an array.");
        }

        foreach (JsonElement itemElement in chainElement.EnumerateArray())
        {
            string regex = GetString(itemElement, "regex") ?? string.Empty;
            string replace = GetString(itemElement, "replace") ?? string.Empty;
            yield return new ChainItem
            {
                RegexExpression = regex,
                Replace = replace
            };
        }
    }

    private static HotkeyStructure ParseHotkey(JsonElement hotkeyElement)
    {
        if (hotkeyElement.ValueKind == JsonValueKind.String)
        {
            return HotkeyFormatter.ParseHotkeyText(hotkeyElement.GetString() ?? string.Empty);
        }

        if (hotkeyElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("hotkey must be a string or an object.");
        }

        JsonElement? keyElement = GetProperty(hotkeyElement, "key");
        if (!keyElement.HasValue)
        {
            throw new ArgumentException("hotkey.key is required.");
        }

        VirtualKeyModifiers modifiers = GetProperty(hotkeyElement, "modifiers") is JsonElement modifiersElement
            ? ParseModifiers(modifiersElement)
            : VirtualKeyModifiers.None;

        VirtualKey key = ParseVirtualKey(keyElement.Value);
        return new HotkeyStructure
        {
            Modifiers = modifiers,
            Key = key
        };
    }

    private static VirtualKeyModifiers ParseModifiers(JsonElement modifiersElement)
    {
        return modifiersElement.ValueKind switch
        {
            JsonValueKind.Number when modifiersElement.TryGetInt32(out int value) => (VirtualKeyModifiers)value,
            JsonValueKind.String => HotkeyFormatter.ParseModifiersText(modifiersElement.GetString() ?? string.Empty),
            JsonValueKind.Array => modifiersElement.EnumerateArray().Aggregate(VirtualKeyModifiers.None, (current, item) => current | ParseModifiers(item)),
            JsonValueKind.Null => VirtualKeyModifiers.None,
            _ => throw new ArgumentException("hotkey.modifiers must be a string, string array, integer, or null.")
        };
    }

    private static VirtualKey ParseVirtualKey(JsonElement keyElement)
    {
        if (keyElement.ValueKind == JsonValueKind.Number && keyElement.TryGetInt32(out int keyCode))
        {
            return HotkeyFormatter.ParseKeyCode(keyCode);
        }

        if (keyElement.ValueKind == JsonValueKind.String)
        {
            return HotkeyFormatter.ParseKeyText(keyElement.GetString() ?? string.Empty);
        }

        throw new ArgumentException("hotkey.key must be a string or integer.");
    }

    private static object ParseSettingValue(JsonElement valueElement)
    {
        return valueElement.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => valueElement.GetString() ?? string.Empty,
            JsonValueKind.Number when valueElement.TryGetInt32(out int intValue) => intValue,
            JsonValueKind.Number when valueElement.TryGetInt64(out long longValue) => longValue,
            JsonValueKind.Number => valueElement.GetDouble(),
            _ => throw new ArgumentException("value must be a boolean, integer, number, or string.")
        };
    }

    private static bool IsSupportedAppSettingKey(string key)
    {
        string[] supportedKeys =
        [
            "Notification",
            "EnableNotification",
            "StartOnTray",
            "EnableStartOnTray",
            "TopMost",
            "EnableTopMost",
            "Theme",
            "AppBackgroundRequestedTheme",
            "Language",
            "LanguageTag",
            "Localization",
            "StartupTask",
            "EnableStartupTask"
        ];

        return supportedKeys.Any(supportedKey => supportedKey.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private RegexPageStructure GetRegexProfileOrThrow(int profileIndex)
    {
        if (profileIndex < 0 || profileIndex >= _regexRepository.RegexConfigs.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(profileIndex), $"Regex profile profileIndex {profileIndex} does not exist. Call get_regex_profiles to inspect available profile indexes.");
        }

        return _regexRepository.RegexConfigs[profileIndex];
    }

    private RegexPageStructure GetSavedRegexProfileOrFallback(int profileIndex, RegexPageStructure fallback)
    {
        return profileIndex >= 0 && profileIndex < _regexRepository.RegexConfigs.Count
            ? _regexRepository.RegexConfigs[profileIndex]
            : fallback;
    }

    private static void EnsureExistingChainIndex(int profileIndex, int chainIndex, int chainCount)
    {
        if (chainIndex >= 0 && chainIndex < chainCount)
        {
            return;
        }

        string validRange = chainCount > 0 ? $"0..{chainCount - 1}" : "none because the chain is empty";
        throw new ArgumentOutOfRangeException(nameof(chainIndex), $"chainIndex {chainIndex} is outside the valid existing range {validRange} for profileIndex {profileIndex}.");
    }

    private static RegexPageStructure CloneRegexProfile(RegexPageStructure source)
    {
        return new RegexPageStructure
        {
            HotkeyName = source.HotkeyName,
            HotkeyComment = source.HotkeyComment,
            Hotkey = CloneHotkey(source.Hotkey),
            InputText = source.InputText,
            IsRegistrationFailed = source.IsRegistrationFailed,
            RegexChain = new RegexChainStructure
            {
                ChainItems = new ObservableCollection<ChainItem>(source.RegexChain.ChainItems.Select(CloneChainItem))
            }
        };
    }

    private static object ToChainItemDto(ChainItem item)
    {
        return new
        {
            regex = item.RegexExpression ?? string.Empty,
            replace = item.Replace ?? string.Empty
        };
    }

    private static object ToProfileDto(RegexPageStructure config, int index)
    {
        return new
        {
            index,
            name = config.HotkeyName,
            comment = config.HotkeyComment,
            inputText = config.InputText,
            hotkey = ToHotkeyDto(config.Hotkey),
            chain = config.RegexChain.ChainItems.Select(item => new
            {
                regex = item.RegexExpression ?? string.Empty,
                replace = item.Replace ?? string.Empty
            }).ToArray()
        };
    }

    private static bool LanguageTagsMatch(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
        {
            return false;
        }

        string trimmedFirst = first.Trim();
        string trimmedSecond = second.Trim();
        if (trimmedFirst.Equals(trimmedSecond, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            string canonicalFirst = new Language(trimmedFirst).LanguageTag;
            string canonicalSecond = new Language(trimmedSecond).LanguageTag;
            return canonicalFirst.Equals(trimmedSecond, StringComparison.OrdinalIgnoreCase)
                || trimmedFirst.Equals(canonicalSecond, StringComparison.OrdinalIgnoreCase)
                || canonicalFirst.Equals(canonicalSecond, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private OcrLanguageActionDto[] BuildManageableLanguageDtos()
    {
        string? currentLanguageTag = _ocrRepository.LanguageTag;
        IEnumerable<OcrLanguageActionDto> installedLanguages = _ocrRepository.AvailableLanguages.Select(language =>
        {
            bool isCurrent = LanguageTagsMatch(language.LanguageTag, currentLanguageTag);
            return new OcrLanguageActionDto(
                language.LanguageTag,
                language.DisplayName,
                IsInstalled: true,
                IsCurrent: isCurrent,
                CanSelect: true,
                Action: isCurrent ? "current" : "delete",
                RecommendedTool: isCurrent ? "none" : "delete_ocr_language");
        });

        IEnumerable<OcrLanguageActionDto> installableLanguages = _ocrRepository.InstallableLanguages
            .Where(language => !_ocrRepository.AvailableLanguageTags.Any(installedTag => LanguageTagsMatch(installedTag, language.LanguageTag)))
            .Select(language => new OcrLanguageActionDto(
                language.LanguageTag,
                language.DisplayName,
                IsInstalled: false,
                IsCurrent: false,
                CanSelect: false,
                Action: "install",
                RecommendedTool: "install_ocr_language"));

        return installedLanguages
            .Concat(installableLanguages)
            .GroupBy(language => language.LanguageTag, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static object ToOcrLanguageDto(McpOcrLanguageInfo language)
    {
        return new
        {
            languageTag = language.LanguageTag,
            displayName = language.DisplayName
        };
    }

    private static object ToHotkeyDto(HotkeyStructure? hotkey)
    {
        HotkeyStructure safeHotkey = CloneHotkey(hotkey);
        return new
        {
            display = HotkeyFormatter.GetDisplayText(safeHotkey.Modifiers, safeHotkey.Key),
            modifiers = safeHotkey.Modifiers.ToString(),
            modifiersValue = (int)safeHotkey.Modifiers,
            key = HotkeyFormatter.GetKeyText(safeHotkey.Key),
            keyValue = (int)safeHotkey.Key
        };
    }

    private static HotkeyStructure CloneHotkey(HotkeyStructure? hotkey)
    {
        return new HotkeyStructure
        {
            Modifiers = hotkey?.Modifiers ?? VirtualKeyModifiers.None,
            Key = hotkey?.Key ?? VirtualKey.None
        };
    }

    private static ChainItem CloneChainItem(ChainItem item)
    {
        return new ChainItem
        {
            RegexExpression = item.RegexExpression,
            Replace = item.Replace
        };
    }

    private static McpToolResult JsonSuccess<T>(T value)
    {
        JsonElement structuredContent = JsonSerializer.SerializeToElement(value, JsonOptions);
        string text = JsonSerializer.Serialize(value, JsonOptions);
        return McpToolResult.Success(text, structuredContent);
    }

    private static JsonElement Schema(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static JsonElement? GetProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        JsonElement? property = GetProperty(element, propertyName);
        return property.HasValue && property.Value.ValueKind == JsonValueKind.String
            ? property.Value.GetString()
            : null;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        JsonElement? property = GetProperty(element, propertyName);
        if (!property.HasValue)
        {
            value = string.Empty;
            return false;
        }

        if (property.Value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"{propertyName} must be a string.");
        }

        value = property.Value.GetString() ?? string.Empty;
        return true;
    }

    private static int GetRequiredInt32(JsonElement element, string propertyName)
    {
        if (TryGetInt32(element, propertyName, out int value))
        {
            return value;
        }

        throw new ArgumentException($"{propertyName} is required and must be an integer.");
    }

    private static bool TryGetInt32(JsonElement element, string propertyName, out int value)
    {
        JsonElement? property = GetProperty(element, propertyName);
        if (property.HasValue && property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
