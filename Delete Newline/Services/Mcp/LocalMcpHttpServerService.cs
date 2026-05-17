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

        Suggested workflow for small or local models: 1) call get_app_settings and get_regex_profiles before changing anything; 2) use test_regex_chain to verify complex regular-expression chains against sample text; 3) for one-rule edits prefer insert_regex_chain_item, update_regex_chain_item, or delete_regex_chain_item so existing rules shift automatically instead of being rebuilt from memory; 4) call upsert_regex_profile only when creating a new profile or replacing a full profile; 5) change McpPort only while McpEnabled is false; 6) setting McpEnabled to false immediately disables external MCP access. Use standard MCP tool descriptions, inputSchema descriptions, and tool annotations as the authoritative guide.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly DeleteNewlineMcpToolService _toolService;
    private readonly SemaphoreSlim _lifetimeLock = new(1, 1);

    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _acceptLoopTask;

    public LocalMcpHttpServerService(DeleteNewlineMcpToolService toolService)
    {
        _toolService = toolService;
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

            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start(backlog: 10);
            IsRunning = true;
            _acceptLoopTask = AcceptLoopAsync(_cancellationTokenSource.Token);
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

            _ = Task.Run(() => ProcessClientAsync(client), CancellationToken.None);
        }
    }

    private async Task ProcessClientAsync(TcpClient client)
    {
        await using NetworkStream stream = client.GetStream();
        using (client)
        {
            try
            {
                HttpRequest? request = await ReadHttpRequestAsync(stream);
                if (request == null)
                {
                    return;
                }

                string normalizedPath = request.Path.Split('?')[0].TrimEnd('/');
                if (!string.Equals(normalizedPath, "/mcp", StringComparison.OrdinalIgnoreCase))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.NotFound, "Not Found");
                    return;
                }

                if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteNoContentResponseAsync(stream, HttpStatusCode.NoContent);
                    return;
                }

                if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteSseReadyResponseAsync(stream);
                    return;
                }

                if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.MethodNotAllowed, "Only POST /mcp is supported for MCP JSON-RPC requests.");
                    return;
                }

                await HandleMcpPostAsync(stream, request.Body);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MCP HTTP request failed: {ex}");
                try
                {
                    await WritePlainTextResponseAsync(stream, HttpStatusCode.InternalServerError, "MCP server error.");
                }
                catch
                {
                    // Ignore secondary write failures while closing the client connection.
                }
            }
        }
    }

    private async Task HandleMcpPostAsync(Stream stream, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            await WriteJsonResponseAsync(stream, HttpStatusCode.BadRequest, CreateErrorResponse(null, -32700, "Empty JSON-RPC request body."));
            return;
        }

        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            JsonArray responses = [];
            foreach (JsonElement message in root.EnumerateArray())
            {
                JsonNode? response = await ProcessJsonRpcMessageAsync(message);
                if (response != null)
                {
                    responses.Add(response);
                }
            }

            if (responses.Count == 0)
            {
                await WriteNoContentResponseAsync(stream, HttpStatusCode.Accepted);
                return;
            }

            await WriteJsonResponseAsync(stream, HttpStatusCode.OK, responses);
            return;
        }

        JsonNode? singleResponse = await ProcessJsonRpcMessageAsync(root);
        if (singleResponse == null)
        {
            await WriteNoContentResponseAsync(stream, HttpStatusCode.Accepted);
            return;
        }

        await WriteJsonResponseAsync(stream, HttpStatusCode.OK, singleResponse);
    }

    private async Task<JsonNode?> ProcessJsonRpcMessageAsync(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object)
        {
            return CreateErrorResponse(null, -32600, "JSON-RPC message must be an object.");
        }

        bool hasId = message.TryGetProperty("id", out JsonElement idElement);
        string? method = message.TryGetProperty("method", out JsonElement methodElement) && methodElement.ValueKind == JsonValueKind.String
            ? methodElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(method))
        {
            return hasId
                ? CreateErrorResponse(idElement, -32600, "JSON-RPC request method is required.")
                : null;
        }

        JsonElement parameters = message.TryGetProperty("params", out JsonElement paramsElement)
            ? paramsElement
            : default;

        if (!hasId)
        {
            // MCP notifications such as notifications/initialized do not require a response.
            return null;
        }

        return method switch
        {
            "initialize" => CreateSuccessResponse(idElement, CreateInitializeResult(parameters)),
            "ping" => CreateSuccessResponse(idElement, new JsonObject()),
            "tools/list" => CreateSuccessResponse(idElement, CreateToolsListResult()),
            "tools/call" => CreateSuccessResponse(idElement, await CreateToolCallResultAsync(parameters)),
            _ => CreateErrorResponse(idElement, -32601, $"Method not found: {method}")
        };
    }

    private static JsonNode CreateInitializeResult(JsonElement parameters)
    {
        string protocolVersion = "2025-06-18";
        if (parameters.ValueKind == JsonValueKind.Object &&
            parameters.TryGetProperty("protocolVersion", out JsonElement requestedVersion) &&
            requestedVersion.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(requestedVersion.GetString()))
        {
            protocolVersion = requestedVersion.GetString()!;
        }

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
                    ["openWorldHint"] = false
                }
            });
        }

        return new JsonObject
        {
            ["tools"] = tools
        };
    }

    private async Task<JsonNode> CreateToolCallResultAsync(JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object ||
            !parameters.TryGetProperty("name", out JsonElement nameElement) ||
            nameElement.ValueKind != JsonValueKind.String)
        {
            return CreateCallToolResult(McpToolResult.Error("tools/call requires params.name."));
        }

        string toolName = nameElement.GetString()!;
        JsonElement arguments = parameters.TryGetProperty("arguments", out JsonElement argumentsElement)
            ? argumentsElement
            : JsonDocument.Parse("{}").RootElement.Clone();

        McpToolResult result = await _toolService.ExecuteAsync(toolName, arguments, CancellationToken.None);
        return CreateCallToolResult(result);
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

    private static async Task<HttpRequest?> ReadHttpRequestAsync(NetworkStream stream)
    {
        byte[]? headerBytes = await ReadHeaderBytesAsync(stream);
        if (headerBytes == null)
        {
            return null;
        }

        string headerText = Encoding.ASCII.GetString(headerBytes);
        string[] lines = headerText.Split("\r\n", StringSplitOptions.None);
        string[] requestLine = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (requestLine.Length < 2)
        {
            return null;
        }

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            int colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            headers[line[..colonIndex].Trim()] = line[(colonIndex + 1)..].Trim();
        }

        int contentLength = headers.TryGetValue("Content-Length", out string? contentLengthText) && int.TryParse(contentLengthText, out int parsedLength)
            ? parsedLength
            : 0;

        if (contentLength < 0 || contentLength > 1_048_576)
        {
            throw new InvalidOperationException("MCP HTTP request body is too large.");
        }

        byte[] bodyBytes = new byte[contentLength];
        int totalRead = 0;
        while (totalRead < contentLength)
        {
            int read = await stream.ReadAsync(bodyBytes.AsMemory(totalRead, contentLength - totalRead));
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        string body = Encoding.UTF8.GetString(bodyBytes, 0, totalRead);
        return new HttpRequest(requestLine[0], requestLine[1], headers, body);
    }

    private static async Task<byte[]?> ReadHeaderBytesAsync(NetworkStream stream)
    {
        MemoryStream header = new();
        byte[] buffer = new byte[1];
        int matched = 0;
        byte[] delimiter = "\r\n\r\n"u8.ToArray();

        while (header.Length < 16_384)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(0, 1));
            if (read == 0)
            {
                return header.Length == 0 ? null : header.ToArray();
            }

            byte current = buffer[0];
            header.WriteByte(current);

            matched = current == delimiter[matched] ? matched + 1 : current == delimiter[0] ? 1 : 0;
            if (matched == delimiter.Length)
            {
                byte[] bytes = header.ToArray();
                return bytes[..^delimiter.Length];
            }
        }

        throw new InvalidOperationException("MCP HTTP request headers are too large.");
    }

    private static async Task WriteJsonResponseAsync(Stream stream, HttpStatusCode statusCode, JsonNode payload)
    {
        byte[] body = Encoding.UTF8.GetBytes(payload.ToJsonString(JsonOptions));
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

    private static Task WriteSseReadyResponseAsync(Stream stream)
    {
        byte[] body = Encoding.UTF8.GetBytes(": Delete Newline MCP server is enabled. Send JSON-RPC requests with POST /mcp.\n\n");
        return WriteResponseAsync(stream, HttpStatusCode.OK, "text/event-stream; charset=utf-8", body);
    }

    private static async Task WriteResponseAsync(Stream stream, HttpStatusCode statusCode, string contentType, byte[] body)
    {
        string reasonPhrase = ReasonPhrases.GetValueOrDefault(statusCode, statusCode.ToString());
        string header = $"HTTP/1.1 {(int)statusCode} {reasonPhrase}\r\n" +
                        $"Content-Type: {contentType}\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Cache-Control: no-cache\r\n" +
                        "Connection: close\r\n" +
                        "Access-Control-Allow-Origin: http://localhost\r\n" +
                        "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                        "Access-Control-Allow-Headers: content-type, mcp-session-id, mcp-protocol-version\r\n" +
                        "Mcp-Session-Id: delete-newline\r\n" +
                        "\r\n";

        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
        await stream.WriteAsync(headerBytes);
        if (body.Length > 0)
        {
            await stream.WriteAsync(body);
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
        [HttpStatusCode.InternalServerError] = "Internal Server Error"
    };

    private sealed record HttpRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers, string Body);
}
