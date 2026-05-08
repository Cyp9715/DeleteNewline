using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services.Mcp;

public sealed class InMemoryRegexConfigurationRepository : IMcpRegexConfigurationRepository
{
    private readonly List<RegexPageStructure> _regexConfigs;

    public InMemoryRegexConfigurationRepository(params RegexPageStructure[] regexConfigs)
    {
        _regexConfigs = regexConfigs.ToList();
    }

    public IReadOnlyList<RegexPageStructure> RegexConfigs => _regexConfigs;

    public int SaveCount { get; private set; }

    public Task UpsertAsync(int? index, RegexPageStructure config, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (index.HasValue && index.Value >= 0 && index.Value < _regexConfigs.Count)
        {
            _regexConfigs[index.Value] = config;
        }
        else
        {
            _regexConfigs.Add(config);
        }

        return SaveAsync(cancellationToken);
    }

    public Task ReplaceAllAsync(IReadOnlyList<RegexPageStructure> configs, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _regexConfigs.Clear();
        _regexConfigs.AddRange(configs);
        return SaveAsync(cancellationToken);
    }

    public Task<bool> DeleteAsync(int index, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (index < 0 || index >= _regexConfigs.Count)
        {
            return Task.FromResult(false);
        }

        _regexConfigs.RemoveAt(index);
        SaveCount++;
        return Task.FromResult(true);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveCount++;
        return Task.CompletedTask;
    }
}
