using Delete_Newline.Contracts.Structures;

namespace Delete_Newline.Services.Mcp;

public interface IMcpRegexConfigurationRepository
{
    IReadOnlyList<RegexPageStructure> RegexConfigs { get; }

    Task UpsertAsync(int? index, RegexPageStructure config, CancellationToken cancellationToken);

    Task ReplaceAllAsync(IReadOnlyList<RegexPageStructure> configs, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int index, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}
