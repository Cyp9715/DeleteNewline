using System.Text.Json;

namespace Delete_Newline.Services.Mcp;

public static class McpPortPolicy
{
    public const string EnabledSettingsKey = "McpEnabled";
    public const string PortSettingsKey = "McpPort";
    public const int DefaultPort = 39333;
    public const int MinPort = 1;
    public const int MaxPort = 65535;

    public static bool IsEnabledKey(string key)
    {
        return key.Equals(EnabledSettingsKey, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPortKey(string key)
    {
        return key.Equals(PortSettingsKey, StringComparison.OrdinalIgnoreCase)
            || key.Equals("Port", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsValidPort(int port)
    {
        return port is >= MinPort and <= MaxPort;
    }

    public static void ValidatePort(int port)
    {
        if (!IsValidPort(port))
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, $"{PortSettingsKey} must be between {MinPort} and {MaxPort}.");
        }
    }

    public static int ParseJsonPortValue(JsonElement valueElement)
    {
        if (valueElement.ValueKind != JsonValueKind.Number || !valueElement.TryGetInt32(out int port))
        {
            throw new ArgumentException($"{PortSettingsKey} requires an integer number between {MinPort} and {MaxPort}.");
        }

        ValidatePort(port);
        return port;
    }

    public static int RequirePortSettingValue(string key, object value)
    {
        int port = value switch
        {
            int intValue => intValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            _ => throw new ArgumentException($"Setting '{key}' requires an integer value between {MinPort} and {MaxPort}.")
        };

        ValidatePort(port);
        return port;
    }

    public static int? TryReadPort(object? value)
    {
        return value switch
        {
            int intValue when IsValidPort(intValue) => intValue,
            long longValue when longValue is >= MinPort and <= MaxPort => (int)longValue,
            _ => null
        };
    }

    public static void EnsureCanChangePort(bool isMcpEnabled, int? currentPort, int requestedPort)
    {
        if (isMcpEnabled && (!currentPort.HasValue || currentPort.Value != requestedPort))
        {
            throw new InvalidOperationException($"{PortSettingsKey} can only be changed while MCP is disabled.");
        }
    }
}
