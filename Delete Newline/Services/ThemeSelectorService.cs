using Microsoft.UI.Xaml;

using Delete_Newline.Contracts.Services;
using Delete_Newline.Helpers;

namespace Delete_Newline.Services;

public sealed class ThemeSelectorService : IThemeSelectorService
{
    private const string SettingsKey = "AppBackgroundRequestedTheme";

    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    private readonly SettingsService _localSettingsService;

    public ThemeSelectorService(SettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public void Initialize()
    {
        Theme = LoadThemeFromSettings();
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        SetRequestedTheme();
        await SaveThemeInSettingsAsync(Theme);
    }

    public void SetRequestedTheme()
    {
        if (App.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
        }
    }

    private ElementTheme LoadThemeFromSettings()
    {
        var themeName = _localSettingsService.ReadSetting<string>(SettingsKey);

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        await _localSettingsService.SaveSettingAsync(SettingsKey, theme.ToString());
    }
}