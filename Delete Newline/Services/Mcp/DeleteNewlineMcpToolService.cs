using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;
using Windows.System;

namespace Delete_Newline.Services.Mcp;

public sealed class DeleteNewlineMcpToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

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
                Description = "Create a new regex profile or update an existing zero-based profile. Supports profile name/comment, hotkey, test input text, and regex chain. Changes are saved immediately and hotkeys are re-registered immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "index": { "type": "integer", "minimum": 0, "description": "Existing zero-based profile index to update. Omit to append." },
                    "name": { "type": "string", "description": "Profile display name. Required for new profiles." },
                    "comment": { "type": "string", "description": "Profile description/comment." },
                    "inputText": { "type": "string", "description": "Saved test input text shown on the Regex page." },
                    "input": { "type": "string", "description": "Alias for inputText." },
                    "hotkey": {
                      "description": "Hotkey string like 'Control+Shift+Q' or object with modifiers/key.",
                      "oneOf": [
                        { "type": "string" },
                        {
                          "type": "object",
                          "properties": {
                            "modifiers": {
                              "oneOf": [
                                { "type": "string" },
                                { "type": "array", "items": { "type": "string" } },
                                { "type": "integer" }
                              ]
                            },
                            "key": { "oneOf": [ { "type": "string" }, { "type": "integer" } ] }
                          },
                          "required": ["key"],
                          "additionalProperties": false
                        }
                      ]
                    },
                    "chain": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "regex": { "type": "string" },
                          "replace": { "type": "string" }
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
                Name = "delete_regex_profile",
                Title = "Delete a regex profile",
                Description = "Delete a regex profile by zero-based index and unregister its hotkey immediately.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "index": { "type": "integer", "minimum": 0 }
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
                Description = "Apply either an existing profile's chain or an inline chain to input text and return the output without changing saved settings.",
                InputSchema = Schema("""
                {
                  "type": "object",
                  "properties": {
                    "input": { "type": "string" },
                    "index": { "type": "integer", "minimum": 0, "description": "Existing profile index to test." },
                    "chain": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "regex": { "type": "string" },
                          "replace": { "type": "string" }
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
                    "languageTag": { "type": "string", "description": "OCR recognizer language tag, e.g. en-US or ko-KR." },
                    "language": { "type": "string", "description": "Alias for languageTag." },
                    "hotkey": {
                      "description": "Hotkey string like 'Control+Menu+O' or object with modifiers/key.",
                      "oneOf": [
                        { "type": "string" },
                        {
                          "type": "object",
                          "properties": {
                            "modifiers": {
                              "oneOf": [
                                { "type": "string" },
                                { "type": "array", "items": { "type": "string" } },
                                { "type": "integer" }
                              ]
                            },
                            "key": { "oneOf": [ { "type": "string" }, { "type": "integer" } ] }
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
                "delete_regex_profile" => await DeleteRegexProfileAsync(arguments, cancellationToken),
                "test_regex_chain" => TestRegexChain(arguments),
                "get_ocr_settings" => GetOcrSettings(),
                "set_ocr_settings" => await SetOcrSettingsAsync(arguments, cancellationToken),
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
        else if (TryGetInt32(arguments, "index", out int index) && index >= 0 && index < _regexRepository.RegexConfigs.Count)
        {
            chain = _regexRepository.RegexConfigs[index].RegexChain.ChainItems.ToArray();
        }
        else
        {
            throw new ArgumentException("Provide either chain or a valid index.");
        }

        string output = ApplyRegexChain(input, chain);
        return JsonSuccess(new { input, output });
    }

    private McpToolResult GetOcrSettings()
    {
        return JsonSuccess(new
        {
            languageTag = _ocrRepository.LanguageTag,
            availableLanguageTags = _ocrRepository.AvailableLanguageTags,
            hotkey = ToHotkeyDto(_ocrRepository.Hotkey)
        });
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

    private static string ApplyRegexChain(string input, IReadOnlyList<ChainItem> chain)
    {
        string output = input;
        foreach (ChainItem item in chain)
        {
            string pattern = item.RegexExpression ?? string.Empty;
            if (string.IsNullOrEmpty(pattern))
            {
                continue;
            }

            Regex regex = new(pattern, RegexOptions.None, RegexTimeout);
            output = regex.Replace(output, item.Replace ?? string.Empty);
        }

        return output;
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
            return ParseHotkeyString(hotkeyElement.GetString() ?? string.Empty);
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

    private static HotkeyStructure ParseHotkeyString(string hotkeyText)
    {
        string[] tokens = SplitHotkeyTokens(hotkeyText);
        if (tokens.Length == 0)
        {
            throw new ArgumentException("hotkey string cannot be empty.");
        }

        VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
        for (int i = 0; i < tokens.Length - 1; i++)
        {
            modifiers |= ParseModifierToken(tokens[i]);
        }

        VirtualKey key = ParseVirtualKey(tokens[^1]);
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
            JsonValueKind.String => ParseModifierString(modifiersElement.GetString() ?? string.Empty),
            JsonValueKind.Array => modifiersElement.EnumerateArray().Aggregate(VirtualKeyModifiers.None, (current, item) => current | ParseModifiers(item)),
            JsonValueKind.Null => VirtualKeyModifiers.None,
            _ => throw new ArgumentException("hotkey.modifiers must be a string, string array, integer, or null.")
        };
    }

    private static VirtualKeyModifiers ParseModifierString(string modifiersText)
    {
        VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
        foreach (string token in SplitHotkeyTokens(modifiersText))
        {
            modifiers |= ParseModifierToken(token);
        }

        return modifiers;
    }

    private static VirtualKeyModifiers ParseModifierToken(string token)
    {
        return token.Trim().ToLowerInvariant() switch
        {
            "" or "none" => VirtualKeyModifiers.None,
            "ctrl" or "control" => VirtualKeyModifiers.Control,
            "shift" => VirtualKeyModifiers.Shift,
            "alt" or "menu" => VirtualKeyModifiers.Menu,
            "win" or "windows" or "meta" => VirtualKeyModifiers.Windows,
            _ => throw new ArgumentException($"Unsupported hotkey modifier '{token}'. Supported modifiers: Control, Shift, Menu/Alt, Windows.")
        };
    }

    private static VirtualKey ParseVirtualKey(JsonElement keyElement)
    {
        if (keyElement.ValueKind == JsonValueKind.Number && keyElement.TryGetInt32(out int keyCode))
        {
            return (VirtualKey)keyCode;
        }

        if (keyElement.ValueKind == JsonValueKind.String)
        {
            return ParseVirtualKey(keyElement.GetString() ?? string.Empty);
        }

        throw new ArgumentException("hotkey.key must be a string or integer.");
    }

    private static VirtualKey ParseVirtualKey(string keyText)
    {
        string normalized = keyText.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("hotkey key cannot be empty.");
        }

        if (int.TryParse(normalized, out int keyCode))
        {
            return (VirtualKey)keyCode;
        }

        normalized = normalized.Equals("Esc", StringComparison.OrdinalIgnoreCase) ? "Escape" : normalized;
        normalized = normalized.Equals("Alt", StringComparison.OrdinalIgnoreCase) ? "Menu" : normalized;

        if (normalized.Length == 1 && char.IsDigit(normalized[0]))
        {
            normalized = $"Number{normalized}";
        }
        else if (normalized.Length == 1 && char.IsLetter(normalized[0]))
        {
            normalized = normalized.ToUpperInvariant();
        }

        if (Enum.TryParse(normalized, ignoreCase: true, out VirtualKey key))
        {
            return key;
        }

        throw new ArgumentException($"Unsupported hotkey key '{keyText}'. Use a Windows.System.VirtualKey name such as A, Q, F1, Enter, Escape, or Space.");
    }

    private static string[] SplitHotkeyTokens(string text)
    {
        return text.Split(['+', ',', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

    private static object ToHotkeyDto(HotkeyStructure? hotkey)
    {
        HotkeyStructure safeHotkey = CloneHotkey(hotkey);
        return new
        {
            display = safeHotkey.ToString(),
            modifiers = safeHotkey.Modifiers.ToString(),
            modifiersValue = (int)safeHotkey.Modifiers,
            key = safeHotkey.Key.ToString(),
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
