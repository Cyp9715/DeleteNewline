using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Delete_Newline.Services.Mcp;

public sealed class McpAccessService
{
    public const string McpEnabledSettingsKey = McpPortPolicy.EnabledSettingsKey;
    public const string McpPortSettingsKey = McpPortPolicy.PortSettingsKey;
    public const int DefaultPort = McpPortPolicy.DefaultPort;

    private readonly SettingsFileService _settingsFileService;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    private bool _enableMcp;
    private int _mcpPort = DefaultPort;
    private bool _initialized;

    public McpAccessService(SettingsFileService settingsFileService, IServiceProvider serviceProvider)
    {
        _settingsFileService = settingsFileService;
        _serviceProvider = serviceProvider;
    }

    public string EndpointUrl => $"http://127.0.0.1:{_mcpPort}/mcp";

    public event EventHandler<bool>? EnableMcpChanged;

    public event EventHandler<int>? McpPortChanged;

    public async Task InitializeAsync()
    {
        int? storedPort = _settingsFileService.ReadSetting<int?>(McpPortSettingsKey);
        if (storedPort.HasValue && McpPortPolicy.IsValidPort(storedPort.Value))
        {
            _mcpPort = storedPort.Value;
        }
        else
        {
            _mcpPort = DefaultPort;
            await _settingsFileService.SaveSettingAsync(McpPortSettingsKey, _mcpPort);
        }

        bool? storedSetting = _settingsFileService.ReadSetting<bool?>(McpEnabledSettingsKey);
        if (!storedSetting.HasValue)
        {
            _enableMcp = false;
            await _settingsFileService.SaveSettingAsync(McpEnabledSettingsKey, false);
        }
        else
        {
            _enableMcp = storedSetting.Value;
        }

        _initialized = true;

        if (_enableMcp)
        {
            try
            {
                await StartServerAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start MCP server during initialization: {ex}");
                _enableMcp = false;
                await _settingsFileService.SaveSettingAsync(McpEnabledSettingsKey, false);
                EnableMcpChanged?.Invoke(this, false);
            }
        }
    }

    public bool GetEnableMcp() => _enableMcp;

    public int GetPort() => _mcpPort;

    public async Task SetEnableMcpAsync(bool enable, bool deferStop = false)
    {
        await _stateLock.WaitAsync();
        try
        {
            _enableMcp = enable;
            await _settingsFileService.SaveSettingAsync(McpEnabledSettingsKey, enable);

            if (enable)
            {
                await StartServerAsync();
            }
            else if (deferStop)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(250);
                    await _stateLock.WaitAsync();
                    try
                    {
                        if (!_enableMcp)
                        {
                            await StopServerAsync();
                        }
                    }
                    finally
                    {
                        _stateLock.Release();
                    }
                });
            }
            else
            {
                await StopServerAsync();
            }

            EnableMcpChanged?.Invoke(this, enable);
        }
        catch (Exception ex) when (enable)
        {
            Debug.WriteLine($"Failed to enable MCP server: {ex}");
            _enableMcp = false;
            await _settingsFileService.SaveSettingAsync(McpEnabledSettingsKey, false);
            EnableMcpChanged?.Invoke(this, false);
            throw;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task SetPortAsync(int port)
    {
        McpPortPolicy.ValidatePort(port);

        await _stateLock.WaitAsync();
        try
        {
            McpPortPolicy.EnsureCanChangePort(_enableMcp, _mcpPort, port);

            if (_mcpPort == port)
            {
                await _settingsFileService.SaveSettingAsync(McpPortSettingsKey, port);
                McpPortChanged?.Invoke(this, port);
                return;
            }

            _mcpPort = port;
            await _settingsFileService.SaveSettingAsync(McpPortSettingsKey, port);
            McpPortChanged?.Invoke(this, port);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task StopServerAsync()
    {
        LocalMcpHttpServerService server = _serviceProvider.GetRequiredService<LocalMcpHttpServerService>();
        await server.StopAsync();
    }

    private async Task StartServerAsync()
    {
        if (!_initialized)
        {
            return;
        }

        LocalMcpHttpServerService server = _serviceProvider.GetRequiredService<LocalMcpHttpServerService>();
        await server.StartAsync(_mcpPort);
    }
}
