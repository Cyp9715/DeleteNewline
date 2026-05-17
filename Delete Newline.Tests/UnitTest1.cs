using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Delete_Newline.Contracts.Structures;
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
        Assert.Contains("VerticalAlignment=\"Top\"", regexCollectPageXaml);
        Assert.Contains("TextChanged=\"RegexSearchTextBox_TextChanged\"", regexCollectPageXaml);
        Assert.Contains("KeyDown=\"RegexSearchTextBox_KeyDown\"", regexCollectPageXaml);

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
    public void RegexCollectPage_RestoresSearchOverlayWhenReturningToFilteredProfiles()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("Loaded=\"RegexCollectPage_Loaded\"", regexCollectPageXaml);
        Assert.Contains("RegexCollectPage_Loaded", regexCollectPageCodeBehind);
        Assert.Contains("SynchronizeRegexSearchOverlayWithViewModel", regexCollectPageCodeBehind);
        Assert.Contains("!string.IsNullOrWhiteSpace(ViewModel.SearchText)", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchTextBox.Text = ViewModel.SearchText", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchOverlay.Visibility = Visibility.Visible", regexCollectPageCodeBehind);
    }

    [Fact]
    public void RegexCollectPage_DismissesSearchWithEscapeAndWhenEmptySearchLosesFocus()
    {
        string regexCollectPageXaml = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml"));
        string regexCollectPageCodeBehind = File.ReadAllText(LocateSourceFile("Delete Newline", "Views", "RegexCollectPage.xaml.cs"));

        Assert.Contains("LostFocus=\"RegexSearchTextBox_LostFocus\"", regexCollectPageXaml);
        Assert.Contains("RegexSearchTextBox_KeyDown", regexCollectPageCodeBehind);
        Assert.Contains("HideRegexSearchBox(restoreFocus: true)", regexCollectPageCodeBehind);
        Assert.Contains("RegexSearchTextBox_LostFocus", regexCollectPageCodeBehind);
        Assert.Contains("string.IsNullOrWhiteSpace(RegexSearchTextBox.Text)", regexCollectPageCodeBehind);
        Assert.Contains("HideRegexSearchBox(restoreFocus: false)", regexCollectPageCodeBehind);
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
            Assert.Contains("Suggested workflow", instructions);
            Assert.Contains("get_regex_profiles", instructions);
            Assert.Contains("test_regex_chain", instructions);
            Assert.Contains("insert_regex_chain_item", instructions);
            Assert.Contains("McpPort", instructions);

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
