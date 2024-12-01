using Delete_Newline.Contracts.Services;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Core.Helpers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Delete_Newline.Services
{
    public class LocalSettingsService : ILocalSettingsService
    {
        private readonly IFileService _fileService;
        private IDictionary<string, JToken> _settings;

        private readonly string _applicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private const string _settingsFileName = "Settings.json";
        private bool _isInitialized = false;

        public LocalSettingsService(IFileService fileService)
        {
            _fileService = fileService;
            _settings = new Dictionary<string, JToken>();
        }

        private async Task InitializeAsync()
        {
            if (_isInitialized)
                return;
            
            if (File.Exists(Path.Combine(_applicationDataDirectory, _settingsFileName)) is false)
                CreateSettingsFile(_applicationDataDirectory, _settingsFileName);

            string? jsonContent = await _fileService.ReadAsStringAsync(_applicationDataDirectory, _settingsFileName);
            _settings = string.IsNullOrWhiteSpace(jsonContent) ? new Dictionary<string, JToken>() : JsonConvert.DeserializeObject<Dictionary<string, JToken>>(jsonContent) ?? new Dictionary<string, JToken>();
            _isInitialized = true;
            
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

        public async Task<T?> ReadSettingAsync<T>(string key)
        {
            await InitializeAsync();

            if (_settings.TryGetValue(key, out JToken? token))
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new PublicPropertiesOnlyContractResolver()
                };
                return token.ToObject<T>(JsonSerializer.Create(settings));
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
    }
}
