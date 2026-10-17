using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Core.Services;
using Delete_Newline.Helpers;
using Delete_Newline.Services;
using Delete_Newline.Services.Mcp;
using Newtonsoft.Json.Linq;

namespace Delete_Newline.Tests;

public sealed class ReliabilityTests
{
    [Theory]
    [InlineData("one\r\ntwo\nthree\rfour", "one two three four")]
    [InlineData("one|two\r\nthree", "one|two three")]
    public void RegexHandlesLineEndingsWithoutRemovingPipes(string input, string expected)
    {
        Assert.Equal(expected, RegexTextProcessor.Transform(input, [new() { RegexExpression = @"(?:\r\n|\r|\n)+", Replace = " " }]));
    }

    [Theory]
    [InlineData("[", "")]
    [InlineData("a", @"\q")]
    public void RegexFailureStopsTheChain(string pattern, string replacement)
    {
        ChainItem[] rules = [new() { RegexExpression = pattern, Replace = replacement }, new() { RegexExpression = ".+", Replace = "wrong" }];
        Assert.ThrowsAny<ArgumentException>(() => RegexTextProcessor.Transform("a", rules));
        Assert.Equal("invalid", RegexTextProcessor.ApplyRegexChain("a", rules, "invalid"));
    }

    [Fact]
    public void CatastrophicBacktrackingIsBounded()
    {
        var timer = Stopwatch.StartNew();
        Assert.Throws<RegexMatchTimeoutException>(() => RegexTextProcessor.Transform(new string('a', 50_000) + "!",
            [new() { RegexExpression = "^(a+)+$", Replace = "" }]));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RegexCanProduceAnEmptyResultAndHonorsCancellation()
    {
        ChainItem[] rules = [new() { RegexExpression = ".+", Replace = "" }];
        Assert.Equal("", RegexTextProcessor.Transform("remove all", rules));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => RegexTextProcessor.Transform("a", rules, cancelled.Token));
    }

    [Fact]
    public void PendingCopyIsConsumedOnceAndExpires()
    {
        var clock = new TestTimeProvider();
        var copy = new PendingHotkeyCopy(clock);
        copy.Set(42);
        Assert.True(copy.TryConsume(out int id));
        Assert.Equal(42, id);
        Assert.False(copy.TryConsume(out _));
        copy.Set(43);
        clock.Ticks += TimeSpan.FromSeconds(3).Ticks;
        Assert.False(copy.TryConsume(out _));
        copy.Set(44);
        copy.Clear();
        Assert.False(copy.TryConsume(out _));
    }

    [Theory]
    [InlineData(16000, 9000, 1, 4096, 4096, 2304)]
    [InlineData(1, 10000, 0.01, 4096, 1, 100)]
    [InlineData(200, 100, 2, 4096, 400, 200)]
    public void OcrSizesStayWithinEngineLimits(int width, int height, double scale, int max, int expectedWidth, int expectedHeight)
    {
        Assert.Equal(new Size(expectedWidth, expectedHeight), OcrImageSizing.Fit(width, height, scale, max));
    }

    [Fact]
    public void OcrSelectionUsesDpiAndClampsOutsideDrags()
    {
        Assert.Equal(new Rectangle(150, 300, 300, 150), OcrImageSizing.SelectionToPixels(
            new RectangleF(100, 200, 200, 100), new SizeF(2560, 1440), new Size(3840, 2160)));
        Assert.Equal(new Rectangle(0, 0, 100, 50), OcrImageSizing.SelectionToPixels(
            new RectangleF(-10, -10, 120, 70), new SizeF(100, 50), new Size(100, 50)));
        Assert.Equal(Rectangle.Empty, OcrImageSizing.SelectionToPixels(new RectangleF(0, 0, 1, 1), SizeF.Empty, new Size(100, 50)));
    }

