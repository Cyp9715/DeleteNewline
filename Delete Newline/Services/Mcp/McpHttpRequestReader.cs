using System.Globalization;
using System.Net;
using System.Text;

namespace Delete_Newline.Services.Mcp;

internal sealed record McpHttpRequest(string Method, string Path, IReadOnlyDictionary<string, string> Headers, string Body);

internal sealed class McpHttpException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

// A bounded, buffered HTTP/1.1 reader. The server closes each connection after one response.
internal sealed class McpHttpRequestReader(Stream stream, CancellationToken cancellationToken)
{
    private const int MaxHeaderBytes = 16_384;
    private const int MaxBodyBytes = 1_048_576;
    private readonly byte[] _buffer = new byte[8192];
    private int _position;
    private int _length;
    private int _headerBytes;

    public async Task<McpHttpRequest?> ReadAsync()
    {
        string? firstLine = await ReadLineAsync();
        if (firstLine == null)
        {
            return null;
        }
        string[] requestLine = firstLine.Split(' ');
        if (requestLine.Length != 3 || requestLine[2] is not ("HTTP/1.1" or "HTTP/1.0"))
        {
            throw BadRequest("Invalid HTTP request line.");
        }

        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            string line = await ReadLineAsync() ?? throw BadRequest("Incomplete HTTP headers.");
            if (line.Length == 0) break;
            int colon = line.IndexOf(':');
            if (colon <= 0 || line[..colon].Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            {
                throw BadRequest("Invalid HTTP header.");
            }
            string name = line[..colon];
            if (!headers.TryAdd(name, line[(colon + 1)..].Trim()))
            {
                throw BadRequest("Duplicate HTTP header.");
            }
        }

        byte[] body;
        if (headers.TryGetValue("Transfer-Encoding", out string? transferEncoding))
        {
            if (headers.ContainsKey("Content-Length") || !transferEncoding.Equals("chunked", StringComparison.OrdinalIgnoreCase))
            {
                throw BadRequest("Unsupported HTTP body framing.");
            }
            using MemoryStream chunks = new();
            while (true)
            {
                string sizeLine = await ReadLineAsync() ?? throw BadRequest("Incomplete HTTP chunk.");
                string sizeText = sizeLine.Split(';', 2)[0];
                if (!int.TryParse(sizeText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int size) || size < 0)
                {
                    throw BadRequest("Invalid HTTP chunk length.");
                }
                if (size == 0)
                {
                    while ((await ReadLineAsync() ?? throw BadRequest("Incomplete HTTP trailers.")).Length > 0) { }
                    break;
                }
                if (size > MaxBodyBytes - chunks.Length) throw TooLarge();
                byte[] chunk = new byte[size];
                await ReadExactlyAsync(chunk);
                chunks.Write(chunk);
                if (await ReadByteAsync() != '\r' || await ReadByteAsync() != '\n')
                {
                    throw BadRequest("Invalid HTTP chunk delimiter.");
                }
            }
            body = chunks.ToArray();
        }
        else
        {
            int contentLength = 0;
            if (headers.TryGetValue("Content-Length", out string? lengthText) &&
                (!int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out contentLength) || contentLength < 0))
            {
                throw BadRequest("Invalid Content-Length.");
            }
            if (contentLength > MaxBodyBytes) throw TooLarge();
            body = new byte[contentLength];
            await ReadExactlyAsync(body);
        }

        try
        {
            return new McpHttpRequest(requestLine[0], requestLine[1], headers, new UTF8Encoding(false, true).GetString(body));
        }
        catch (DecoderFallbackException)
        {
            throw BadRequest("MCP JSON must be valid UTF-8.");
        }
    }

    private async Task<string?> ReadLineAsync()
    {
        using MemoryStream line = new();
        while (true)
        {
            int value = await ReadByteAsync();
            if (value < 0)
            {
                if (line.Length == 0) return null;
                throw BadRequest("Incomplete HTTP line.");
            }
            if (++_headerBytes > MaxHeaderBytes)
            {
                throw new McpHttpException(HttpStatusCode.RequestHeaderFieldsTooLarge, "HTTP headers are too large.");
            }
            if (value == '\r')
            {
                if (await ReadByteAsync() != '\n') throw BadRequest("Invalid HTTP line ending.");
                return Encoding.ASCII.GetString(line.ToArray());
            }
            if (value < 32 && value != '\t' || value > 126) throw BadRequest("Invalid HTTP header character.");
            line.WriteByte((byte)value);
        }
    }

    private async Task ReadExactlyAsync(byte[] target)
    {
        for (int offset = 0; offset < target.Length;)
        {
            if (_position == _length && !await FillAsync()) throw BadRequest("Incomplete HTTP body.");
            int count = Math.Min(_length - _position, target.Length - offset);
            _buffer.AsSpan(_position, count).CopyTo(target.AsSpan(offset));
            _position += count;
            offset += count;
        }
    }

    private async Task<int> ReadByteAsync()
    {
        if (_position == _length && !await FillAsync()) return -1;
        return _buffer[_position++];
    }

    private async Task<bool> FillAsync()
    {
        _position = 0;
        _length = await stream.ReadAsync(_buffer, cancellationToken);
        return _length > 0;
    }

    private static McpHttpException BadRequest(string message) => new(HttpStatusCode.BadRequest, message);
    private static McpHttpException TooLarge() => new(HttpStatusCode.RequestEntityTooLarge, "MCP request body exceeds 1 MiB.");
}
