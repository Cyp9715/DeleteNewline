using System.Text.Json;

namespace Delete_Newline.Services.Mcp;

public sealed class McpToolResult
{
    public bool IsError { get; init; }
    public required string Text { get; init; }
    public JsonElement? StructuredContent { get; init; }

    public static McpToolResult Success(string text, JsonElement? structuredContent = null)
    {
        return new McpToolResult
        {
            IsError = false,
            Text = text,
            StructuredContent = structuredContent
        };
    }

    public static McpToolResult Error(string text)
    {
        return new McpToolResult
        {
            IsError = true,
            Text = text
        };
    }
}
