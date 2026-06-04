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
using Windows.Globalization;
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
        Assert.Contains("get_ocr_languages", toolNames);
        Assert.Contains("install_ocr_language", toolNames);
        Assert.Contains("delete_ocr_language", toolNames);
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
    public async Task OcrLanguageMcpTools_ListInstallDeleteAndApplyLanguagesForSmallModels()
    {
        var regexRepository = new InMemoryRegexConfigurationRepository();
        var settingsRepository = new InMemoryMcpSettingsRepository();
        var ocrRepository = new InMemoryMcpOcrConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(regexRepository, settingsRepository, ocrRepository);

        McpToolDescriptor getLanguagesTool = service.ListTools().Single(tool => tool.Name == "get_ocr_languages");
        McpToolDescriptor installLanguageTool = service.ListTools().Single(tool => tool.Name == "install_ocr_language");
        McpToolDescriptor deleteLanguageTool = service.ListTools().Single(tool => tool.Name == "delete_ocr_language");

        Assert.True(getLanguagesTool.ReadOnly);
        Assert.False(installLanguageTool.ReadOnly);
        Assert.False(deleteLanguageTool.ReadOnly);
        Assert.True(deleteLanguageTool.Destructive);
        Assert.Contains("manageableLanguages", getLanguagesTool.Description);
        Assert.Contains("useTool", getLanguagesTool.Description);
        Assert.Contains("deleteTool", getLanguagesTool.Description);
        Assert.DoesNotContain("recommendedTool", getLanguagesTool.Description);
        Assert.True(getLanguagesTool.Description.Length <= 340, getLanguagesTool.Description);
        Assert.DoesNotContain("Step 1", installLanguageTool.Description);
        Assert.Contains("status=missing", installLanguageTool.Description);
        Assert.Contains("exact languageTag", installLanguageTool.Description);
        Assert.True(installLanguageTool.Description.Length <= 260, installLanguageTool.Description);
        Assert.DoesNotContain("Step 1", deleteLanguageTool.Description);
        Assert.Contains("deleteTool=delete_ocr_language", deleteLanguageTool.Description);
        Assert.Contains("Never delete status=current", deleteLanguageTool.Description);
        Assert.Contains("get_ocr_languages", deleteLanguageTool.Description);
        Assert.True(deleteLanguageTool.Description.Length <= 260, deleteLanguageTool.Description);
        Assert.Contains("languageTag", deleteLanguageTool.InputSchema.GetRawText());
        Assert.Contains("ko-KR", deleteLanguageTool.InputSchema.GetRawText());

        using JsonDocument emptyArguments = JsonDocument.Parse("{}");
        McpToolResult listResult = await service.ExecuteAsync("get_ocr_languages", emptyArguments.RootElement, CancellationToken.None);

        Assert.False(listResult.IsError, listResult.Text);
        JsonElement listContent = listResult.StructuredContent!.Value;
        Assert.Equal("en-US", listContent.GetProperty("languageTag").GetString());
        Assert.Contains("en-US", listContent.GetProperty("availableLanguageTags").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("ko-KR", listContent.GetProperty("deletableLanguageTags").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("ja-JP", listContent.GetProperty("installableLanguageTags").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("delete_ocr_language", listContent.GetProperty("recommendedWorkflow").GetString());

        JsonElement[] manageableLanguages = listContent.GetProperty("manageableLanguages").EnumerateArray().ToArray();
        JsonElement english = manageableLanguages.Single(item => item.GetProperty("languageTag").GetString() == "en-US");
        Assert.Equal("current", english.GetProperty("status").GetString());
        Assert.Equal("none", english.GetProperty("useTool").GetString());
        Assert.Equal("none", english.GetProperty("deleteTool").GetString());

        JsonElement korean = manageableLanguages.Single(item => item.GetProperty("languageTag").GetString() == "ko-KR");
        Assert.Equal("installed", korean.GetProperty("status").GetString());
        Assert.Equal("set_ocr_settings", korean.GetProperty("useTool").GetString());
        Assert.Equal("delete_ocr_language", korean.GetProperty("deleteTool").GetString());

        JsonElement japanese = manageableLanguages.Single(item => item.GetProperty("languageTag").GetString() == "ja-JP");
        Assert.Equal("missing", japanese.GetProperty("status").GetString());
        Assert.Equal("install_ocr_language", japanese.GetProperty("useTool").GetString());
        Assert.Equal("none", japanese.GetProperty("deleteTool").GetString());

        using JsonDocument deleteCurrentArguments = JsonDocument.Parse("""
        {
          "languageTag": "en-US"
        }
        """);

        McpToolResult deleteCurrentResult = await service.ExecuteAsync("delete_ocr_language", deleteCurrentArguments.RootElement, CancellationToken.None);
        Assert.True(deleteCurrentResult.IsError);
        Assert.Contains("current OCR language", deleteCurrentResult.Text);

        using JsonDocument deleteArguments = JsonDocument.Parse("""
        {
          "languageTag": "ko-KR"
        }
        """);

        McpToolResult deleteResult = await service.ExecuteAsync("delete_ocr_language", deleteArguments.RootElement, CancellationToken.None);

        Assert.False(deleteResult.IsError, deleteResult.Text);
        JsonElement deleteContent = deleteResult.StructuredContent!.Value;
        Assert.True(deleteContent.GetProperty("deleted").GetBoolean());
        Assert.Equal("ko-KR", deleteContent.GetProperty("languageTag").GetString());
        Assert.DoesNotContain("ko-KR", ocrRepository.AvailableLanguageTags);
        Assert.Contains("ko-KR", ocrRepository.InstallableLanguages.Select(language => language.LanguageTag));
        Assert.Equal(1, ocrRepository.DeleteCount);

        using JsonDocument installArguments = JsonDocument.Parse("""
        {
          "languageTag": "ja-JP"
        }
        """);

        McpToolResult installResult = await service.ExecuteAsync("install_ocr_language", installArguments.RootElement, CancellationToken.None);

        Assert.False(installResult.IsError, installResult.Text);
        JsonElement installContent = installResult.StructuredContent!.Value;
        Assert.True(installContent.GetProperty("installed").GetBoolean());
        Assert.True(installContent.GetProperty("applied").GetBoolean());
        Assert.Equal("ja-JP", installContent.GetProperty("languageTag").GetString());
        Assert.Equal("ja-JP", ocrRepository.LanguageTag);
        Assert.Contains("ja-JP", ocrRepository.AvailableLanguageTags);
        Assert.Equal(1, ocrRepository.SaveCount);
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
    public void DeletedRegexProfilesInvalidateDetailSelectionAndNavigationHistory()
    {
        string regexCollectViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "RegexCollectViewModel.cs"));
        string regexViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "RegexViewModel.cs"));
        string navigationServiceInterface = File.ReadAllText(LocateSourceFile("Delete Newline", "Contracts", "Services", "INavigationService.cs"));
        string navigationService = File.ReadAllText(LocateSourceFile("Delete Newline", "Services", "NavigationService.cs"));

        Assert.Contains("if (!RegexConfigs.Contains(regexConfig))", regexCollectViewModel);
        Assert.Contains("RegexCollectSaveService regexCollectSaveService", regexViewModel);
        Assert.Contains("INavigationService navigationService", regexViewModel);
        Assert.Contains("_regexCollectSaveService.RegexConfigs.CollectionChanged += RegexConfigs_CollectionChanged", regexViewModel);
        Assert.Contains("InvalidateCurrentRegexConfigIfMissing", regexViewModel);
        Assert.Contains("if (CurrentRegexConfig == null || _regexCollectSaveService.RegexConfigs.Contains(CurrentRegexConfig))", regexViewModel);
        Assert.Contains("CurrentRegexConfig = null", regexViewModel);
        Assert.Contains("_navigationService.RemoveHistoryEntries(typeof(RegexViewModel).FullName!)", regexViewModel);
        Assert.Contains("_navigationService.IsCurrentPage(typeof(RegexViewModel).FullName!)", regexViewModel);
        Assert.Contains("_navigationService.NavigateTo(typeof(RegexCollectViewModel).FullName!, clearNavigation: true)", regexViewModel);

        Assert.Contains("bool IsCurrentPage(string pageKey);", navigationServiceInterface);
        Assert.Contains("void RemoveHistoryEntries(string pageKey);", navigationServiceInterface);
        Assert.Contains("public bool IsCurrentPage(string pageKey)", navigationService);
        Assert.Contains("public void RemoveHistoryEntries(string pageKey)", navigationService);
        Assert.Contains("RemoveHistoryEntries(Frame.BackStack", navigationService);
        Assert.Contains("RemoveHistoryEntries(Frame.ForwardStack", navigationService);
        Assert.Contains("SourcePageType == pageType", navigationService);
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
    public void OcrCaptureWindow_UsesFrozenScreenshotPreviewThenDarkensIt()
    {
        string ocrCaptureWindowXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml"));
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));
        string ocrHelper = File.ReadAllText(LocateSourceFile("Delete Newline", "Helpers", "OcrHelper.cs"));

        string topOverlayBlock = ExtractElementBlock(ocrCaptureWindowXaml, "<Rectangle x:Name=\"TopOverlay\"", "/>");
        string leftOverlayBlock = ExtractElementBlock(ocrCaptureWindowXaml, "<Rectangle x:Name=\"LeftOverlay\"", "/>");
        string rightOverlayBlock = ExtractElementBlock(ocrCaptureWindowXaml, "<Rectangle x:Name=\"RightOverlay\"", "/>");
        string bottomOverlayBlock = ExtractElementBlock(ocrCaptureWindowXaml, "<Rectangle x:Name=\"BottomOverlay\"", "/>");

        Assert.Contains("GetFullDesktopScreenshotAsImageSource()", ocrViewModel);
        Assert.Contains("BackgroundImage.Source = backgroundImage", ocrCaptureWindowCodeBehind);
        Assert.Contains("Opacity=\"0\"", topOverlayBlock);
        Assert.Contains("Opacity=\"0\"", leftOverlayBlock);
        Assert.Contains("Opacity=\"0\"", rightOverlayBlock);
        Assert.Contains("Opacity=\"0\"", bottomOverlayBlock);
        string dimFadeBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    private void BeginOverlayDarkenFade()",
            "    private void SetOverlayOpacity");

        Assert.Contains("private const double OverlayTargetOpacity = 0.6", ocrCaptureWindowCodeBehind);
        Assert.Contains("BeginOverlayDarkenFade()", ocrCaptureWindowCodeBehind);
        Assert.DoesNotContain("SetLayeredWindowAttributes", dimFadeBlock);
        Assert.DoesNotContain("byte alpha", ocrCaptureWindowCodeBehind);
        Assert.DoesNotContain("Opacity=\"0.34\"", ocrCaptureWindowXaml);
        Assert.DoesNotContain("blurForCaptureOverlay", ocrViewModel);
        Assert.DoesNotContain("CapturePreviewBlurScale", ocrHelper);
        Assert.DoesNotContain("CreateBlurredPreviewBitmap", ocrHelper);
    }

    [Fact]
    public void OcrCaptureWindow_PreparesFrozenPreviewBeforeActivationCanExposeIt()
    {
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));

        string setupFullscreenBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    public void SetupFullscreen(",
            "    private void SetupLanguageSelector");

        int setupIndex = ocrViewModel.IndexOf("ocrWindow.SetupFullscreen(backgroundImage, SelectedLanguage, AvailableLanguages.ToArray(), OnCaptureLanguageChanged);", StringComparison.Ordinal);
        int activateIndex = ocrViewModel.IndexOf("ocrWindow.Activate();", StringComparison.Ordinal);
        Assert.True(setupIndex >= 0 && activateIndex >= 0 && setupIndex < activateIndex,
            "The capture window must be fully prepared before Activate can display the first frame.");

        Assert.Contains("BackgroundImage.Source = backgroundImage", setupFullscreenBlock);
        Assert.Contains("NativeMethods.SWP_NOACTIVATE", setupFullscreenBlock);
        Assert.DoesNotContain("NativeMethods.SWP_SHOWWINDOW", setupFullscreenBlock);
        Assert.DoesNotContain("SetForegroundWindow(hwnd)", setupFullscreenBlock);
    }

    [Fact]
    public void OcrCaptureWindow_HidesNativeWindowUntilFirstRenderedFrameThenRevealsImmediately()
    {
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));

        string setupFullscreenBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    public void SetupFullscreen(",
            "    private void SetupLanguageSelector");
        string revealBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    private async void RevealOverlayAfterFirstRender()",
            "    private void RevealOverlayWindow()");
        string revealWindowBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    private void RevealOverlayWindow()",
            "    private void BeginOverlayDarkenFade()");
        string dimFadeBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    private void BeginOverlayDarkenFade()",
            "    private void SetOverlayOpacity");

        Assert.Contains("WS_EX_LAYERED", ocrCaptureWindowCodeBehind);
        Assert.Contains("NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 0, NativeMethods.LWA_ALPHA)", setupFullscreenBlock);
        Assert.Contains("MainGrid.Loaded += OnOverlayContentLoaded", ocrCaptureWindowCodeBehind);
        Assert.Contains("RevealOverlayAfterFirstRender()", ocrCaptureWindowCodeBehind);
        Assert.Contains("StartOverlayRevealFallbackTimer()", ocrCaptureWindowCodeBehind);
        Assert.Contains("Task.Delay(OverlayRevealDelay)", revealBlock);
        Assert.Contains("NativeMethods.SetLayeredWindowAttributes(_overlayHwnd, 0, 255, NativeMethods.LWA_ALPHA)", revealWindowBlock);
        Assert.Contains("BeginOverlayDarkenFade()", revealWindowBlock);
        Assert.DoesNotContain("ImageOpened", ocrCaptureWindowCodeBehind);
        Assert.DoesNotContain("SetLayeredWindowAttributes", dimFadeBlock);
        Assert.DoesNotContain("byte alpha", ocrCaptureWindowCodeBehind);
    }

    [Fact]
    public void OcrHelper_DisposesNativeBitmapAndStreamWrappersDuringRepeatedOcr()
    {
        string ocrHelper = File.ReadAllText(LocateSourceFile("Delete Newline", "Helpers", "OcrHelper.cs"));

        string getRegionsTextBlock = ExtractElementBlock(
            ocrHelper,
            "    public static async Task<string> GetRegionsTextAsync(",
            "    public static async Task<string> GetTextFromBitmapAsync");
        string getOcrResultBlock = ExtractElementBlock(
            ocrHelper,
            "    public static async Task<OcrResult> GetOcrResultFromBitmapAsync(",
            "    private static string GetTextFromOcrResult");
        string getClickedWordBlock = ExtractElementBlock(
            ocrHelper,
            "    public static async Task<string> GetClickedWordAsync(",
            "public static class ImageHelper");
        string getRegionBitmapBlock = ExtractElementBlock(
            ocrHelper,
            "    public static Bitmap GetRegionOfScreenAsBitmap(",
            "    public static Bitmap GetFullDesktopScreenshot()");
        string padImageBlock = ExtractElementBlock(
            ocrHelper,
            "    public static Bitmap PadImage(",
            "    public static Bitmap GetWindowsBoundsBitmap");
        string bitmapToImageSourceBlock = ExtractElementBlock(
            ocrHelper,
            "    public static BitmapImage BitmapToImageSource(",
            "    public static Bitmap ScaleBitmapUniform");
        string bitmapToSoftwareBitmapBlock = ExtractElementBlock(
            ocrHelper,
            "    public static async Task<SoftwareBitmap> BitmapToSoftwareBitmapAsync(",
            "    public static BitmapImage GetWindowBoundsImage");

        Assert.Contains("using Bitmap bmp = ImageHelper.GetRegionOfScreenAsBitmap(correctedRegion)", getRegionsTextBlock);
        Assert.Contains("using Bitmap bmp = ImageHelper.GetWindowsBoundsBitmap(window)", getClickedWordBlock);
        Assert.Contains("using Bitmap bmp = new(region.Width, region.Height, PixelFormat.Format32bppArgb)", getRegionBitmapBlock);
        Assert.Contains("return PadImage(bmp)", getRegionBitmapBlock);
        Assert.Contains("return new Bitmap(image)", padImageBlock);
        Assert.DoesNotContain("return image;", padImageBlock);
        Assert.Contains("using var randomAccessStream = stream.AsRandomAccessStream()", getOcrResultBlock);
        Assert.Contains("BitmapDecoder.CreateAsync(randomAccessStream)", getOcrResultBlock);
        Assert.Contains("using var randomAccessStream = memory.AsRandomAccessStream()", bitmapToImageSourceBlock);
        Assert.Contains("bitmapImage.SetSource(randomAccessStream)", bitmapToImageSourceBlock);
        Assert.Contains("using var randomAccessStream = stream.AsRandomAccessStream()", bitmapToSoftwareBitmapBlock);
        Assert.Contains("BitmapDecoder.CreateAsync(randomAccessStream)", bitmapToSoftwareBitmapBlock);
    }

    [Fact]
    public void OcrCaptureWindow_ReleasesPreviewHandlersAndSessionAfterBackgroundOcrCompletes()
    {
        string ocrCaptureWindowCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrCaptureWindow.xaml.cs"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));

        string constructorBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    public OcrCaptureWindow()",
            "    private void OnWindowActivated_FirstTime");
        string cleanupBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    private void CleanupWindowResources()",
            "    private void OnWindowActivated_FirstTime");
        string pointerReleasedBlock = ExtractElementBlock(
            ocrCaptureWindowCodeBehind,
            "    private async void Canvas_PointerReleased(",
            "    private async Task ProcessOcrAsync");
        string viewModelClosedBlock = ExtractElementBlock(
            ocrViewModel,
            "    private void OcrWindow_Closed(",
            "    private void OnCaptureLanguageChanged");

        Assert.Contains("this.Closed += OcrCaptureWindow_Closed", constructorBlock);
        Assert.Contains("_overlayFadeTimer", ocrCaptureWindowCodeBehind);
        Assert.Contains("_overlayRevealFallbackTimer", ocrCaptureWindowCodeBehind);
        Assert.Contains("StopOverlayFadeTimer()", cleanupBlock);
        Assert.Contains("StopOverlayRevealFallbackTimer()", cleanupBlock);
        Assert.Contains("BackgroundImage.Source = null", cleanupBlock);
        Assert.Contains("backgroundImage = null", cleanupBlock);
        Assert.Contains("CaptureLanguageComboBox.ItemsSource = null", cleanupBlock);
        Assert.Contains("_languageChanged = null", cleanupBlock);
        Assert.Contains("RegionClickCanvas.PointerPressed -= Canvas_PointerPressed", cleanupBlock);
        Assert.Contains("RegionClickCanvas.PointerMoved -= Canvas_PointerMoved", cleanupBlock);
        Assert.Contains("RegionClickCanvas.PointerReleased -= Canvas_PointerReleased", cleanupBlock);
        Assert.Contains("IsOcrProcessing = true", pointerReleasedBlock);
        Assert.Contains("NotifyOcrProcessingCompleted()", pointerReleasedBlock);
        Assert.Contains("public event EventHandler? OcrProcessingCompleted", ocrCaptureWindowCodeBehind);
        Assert.Contains("OcrProcessingCompleted += OcrWindow_OcrProcessingCompleted", ocrViewModel);
        Assert.Contains("if (!closedWindow.IsOcrProcessing)", viewModelClosedBlock);
        Assert.Contains("ReleaseOcrSession()", viewModelClosedBlock);
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
    public void OcrLanguageInstallHelper_ListsManageableLanguagesWithInstalledAndCurrentStatus()
    {
        Language[] installedLanguages = [new("en-US"), new("ko-KR")];

        OcrLanguageManagementItem[] items = OcrLanguageInstallHelper
            .GetManageableLanguages(installedLanguages, currentLanguageTag: "ko-KR", installedStatusText: "(installed)")
            .Where(item => item.LanguageTag is "en-US" or "ko-KR" or "ja-JP")
            .ToArray();

        OcrLanguageManagementItem english = Assert.Single(items, item => item.LanguageTag == "en-US");
        Assert.True(english.IsInstalled);
        Assert.False(english.IsCurrent);
        Assert.EndsWith(" (installed)", english.DisplayText, StringComparison.Ordinal);

        OcrLanguageManagementItem korean = Assert.Single(items, item => item.LanguageTag == "ko-KR");
        Assert.True(korean.IsInstalled);
        Assert.True(korean.IsCurrent);
        Assert.EndsWith(" (installed)", korean.DisplayText, StringComparison.Ordinal);

        OcrLanguageManagementItem japanese = Assert.Single(items, item => item.LanguageTag == "ja-JP");
        Assert.False(japanese.IsInstalled);
        Assert.False(japanese.IsCurrent);
        Assert.Equal(japanese.DisplayName, japanese.DisplayText);
    }

    [Fact]
    public void OcrLanguageRefreshPreservesCurrentSelectionWhenInstallIsCancelled()
    {
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));
        string refreshMethod = ExtractElementBlock(
            ocrViewModel,
            "    private void RefreshAvailableOcrLanguages()",
            "    private void RefreshInstallableOcrLanguages()");

        Assert.Contains("string? selectedLanguageTag = SelectedLanguage?.LanguageTag", refreshMethod);
        Assert.Contains("RestoreSelectedLanguageAfterRefresh(selectedLanguageTag)", refreshMethod);
        Assert.Contains("private void RestoreSelectedLanguageAfterRefresh(string? selectedLanguageTag)", ocrViewModel);
        Assert.Contains("Language? restoredLanguage = FindLanguageByTag(AvailableLanguages, selectedLanguageTag)", ocrViewModel);
        Assert.Contains("_suppressLanguageAutoSave = true", ocrViewModel);
        Assert.Contains("SelectedLanguage = restoredLanguage", ocrViewModel);
        Assert.Contains("_suppressLanguageAutoSave = false", ocrViewModel);

        int restoreIndex = refreshMethod.IndexOf("RestoreSelectedLanguageAfterRefresh(selectedLanguageTag)", StringComparison.Ordinal);
        int refreshManageableIndex = refreshMethod.IndexOf("RefreshManageableOcrLanguages();", StringComparison.Ordinal);
        Assert.True(restoreIndex >= 0 && restoreIndex < refreshManageableIndex,
            "The OCR selection must be restored before rebuilding manageable languages so the current item remains marked after a cancelled install.");
    }

    [Fact]
    public void OcrPage_ManagesInstalledAndMissingOcrLanguagesInOneSection()
    {
        string ocrPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OCRPage.xaml"));
        string ocrPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OcrPage.xaml.cs"));
        string ocrViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "OCRViewModel.cs"));
        string installerHelper = File.ReadAllText(LocateSourceFile("Delete Newline", "Helpers", "OcrLanguageInstallHelper.cs"));
        XDocument englishResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "en-US", "Resources.resw"));
        XDocument koreanResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "ko-KR", "Resources.resw"));

        string languageSettingsBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Language Settings -->", "</Border>");
        string languageManagementBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Language Management -->", "</Border>");

        Assert.Contains("x:Uid=\"OCRPage_Description_LanguageSettings\"", languageSettingsBlock);
        Assert.DoesNotContain("OCRPage_Text_AutoInstallLanguageHelp", languageSettingsBlock);
        Assert.Contains("x:Uid=\"OCRPage_Title_LanguageManagement\"", languageManagementBlock);
        Assert.Contains("x:Uid=\"OCRPage_Description_LanguageManagement\"", languageManagementBlock);
        Assert.Contains("x:Name=\"ManageLanguageRow\"", languageManagementBlock);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", languageManagementBlock);
        Assert.Contains("ItemsSource=\"{x:Bind ViewModel.ManageableLanguages}\"", languageManagementBlock);
        Assert.Contains("SelectedItem=\"{x:Bind ViewModel.SelectedManageLanguage, Mode=TwoWay}\"", languageManagementBlock);
        Assert.Contains("Text=\"{Binding DisplayText}\"", languageManagementBlock);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", languageManagementBlock);
        Assert.DoesNotContain("Width=\"300\"", languageManagementBlock);
        Assert.Contains("x:Uid=\"OCRPage_Button_InstallLanguage\"", languageManagementBlock);
        Assert.Contains("Command=\"{x:Bind ViewModel.InstallManagedOcrLanguageCommand}\"", languageManagementBlock);
        Assert.Contains("x:Uid=\"OCRPage_Button_DeleteLanguage\"", languageManagementBlock);
        Assert.Contains("Click=\"DeleteManagedLanguageButton_Click\"", languageManagementBlock);
        Assert.Contains("x:Uid=\"OCRPage_Button_CurrentLanguage\"", languageManagementBlock);
        Assert.Contains("IsEnabled=\"False\"", languageManagementBlock);

        Assert.Contains("ContentDialog", ocrPageCodeBehind);
        Assert.Contains("DeleteManagedLanguageButton_Click", ocrPageCodeBehind);
        Assert.Contains("OCRPage_DeleteLanguageDialog.Message", ocrPageCodeBehind);
        Assert.Contains("await ViewModel.DeleteManagedOcrLanguageAsync", ocrPageCodeBehind);

        Assert.Contains("ObservableCollection<OcrLanguageManagementItem> _manageableLanguages", ocrViewModel);
        Assert.Contains("OcrLanguageManagementItem? _selectedManageLanguage", ocrViewModel);
        Assert.Contains("InstallManagedOcrLanguageCommand", ocrViewModel);
        Assert.Contains("public async Task<McpOcrLanguageDeleteResult> DeleteManagedOcrLanguageAsync(string languageTag", ocrViewModel);
        Assert.Contains("string requestedLanguageTag = languageTag.Trim()", ocrViewModel);
        Assert.Contains("LanguageTagsMatch(SelectedLanguage.LanguageTag, requestedLanguageTag)", ocrViewModel);
        Assert.Contains("string capabilityLanguageTag = managedLanguageToDelete?.LanguageTag ?? requestedLanguageTag", ocrViewModel);
        Assert.Contains("await OcrLanguageInstallHelper.RemoveOcrLanguageCapabilityAsync(capabilityLanguageTag)", ocrViewModel);
        Assert.Contains("RefreshAvailableOcrLanguages()", ocrViewModel);
        Assert.Contains("SelectedLanguage = installedLanguage", ocrViewModel);
        Assert.Contains("await SaveOcrLanguageAsync()", ocrViewModel);
        Assert.Contains("Notification_OcrLanguageInstallSuccess_Title", ocrViewModel);
        Assert.Contains("Notification_OcrLanguageDeleteSuccess_Title", ocrViewModel);
        Assert.Contains("Notification_OcrLanguageDeleteFailed_Title", ocrViewModel);
        Assert.Contains("InfoBarSeverity.Success", ocrViewModel);
        Assert.Contains("Notification_OcrLanguageInstallFailed", ocrViewModel);

        Assert.Contains("public static string GetOcrCapabilityName(string languageTag)", installerHelper);
        Assert.Contains("public sealed class OcrLanguageManagementItem", installerHelper);
        Assert.Contains("public static IReadOnlyList<OcrLanguageManagementItem> GetManageableLanguages", installerHelper);
        Assert.Contains("$\"Language.OCR~~~{languageTag}~0.0.1.0\"", installerHelper);
        Assert.Contains("Add-WindowsCapability -Online -Name", installerHelper);
        Assert.Contains("Remove-WindowsCapability -Online -Name", installerHelper);
        Assert.Contains("public static async Task<int> RemoveOcrLanguageCapabilityAsync", installerHelper);
        Assert.Contains("Verb = \"runas\"", installerHelper);
        Assert.Contains("UseShellExecute = true", installerHelper);

        Assert.Equal("OCR Language Management", GetReswValue(englishResources, "OCRPage_Title_LanguageManagement.Text"));
        Assert.Equal("Download missing Windows OCR languages or delete installed languages that are not in use.", GetReswValue(englishResources, "OCRPage_Description_LanguageManagement.Text"));
        Assert.Equal("Download and Apply", GetReswValue(englishResources, "OCRPage_Button_InstallLanguage.Content"));
        Assert.Equal("Delete", GetReswValue(englishResources, "OCRPage_Button_DeleteLanguage.Content"));
        Assert.Equal("Currently in Use", GetReswValue(englishResources, "OCRPage_Button_CurrentLanguage.Content"));
        Assert.Equal("(installed)", GetReswValue(englishResources, "OCRPage_Text_LanguageInstalledSuffix.Text"));
        Assert.Equal("Delete OCR Language", GetReswValue(englishResources, "OCRPage_DeleteLanguageDialog.Title"));
        Assert.Equal("Delete", GetReswValue(englishResources, "OCRPage_DeleteLanguageDialog.PrimaryButtonText"));
        Assert.Equal("Cancel", GetReswValue(englishResources, "OCRPage_DeleteLanguageDialog.CloseButtonText"));
        Assert.Equal("OCR Language Installed", GetReswValue(englishResources, "Notification_OcrLanguageInstallSuccess_Title"));
        Assert.Equal("{0} is ready for OCR.", GetReswValue(englishResources, "Notification_OcrLanguageInstallSuccess_Message"));
        Assert.Equal("OCR Language Deleted", GetReswValue(englishResources, "Notification_OcrLanguageDeleteSuccess_Title"));

        Assert.Equal("OCR 언어 관리", GetReswValue(koreanResources, "OCRPage_Title_LanguageManagement.Text"));
        Assert.Equal("필요한 Windows OCR 언어를 다운로드하거나 사용 중이 아닌 설치된 언어를 삭제합니다.", GetReswValue(koreanResources, "OCRPage_Description_LanguageManagement.Text"));
        Assert.Equal("다운로드 후 적용", GetReswValue(koreanResources, "OCRPage_Button_InstallLanguage.Content"));
        Assert.Equal("삭제", GetReswValue(koreanResources, "OCRPage_Button_DeleteLanguage.Content"));
        Assert.Equal("현재 사용 중", GetReswValue(koreanResources, "OCRPage_Button_CurrentLanguage.Content"));
        Assert.Equal("(설치됨)", GetReswValue(koreanResources, "OCRPage_Text_LanguageInstalledSuffix.Text"));
        Assert.Equal("OCR 언어 삭제", GetReswValue(koreanResources, "OCRPage_DeleteLanguageDialog.Title"));
        Assert.Equal("삭제", GetReswValue(koreanResources, "OCRPage_DeleteLanguageDialog.PrimaryButtonText"));
        Assert.Equal("취소", GetReswValue(koreanResources, "OCRPage_DeleteLanguageDialog.CloseButtonText"));
        Assert.Equal("OCR 언어 설치됨", GetReswValue(koreanResources, "Notification_OcrLanguageInstallSuccess_Title"));
        Assert.Equal("{0} OCR을 사용할 수 있습니다.", GetReswValue(koreanResources, "Notification_OcrLanguageInstallSuccess_Message"));
        Assert.Equal("OCR 언어 삭제됨", GetReswValue(koreanResources, "Notification_OcrLanguageDeleteSuccess_Title"));

        Assert.DoesNotContain("OCRPage_Title_LanguageDownload.Text", englishResources.ToString());
        Assert.DoesNotContain("OCRPage_Title_LanguageDownload.Text", koreanResources.ToString());
        Assert.DoesNotContain("OCRPage_Header_InstallLanguage.Header", englishResources.ToString());
        Assert.DoesNotContain("OCRPage_Text_AutoInstallLanguageHelp.Text", englishResources.ToString());
        Assert.DoesNotContain("OCRPage_Header_InstallLanguage.Header", koreanResources.ToString());
        Assert.DoesNotContain("OCRPage_Text_AutoInstallLanguageHelp.Text", koreanResources.ToString());
    }

    [Fact]
    public void OcrPage_GroupsHotkeyLanguageAndLanguageManagementIntoSeparateCards()
    {
        string ocrPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OCRPage.xaml"));
        XDocument englishResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "en-US", "Resources.resw"));
        XDocument koreanResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "ko-KR", "Resources.resw"));

        string hotkeySettingsBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Hotkey Settings -->", "</Border>");
        string languageSettingsBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Language Settings -->", "<!-- OCR Language Management -->");
        string languageManagementBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Language Management -->", "</Border>");

        Assert.Contains("x:Uid=\"OCRPage_Title_HotkeySettings\"", hotkeySettingsBlock);
        Assert.Contains("x:Uid=\"OCRPage_Title_LanguageSettings\"", languageSettingsBlock);
        Assert.Contains("x:Name=\"LanguageComboBox\"", languageSettingsBlock);
        Assert.DoesNotContain("ManageLanguageRow", languageSettingsBlock);

        Assert.Contains("x:Uid=\"OCRPage_Title_LanguageManagement\"", languageManagementBlock);
        Assert.Contains("x:Uid=\"OCRPage_Description_LanguageManagement\"", languageManagementBlock);
        Assert.Contains("x:Name=\"ManageLanguageRow\"", languageManagementBlock);
        Assert.DoesNotContain("x:Uid=\"OCRPage_Header_InstallLanguage\"", languageManagementBlock);
        Assert.Contains("Command=\"{x:Bind ViewModel.InstallManagedOcrLanguageCommand}\"", languageManagementBlock);
        Assert.Contains("Click=\"DeleteManagedLanguageButton_Click\"", languageManagementBlock);

        Assert.Equal("OCR Language Management", GetReswValue(englishResources, "OCRPage_Title_LanguageManagement.Text"));
        Assert.Equal("Download missing Windows OCR languages or delete installed languages that are not in use.", GetReswValue(englishResources, "OCRPage_Description_LanguageManagement.Text"));
        Assert.Equal("OCR 언어 관리", GetReswValue(koreanResources, "OCRPage_Title_LanguageManagement.Text"));
        Assert.Equal("필요한 Windows OCR 언어를 다운로드하거나 사용 중이 아닌 설치된 언어를 삭제합니다.", GetReswValue(koreanResources, "OCRPage_Description_LanguageManagement.Text"));
    }

    [Fact]
    public void OcrPage_UsesEqualLanguageManagementActionButtonWidths()
    {
        string ocrPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "OCRPage.xaml"));
        string languageManagementBlock = ExtractElementBlock(ocrPageXaml, "<!-- OCR Language Management -->", "</Border>");

        string installButton = ExtractElementBlock(languageManagementBlock, "<Button x:Name=\"InstallManagedLanguageButton\"", "/>" );
        string deleteButton = ExtractElementBlock(languageManagementBlock, "<Button x:Name=\"DeleteManagedLanguageButton\"", "/>" );
        string currentButton = ExtractElementBlock(languageManagementBlock, "<Button x:Name=\"CurrentManagedLanguageButton\"", "/>" );

        Assert.Contains("Width=\"160\"", installButton);
        Assert.Contains("Width=\"160\"", deleteButton);
        Assert.Contains("Width=\"160\"", currentButton);
        Assert.Contains("HorizontalAlignment=\"Right\"", installButton);
        Assert.Contains("HorizontalAlignment=\"Right\"", deleteButton);
        Assert.Contains("HorizontalAlignment=\"Right\"", currentButton);
    }

    [Fact]
    public void SettingsPage_AppLanguageChangesWithoutBottomSuccessNotification()
    {
        string settingsViewModel = File.ReadAllText(LocateSourceFile("Delete Newline", "ViewModels", "SettingsViewModel.cs"));
        XDocument englishResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "en-US", "Resources.resw"));
        XDocument koreanResources = XDocument.Load(LocateSourceFile("Delete Newline", "Strings", "ko-KR", "Resources.resw"));

        string switchLanguageBlock = ExtractElementBlock(settingsViewModel, "private async Task SwitchLanguageAsync(LanguageItem param)", "private async Task ToggleNotificationAsync(bool isChecked)");

        Assert.Contains("await _localizationService.SetLanguage(param)", switchLanguageBlock);
        Assert.Contains("RefreshCurrentUiLanguage()", switchLanguageBlock);
        Assert.DoesNotContain("ShowInAppNotification", switchLanguageBlock);
        Assert.DoesNotContain("Notification_LanguageChanged", switchLanguageBlock);

        Assert.DoesNotContain("Notification_LanguageChanged", englishResources.ToString());
        Assert.DoesNotContain("Notification_LanguageChanged", koreanResources.ToString());
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
            Assert.Contains("get_ocr_languages", instructions);
            Assert.Contains("useTool", instructions);
            Assert.Contains("deleteTool", instructions);
            Assert.Contains("install_ocr_language", instructions);
            Assert.Contains("delete_ocr_language", instructions);
            Assert.Contains("current OCR language", instructions);
            Assert.DoesNotContain("recommendedTool", instructions);
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
            Assert.Contains("get_ocr_languages", toolNames);
            Assert.Contains("install_ocr_language", toolNames);
            Assert.Contains("delete_ocr_language", toolNames);

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