    [Fact]
    public async Task SettingsWritesAreSerializedAndSnapshotsAreIsolated()
    {
        var files = new TestFileService();
        var settings = new SettingsFileService(files, "unused");
        await Task.WhenAll(Enumerable.Range(0, 30).Select(i => settings.SaveSettingAsync($"key{i}", i)));
        JObject saved = JObject.Parse(files.Content);
        Assert.Equal(30, saved.Count);
        Assert.Equal(1, files.MaxConcurrentWrites);
        await settings.SaveSettingAsync("nested", new Dictionary<string, string> { ["value"] = "original" });
        ((JObject)settings.GetAllSettingsSnapshot()["nested"])["value"] = "changed";
        Assert.Equal("original", settings.ReadSetting<JObject>("nested")!["value"]!.Value<string>());
        files.FailWrites = true;
        await Assert.ThrowsAsync<IOException>(() => settings.SaveSettingAsync("nested", new Dictionary<string, string> { ["value"] = "lost" }));
        Assert.Equal("original", settings.ReadSetting<JObject>("nested")!["value"]!.Value<string>());
    }

    [Fact]
    public async Task AtomicFileWritesLeaveNoTemporaryFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"delete-newline-tests-{Guid.NewGuid():N}");
        try
        {
            var files = new FileService();
            await files.SaveAsync(directory, "Settings.json", "first");
            await files.SaveAsync(directory, "Settings.json", "second");
            Assert.Equal("second", await files.ReadAsStringAsync(directory, "Settings.json"));
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData("{\"index\":99,\"name\":\"bad\",\"chain\":[]}")]
    [InlineData("{\"index\":\"0\",\"name\":\"bad\",\"chain\":[]}")]
    [InlineData("{\"name\":\"bad\",\"chain\":[{\"regex\":\"[\",\"replace\":\"\"}]}")]
    public async Task InvalidMcpEditsDoNotPersist(string json)
    {
        var repository = new InMemoryRegexConfigurationRepository();
        var service = CreateToolService(repository);
        using JsonDocument arguments = JsonDocument.Parse(json);
        Assert.True((await service.ExecuteAsync("upsert_regex_profile", arguments.RootElement, default)).IsError);
        Assert.Empty(repository.RegexConfigs);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task McpRegexErrorsAreToolErrorsAndProfileListsAreObjects()
    {
        var service = CreateToolService();
        using JsonDocument invalid = JsonDocument.Parse("""{"input":"a","chain":[{"regex":"[","replace":""}]}""");
        Assert.True((await service.ExecuteAsync("test_regex_chain", invalid.RootElement, default)).IsError);
        using JsonDocument empty = JsonDocument.Parse("{}");
        var profiles = await service.ExecuteAsync("get_regex_profiles", empty.RootElement, default);
        Assert.Equal(JsonValueKind.Object, profiles.StructuredContent!.Value.ValueKind);
        Assert.Equal(JsonValueKind.Array, profiles.StructuredContent.Value.GetProperty("profiles").ValueKind);
    }

    private static DeleteNewlineMcpToolService CreateToolService(InMemoryRegexConfigurationRepository? repository = null) =>
        new(repository ?? new(), new InMemoryMcpSettingsRepository(), new InMemoryMcpOcrConfigurationRepository());

    private sealed class TestTimeProvider : TimeProvider
    {
        public long Ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Ticks;
    }

    private sealed class TestFileService : Delete_Newline.Core.Contracts.Services.IFileService
    {
        public string Content = "{}";
        public bool FailWrites;
        public int MaxConcurrentWrites;
        private int _writes;
        public Task<string> ReadAsStringAsync(string directory, string fileName) => Task.FromResult(Content);
        public async Task SaveAsync(string directory, string fileName, string content)
        {
            MaxConcurrentWrites = Math.Max(MaxConcurrentWrites, Interlocked.Increment(ref _writes));
            try
            {
                await Task.Delay(2);
                if (FailWrites) throw new IOException("Simulated write failure");
                Content = content;
            }
            finally { Interlocked.Decrement(ref _writes); }
        }
        public void Delete(string folderPath, string fileName) => throw new NotSupportedException();
    }
}
