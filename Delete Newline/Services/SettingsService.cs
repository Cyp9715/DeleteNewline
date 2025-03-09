using Delete_Newline.Contracts.Services;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Core.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Delete_Newline.Services;

public class SettingsService : ISettingsService
{
    private readonly IFileService _fileService;
    private IDictionary<string, JToken> _settings;

    private readonly string _applicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private const string _settingsFileName = "Settings.json";

    public SettingsService(IFileService fileService)
    {
        _fileService = fileService;
        _settings = new Dictionary<string, JToken>();
    }

    public async Task InitializeAsync()
    {
        if (File.Exists(Path.Combine(_applicationDataDirectory, _settingsFileName)) is false)
            CreateSettingsFile(_applicationDataDirectory, _settingsFileName);

        string? jsonContent = await _fileService.ReadAsStringAsync(_applicationDataDirectory, _settingsFileName);
        _settings = string.IsNullOrWhiteSpace(jsonContent) ? new Dictionary<string, JToken>() : JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonContent) ?? new Dictionary<string, JToken>();
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

        await InitializeAsync();

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new PublicPropertiesOnlyContractResolver(),
            Formatting = Formatting.Indented
        };

        _settings[key] = JToken.FromObject(value, JsonSerializer.Create(settings));

        var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        await _fileService.SaveAsync(_applicationDataDirectory, _settingsFileName, json);
    }

    public async Task ExportSettingsAsync(string exportFilePath)
    {
        // Read the contents of the currently saved settings file.
        string jsonContent = await _fileService.ReadAsStringAsync(_applicationDataDirectory, _settingsFileName);

        // Separate the directory and file name from exportFilePath.
        var exportDirectory = Path.GetDirectoryName(exportFilePath)!;
        var exportFileName = Path.GetFileName(exportFilePath);

        // Save the read JSON to the user-specified path.
        await _fileService.SaveAsync(exportDirectory, exportFileName, jsonContent);
    }

    public async Task ImportSettingsAsync(string importFilePath)
    {
        // Separate the directory and file name from importFilePath.
        var importDirectory = Path.GetDirectoryName(importFilePath)!;
        var importFileName = Path.GetFileName(importFilePath);

        // Read the JSON content from the specified file.
        string jsonContent = await _fileService.ReadAsStringAsync(importDirectory, importFileName);

        // Overwrite the current app settings file with the read content.
        await _fileService.SaveAsync(_applicationDataDirectory, _settingsFileName, jsonContent);

        // Update the in-memory _settings object as well.
        _settings = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonContent) ?? new Dictionary<string, JToken>();
    }
}
