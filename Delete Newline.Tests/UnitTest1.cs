using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Delete_Newline.Services.Mcp;
using Windows.System;

namespace Delete_Newline.Tests;

public sealed class DeleteNewlineMcpToolServiceTests
{
    [Fact]
    public void ListTools_ExposesStandardMcpToolNames()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        var toolNames = service.ListTools().Select(tool => tool.Name).ToArray();

        Assert.Contains("get_regex_profiles", toolNames);
        Assert.Contains("upsert_regex_profile", toolNames);
        Assert.Contains("insert_regex_chain_item", toolNames);
        Assert.Contains("update_regex_chain_item", toolNames);
        Assert.Contains("delete_regex_chain_item", toolNames);
        Assert.Contains("delete_regex_profile", toolNames);
        Assert.Contains("test_regex_chain", toolNames);
        Assert.Contains("get_app_settings", toolNames);
        Assert.Contains("set_app_setting", toolNames);
        Assert.Contains("get_ocr_settings", toolNames);
        Assert.Contains("set_ocr_settings", toolNames);
        Assert.All(toolNames, name => Assert.Matches("^[a-z0-9_]+$", name));
    }

    [Fact]
    public async Task UpsertRegexProfile_ReplacesExistingChainAndPersistsImmediately()
    {
        var existing = new RegexPageStructure
        {
            HotkeyName = "Browser cleanup",
            HotkeyComment = "old comment"
        };
        existing.RegexChain.ChainItems.Add(new ChainItem { RegexExpression = "old", Replace = "x" });

        var regexRepository = new InMemoryRegexConfigurationRepository(existing);
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "index": 0,
          "name": "Browser cleanup",
          "comment": "Normalize whitespace copied from browser",
          "chain": [
            { "regex": "\\s+", "replace": " " },
            { "regex": "^\\s+|\\s+$", "replace": "" }
          ]
        }
        """);

        var result = await service.ExecuteAsync("upsert_regex_profile", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal(1, regexRepository.SaveCount);
        Assert.Single(regexRepository.RegexConfigs);
        Assert.Equal("Normalize whitespace copied from browser", regexRepository.RegexConfigs[0].HotkeyComment);
        Assert.Equal(2, regexRepository.RegexConfigs[0].RegexChain.ChainItems.Count);
        Assert.Equal("\\s+", regexRepository.RegexConfigs[0].RegexChain.ChainItems[0].RegexExpression);
        Assert.Equal(" ", regexRepository.RegexConfigs[0].RegexChain.ChainItems[0].Replace);
    }

    [Fact]
    public async Task UpsertRegexProfile_AcceptsHotkeyAndInputTextAndPersistsImmediately()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "name": "Quote cleanup",
          "comment": "Normalize quoted text",
          "inputText": "hello---world",
          "hotkey": {
            "modifiers": ["Control", "Shift"],
            "key": "Q"
          },
          "chain": [
            { "regex": "-+", "replace": " " }
          ]
        }
        """);

        var result = await service.ExecuteAsync("upsert_regex_profile", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal(1, regexRepository.SaveCount);
        Assert.Single(regexRepository.RegexConfigs);
        RegexPageStructure saved = regexRepository.RegexConfigs[0];
        Assert.Equal("hello---world", saved.InputText);
        Assert.Equal(VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, saved.Hotkey.Modifiers);
        Assert.Equal(VirtualKey.Q, saved.Hotkey.Key);
    }

    [Fact]
    public async Task UpsertRegexProfile_TreatsDigitHotkeyStringAsKeyboardNumberKey()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "name": "Numeric hotkey",
          "hotkey": "Alt+1",
          "chain": []
        }
        """);

        var result = await service.ExecuteAsync("upsert_regex_profile", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal(VirtualKeyModifiers.Menu, regexRepository.RegexConfigs[0].Hotkey.Modifiers);
        Assert.Equal(VirtualKey.Number1, regexRepository.RegexConfigs[0].Hotkey.Key);
        Assert.Equal("Alt + 1", regexRepository.RegexConfigs[0].Hotkey.DisplayText);
    }

    [Fact]
    public async Task SetOcrSettings_TreatsSingleDigitKeyNumberAsKeyboardNumberKey()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "hotkey": {
            "modifiers": "Alt",
            "key": 1
          }
        }
        """);

        var result = await service.ExecuteAsync("set_ocr_settings", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal(VirtualKeyModifiers.Menu, ocrRepository.Hotkey.Modifiers);
        Assert.Equal(VirtualKey.Number1, ocrRepository.Hotkey.Key);
        Assert.Equal("Alt + 1", ocrRepository.Hotkey.DisplayText);
    }

    [Theory]
    [InlineData("\"Ctrl+Backspace\"", VirtualKeyModifiers.Control, VirtualKey.Back)]
    [InlineData("\"Ctrl+Left Arrow\"", VirtualKeyModifiers.Control, VirtualKey.Left)]
    [InlineData("\"Ctrl+Page Up\"", VirtualKeyModifiers.Control, VirtualKey.PageUp)]
    [InlineData("{\"modifiers\":\"Shift\",\"key\":\"Spacebar\"}", VirtualKeyModifiers.Shift, VirtualKey.Space)]
    [InlineData("{\"modifiers\":\"Alt\",\"key\":\"Return\"}", VirtualKeyModifiers.Menu, VirtualKey.Enter)]
    [InlineData("{\"modifiers\":\"Control\",\"key\":\"Del\"}", VirtualKeyModifiers.Control, VirtualKey.Delete)]
    public async Task SetOcrSettings_AcceptsCommonHumanHotkeyAliases(string hotkeyJson, VirtualKeyModifiers expectedModifiers, VirtualKey expectedKey)
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse($"{{\"hotkey\":{hotkeyJson}}}");

        var result = await service.ExecuteAsync("set_ocr_settings", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal(expectedModifiers, ocrRepository.Hotkey.Modifiers);
        Assert.Equal(expectedKey, ocrRepository.Hotkey.Key);
    }

    [Fact]
    public void ListTools_GuidesModelsToUseNamedNumberHotkeysInsteadOfRawIntegers()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        McpToolDescriptor upsertTool = service.ListTools().Single(tool => tool.Name == "upsert_regex_profile");
        McpToolDescriptor setOcrTool = service.ListTools().Single(tool => tool.Name == "set_ocr_settings");

        AssertHotkeyKeySchemaGuidesModelsToNamedAndCommonKeys(upsertTool);
        AssertHotkeyKeySchemaGuidesModelsToNamedAndCommonKeys(setOcrTool);
    }

    [Fact]
    public async Task RegexChainTools_InsertUpdateAndDeleteSingleRulesWithoutRebuildingWholeProfile()
    {
        var existing = new RegexPageStructure
        {
            HotkeyName = "Cleanup",
            HotkeyComment = "profile used by MCP"
        };
        existing.RegexChain.ChainItems.Add(new ChainItem { RegexExpression = "one", Replace = "1" });
        existing.RegexChain.ChainItems.Add(new ChainItem { RegexExpression = "three", Replace = "3" });

        var regexRepository = new InMemoryRegexConfigurationRepository(existing);
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var insertArguments = JsonDocument.Parse("""
        {
          "profileIndex": 0,
          "chainIndex": 1,
          "regex": "two",
          "replace": "2"
        }
        """);
        McpToolResult insertResult = await service.ExecuteAsync("insert_regex_chain_item", insertArguments.RootElement, CancellationToken.None);

        Assert.False(insertResult.IsError, insertResult.Text);
        Assert.Equal(["one", "two", "three"], regexRepository.RegexConfigs[0].RegexChain.ChainItems.Select(item => item.RegexExpression));
        Assert.Equal(["1", "2", "3"], regexRepository.RegexConfigs[0].RegexChain.ChainItems.Select(item => item.Replace));

        using var updateArguments = JsonDocument.Parse("""
        {
          "profileIndex": 0,
          "chainIndex": 1,
          "regex": "two+",
          "replace": "TWO"
        }
        """);
        McpToolResult updateResult = await service.ExecuteAsync("update_regex_chain_item", updateArguments.RootElement, CancellationToken.None);

        Assert.False(updateResult.IsError, updateResult.Text);
        Assert.Equal("two+", regexRepository.RegexConfigs[0].RegexChain.ChainItems[1].RegexExpression);
        Assert.Equal("TWO", regexRepository.RegexConfigs[0].RegexChain.ChainItems[1].Replace);

        using var deleteArguments = JsonDocument.Parse("""
        {
          "profileIndex": 0,
          "chainIndex": 0
        }
        """);
        McpToolResult deleteResult = await service.ExecuteAsync("delete_regex_chain_item", deleteArguments.RootElement, CancellationToken.None);

        Assert.False(deleteResult.IsError, deleteResult.Text);
        Assert.Equal(["two+", "three"], regexRepository.RegexConfigs[0].RegexChain.ChainItems.Select(item => item.RegexExpression));
        Assert.Equal(3, regexRepository.SaveCount);
    }

    [Fact]
    public void ListTools_DescribesIndexSafeRegexChainEditingWorkflow()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        var tools = service.ListTools();
        string[] toolNames = tools.Select(tool => tool.Name).ToArray();

        Assert.Contains("insert_regex_chain_item", toolNames);
        Assert.Contains("update_regex_chain_item", toolNames);
        Assert.Contains("delete_regex_chain_item", toolNames);

        McpToolDescriptor insertTool = tools.Single(tool => tool.Name == "insert_regex_chain_item");
        Assert.Contains("shifts", insertTool.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("get_regex_profiles", insertTool.Description);
        Assert.Contains("test_regex_chain", insertTool.Description);
        Assert.Contains("profileIndex", insertTool.InputSchema.GetRawText());
        Assert.Contains("chainIndex", insertTool.InputSchema.GetRawText());
    }

    [Fact]
    public async Task TestRegexChain_UsesSameReplacementSemanticsAsRuntimeRegexService()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "input": "first,second",
          "chain": [
            { "regex": ",", "replace": "\\n" }
          ]
        }
        """);

        McpToolResult result = await service.ExecuteAsync("test_regex_chain", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal("first\nsecond", result.StructuredContent!.Value.GetProperty("output").GetString());
    }

    [Fact]
    public async Task SetOcrSettings_ChangesLanguageAndHotkeyAndPersistsImmediately()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "languageTag": "ko-KR",
          "hotkey": {
            "modifiers": "Control+Menu",
            "key": "O"
          }
        }
        """);

        var result = await service.ExecuteAsync("set_ocr_settings", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal("ko-KR", ocrRepository.LanguageTag);
        Assert.Equal(VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, ocrRepository.Hotkey.Modifiers);
        Assert.Equal(VirtualKey.O, ocrRepository.Hotkey.Key);
        Assert.Equal(1, ocrRepository.SaveCount);
    }

    [Fact]
    public async Task SetAppSetting_AcceptsMcpPortAndStringSettings()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var portArguments = JsonDocument.Parse("""
        {
          "key": "McpPort",
          "value": 40333
        }
        """);
        using var themeArguments = JsonDocument.Parse("""
        {
          "key": "Theme",
          "value": "Dark"
        }
        """);

        var portResult = await service.ExecuteAsync("set_app_setting", portArguments.RootElement, CancellationToken.None);
        var themeResult = await service.ExecuteAsync("set_app_setting", themeArguments.RootElement, CancellationToken.None);

        Assert.False(portResult.IsError, portResult.Text);
        Assert.False(themeResult.IsError, themeResult.Text);
        Assert.Equal(40333, settingsRepository.GetValue<int>("McpPort"));
        Assert.Equal("Dark", settingsRepository.GetValue<string>("Theme"));
        Assert.Equal(2, settingsRepository.SaveCount);
    }

    [Theory]
    [InlineData("{\"key\":\"McpPort\",\"value\":0}")]
    [InlineData("{\"key\":\"McpPort\",\"value\":65536}")]
    [InlineData("{\"key\":\"McpPort\",\"value\":12.5}")]
    [InlineData("{\"key\":\"McpPort\",\"value\":\"40333\"}")]
    public async Task SetAppSetting_RejectsInvalidMcpPortValues(string json)
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse(json);

        var result = await service.ExecuteAsync("set_app_setting", arguments.RootElement, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("McpPort", result.Text);
        Assert.Null(settingsRepository.GetValue("McpPort"));
    }

    [Fact]
    public async Task SetAppSetting_RejectsUnsupportedSettingKeys()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "key": "UnexpectedSetting",
          "value": true
        }
        """);

        var result = await service.ExecuteAsync("set_app_setting", arguments.RootElement, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("Unsupported setting key", result.Text);
        Assert.Null(settingsRepository.GetValue("UnexpectedSetting"));
    }

    [Fact]
    public async Task SetAppSetting_RejectsMcpPortChangeWhenMcpServerIsEnabled()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);
        await settingsRepository.SetBooleanAsync("McpEnabled", true, CancellationToken.None);
        await settingsRepository.SetValueAsync("McpPort", 39333, CancellationToken.None);

        using var arguments = JsonDocument.Parse("""
        {
          "key": "McpPort",
          "value": 40333
        }
        """);

        var result = await service.ExecuteAsync("set_app_setting", arguments.RootElement, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("disabled", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(39333, settingsRepository.GetValue<int>("McpPort"));
    }

    [Fact]
    public async Task SetAppSetting_ChangesMcpEnabledAndPersistsImmediately()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("""
        {
          "key": "McpEnabled",
          "value": true
        }
        """);

        var result = await service.ExecuteAsync("set_app_setting", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.True(settingsRepository.GetBoolean("McpEnabled"));
        Assert.Equal(1, settingsRepository.SaveCount);
    }

    [Fact]
    public void HotkeyStructure_NotifiesDisplayTextWhenHotkeyPartsChange()
    {
        HotkeyStructure hotkey = new();
        List<string?> changedProperties = [];
        hotkey.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        hotkey.Modifiers = VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift;
        hotkey.Key = VirtualKey.S;

        Assert.Equal("Ctrl + Shift + S", hotkey.DisplayText);
        Assert.Contains(nameof(HotkeyStructure.DisplayText), changedProperties);
        Assert.True(changedProperties.Count(propertyName => propertyName == nameof(HotkeyStructure.DisplayText)) >= 2);
    }

    [Theory]
    [InlineData(VirtualKey.Number1, "Ctrl + 1")]
    [InlineData(VirtualKey.Back, "Ctrl + Backspace")]
    [InlineData(VirtualKey.Left, "Ctrl + Left Arrow")]
    [InlineData(VirtualKey.Up, "Ctrl + Up Arrow")]
    [InlineData(VirtualKey.Right, "Ctrl + Right Arrow")]
    [InlineData(VirtualKey.Down, "Ctrl + Down Arrow")]
    [InlineData(VirtualKey.PageUp, "Ctrl + Page Up")]
    [InlineData(VirtualKey.PageDown, "Ctrl + Page Down")]
    [InlineData(VirtualKey.Space, "Ctrl + Space")]
    [InlineData(VirtualKey.Enter, "Ctrl + Enter")]
    public void HotkeyFormatter_DisplaysCommonKeysIntuitively(VirtualKey key, string expectedDisplayText)
    {
        Assert.Equal(expectedDisplayText, HotkeyFormatter.GetDisplayText(VirtualKeyModifiers.Control, key));
    }

    [Theory]
    [InlineData(0xBA, "Ctrl + ;", "Semicolon")]
    [InlineData(0xBB, "Ctrl + =", "Equals")]
    [InlineData(0xBC, "Ctrl + ,", "Comma")]
    [InlineData(0xBD, "Ctrl + -", "Minus")]
    [InlineData(0xBE, "Ctrl + .", "Period")]
    [InlineData(0xBF, "Ctrl + /", "Slash")]
    [InlineData(0xC0, "Ctrl + `", "Backtick")]
    [InlineData(0xDB, "Ctrl + [", "LeftBracket")]
    [InlineData(0xDC, "Ctrl + \\", "Backslash")]
    [InlineData(0xDD, "Ctrl + ]", "RightBracket")]
    [InlineData(0xDE, "Ctrl + '", "Quote")]
    public void HotkeyFormatter_DisplaysOemKeysAsConciseSymbols(int keyValue, string expectedDisplayText, string expectedKeyText)
    {
        VirtualKey key = (VirtualKey)keyValue;

        Assert.Equal(expectedDisplayText, HotkeyFormatter.GetDisplayText(VirtualKeyModifiers.Control, key));
        Assert.Equal(expectedKeyText, HotkeyFormatter.GetKeyText(key));
    }

    [Theory]
    [InlineData("Backtick", 0xC0, "Ctrl + `", "Backtick")]
    [InlineData("`", 0xC0, "Ctrl + `", "Backtick")]
    [InlineData("Semicolon", 0xBA, "Ctrl + ;", "Semicolon")]
    [InlineData("LeftBracket", 0xDB, "Ctrl + [", "LeftBracket")]
    [InlineData("Quote", 0xDE, "Ctrl + '", "Quote")]
    public async Task SetOcrSettings_AcceptsSymbolKeyAliasesAndReturnsUnifiedKeyText(string keyAlias, int expectedKeyValue, string expectedDisplay, string expectedKeyText)
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);
        string json = JsonSerializer.Serialize(new
        {
            hotkey = new
            {
                modifiers = "Control",
                key = keyAlias
            }
        });
        using var arguments = JsonDocument.Parse(json);

        McpToolResult result = await service.ExecuteAsync("set_ocr_settings", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        Assert.Equal(VirtualKeyModifiers.Control, ocrRepository.Hotkey.Modifiers);
        Assert.Equal((VirtualKey)expectedKeyValue, ocrRepository.Hotkey.Key);
        Assert.Equal(expectedDisplay, ocrRepository.Hotkey.DisplayText);
        JsonElement hotkey = result.StructuredContent!.Value.GetProperty("hotkey");
        Assert.Equal(expectedDisplay, hotkey.GetProperty("display").GetString());
        Assert.Equal(expectedKeyText, hotkey.GetProperty("key").GetString());
        Assert.Equal(expectedKeyValue, hotkey.GetProperty("keyValue").GetInt32());
    }

    [Fact]
    public async Task McpHotkeyDtos_FormatRawVirtualKeyCodesThroughUnifiedKeyText()
    {
        var existing = new RegexPageStructure
        {
            HotkeyName = "Backtick profile",
            Hotkey = new HotkeyStructure
            {
                Modifiers = VirtualKeyModifiers.Control,
                Key = (VirtualKey)0xC0
            }
        };
        var regexRepository = new InMemoryRegexConfigurationRepository(existing);
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        using var arguments = JsonDocument.Parse("{}");

        McpToolResult result = await service.ExecuteAsync("get_regex_profiles", arguments.RootElement, CancellationToken.None);

        Assert.False(result.IsError, result.Text);
        JsonElement hotkey = result.StructuredContent!.Value[0].GetProperty("hotkey");
        Assert.Equal("Ctrl + `", hotkey.GetProperty("display").GetString());
        Assert.Equal("Backtick", hotkey.GetProperty("key").GetString());
        Assert.Equal(0xC0, hotkey.GetProperty("keyValue").GetInt32());
    }

    [Fact]
    public void McpHotkeyParsing_UsesTheSharedHotkeyFormatterSolution()
    {
        string mcpToolService = File.ReadAllText(LocateSourceFile("Delete Newline", "Services", "Mcp", "DeleteNewlineMcpToolService.cs"));

        Assert.Contains("HotkeyFormatter.ParseHotkeyText", mcpToolService);
        Assert.Contains("HotkeyFormatter.ParseKeyText", mcpToolService);
        Assert.DoesNotContain("NormalizeVirtualKeyText", mcpToolService);
    }

    [Fact]
    public void RegexCollectPage_BindsCardHotkeyToReactiveDisplayText()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));

        Assert.Contains("Text=\"{Binding Hotkey.DisplayText, Mode=OneWay}\"", regexCollectPageXaml);
        Assert.DoesNotContain("Text=\"{Binding Hotkey, Mode=OneWay}\"", regexCollectPageXaml);
    }

    [Fact]
    public void SettingsPage_CommitsMcpPortOnlyAfterUserFinishesEditing()
    {
        string settingsPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "SettingsPage.xaml"));
        string mcpPortNumberBoxBlock = ExtractElementBlock(settingsPageXaml, "<NumberBox x:Name=\"McpPortNumberBox\"", "</NumberBox>");

        Assert.DoesNotContain("EventName=\"ValueChanged\"", mcpPortNumberBoxBlock);
        Assert.Contains("ValidationMode=\"Disabled\"", mcpPortNumberBoxBlock);
        Assert.Contains("Width=\"68\"", mcpPortNumberBoxBlock);
        Assert.Contains("MinWidth=\"68\"", mcpPortNumberBoxBlock);
        Assert.Contains("PlaceholderText=\"Port\"", mcpPortNumberBoxBlock);
        Assert.Contains("EventName=\"LostFocus\"", mcpPortNumberBoxBlock);
        Assert.Contains("EventName=\"TextSubmitted\"", mcpPortNumberBoxBlock);
    }

    [Fact]
    public void SettingsPage_AlignsMcpPortInputWithThemeAndLanguageSelectors()
    {
        string settingsPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "SettingsPage.xaml"));
        string themeBlock = ExtractElementBlock(settingsPageXaml, "<!-- Theme Setting -->", "</Border>");
        string languageBlock = ExtractElementBlock(settingsPageXaml, "<!-- Language Setting -->", "</Border>");
        string mcpServerBlock = ExtractElementBlock(settingsPageXaml, "<!-- MCP Server Setting -->", "</Border>");

        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", themeBlock);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", languageBlock);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", mcpServerBlock);
        Assert.Contains("HorizontalAlignment=\"Right\"", themeBlock);
        Assert.Contains("HorizontalAlignment=\"Right\"", languageBlock);
        Assert.Contains("HorizontalAlignment=\"Right\"", mcpServerBlock);
        Assert.DoesNotContain("Settings_McpServer_Port", mcpServerBlock);
        Assert.Contains("Width=\"125\"", mcpServerBlock);
        Assert.Contains("<ColumnDefinition Width=\"68\" />", mcpServerBlock);
    }

    [Fact]
    public void McpPortPolicy_DefaultsMcpServerOffAndUsesBlankPortDisplayUntilExplicitlyConfigured()
    {
        Assert.False(McpPortPolicy.DefaultEnabled);
        Assert.True(double.IsNaN(McpPortPolicy.ToDisplayPortValue(McpPortPolicy.DefaultPort, isMcpEnabled: false, hasExplicitPort: false)));
        Assert.Equal(McpPortPolicy.DefaultPort, McpPortPolicy.ToDisplayPortValue(McpPortPolicy.DefaultPort, isMcpEnabled: true, hasExplicitPort: false));
        Assert.Equal(McpPortPolicy.DefaultPort, McpPortPolicy.ToDisplayPortValue(McpPortPolicy.DefaultPort, isMcpEnabled: false, hasExplicitPort: true));
    }

    [Fact]
    public void RegexCollectPage_AllowsShiftRangeSelectionAndRemovesSelectedItems()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));

        Assert.Contains("x:Name=\"RegexConfigGridView\"", regexCollectPageXaml);
        Assert.Contains("SelectionMode=\"Extended\"", regexCollectPageXaml);
        Assert.Contains("Click=\"RemoveRegexMenuFlyoutItem_Click\"", regexCollectPageXaml);
        Assert.DoesNotContain("CommandParameter=\"{Binding SelectedItems, ElementName=RegexConfigGridView}\"", regexCollectPageXaml);
        Assert.Contains("RightTapped=\"RegexConfigCard_RightTapped\"", regexCollectPageXaml);
    }

    [Fact]
    public void RegexCollectPage_RightClickSelectsUnselectedCardBeforeRemoving()
    {
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("RegexConfigCard_RightTapped", regexCollectPageCodeBehind);
        Assert.Contains("RemoveRegexMenuFlyoutItem_Click", regexCollectPageCodeBehind);
        Assert.Contains("RegexConfigGridView.SelectedItems", regexCollectPageCodeBehind);
        Assert.Contains("SelectedItems.Clear", regexCollectPageCodeBehind);
        Assert.Contains("await ViewModel.RemoveRegexConfigsAsync", regexCollectPageCodeBehind);
    }

    [Fact]
    public void RegexCollectPage_EscapeClearsExtendedSelection()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("KeyDown=\"RegexConfigGridView_KeyDown\"", regexCollectPageXaml);
        Assert.Contains("RegexConfigGridView_KeyDown", regexCollectPageCodeBehind);
        Assert.Contains("VirtualKey.Escape", regexCollectPageCodeBehind);
        Assert.Contains("RegexConfigGridView.SelectedItems.Clear", regexCollectPageCodeBehind);
        Assert.Contains("e.Handled = true", regexCollectPageCodeBehind);
    }

    [Fact]
    public void RegexProfileFilter_FiltersByHotkeyNameCaseInsensitivelyAndRestoresAllForEmptySearch()
    {
        RegexPageStructure[] profiles =
        [
            new() { HotkeyName = "Markdown Cleanup" },
            new() { HotkeyName = "URL Cleaner" },
            new() { HotkeyName = "OCR Normalize" }
        ];

        string[] matchedNames = RegexProfileFilter.FilterByName(profiles, "mark")
            .Select(profile => profile.HotkeyName)
            .ToArray();
        string[] allNames = RegexProfileFilter.FilterByName(profiles, string.Empty)
            .Select(profile => profile.HotkeyName)
            .ToArray();

        Assert.Equal(["Markdown Cleanup"], matchedNames);
        Assert.Equal(["Markdown Cleanup", "URL Cleaner", "OCR Normalize"], allNames);
    }

    [Fact]
    public void RegexCollectPage_CtrlFShowsLiveNameSearchOverlay()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));
        string regexCollectViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "RegexCollectViewModel.cs"));

        Assert.Contains("ItemsSource=\"{x:Bind ViewModel.FilteredRegexConfigs, Mode=OneWay}\"", regexCollectPageXaml);
        Assert.Contains("x:Name=\"RegexSearchOverlay\"", regexCollectPageXaml);
        Assert.Contains("HorizontalAlignment=\"Center\"", regexCollectPageXaml);
        Assert.Contains("VerticalAlignment=\"Bottom\"", regexCollectPageXaml);
        Assert.Contains("TextChanged=\"RegexSearchTextBox_TextChanged\"", regexCollectPageXaml);
        Assert.DoesNotContain("KeyDown=\"RegexSearchTextBox_KeyDown\"", regexCollectPageXaml);

        Assert.Contains("<KeyboardAccelerator", regexCollectPageXaml);
        Assert.Contains("Key=\"F\"", regexCollectPageXaml);
        Assert.Contains("Modifiers=\"Control\"", regexCollectPageXaml);
        Assert.Contains("Invoked=\"SearchKeyboardAccelerator_Invoked\"", regexCollectPageXaml);
        Assert.Contains("SearchKeyboardAccelerator_Invoked", regexCollectPageCodeBehind);
        Assert.Contains("ViewModel.ShowSearch()", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchTextBox.Focus", regexCollectPageCodeBehind);
        Assert.Contains("ViewModel.SearchText = RegexSearchTextBox.Text", regexCollectPageCodeBehind);

        Assert.Contains("FilteredRegexConfigs", regexCollectViewModel);
        Assert.Contains("RegexProfileFilter.FilterByName", regexCollectViewModel);
        Assert.Contains("partial void OnSearchTextChanged", regexCollectViewModel);
    }

    [Fact]
    public void RegexCollectPage_HidesKeyboardAcceleratorTooltipBox()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));

        Assert.Contains("KeyboardAcceleratorPlacementMode=\"Hidden\"", regexCollectPageXaml);
    }

    [Fact]
    public void RegexCollectPage_CtrlFTogglesSearchAndPreservesPreviousQuery()
    {
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));
        string regexCollectViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "RegexCollectViewModel.cs"));

        Assert.Contains("if (ViewModel.IsSearchVisible)", regexCollectPageCodeBehind);
        Assert.Contains("HideRegexSearchBox(clearSearchText: false)", regexCollectPageCodeBehind);
        Assert.Contains("private void HideRegexSearchBox(bool clearSearchText = true)", regexCollectPageCodeBehind);
        Assert.Contains("ViewModel.HideSearch(clearSearchText)", regexCollectPageCodeBehind);
        Assert.Contains("if (clearSearchText && RegexSearchTextBox.Text.Length > 0)", regexCollectPageCodeBehind);

        Assert.Contains("public bool CanReorderRegexConfigs => !IsSearchVisible || string.IsNullOrWhiteSpace(SearchText);", regexCollectViewModel);
        Assert.Contains("partial void OnIsSearchVisibleChanged(bool value)", regexCollectViewModel);
        Assert.Contains("public void HideSearch(bool clearSearchText = true)", regexCollectViewModel);
        Assert.Contains("if (clearSearchText)", regexCollectViewModel);
        Assert.Contains("!IsSearchVisible || string.IsNullOrWhiteSpace(SearchText)", regexCollectViewModel);
    }

    [Fact]
    public void RegexCollectPage_DoesNotFocusGridViewWhenCtrlFToggleHidesSearch()
    {
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.DoesNotContain("RegexConfigGridView.Focus", regexCollectPageCodeBehind);
        Assert.DoesNotContain("restoreFocus", regexCollectPageCodeBehind);
    }

    [Fact]
    public void RegexCollectPage_DoesNotMoveVisibleFocusToNavigationMenuWhenCtrlFToggleHidesSearch()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("x:Name=\"RegexSearchFocusSink\"", regexCollectPageXaml);
        Assert.Contains("Opacity=\"0\"", regexCollectPageXaml);
        Assert.Contains("IsHitTestVisible=\"False\"", regexCollectPageXaml);
        Assert.Contains("IsTabStop=\"True\"", regexCollectPageXaml);
        Assert.Contains("UseSystemFocusVisuals=\"False\"", regexCollectPageXaml);
        Assert.Contains("AutomationProperties.AccessibilityView=\"Raw\"", regexCollectPageXaml);
        Assert.Contains("RegexSearchFocusSink.Focus(FocusState.Programmatic)", regexCollectPageCodeBehind);
        Assert.Contains("if (_isHidingSearch)", regexCollectPageCodeBehind);

        int focusSinkIndex = regexCollectPageCodeBehind.IndexOf("RegexSearchFocusSink.Focus(FocusState.Programmatic)", StringComparison.Ordinal);
        int collapseIndex = regexCollectPageCodeBehind.IndexOf("RegexSearchOverlay.Visibility = Visibility.Collapsed", StringComparison.Ordinal);
        Assert.True(focusSinkIndex >= 0, "Ctrl+F hide should move focus to the invisible sink before hiding the focused TextBox.");
        Assert.True(collapseIndex >= 0, "Ctrl+F hide should still collapse the search overlay.");
        Assert.True(focusSinkIndex < collapseIndex, "Focus must leave the search TextBox before its overlay is collapsed so WinUI does not fall back to the NavigationView menu button.");
    }

    [Fact]
    public void RegexCollectPage_SearchOverlayFloatsAtBottomWithoutChangingTopScrollSpacing()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string normalizedRegexCollectPageXaml = regexCollectPageXaml.Replace("\r\n", "\n");

        Assert.DoesNotContain("\n        <Grid.RowDefinitions>", normalizedRegexCollectPageXaml);
        Assert.Contains("<GridView x:Name=\"RegexConfigGridView\"\n                  ItemsSource=", normalizedRegexCollectPageXaml);
        Assert.Contains("Padding=\"12,12,12,96\"", normalizedRegexCollectPageXaml);
        Assert.Contains("<Border x:Name=\"RegexSearchOverlay\"\n                HorizontalAlignment=\"Center\"\n                VerticalAlignment=\"Bottom\"", normalizedRegexCollectPageXaml);
        Assert.Contains("Margin=\"0,0,0,24\"", normalizedRegexCollectPageXaml);
        Assert.Contains("Canvas.ZIndex=\"1\"", normalizedRegexCollectPageXaml);
    }

    [Fact]
    public void RegexCollectPage_SearchOverlayUsesCompactSingleTextBoxChrome()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string searchOverlayBlock = ExtractElementBlock(regexCollectPageXaml, "<Border x:Name=\"RegexSearchOverlay\"", "</Border>");

        Assert.Contains("Margin=\"0,0,0,24\"", searchOverlayBlock);
        Assert.DoesNotContain("Padding=", searchOverlayBlock);
        Assert.DoesNotContain("CornerRadius=", searchOverlayBlock);
        Assert.DoesNotContain("Background=", searchOverlayBlock);
        Assert.Contains("Width=\"260\"", searchOverlayBlock);
        Assert.Contains("PlaceholderText=\"Search by Name\"", searchOverlayBlock);
    }

    [Fact]
    public void RegexCollectPage_SearchPlaceholderIsLocalizedForEnglishAndKorean()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string searchOverlayBlock = ExtractElementBlock(regexCollectPageXaml, "<Border x:Name=\"RegexSearchOverlay\"", "</Border>");
        XDocument englishResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "en-US", "Resources.resw"));
        XDocument koreanResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "ko-KR", "Resources.resw"));

        Assert.Contains("x:Uid=\"RegexCollect_SearchTextBox\"", searchOverlayBlock);
        Assert.Equal("Search by Name", GetReswValue(englishResources, "RegexCollect_SearchTextBox.PlaceholderText"));
        Assert.Equal("이름으로 검색", GetReswValue(koreanResources, "RegexCollect_SearchTextBox.PlaceholderText"));
    }

    [Fact]
    public void RegexCollectPage_RestoresSearchOverlayWhenReturningToFilteredProfiles()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("Loaded=\"RegexCollectPage_Loaded\"", regexCollectPageXaml);
        Assert.Contains("RegexCollectPage_Loaded", regexCollectPageCodeBehind);
        Assert.Contains("SynchronizeRegexSearchOverlayWithViewModel", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchTextBox.Text != ViewModel.SearchText", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchTextBox.Text = ViewModel.SearchText", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchOverlay.Visibility = ViewModel.IsSearchVisible", regexCollectPageCodeBehind);
    }

    [Fact]
    public void RegexCollectPage_DoesNotDismissSearchWithEscapeAndStillHidesEmptySearchOnLostFocus()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("LostFocus=\"RegexSearchTextBox_LostFocus\"", regexCollectPageXaml);
        Assert.DoesNotContain("Key=\"Escape\"", regexCollectPageXaml);
        Assert.DoesNotContain("Invoked=\"EscapeKeyboardAccelerator_Invoked\"", regexCollectPageXaml);
        Assert.DoesNotContain("KeyDown=\"RegexSearchTextBox_KeyDown\"", regexCollectPageXaml);
        Assert.DoesNotContain("EscapeKeyboardAccelerator_Invoked", regexCollectPageCodeBehind);
        Assert.DoesNotContain("RegexSearchTextBox_KeyDown", regexCollectPageCodeBehind);
        Assert.DoesNotContain("IsRegexSearchActive", regexCollectPageCodeBehind);
        string lostFocusHandler = ExtractElementBlock(
            regexCollectPageCodeBehind,
            "private void RegexSearchTextBox_LostFocus",
            "    private void ShowRegexSearchBox()");

        Assert.Contains("RegexSearchTextBox_LostFocus", regexCollectPageCodeBehind);
        Assert.Contains("string.IsNullOrWhiteSpace(RegexSearchTextBox.Text)", lostFocusHandler);
        Assert.Contains("HideRegexSearchBox()", lostFocusHandler);
    }

    [Fact]
    public void OcrCaptureWindow_AllowsLanguageChangeFromTopCenterOverlay()
    {
        string ocrCaptureWindowXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml"));
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));
        XDocument englishResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "en-US", "Resources.resw"));
        XDocument koreanResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "ko-KR", "Resources.resw"));

        Assert.Contains("x:Name=\"LanguageToolbarCanvas\"", ocrCaptureWindowXaml);
        Assert.Contains("x:Name=\"LanguageToolbar\"", ocrCaptureWindowXaml);
        Assert.Contains("x:Uid=\"OcrCapture_LanguageLabel\"", ocrCaptureWindowXaml);
        Assert.Contains("x:Name=\"CaptureLanguageComboBox\"", ocrCaptureWindowXaml);
        Assert.Contains("SelectionChanged=\"CaptureLanguageComboBox_SelectionChanged\"", ocrCaptureWindowXaml);
        Assert.Contains("Canvas.ZIndex=\"2\"", ocrCaptureWindowXaml);

        Assert.Contains("IReadOnlyList<Language>? availableLanguages = null", ocrCaptureWindowCodeBehind);
        Assert.Contains("Action<Language>? languageChanged = null", ocrCaptureWindowCodeBehind);
        Assert.Contains("SetupLanguageSelector", ocrCaptureWindowCodeBehind);
        Assert.Contains("PositionLanguageToolbar", ocrCaptureWindowCodeBehind);
        Assert.Contains("ImageHelper.GetPrimaryScreenBounds()", ocrCaptureWindowCodeBehind);
        Assert.Contains("CaptureLanguageComboBox_SelectionChanged", ocrCaptureWindowCodeBehind);
        Assert.Contains("currentLanguage = selectedLanguage", ocrCaptureWindowCodeBehind);
        Assert.Contains("_languageChanged?.Invoke(currentLanguage)", ocrCaptureWindowCodeBehind);

        Assert.Contains("SetupFullscreen(backgroundImage, SelectedLanguage, AvailableLanguages.ToArray(), OnCaptureLanguageChanged)", ocrViewModel);
        Assert.Contains("private void OnCaptureLanguageChanged(Language language)", ocrViewModel);
        Assert.Contains("SelectedLanguage = language", ocrViewModel);

        Assert.Equal("OCR Language", GetReswValue(englishResources, "OcrCapture_LanguageLabel.Text"));
        Assert.Equal("OCR 언어", GetReswValue(koreanResources, "OcrCapture_LanguageLabel.Text"));
    }

    [Fact]
    public void OcrCaptureWindow_EscapeClosesEvenWhenLanguageSelectorHasFocus()
    {
        string ocrCaptureWindowXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml"));
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));

        Assert.Contains("KeyboardAcceleratorPlacementMode=\"Hidden\"", ocrCaptureWindowXaml);
        Assert.Contains("<KeyboardAccelerator Key=\"Escape\"", ocrCaptureWindowXaml);
        Assert.Contains("Invoked=\"EscapeKeyboardAccelerator_Invoked\"", ocrCaptureWindowXaml);
        Assert.Contains("private void EscapeKeyboardAccelerator_Invoked", ocrCaptureWindowCodeBehind);
        Assert.Contains("RegisterEscapeMessageHook(hwnd)", ocrCaptureWindowCodeBehind);
        Assert.Contains("WM_KEYDOWN", ocrCaptureWindowCodeBehind);
        Assert.Contains("VK_ESCAPE", ocrCaptureWindowCodeBehind);
        Assert.Contains("AddHandler(UIElement.KeyDownEvent", ocrCaptureWindowCodeBehind);
        Assert.Contains("handledEventsToo: true", ocrCaptureWindowCodeBehind);
        Assert.Contains("private void CloseCaptureOverlay()", ocrCaptureWindowCodeBehind);
        Assert.Contains("CloseCaptureOverlay();", ocrCaptureWindowCodeBehind);
    }

    [Fact]
    public void OcrCaptureWindow_DrawsWindowsStyleWhiteDashedSelectionBorder()
    {
        string ocrCaptureWindowXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml"));
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));

        string selectionBorderBlock = ExtractElementBlock(ocrCaptureWindowXaml, "<Rectangle x:Name=\"SelectionBorder\"", "/>");
        Assert.Contains("Stroke=\"White\"", selectionBorderBlock);
        Assert.Contains("StrokeDashArray=\"2,2\"", selectionBorderBlock);
        Assert.Contains("StrokeThickness=\"1\"", selectionBorderBlock);
        Assert.Contains("Fill=\"Transparent\"", selectionBorderBlock);
        Assert.Contains("Visibility=\"Collapsed\"", selectionBorderBlock);
        Assert.DoesNotContain("BorderBrush=\"Teal\"", ocrCaptureWindowXaml);
        Assert.Contains("HighlightSelectionOverlay(selectionRect)", ocrCaptureWindowCodeBehind);
        Assert.Contains("SelectionBorder.Visibility = Visibility.Visible", ocrCaptureWindowCodeBehind);
        Assert.Contains("Canvas.SetLeft(SelectionBorder, selectionRect.X)", ocrCaptureWindowCodeBehind);
        Assert.Contains("Canvas.SetTop(SelectionBorder, selectionRect.Y)", ocrCaptureWindowCodeBehind);
        Assert.Contains("SelectionBorder.Width = Math.Max(0, selectionRect.Width)", ocrCaptureWindowCodeBehind);
        Assert.Contains("SelectionBorder.Height = Math.Max(0, selectionRect.Height)", ocrCaptureWindowCodeBehind);
    }

    [Fact]
    public void OcrCaptureWindow_LanguageToolbarUsesBalancedStretchLayout()
    {
        string ocrCaptureWindowXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml"));
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));

        Assert.Contains("MinWidth=\"360\"", ocrCaptureWindowXaml);
        Assert.DoesNotContain("                    Width=\"360\"", ocrCaptureWindowXaml);
        Assert.Contains("x:Name=\"LanguageToolbarLayout\"", ocrCaptureWindowXaml);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", ocrCaptureWindowXaml);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", ocrCaptureWindowXaml);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", ocrCaptureWindowXaml);
        Assert.DoesNotContain("<StackPanel Orientation=\"Horizontal\" Spacing=\"10\">", ocrCaptureWindowXaml);
        Assert.Contains("LanguageToolbar.MinWidth", ocrCaptureWindowCodeBehind);
    }

    [Fact]
    public void OcrCaptureWindow_UsesSharpScreenshotPreviewWithDarkFilter()
    {
        string ocrCaptureWindowXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));
        string ocrHelper = File.ReadAllText(LocateSourceFile("Delete Newline", "Helpers", "OcrHelper.cs"));

        Assert.Contains("Opacity=\"0.6\"", ocrCaptureWindowXaml);
        Assert.DoesNotContain("Opacity=\"0.34\"", ocrCaptureWindowXaml);
        Assert.Contains("GetFullDesktopScreenshotAsImageSource()", ocrViewModel);
        Assert.DoesNotContain("blurForCaptureOverlay", ocrViewModel);
        Assert.DoesNotContain("CapturePreviewBlurScale", ocrHelper);
        Assert.DoesNotContain("CreateBlurredPreviewBitmap", ocrHelper);
    }

    [Fact]
    public void OcrCaptureOverlayLayout_CentersToolbarInPrimaryMonitorUsingWindowViewPixels()
    {
        Rectangle virtualScreen = new(0, 0, 3840, 2160);
        Rectangle primaryScreen = new(0, 0, 3840, 2160);

        (double left, double top) = OcrCaptureOverlayLayoutHelper.CalculateTopCenterToolbarPosition(
            virtualScreen,
            primaryScreen,
            windowWidth: 2560,
            windowHeight: 1440,
            toolbarWidth: 360,
            topMargin: 16);

        Assert.Equal(1100, left, precision: 3);
        Assert.Equal(16, top, precision: 3);
    }

    [Fact]
    public void OcrCaptureOverlayLayout_CentersToolbarOnPrimaryMonitorWhenVirtualScreenStartsLeftOfIt()
    {
        Rectangle virtualScreen = new(-1920, 0, 5760, 2160);
        Rectangle primaryScreen = new(0, 0, 3840, 2160);

        (double left, double top) = OcrCaptureOverlayLayoutHelper.CalculateTopCenterToolbarPosition(
            virtualScreen,
            primaryScreen,
            windowWidth: 3840,
            windowHeight: 1440,
            toolbarWidth: 360,
            topMargin: 16);

        Assert.Equal(2380, left, precision: 3);
        Assert.Equal(16, top, precision: 3);
    }

    [Fact]
    public void OcrPage_CanDownloadAndApplyWindowsOcrLanguages()
    {
        string ocrPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OCRPage.xaml"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));
        string installerHelper = File.ReadAllText(LocateSourceFile("Delete Newline", "Helpers", "OcrLanguageInstallHelper.cs"));
        XDocument englishResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "en-US", "Resources.resw"));
        XDocument koreanResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "ko-KR", "Resources.resw"));

        string languageSettingsBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Language Settings -->", "</Border>");
        string installLanguageBlock = ExtractElementBlock(ocrPageXaml, "<Grid x:Name=\"InstallLanguageRow\"", "</Grid>");

        Assert.Contains("x:Uid=\"OCRPage_Description_LanguageSettings\"", languageSettingsBlock);
        Assert.DoesNotContain("OCRPage_Text_AutoInstallLanguageHelp", languageSettingsBlock);
        Assert.Contains("Margin=\"5,10,0,0\"", installLanguageBlock);
        Assert.Contains("x:Uid=\"OCRPage_Header_InstallLanguage\"", installLanguageBlock);
        Assert.Contains("x:Name=\"InstallLanguageRow\"", installLanguageBlock);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", installLanguageBlock);
        Assert.Contains("ItemsSource=\"{x:Bind ViewModel.InstallableLanguages}\"", installLanguageBlock);
        Assert.Contains("SelectedItem=\"{x:Bind ViewModel.SelectedInstallLanguage, Mode=TwoWay}\"", installLanguageBlock);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", installLanguageBlock);
        Assert.DoesNotContain("Width=\"300\"", installLanguageBlock);
        Assert.Contains("Command=\"{x:Bind ViewModel.InstallOcrLanguageCommand}\"", installLanguageBlock);
        Assert.Contains("x:Uid=\"OCRPage_Button_InstallLanguage\"", installLanguageBlock);

        Assert.Contains("ObservableCollection<Language> _installableLanguages", ocrViewModel);
        Assert.Contains("Language? _selectedInstallLanguage", ocrViewModel);
        Assert.Contains("private async Task InstallOcrLanguageAsync()", ocrViewModel);
        Assert.Contains("await OcrLanguageInstallHelper.InstallOcrLanguageCapabilityAsync(SelectedInstallLanguage.LanguageTag)", ocrViewModel);
        Assert.Contains("RefreshAvailableOcrLanguages()", ocrViewModel);
        Assert.Contains("SelectedLanguage = installedLanguage", ocrViewModel);
        Assert.Contains("await SaveOcrLanguageAsync()", ocrViewModel);
        Assert.DoesNotContain("Notification_OcrLanguageInstallSuccess", ocrViewModel);
        Assert.Contains("Notification_OcrLanguageInstallFailed", ocrViewModel);

        Assert.Contains("public static string GetOcrCapabilityName(string languageTag)", installerHelper);
        Assert.Contains("$\"Language.OCR~~~{languageTag}~0.0.1.0\"", installerHelper);
        Assert.Contains("Add-WindowsCapability -Online -Name", installerHelper);
        Assert.Contains("Verb = \"runas\"", installerHelper);
        Assert.Contains("UseShellExecute = true", installerHelper);

        Assert.Equal("Download OCR Language", GetReswValue(englishResources, "OCRPage_Header_InstallLanguage.Header"));
        Assert.Equal("Download and Apply", GetReswValue(englishResources, "OCRPage_Button_InstallLanguage.Content"));
        Assert.DoesNotContain("OCRPage_Text_AutoInstallLanguageHelp.Text", englishResources.ToString());
        Assert.DoesNotContain("Notification_OcrLanguageInstallSuccess", englishResources.ToString());
        Assert.Equal("OCR 언어 다운로드", GetReswValue(koreanResources, "OCRPage_Header_InstallLanguage.Header"));
        Assert.Equal("다운로드 후 적용", GetReswValue(koreanResources, "OCRPage_Button_InstallLanguage.Content"));
        Assert.DoesNotContain("OCRPage_Text_AutoInstallLanguageHelp.Text", koreanResources.ToString());
        Assert.DoesNotContain("Notification_OcrLanguageInstallSuccess", koreanResources.ToString());
    }

    [Fact]
    public async Task LocalHttpServer_HandlesInitializeAndToolsListOverMcpJsonRpc()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var toolService = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);
        var server = new LocalMcpHttpServerService(toolService);
        int port = GetFreeTcpPort();

        await server.StartAsync(port);
        try
        {
            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(5)
            };

            using JsonDocument initialize = await PostJsonRpcAsync(client, port, """
            {
              "jsonrpc": "2.0",
              "id": 1,
              "method": "initialize",
              "params": {
                "protocolVersion": "2025-06-18",
                "capabilities": {},
                "clientInfo": { "name": "delete-newline-tests", "version": "1.0" }
              }
            }
            """);

            JsonElement initializeResult = initialize.RootElement.GetProperty("result");
            Assert.Equal("2025-06-18", initializeResult.GetProperty("protocolVersion").GetString());
            Assert.Equal("delete-newline", initializeResult.GetProperty("serverInfo").GetProperty("name").GetString());
            Assert.True(initializeResult.GetProperty("capabilities").TryGetProperty("tools", out _));

            string instructions = initializeResult.GetProperty("instructions").GetString()!;
            Assert.Contains("system-wide hotkey tool", instructions);
            Assert.Contains("selected text", instructions);
            Assert.Contains("regex rules", instructions);
            Assert.Contains("clipboard", instructions);
            Assert.Contains("Suggested workflow", instructions);
            Assert.Contains("get_regex_profiles", instructions);
            Assert.Contains("test_regex_chain", instructions);
            Assert.Contains("insert_regex_chain_item", instructions);
            Assert.Contains("McpPort", instructions);
            Assert.Contains("UTF-8", instructions);
            Assert.Contains("PowerShell", instructions);
            Assert.Contains("ANSI", instructions);
            Assert.Contains("Number0", instructions);
            Assert.Contains("Number9", instructions);
            Assert.Contains("Do not send JSON numbers", instructions);

            using JsonDocument tools = await PostJsonRpcAsync(client, port, """
            {
              "jsonrpc": "2.0",
              "id": 2,
              "method": "tools/list",
              "params": {}
            }
            """);

            JsonElement[] listedTools = tools.RootElement
                .GetProperty("result")
                .GetProperty("tools")
                .EnumerateArray()
                .ToArray();
            string[] toolNames = listedTools.Select(tool => tool.GetProperty("name").GetString()!).ToArray();

            Assert.Contains("get_regex_profiles", toolNames);
            Assert.Contains("set_app_setting", toolNames);

            JsonElement getProfilesAnnotations = listedTools.Single(tool => tool.GetProperty("name").GetString() == "get_regex_profiles").GetProperty("annotations");
            Assert.True(getProfilesAnnotations.GetProperty("readOnlyHint").GetBoolean());
            Assert.False(getProfilesAnnotations.GetProperty("destructiveHint").GetBoolean());
            Assert.True(getProfilesAnnotations.GetProperty("idempotentHint").GetBoolean());
            Assert.False(getProfilesAnnotations.GetProperty("openWorldHint").GetBoolean());

            JsonElement upsertAnnotations = listedTools.Single(tool => tool.GetProperty("name").GetString() == "upsert_regex_profile").GetProperty("annotations");
            Assert.False(upsertAnnotations.GetProperty("readOnlyHint").GetBoolean());
            Assert.True(upsertAnnotations.GetProperty("destructiveHint").GetBoolean());
        }
        finally
        {
            await server.StopAsync();
        }
    }

    [Fact]
    public async Task SettingsImportApplyService_RejectsInvalidMcpPortBeforeChangingRuntimeState()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        await settingsRepository.SetValueAsync("McpEnabled", true, CancellationToken.None);
        await settingsRepository.SetValueAsync("McpPort", 39333, CancellationToken.None);
        var service = new SettingsImportApplyService(settingsRepository, regexRepository, ocrRepository);

        var importedSettings = Newtonsoft.Json.Linq.JObject.Parse("""
        {
          "McpPort": 70000,
          "McpEnabled": true
        }
        """).ToObject<Dictionary<string, Newtonsoft.Json.Linq.JToken>>()!;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ApplyAsync(importedSettings, CancellationToken.None));
        Assert.True(settingsRepository.GetBoolean("McpEnabled"));
        Assert.Equal(39333, settingsRepository.GetValue<int>("McpPort"));
    }

    [Fact]
    public async Task SettingsImportApplyService_AppliesImportedSettingsToRuntimeRepositoriesWithoutRestart()
    {
        var oldProfile = new RegexPageStructure
        {
            HotkeyName = "Old profile",
            HotkeyComment = "before import"
        };
        oldProfile.RegexChain.ChainItems.Add(new ChainItem { RegexExpression = "old", Replace = "" });

        var regexRepository = new InMemoryRegexConfigurationRepository(oldProfile);
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        await settingsRepository.SetValueAsync("McpEnabled", true, CancellationToken.None);
        await settingsRepository.SetValueAsync("McpPort", 39333, CancellationToken.None);
        var service = new SettingsImportApplyService(settingsRepository, regexRepository, ocrRepository);

        var importedSettings = Newtonsoft.Json.Linq.JObject.Parse("""
        {
          "Notification": false,
          "StartOnTray": true,
          "TopMost": true,
          "AppBackgroundRequestedTheme": "Dark",
          "Localization": "ko-KR",
          "McpPort": 40333,
          "McpEnabled": true,
          "RegexCollection": [
            {
              "HotkeyName": "Imported cleanup",
              "HotkeyComment": "after import",
              "InputText": "hello---world",
              "Hotkey": { "Modifiers": "Control, Shift", "Key": "Q" },
              "RegexChain": {
                "ChainItems": [
                  { "RegexExpression": "-+", "Replace": " " }
                ]
              }
            }
          ],
          "OCR_LanguageTag": "ko-KR",
          "OCR_Hotkey": { "Modifiers": "Control, Menu", "Key": "O" }
        }
        """).ToObject<Dictionary<string, Newtonsoft.Json.Linq.JToken>>()!;

        await service.ApplyAsync(importedSettings, CancellationToken.None);

        Assert.False(settingsRepository.GetBoolean("Notification"));
        Assert.True(settingsRepository.GetBoolean("StartOnTray"));
        Assert.True(settingsRepository.GetBoolean("TopMost"));
        Assert.Equal("Dark", settingsRepository.GetValue<string>("AppBackgroundRequestedTheme"));
        Assert.Equal("ko-KR", settingsRepository.GetValue<string>("Localization"));
        Assert.Equal(40333, settingsRepository.GetValue<int>("McpPort"));
        Assert.True(settingsRepository.GetBoolean("McpEnabled"));

        RegexPageStructure importedProfile = Assert.Single(regexRepository.RegexConfigs);
        Assert.Equal("Imported cleanup", importedProfile.HotkeyName);
        Assert.Equal("hello---world", importedProfile.InputText);
        Assert.Equal(VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, importedProfile.Hotkey.Modifiers);
        Assert.Equal(VirtualKey.Q, importedProfile.Hotkey.Key);
        ChainItem importedChainItem = Assert.Single(importedProfile.RegexChain.ChainItems);
        Assert.Equal("-+", importedChainItem.RegexExpression);
        Assert.Equal(" ", importedChainItem.Replace);

        Assert.Equal("ko-KR", ocrRepository.LanguageTag);
        Assert.Equal(VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu, ocrRepository.Hotkey.Modifiers);
        Assert.Equal(VirtualKey.O, ocrRepository.Hotkey.Key);
        Assert.Equal(1, ocrRepository.SaveCount);
        Assert.Equal(1, regexRepository.SaveCount);
    }

    [Fact]
    public void ProjectVersion_IsConsistentAt317()
    {
        XDocument project = XDocument.Load(LocateSourceFile("Delete Newline", "Delete Newline.csproj"));
        XDocument manifest = XDocument.Load(LocateSourceFile("Delete Newline", "Package.appxmanifest"));

        Assert.Equal("3.1.7", project.Descendants("Version").Single().Value);
        Assert.Equal("3.1.7.0", project.Descendants("AssemblyVersion").Single().Value);
        Assert.Equal("3.1.7.0", project.Descendants("FileVersion").Single().Value);

        XNamespace packageNamespace = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XElement identity = manifest.Root!.Element(packageNamespace + "Identity")!;
        Assert.Equal("3.1.7.0", identity.Attribute("Version")!.Value);
    }

    private static void AssertHotkeyKeySchemaGuidesModelsToNamedAndCommonKeys(McpToolDescriptor tool)
    {
        JsonElement hotkeySchema = tool.InputSchema
            .GetProperty("properties")
            .GetProperty("hotkey");
        string hotkeyDescription = hotkeySchema.GetProperty("description").GetString()!;
        Assert.Contains("Number0", hotkeyDescription);
        Assert.Contains("Number9", hotkeyDescription);
        Assert.Contains("Do not send JSON numbers", hotkeyDescription);
        Assert.Contains("Backspace", hotkeyDescription);
        Assert.Contains("Left Arrow", hotkeyDescription);
        Assert.Contains("Page Up", hotkeyDescription);
        Assert.Contains("Backtick", hotkeyDescription);
        Assert.Contains("Semicolon", hotkeyDescription);
        Assert.DoesNotContain("Backtick (`)", hotkeyDescription);
        Assert.DoesNotContain("Bracket ([ ])", hotkeyDescription);

        JsonElement keySchema = hotkeySchema
            .GetProperty("oneOf")[1]
            .GetProperty("properties")
            .GetProperty("key");
        Assert.True(keySchema.TryGetProperty("type", out JsonElement keyType), keySchema.GetRawText());
        Assert.Equal("string", keyType.GetString());

        string keyDescription = keySchema.GetProperty("description").GetString()!;
        Assert.Contains("Number1", keyDescription);
        Assert.Contains("JSON numbers", keyDescription);
        Assert.Contains("Backspace", keyDescription);
        Assert.Contains("Spacebar", keyDescription);
        Assert.Contains("Del", keyDescription);
        Assert.Contains("Backtick", keyDescription);
        Assert.Contains("Slash", keyDescription);
        Assert.DoesNotContain("Backtick (`)", keyDescription);
        Assert.DoesNotContain("Bracket ([ ])", keyDescription);
    }

    private static async Task<JsonDocument> PostJsonRpcAsync(HttpClient client, int port, string json)
    {
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync($"http://127.0.0.1:{port}/mcp", content);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(body);
    }

    private static string LocateSourceFile(params string[] relativePathSegments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(new[] { directory.FullName }.Concat(relativePathSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate source file: {Path.Combine(relativePathSegments)}");
    }

    private static string GetReswValue(XDocument resources, string name)
    {
        XElement data = resources.Root!
            .Elements("data")
            .Single(element => (string?)element.Attribute("name") == name);

        return data.Element("value")!.Value;
    }

    private static string ExtractElementBlock(string source, string startMarker, string endMarker)
    {
        int startIndex = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Missing start marker: {startMarker}");

        int endIndex = source.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        Assert.True(endIndex >= 0, $"Missing end marker: {endMarker}");

        return source[startIndex..(endIndex + endMarker.Length)];
    }

    private static int GetFreeTcpPort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
