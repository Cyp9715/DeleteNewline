using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Delete_Newline.Services.Mcp;

public sealed class LocalMcpHttpServerService
{
    private const string ServerInstructions = """
        Delete Newline is a Windows system-wide hotkey tool for instant text transformation. Users select text in any application, press a configured hotkey, and Delete Newline runs the selected text through the profile's ordered regex rules, then places the transformed text on the clipboard ready to paste. A regex profile is a named hotkey configuration with optional notes, sample input text, and a regex chain; rule order matters. Delete Newline also exposes OCR hotkey/language settings and app settings through MCP.

        Encoding note: MCP HTTP JSON is UTF-8 end-to-end. Profile names, comments, sample input, regex patterns, replacements, OCR language tags, and app setting strings can contain Korean and other Unicode text directly. Prefer MCP tools over PowerShell/ANSI file-editing workarounds for settings changes; do not assume a Windows ANSI code page and do not expand readable Unicode into \uXXXX escapes unless the client strictly requires it.

        Hotkey note: hotkeys use Windows.System.VirtualKey names plus modifiers, with shared human-friendly aliases for both UI display and MCP parsing. For keyboard number-row keys, use Number0..Number9 in object form (for example { "modifiers": "Alt", "key": "Number1" }) or use hotkey strings such as Alt+Number1 / Alt+1. Do not send JSON numbers for hotkey keys: raw Windows virtual-key value 1 means LeftButton, not the keyboard 1 key. Digit text 0..9 is accepted and normalized to Number0..Number9 for model convenience. Common human aliases such as Backspace, Return, Del, Spacebar, Page Up/Page Down, Left/Right/Up/Down Arrow, Backtick, Semicolon, Slash, Backslash, Minus, Equals, Comma, Period, LeftBracket/RightBracket, and Quote are accepted; symbol aliases such as `, ;, /, \\, -, =, ,, ., [, ], and ' are also accepted.

        Suggested workflow for MCP clients: 1) call get_app_settings and get_regex_profiles before changing anything; 2) use test_regex_chain to verify complex regular-expression chains against sample text; 3) for one-rule edits prefer insert_regex_chain_item, update_regex_chain_item, or delete_regex_chain_item so existing rules shift automatically instead of being rebuilt from memory; 4) call upsert_regex_profile only when creating a new profile or replacing a full profile; 5) OCR rule: call get_ocr_languages, copy an exact languageTag, then call the item's useTool (set_ocr_settings, install_ocr_language, or none) to use that language; call deleteTool only when it equals delete_ocr_language; never delete the current OCR language; 6) change McpPort only while McpEnabled is false; 7) setting McpEnabled to false immediately disables external MCP access. Use standard MCP tool descriptions, inputSchema descriptions, and tool annotations as the authoritative guide.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly DeleteNewlineMcpToolService _toolService;
    private readonly SemaphoreSlim _lifetimeLock = new(1, 1);
    private readonly SemaphoreSlim _toolLock = new(1, 1);
    private readonly object _clientsLock = new();
    private readonly Dictionary<TcpClient, Task> _clients = [];
    private readonly Func<string, JsonElement, CancellationToken, Task<McpToolResult>> _executeTool;
    private static readonly HashSet<string> SupportedVersions = ["2025-03-26", "2025-06-18", "2025-11-25"];

    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _acceptLoopTask;

    public LocalMcpHttpServerService(DeleteNewlineMcpToolService toolService,
        Func<string, JsonElement, CancellationToken, Task<McpToolResult>>? executeTool = null)
    {
        _toolService = toolService;
        _executeTool = executeTool ?? toolService.ExecuteAsync;
    }

    public bool IsRunning { get; private set; }

    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        await _lifetimeLock.WaitAsync(cancellationToken);
        try
        {
            if (IsRunning)
            {
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                McpPortPolicy.ValidatePort(port);
                _listener = new TcpListener(IPAddress.Loopback, port);
                _listener.Start(backlog: 10);
                IsRunning = true;
                _acceptLoopTask = AcceptLoopAsync(_cancellationTokenSource.Token);
            }
            catch
            {
                _listener?.Stop();
                _listener = null;
                _acceptLoopTask = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                IsRunning = false;
                throw;
            }
        }
        finally
        {
            _lifetimeLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifetimeLock.WaitAsync();
        try
        {
            if (!IsRunning)
            {
                return;
            }

            _cancellationTokenSource?.Cancel();
            _listener?.Stop();

            if (_acceptLoopTask != null)
            {
                try
                {
                    await _acceptLoopTask;
                }
                catch (OperationCanceledException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                catch (SocketException)
                {
                }
            }

            Task[] pending;
            lock (_clientsLock)
            {
                foreach (TcpClient client in _clients.Keys) client.Dispose();
                pending = _clients.Values.ToArray();
            }
            await Task.WhenAll(pending);

            _listener = null;
            _acceptLoopTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            IsRunning = false;
        }
        finally
        {
            _lifetimeLock.Release();
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(_listener != null);

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            lock (_clientsLock)
            {
                if (_clients.Count >= 32)
                {
                    client.Dispose();
                    continue;
                }
                _clients[client] = Task.Run(async () =>
                {
                    try { await ProcessClientAsync(client, cancellationToken); }
                    finally { lock (_clientsLock) _clients.Remove(client); }
                });
            }
        }
    }

    private async Task ProcessClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            Stream? stream = null;
            try
            {
                stream = client.GetStream();
                using CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                readTimeout.CancelAfter(TimeSpan.FromSeconds(10));
                McpHttpRequest? request = await new McpHttpRequestReader(stream, readTimeout.Token).ReadAsync();
                if (request == null)
                {
                    return;
                }

                if (!IsLoopbackHost(request.Headers.GetValueOrDefault("Host"), requireScheme: false) ||
                    request.Headers.TryGetValue("Origin", out string? origin) && !IsLoopbackHost(origin, requireScheme: true))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.Forbidden, "Only loopback hosts and origins are allowed.");
                    return;
                }

                if (request.Headers.TryGetValue("MCP-Protocol-Version", out string? version) && !SupportedVersions.Contains(version))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.BadRequest, "Unsupported MCP protocol version.");
                    return;
                }

                string normalizedPath = request.Path.Split('?')[0].TrimEnd('/');
                if (!string.Equals(normalizedPath, "/mcp", StringComparison.OrdinalIgnoreCase))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.NotFound, "Not Found");
                    return;
                }

                if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.MethodNotAllowed, "Only POST /mcp is supported for MCP JSON-RPC requests.");
                    return;
                }

                if (!request.Headers.TryGetValue("Content-Type", out string? contentType) ||
                    !contentType.Split(';', 2)[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.UnsupportedMediaType, "Use application/json.");
                    return;
                }
                await HandleMcpPostAsync(stream, request.Body, cancellationToken);
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException)
            {
                // Idle, disconnected and stopped clients do not outlive the server.
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MCP HTTP request failed: {ex}");
                try
                {
                    if (stream != null)
                    {
                        HttpStatusCode status = ex is McpHttpException httpError ? httpError.StatusCode : HttpStatusCode.InternalServerError;
                        await WritePlainTextResponseAsync(stream, status, ex is McpHttpException ? ex.Message : "MCP server error.");
                    }
                }
                catch
                {
                    // Ignore secondary write failures while closing the client connection.
                }
            }
        }
    }

    private static bool IsLoopbackHost(string? value, bool requireScheme)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        string address = requireScheme ? value : $"http://{value}";
        return Uri.TryCreate(address, UriKind.Absolute, out Uri? uri)
            && uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0
            && uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || IPAddress.TryParse(uri.Host.Trim('[', ']'), out IPAddress? ip) && IPAddress.IsLoopback(ip));
    }

    private async Task HandleMcpPostAsync(Stream stream, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            await WriteJsonResponseAsync(stream, HttpStatusCode.BadRequest, CreateErrorResponse(null, -32700, "Empty JSON-RPC request body."));
            return;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            await WriteJsonResponseAsync(stream, HttpStatusCode.BadRequest, CreateErrorResponse(null, -32700, "Invalid JSON."));
            return;
        }
        using (document)
        {
            JsonNode? response = await ProcessJsonRpcMessageAsync(document.RootElement, cancellationToken);
            if (response == null)
            {
                await WriteNoContentResponseAsync(stream, HttpStatusCode.Accepted);
            }
            else
            {
                await WriteJsonResponseAsync(stream, HttpStatusCode.OK, response);
            }
        }
    }

    private async Task<JsonNode?> ProcessJsonRpcMessageAsync(JsonElement message, CancellationToken cancellationToken)
    {
        if (message.ValueKind != JsonValueKind.Object)
        {
            return CreateErrorResponse(null, -32600, "JSON-RPC message must be an object.");
        }

        bool hasId = message.TryGetProperty("id", out JsonElement idElement);
        if (!message.TryGetProperty("jsonrpc", out JsonElement jsonrpc) || jsonrpc.ValueKind != JsonValueKind.String ||
            jsonrpc.GetString() != "2.0" || hasId && idElement.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
        {
            return CreateErrorResponse(null, -32600, "Invalid JSON-RPC 2.0 message.");
        }
        string? method = message.TryGetProperty("method", out JsonElement methodElement) && methodElement.ValueKind == JsonValueKind.String
            ? methodElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(method))
        {
            // Responses to server messages are accepted without a response of their own.
            if (hasId && (message.TryGetProperty("result", out _) ^ message.TryGetProperty("error", out _))) return null;
            return CreateErrorResponse(hasId ? idElement : null, -32600, "JSON-RPC request method is required.");
        }

        JsonElement parameters = message.TryGetProperty("params", out JsonElement paramsElement)
            ? paramsElement
            : default;
        if (parameters.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Object))
        {
            return CreateErrorResponse(hasId ? idElement : null, -32602, "MCP params must be an object.");
        }

        if (!hasId)
        {
            // MCP notifications such as notifications/initialized do not require a response.
            return null;
        }

        try
        {
            return method switch
            {
                "initialize" => CreateSuccessResponse(idElement, CreateInitializeResult(parameters)),
                "ping" => CreateSuccessResponse(idElement, new JsonObject()),
                "tools/list" => CreateSuccessResponse(idElement, CreateToolsListResult()),
                "tools/call" => CreateSuccessResponse(idElement, await CreateToolCallResultAsync(parameters, cancellationToken)),
                _ => CreateErrorResponse(idElement, -32601, $"Method not found: {method}")
            };
        }
        catch (ArgumentException ex)
        {
            return CreateErrorResponse(idElement, -32602, ex.Message);
        }
    }

    private static JsonNode CreateInitializeResult(JsonElement parameters)
    {
        string protocolVersion = "2025-11-25";
        if (parameters.ValueKind == JsonValueKind.Object &&
            parameters.TryGetProperty("protocolVersion", out JsonElement requestedVersion) &&
            requestedVersion.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(requestedVersion.GetString()))
        {
            if (SupportedVersions.Contains(requestedVersion.GetString()!)) protocolVersion = requestedVersion.GetString()!;
        }
        else throw new ArgumentException("initialize requires protocolVersion.");

        Version? assemblyVersion = typeof(LocalMcpHttpServerService).Assembly.GetName().Version;

        return new JsonObject
        {
            ["protocolVersion"] = protocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject
                {
                    ["listChanged"] = false
                }
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "delete-newline",
                ["title"] = "Delete Newline",
                ["version"] = assemblyVersion?.ToString() ?? "0.0.0"
            },
            ["instructions"] = ServerInstructions
        };
    }

    private JsonNode CreateToolsListResult()
    {
        JsonArray tools = [];
        foreach (McpToolDescriptor tool in _toolService.ListTools())
        {
            tools.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["title"] = tool.Title,
                ["description"] = tool.Description,
                ["inputSchema"] = JsonNode.Parse(tool.InputSchema.GetRawText()),
                ["annotations"] = new JsonObject
                {
                    ["title"] = tool.Title,
                    ["readOnlyHint"] = tool.ReadOnly,
                    ["destructiveHint"] = tool.Destructive,
                    ["idempotentHint"] = tool.ReadOnly,
                    ["openWorldHint"] = tool.OpenWorld
                }
            });
        }

        return new JsonObject
        {
            ["tools"] = tools
        };
    }

    private async Task<JsonNode> CreateToolCallResultAsync(JsonElement parameters, CancellationToken cancellationToken)
    {
        if (parameters.ValueKind != JsonValueKind.Object ||
            !parameters.TryGetProperty("name", out JsonElement nameElement) ||
            nameElement.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("tools/call requires params.name.");
        }

        string toolName = nameElement.GetString()!;
        if (!_toolService.ListTools().Any(tool => tool.Name == toolName))
        {
            throw new ArgumentException($"Unknown tool: {toolName}");
        }
        JsonElement arguments;
        if (parameters.TryGetProperty("arguments", out JsonElement argumentsElement))
        {
            arguments = argumentsElement;
        }
        else
        {
            using JsonDocument emptyArguments = JsonDocument.Parse("{}");
            arguments = emptyArguments.RootElement.Clone();
        }
        if (arguments.ValueKind != JsonValueKind.Object) throw new ArgumentException("Tool arguments must be an object.");
        await _toolLock.WaitAsync(cancellationToken);
        try
        {
            McpToolResult result = await _executeTool(toolName, arguments, cancellationToken);
            return CreateCallToolResult(result);
        }
        finally { _toolLock.Release(); }
    }

    private static JsonNode CreateCallToolResult(McpToolResult result)
    {
        JsonObject response = new()
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = result.Text
                }
            },
            ["isError"] = result.IsError
        };

        if (result.StructuredContent.HasValue)
        {
            response["structuredContent"] = JsonNode.Parse(result.StructuredContent.Value.GetRawText());
        }

        return response;
    }

    private static JsonObject CreateSuccessResponse(JsonElement idElement, JsonNode result)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneJsonNode(idElement),
            ["result"] = result
        };
    }

    private static JsonObject CreateErrorResponse(JsonElement? idElement, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = idElement.HasValue ? CloneJsonNode(idElement.Value) : null,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }

    private static JsonNode? CloneJsonNode(JsonElement element)
    {
        return JsonNode.Parse(element.GetRawText());
    }

    private static async Task WriteJsonResponseAsync(Stream stream, HttpStatusCode statusCode, JsonNode payload)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        await WriteResponseAsync(stream, statusCode, "application/json; charset=utf-8", body);
    }

    private static Task WritePlainTextResponseAsync(Stream stream, HttpStatusCode statusCode, string text)
    {
        byte[] body = Encoding.UTF8.GetBytes(text);
        return WriteResponseAsync(stream, statusCode, "text/plain; charset=utf-8", body);
    }

    private static Task WriteNoContentResponseAsync(Stream stream, HttpStatusCode statusCode)
    {
        return WriteResponseAsync(stream, statusCode, "text/plain; charset=utf-8", []);
    }

    private static async Task WriteResponseAsync(Stream stream, HttpStatusCode statusCode, string contentType, byte[] body)
    {
        string reasonPhrase = ReasonPhrases.GetValueOrDefault(statusCode, statusCode.ToString());
        string header = $"HTTP/1.1 {(int)statusCode} {reasonPhrase}\r\n" +
                        $"Content-Type: {contentType}\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Cache-Control: no-cache\r\n" +
                        "Connection: close\r\n" +
                        (statusCode == HttpStatusCode.MethodNotAllowed ? "Allow: POST\r\n" : "") +
                        "\r\n";

        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await stream.WriteAsync(headerBytes, timeout.Token);
        if (body.Length > 0)
        {
            await stream.WriteAsync(body, timeout.Token);
        }
    }

    private static readonly Dictionary<HttpStatusCode, string> ReasonPhrases = new()
    {
        [HttpStatusCode.OK] = "OK",
        [HttpStatusCode.Accepted] = "Accepted",
        [HttpStatusCode.NoContent] = "No Content",
        [HttpStatusCode.BadRequest] = "Bad Request",
        [HttpStatusCode.NotFound] = "Not Found",
        [HttpStatusCode.MethodNotAllowed] = "Method Not Allowed",
        [HttpStatusCode.Forbidden] = "Forbidden",
        [HttpStatusCode.RequestEntityTooLarge] = "Payload Too Large",
        [HttpStatusCode.RequestHeaderFieldsTooLarge] = "Request Header Fields Too Large",
        [HttpStatusCode.UnsupportedMediaType] = "Unsupported Media Type",
        [HttpStatusCode.InternalServerError] = "Internal Server Error"
    };

}
