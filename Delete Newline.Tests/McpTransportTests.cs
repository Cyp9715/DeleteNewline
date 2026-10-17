using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Delete_Newline.Services.Mcp;

namespace Delete_Newline.Tests;

public sealed class McpTransportTests : IAsyncLifetime
{
    private readonly LocalMcpHttpServerService _server = new(new DeleteNewlineMcpToolService(
        new InMemoryRegexConfigurationRepository(), new InMemoryMcpSettingsRepository(), new InMemoryMcpOcrConfigurationRepository()));
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(5) };
    private int _port;
    private string Endpoint => $"http://127.0.0.1:{_port}/mcp";

    public async Task InitializeAsync()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        _port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        await _server.StartAsync(_port);
    }

    public async Task DisposeAsync()
    {
        await _server.StopAsync();
        _client.Dispose();
    }

    [Theory]
    [InlineData("{", -32700)]
    [InlineData("[]", -32600)]
    [InlineData("{}", -32600)]
    [InlineData("{\"jsonrpc\":\"1.0\",\"id\":1,\"method\":\"ping\"}", -32600)]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":{},\"method\":\"ping\"}", -32600)]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\",\"params\":[]}", -32602)]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"missing\"}", -32601)]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"missing\"}}", -32602)]
    public async Task InvalidRequestsReturnJsonRpcErrors(string body, int expectedCode)
    {
        using var response = await _client.PostAsync(Endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, json.RootElement.GetProperty("error").GetProperty("code").GetInt32());
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData("Origin", "https://attacker.example", HttpStatusCode.Forbidden)]
    [InlineData("Origin", "null", HttpStatusCode.Forbidden)]
    [InlineData("Host", "attacker.example", HttpStatusCode.Forbidden)]
    [InlineData("MCP-Protocol-Version", "9999-01-01", HttpStatusCode.BadRequest)]
    public async Task RejectsUntrustedHostsOriginsAndUnsupportedVersions(string header, string value, HttpStatusCode expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"ping"}""", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation(header, value);
        using var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task GetDoesNotAdvertiseAFakeSseSession()
    {
        using var response = await _client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Contains("POST", response.Content.Headers.Allow);
        Assert.False(response.Headers.Contains("Mcp-Session-Id"));
    }

    [Fact]
    public async Task NotificationsReceiveEmptyAcceptedResponses()
    {
        using var response = await _client.PostAsync(Endpoint,
            new StringContent("""{"jsonrpc":"2.0","method":"notifications/initialized"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UnknownInitializationVersionNegotiatesSupportedVersion()
    {
        using var response = await _client.PostAsync(Endpoint, new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"future-version","clientInfo":{"name":"tests","version":"1"},"capabilities":{}}}""",
            Encoding.UTF8, "application/json"));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("2025-11-25", json.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.False(response.Headers.Contains("Mcp-Session-Id"));
    }

    [Theory]
    [InlineData("Content-Length: -1\r\n", "", "400")]
    [InlineData("Content-Length: nope\r\n", "", "400")]
    [InlineData("Content-Length: 1048577\r\n", "", "413")]
    [InlineData("Content-Length: 2\r\nContent-Length: 2\r\n", "{}", "400")]
    [InlineData("Content-Length: 2\r\nTransfer-Encoding: chunked\r\n", "{}", "400")]
    [InlineData("Content-Length: 10\r\n", "{}", "400")]
    public async Task RejectsMalformedHttpFraming(string headers, string body, string status)
    {
        string response = await SendRawAsync($"POST /mcp HTTP/1.1\r\nHost: 127.0.0.1:{_port}\r\nContent-Type: application/json\r\n{headers}\r\n{body}");
        Assert.StartsWith($"HTTP/1.1 {status} ", response);
    }

    [Fact]
    public async Task ChunkedRequestsPreserveUnicodeAcrossChunks()
    {
        const string json = """{"jsonrpc":"2.0","id":"한글","method":"ping"}""";
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        using var stream = new MemoryStream();
        stream.Write(Encoding.ASCII.GetBytes($"POST /mcp HTTP/1.1\r\nHost: localhost:{_port}\r\nContent-Type: application/json\r\nTransfer-Encoding: chunked\r\n\r\n"));
        foreach (byte item in bytes) stream.Write([.. "1\r\n"u8, item, .. "\r\n"u8]);
        stream.Write("0\r\n\r\n"u8);
        string response = await SendRawAsync(stream.ToArray());
        Assert.StartsWith("HTTP/1.1 200 ", response);
        using var result = JsonDocument.Parse(response[(response.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4)..]);
        Assert.Equal("한글", result.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task StopClosesIncompleteConnectionsAndCanRestart()
    {
        using var pending = new TcpClient();
        await pending.ConnectAsync(IPAddress.Loopback, _port);
        await pending.GetStream().WriteAsync("POST /mcp HTTP/1.1\r\n"u8.ToArray());
        await _server.StopAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.False(_server.IsRunning);
        await _server.StartAsync(_port);
        using var response = await _client.PostAsync(Endpoint,
            new StringContent("""{"jsonrpc":"2.0","id":1,"method":"ping"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<string> SendRawAsync(string request) => SendRawAsync(Encoding.UTF8.GetBytes(request));

    [Fact]
    public async Task ConcurrentProfileCreatesAreNotLost()
    {
        await Task.WhenAll(Enumerable.Range(0, 16).Select(async i =>
        {
            string body = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0", id = i, method = "tools/call",
                @params = new { name = "upsert_regex_profile", arguments = new { name = $"Profile {i}", chain = Array.Empty<object>() } }
            });
            using var response = await _client.PostAsync(Endpoint, new StringContent(body, Encoding.UTF8, "application/json"));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());
        }));
        using var result = await _client.PostAsync(Endpoint, new StringContent(
            """{"jsonrpc":"2.0","id":20,"method":"tools/call","params":{"name":"get_regex_profiles","arguments":{}}}""",
            Encoding.UTF8, "application/json"));
        using var profiles = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
        Assert.Equal(16, profiles.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("profiles").GetArrayLength());
    }

    private async Task<string> SendRawAsync(byte[] request)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _port);
        var stream = client.GetStream();
        await stream.WriteAsync(request);
        client.Client.Shutdown(SocketShutdown.Send);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }
}
