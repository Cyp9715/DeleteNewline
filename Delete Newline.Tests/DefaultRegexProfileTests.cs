using Delete_Newline.Contracts.Structures;
using Delete_Newline.Core.Services;
using Delete_Newline.Services;
using Windows.System;

namespace Delete_Newline.Tests;

public sealed class DefaultRegexProfileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"DeleteNewline-defaults-{Guid.NewGuid():N}");

    private async Task<SettingsFileService> OpenSettingsAsync()
    {
        var settings = new SettingsFileService(new FileService(), _directory);
        await settings.InitializeAsync();
        return settings;
    }

    [Fact]
    public async Task FirstUsePersistsOneLocalizedStarterWithAltF1()
    {
        var settings = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(settings, "줄바꿈 정리", "줄바꿈을 공백 한 칸으로 바꿉니다.");

        var reopened = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(reopened, "different name", "different description");
        var profile = Assert.Single(reopened.ReadSetting<List<RegexPageStructure>>(DefaultRegexProfile.CollectionSettingsKey)!);
        Assert.Equal("줄바꿈 정리", profile.HotkeyName);
        Assert.Equal("줄바꿈을 공백 한 칸으로 바꿉니다.", profile.HotkeyComment);
        Assert.Equal(VirtualKeyModifiers.Menu, profile.Hotkey.Modifiers);
        Assert.Equal(VirtualKey.F1, profile.Hotkey.Key);
        var rule = Assert.Single(profile.RegexChain.ChainItems);
        Assert.Equal(@"[\r\n]+", rule.RegexExpression);
        Assert.Equal(" ", rule.Replace);
    }

    [Fact]
    public async Task DeletingTheStarterStaysDeletedAfterRestart()
    {
        var settings = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(settings, "starter", "description");
        var profiles = settings.ReadSetting<List<RegexPageStructure>>(DefaultRegexProfile.CollectionSettingsKey)!;
        profiles.Clear();
        await settings.SaveSettingAsync(DefaultRegexProfile.CollectionSettingsKey, profiles);

        var reopened = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(reopened, "must not return", "description");
        Assert.Empty(reopened.ReadSetting<List<RegexPageStructure>>(DefaultRegexProfile.CollectionSettingsKey)!);
        Assert.True(reopened.GetAllSettingsSnapshot().ContainsKey(DefaultRegexProfile.CollectionSettingsKey));
    }

    [Fact]
    public async Task EditedStarterIsNotResetAfterRestart()
    {
        var settings = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(settings, "starter", "description");
        var profiles = settings.ReadSetting<List<RegexPageStructure>>(DefaultRegexProfile.CollectionSettingsKey)!;
        profiles[0].HotkeyName = "My cleanup";
        profiles[0].RegexChain.ChainItems[0].RegexExpression = "custom";
        profiles[0].RegexChain.ChainItems[0].Replace = "edited";
        profiles[0].Hotkey.Key = VirtualKey.F2;
        await settings.SaveSettingAsync(DefaultRegexProfile.CollectionSettingsKey, profiles);

        var reopened = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(reopened, "starter", "description");
        var profile = Assert.Single(reopened.ReadSetting<List<RegexPageStructure>>(DefaultRegexProfile.CollectionSettingsKey)!);
        Assert.Equal("My cleanup", profile.HotkeyName);
        Assert.Equal("custom", Assert.Single(profile.RegexChain.ChainItems).RegexExpression);
        Assert.Equal("edited", profile.RegexChain.ChainItems[0].Replace);
        Assert.Equal(VirtualKey.F2, profile.Hotkey.Key);
    }

    [Fact]
    public async Task ExistingProfilesAreNotChangedOrGivenAnExtraStarter()
    {
        var settings = await OpenSettingsAsync();
        RegexPageStructure[] existing = [new() { HotkeyName = "first" }, new() { HotkeyName = "second" }];
        existing[0].RegexChain.ChainItems.Add(new() { RegexExpression = @"\r\n", Replace = " " });
        await settings.SaveSettingAsync(DefaultRegexProfile.CollectionSettingsKey, existing);
        string before = await File.ReadAllTextAsync(Path.Combine(_directory, "Settings.json"));

        await DefaultRegexProfile.EnsureCreatedAsync(settings, "starter", "description");
        Assert.Equal(before, await File.ReadAllTextAsync(Path.Combine(_directory, "Settings.json")));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task ExplicitlySavedEmptyOrNullCollectionsAreNotSeeded(string collection)
    {
        Directory.CreateDirectory(_directory);
        string json = "{\"RegexCollection\":" + collection + "}";
        await File.WriteAllTextAsync(Path.Combine(_directory, "Settings.json"), json);
        var settings = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(settings, "starter", "description");
        Assert.Equal(json, await File.ReadAllTextAsync(Path.Combine(_directory, "Settings.json")));
    }

    [Theory]
    [InlineData("one\ntwo", "one two")]
    [InlineData("one\rtwo", "one two")]
    [InlineData("one\r\ntwo", "one two")]
    [InlineData("one\r\ntwo\n\nthree\r\r\nfour", "one two three four")]
    public async Task StarterCleansAllLineEndingsWithoutDoubleSpaces(string input, string expected)
    {
        var settings = await OpenSettingsAsync();
        await DefaultRegexProfile.EnsureCreatedAsync(settings, "starter", "description");
        var profile = Assert.Single(settings.ReadSetting<List<RegexPageStructure>>(DefaultRegexProfile.CollectionSettingsKey)!);
        Assert.Equal(expected, RegexTextProcessor.Transform(input, profile.RegexChain.ChainItems));
    }

    public void Dispose()
    {
        string parent = Path.GetDirectoryName(Path.GetFullPath(_directory))!;
        if (!string.Equals(parent, Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unexpected test cleanup directory.");
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
