namespace Delete_Newline.Services.Mcp;

public sealed class InMemoryMcpSettingsRepository : IMcpSettingsRepository
{
    private readonly Dictionary<string, object?> _settings = new(StringComparer.OrdinalIgnoreCase);

    public int SaveCount { get; private set; }

    public bool GetBoolean(string key)
    {
        return _settings.TryGetValue(key, out object? value) && value is bool boolValue && boolValue;
    }

    public object? GetValue(string key)
    {
        return _settings.TryGetValue(key, out object? value) ? value : null;
    }

    public T? GetValue<T>(string key)
    {
        object? value = GetValue(key);
        if (value is T typedValue)
        {
            return typedValue;
        }

        if (value != null)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

        return default;
    }

    public IReadOnlyDictionary<string, object?> GetAllSettings()
    {
        return _settings.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    public Task SetBooleanAsync(string key, bool value, CancellationToken cancellationToken)
    {
        return key.Equals(McpPortPolicy.EnabledSettingsKey, StringComparison.OrdinalIgnoreCase)
            ? SetMcpEnabledAsync(value, deferStop: false, cancellationToken)
            : SetValueAsync(key, value, cancellationToken);
    }

    public Task SetMcpEnabledAsync(bool value, bool deferStop, CancellationToken cancellationToken)
    {
        return SetValueAsync(McpPortPolicy.EnabledSettingsKey, value, cancellationToken);
    }

    public Task SetValueAsync(string key, object value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _settings[key] = value;
        SaveCount++;
        return Task.CompletedTask;
    }
}
