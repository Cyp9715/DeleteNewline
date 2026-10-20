using System.Net;
using System.Text;
using System.Text.Json;
using Delete_Newline.Contracts.Structures;
using Delete_Newline.Services.Mcp;

namespace Delete_Newline.Tests;

public sealed class McpRegressionTests
{
    [Theory]
    [InlineData("-1")]
    [InlineData("9")]
    [InlineData("1.5")]
    [InlineData("\"0\"")]
    [InlineData("null")]
    public async Task InvalidExplicitProfileIndexDoesNotFallBackToLegacyAlias(string index)
    {
        var repository = new InMemoryRegexConfigurationRepository(new RegexPageStructure
        {
            RegexChain = new RegexChainStructure { ChainItems = [new ChainItem { RegexExpression = "a", Replace = "b" }] }
        });
        var service = new DeleteNewlineMcpToolService(repository, new InMemoryMcpSettingsRepository(), new InMemoryMcpOcrConfigurationRepository());
        using var arguments = JsonDocument.Parse($$"""{"input":"a","profileIndex":{{index}},"index":0}""");
        var result = await service.ExecuteAsync("test_regex_chain", arguments.RootElement, CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Null(result.StructuredContent);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpReaderAcceptsMaximumBodyAcrossFragmentedReads(bool chunked)
    {
        byte[] body = Encoding.UTF8.GetBytes("한글" + new string('x', 1_048_576 - 6));
        using var stream = new FragmentedStream(EncodeRequest(body, chunked), 1021);
        var request = await new McpHttpRequestReader(stream, CancellationToken.None).ReadAsync();
        Assert.NotNull(request);
        Assert.Equal(Encoding.UTF8.GetString(body), request.Body);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpReaderRejectsInvalidUtf8AfterFragmentedTransfer(bool chunked)
    {
        using var stream = new FragmentedStream(EncodeRequest([0xC3, 0x28], chunked), 1);
        var error = await Assert.ThrowsAsync<McpHttpException>(() => new McpHttpRequestReader(stream, CancellationToken.None).ReadAsync());
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task ChunkedReaderRejectsCumulativeBodyLimit()
    {
        using var stream = new FragmentedStream(EncodeRequest(new byte[1_048_577], true), 4093);
        var error = await Assert.ThrowsAsync<McpHttpException>(() => new McpHttpRequestReader(stream, CancellationToken.None).ReadAsync());
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, error.StatusCode);
    }

    private static byte[] EncodeRequest(byte[] body, bool chunked)
    {
        using var stream = new MemoryStream();
        string framing = chunked ? "Transfer-Encoding: chunked" : $"Content-Length: {body.Length}";
        stream.Write(Encoding.ASCII.GetBytes($"POST /mcp HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/json\r\n{framing}\r\n\r\n"));
        if (!chunked) stream.Write(body);
        else
        {
            for (int offset = 0; offset < body.Length; offset += 65_536)
            {
                int size = Math.Min(65_536, body.Length - offset);
                stream.Write(Encoding.ASCII.GetBytes($"{size:X}\r\n"));
                stream.Write(body.AsSpan(offset, size));
                stream.Write("\r\n"u8);
            }
            stream.Write("0\r\n\r\n"u8);
        }
        return stream.ToArray();
    }

    private sealed class FragmentedStream(byte[] bytes, int fragmentSize) : MemoryStream(bytes, writable: false)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, fragmentSize)], cancellationToken);
    }
}
