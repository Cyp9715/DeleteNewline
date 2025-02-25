using Microsoft.UI.Xaml;

namespace Delete_Newline.Contracts.Services;

public interface IThemeSelectorService
{
    ElementTheme Theme
    {
        get;
    }

    void Initialize();

    Task SetThemeAsync(ElementTheme theme);

    void SetRequestedTheme();
}
