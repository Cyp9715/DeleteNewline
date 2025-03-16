using Delete_Newline.Contracts.Services;
using Microsoft.UI.Xaml;

namespace Delete_Newline.Helpers;

public static class TopMostHelper
{
    private static ISettingsService? _localSettingsService;
    private const string DefaultTopMostKey = "TopMost";
    public static bool EnableTopMost { get; private set; } = false;

    public static void Initialize(Window window)
    {
        _localSettingsService = App.GetService<ISettingsService>();
        bool? storedSetting = _localSettingsService.ReadSetting<bool?>(DefaultTopMostKey);

        if (storedSetting.HasValue)
        {
            EnableTopMost = storedSetting.Value;
            SetWindowTopMost(App.MainWindow, EnableTopMost);
        }
    }

    public static void SetWindowTopMost(Window window, bool topMost)
    {
        if (window == null) return;

        var appWindow = GetAppWindow(window);
        if (appWindow != null)
        {
            var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
            if (presenter != null)
            {
                EnableTopMost = topMost;
                presenter.IsAlwaysOnTop = topMost;
            }
        }
    }

    public async static Task SaveTopMostSettingAsync()
    {
        if (_localSettingsService == null) return;
        await _localSettingsService.SaveSettingAsync(DefaultTopMostKey, EnableTopMost);
    }

    private static Microsoft.UI.Windowing.AppWindow GetAppWindow(Window window)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }
}
