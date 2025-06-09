using Microsoft.UI.Xaml;

namespace Delete_Newline.Services;

public class TopMostService
{
    private static SettingsFileService? _localSettingsService;
    public const string DefaultTopMostKey = "TopMost";
    public bool EnableTopMost { get; private set; } = false;

    public void Initialize()
    {
        _localSettingsService = App.GetService<SettingsFileService>();
        bool? storedSetting = _localSettingsService.ReadSetting<bool?>(DefaultTopMostKey);

        if (storedSetting.HasValue)
        {
            EnableTopMost = storedSetting.Value;
            SetWindowTopMost(App.MainWindow, EnableTopMost);
        }
    }

    public void SetWindowTopMost(Window window, bool topMost)
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

    public async Task SaveTopMostSettingAsync()
    {
        if (_localSettingsService == null) return;
        await _localSettingsService.SaveSettingAsync(DefaultTopMostKey, EnableTopMost);
    }

    private Microsoft.UI.Windowing.AppWindow GetAppWindow(Window window)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }
}
