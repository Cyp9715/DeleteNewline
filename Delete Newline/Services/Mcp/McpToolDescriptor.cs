using System.Text.Json;

namespace Delete_Newline.Services.Mcp;

public sealed class McpToolDescriptor
{
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required JsonElement InputSchema { get; init; }
    public bool ReadOnly { get; init; }
    public bool Destructive { get; init; }
}
