using System.Text.Json;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Delete_Newline.Services.Mcp;
using Newtonsoft.Json;

namespace Delete_Newline.Tests;

public sealed class RegexTestInputTests
{
    [Theory]
    [InlineData(TestInputLineEnding.CrLf, "one\r\ntwo\r\n\r\n")]
    [InlineData(TestInputLineEnding.Lf, "one\ntwo\n\n")]
    [InlineData(TestInputLineEnding.Cr, "one\rtwo\r\r")]
    public void TypedReturnsUseTheSelectedFormat(TestInputLineEnding format, string expected)
    {
        string input = RegexTestInput.Normalize("one\rtwo\r\r", format);
        Assert.Equal(expected, input);
        Assert.Equal("one\rtwo\r\r", RegexTestInput.ToEditor(input));
        Assert.Equal(input, RegexTestInput.Normalize(input, format));
    }

    [Theory]
    [InlineData(TestInputLineEnding.CrLf, "one\r\ntwo\r\nthree\r\nfour")]
    [InlineData(TestInputLineEnding.Lf, "one\ntwo\nthree\nfour")]
    [InlineData(TestInputLineEnding.Cr, "one\rtwo\rthree\rfour")]
    public void NormalizationHandlesMixedEndingsWithoutDuplicatingCrLf(TestInputLineEnding format, string expected)
    {
        Assert.Equal(expected, RegexTestInput.Normalize("one\r\ntwo\nthree\rfour", format));
    }

    [Fact]
    public void NormalizationLeavesUnicodeSeparatorsAndLiteralEscapesAlone()
    {
        const string input = "one\u0085two\u2028three\u2029four\vf\f\\r\\n";
        Assert.Equal(input, RegexTestInput.Normalize(input, TestInputLineEnding.CrLf));
    }

    [Theory]
    [InlineData(TestInputLineEnding.CrLf, @"\r\n", "one two")]
    [InlineData(TestInputLineEnding.Lf, @"\n", "one two")]
    [InlineData(TestInputLineEnding.Cr, @"\r", "one two")]
    public void EditorAndRuntimeProduceTheSameResult(TestInputLineEnding format, string pattern, string expected)
    {
        string input = RegexTestInput.Normalize("one\rtwo", format);
        ChainItem[] chain = [new() { RegexExpression = pattern, Replace = " " }];
        Assert.Equal(expected, RegexTextProcessor.ApplyRegexChain(input, chain, "invalid"));
        Assert.Equal(expected, RegexTextProcessor.Transform(input, chain));
    }

    [Fact]
    public void WindowsModeMakesLfPatternsAndMultilineAnchorsWork()
    {
        string input = RegexTestInput.Normalize("one\rtwo", TestInputLineEnding.CrLf);
        Assert.Equal("one\rtwo", RegexTextProcessor.Transform(input, [new() { RegexExpression = @"\n", Replace = "" }]));
        Assert.Equal("one\r\nTWO", RegexTextProcessor.Transform(input, [new() { RegexExpression = "^two$", Replace = "TWO" }]));
    }

    [Fact]
    public void LegacyCrSamplesDefaultToWindowsButExplicitCrSamplesStayCr()
    {
        Assert.Equal(TestInputLineEnding.CrLf, RegexTestInput.Detect("one\rtwo", legacyTextBox: true));
        Assert.Equal(TestInputLineEnding.Cr, RegexTestInput.Detect("one\rtwo"));
        Assert.Equal(TestInputLineEnding.Lf, RegexTestInput.Detect("one\ntwo", legacyTextBox: true));
        Assert.Equal(TestInputLineEnding.CrLf, RegexTestInput.Detect("one\r\ntwo", legacyTextBox: true));
    }

    [Theory]
    [InlineData(TestInputLineEnding.CrLf)]
    [InlineData(TestInputLineEnding.Lf)]
    [InlineData(TestInputLineEnding.Cr)]
    public void ProfileRoundTripRetainsTheEditingFormat(TestInputLineEnding format)
    {
        var profile = new RegexPageStructure { InputText = "one\rtwo", TestInputLineEnding = format };
        var restored = JsonConvert.DeserializeObject<RegexPageStructure>(JsonConvert.SerializeObject(profile));
        Assert.Equal(format, restored!.TestInputLineEnding);
        Assert.Equal(profile.InputText, restored.InputText);
        Assert.Null(JsonConvert.DeserializeObject<RegexPageStructure>("{\"InputText\":\"one\\rtwo\"}")!.TestInputLineEnding);
    }

    [Theory]
    [InlineData("one\r\ntwo", TestInputLineEnding.CrLf)]
    [InlineData("one\ntwo", TestInputLineEnding.Lf)]
    [InlineData("one\rtwo", TestInputLineEnding.Cr)]
    public async Task McpSamplesKeepExactInputAndHaveAnExplicitEditorFormat(string input, TestInputLineEnding format)
    {
        var repository = new InMemoryRegexConfigurationRepository();
        var service = new DeleteNewlineMcpToolService(repository, new InMemoryMcpSettingsRepository(), new InMemoryMcpOcrConfigurationRepository());
        using JsonDocument arguments = JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new { name = "test", inputText = input, chain = Array.Empty<object>() }));
        Assert.False((await service.ExecuteAsync("upsert_regex_profile", arguments.RootElement, default)).IsError);
        Assert.Equal(input, repository.RegexConfigs[0].InputText);
        Assert.Equal(format, repository.RegexConfigs[0].TestInputLineEnding);

        using JsonDocument edit = JsonDocument.Parse("""{"index":0,"comment":"changed"}""");
        Assert.False((await service.ExecuteAsync("upsert_regex_profile", edit.RootElement, default)).IsError);
        Assert.Equal(format, repository.RegexConfigs[0].TestInputLineEnding);
    }
}
