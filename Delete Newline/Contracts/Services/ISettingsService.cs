namespace Delete_Newline.Contracts.Services;

public interface ISettingsService
{
    Task InitializeAsync();

    T? ReadSetting<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);

    Task ImportSettingsAsync(string importFilePath);

    Task ExportSettingsAsync(string exportFilePath);
}
