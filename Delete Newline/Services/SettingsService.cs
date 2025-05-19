using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Core.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Delete_Newline.Services;

public sealed class SettingsService
{
    private readonly IFileService _fileService;
    private IDictionary<string, JToken> _settings;

    private readonly string _applicationDataDirectory = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
    private const string _settingsFileName = "Settings.json";

    public SettingsService(IFileService fileService)
    {
        _fileService = fileService;
        _settings = new Dictionary<string, JToken>();
    }

    public async Task InitializeAsync()
    {
        if (File.Exists(Path.Combine(_applicationDataDirectory, _settingsFileName)) == false)
            CreateSettingsFile(_applicationDataDirectory, _settingsFileName);

        string? jsonContent = await _fileService.ReadAsStringAsync(_applicationDataDirectory, _settingsFileName);

        try
        {
            _settings = string.IsNullOrWhiteSpace(jsonContent) ? new Dictionary<string, JToken>() : JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonContent) ?? new Dictionary<string, JToken>();
        }
        catch // json is wrong format 
        {
            _settings = new Dictionary<string, JToken>();
        }
    }

    private void CreateSettingsFile(string directory, string fileName)
    {
        string filePath = Path.Combine(directory, fileName);

        // check directory
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Json.
        File.WriteAllText(filePath, "{}");
    }

    public T? ReadSetting<T>(string key)
    {
        if (_settings.TryGetValue(key, out JToken? token))
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new PublicPropertiesOnlyContractResolver()
            };
            return token.ToObject<T?>(JsonSerializer.Create(settings));
        }
        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        //await InitializeAsync();
        Debug.WriteLine($"SaveSettingAsync : {key} |a| {value}");

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new PublicPropertiesOnlyContractResolver(),
            Formatting = Formatting.Indented
        };

        _settings[key] = JToken.FromObject(value, JsonSerializer.Create(settings));

        var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        await _fileService.SaveAsync(_applicationDataDirectory, _settingsFileName, json).ConfigureAwait(false);
    }

    public async Task<bool> ExportSettingsAsync(string exportFilePath)
    {
        try
        {
            string jsonContent = await _fileService.ReadAsStringAsync(_applicationDataDirectory, _settingsFileName).ConfigureAwait(false);
            var exportDirectory = Path.GetDirectoryName(exportFilePath)!;
            var exportFileName = Path.GetFileName(exportFilePath);
            await _fileService.SaveAsync(exportDirectory, exportFileName, jsonContent).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exporting settings: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ImportSettingsAsync(string importFilePath)
    {
        var importDirectory = Path.GetDirectoryName(importFilePath)!;
        var importFileName = Path.GetFileName(importFilePath);

        string jsonContent;
        try
        {
            jsonContent = await _fileService.ReadAsStringAsync(importDirectory, importFileName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading import file: {ex.Message}");
            return false; // Failed to read the file
        }

        var backupSettings = new Dictionary<string, JToken>(_settings);

        try
        {
            var tempSettings = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonContent);
            _settings = tempSettings ?? new Dictionary<string, JToken>();
            await _fileService.SaveAsync(_applicationDataDirectory, _settingsFileName, jsonContent).ConfigureAwait(false);
            return true;
        }
        catch (Newtonsoft.Json.JsonReaderException ex)
        {
            _settings = backupSettings;
            Debug.WriteLine($"Error deserializing settings on import: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _settings = backupSettings;
            Debug.WriteLine($"Generic error during import process: {ex.Message}");
            return false;
        }
    }
}
