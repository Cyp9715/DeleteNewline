namespace Delete_Newline.Services.Mcp;

public interface IMcpSettingsRepository
{
    bool GetBoolean(string key);

    object? GetValue(string key);

    IReadOnlyDictionary<string, object?> GetAllSettings();

    Task SetBooleanAsync(string key, bool value, CancellationToken cancellationToken);

    Task SetMcpEnabledAsync(bool value, bool deferStop, CancellationToken cancellationToken);

    Task SetValueAsync(string key, object value, CancellationToken cancellationToken);
}
