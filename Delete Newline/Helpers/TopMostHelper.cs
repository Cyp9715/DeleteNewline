using Delete_Newline.Contracts.Services;
using Delete_Newline.Services;
using Microsoft.UI.Xaml;

namespace Delete_Newline.Helpers
{
    public static class TopMostHelper
    {
        private static ILocalSettingsService? _localSettingsService;

        private const string DefaultTopMostKey = "TopMost";
        public static bool EnableTopMost { get; private set; } = false;

        public async static Task Initialize(Window window, string topMostKey=DefaultTopMostKey)
        {
            _localSettingsService = App.GetService<ILocalSettingsService>();
            bool? storedSetting = await _localSettingsService.ReadSettingAsync<bool?>(topMostKey);

            // default setting
            if (storedSetting.HasValue is false)
            {
                EnableTopMost = false;
                await _localSettingsService.SaveSettingAsync(topMostKey, false);
            }
            else
            {
                EnableTopMost = storedSetting.Value;
                await SetWindowTopMost(App.MainWindow, EnableTopMost);
            }
        }

        public async static Task SetWindowTopMost(Window window, bool topMost, string topMostKey=DefaultTopMostKey)
        {
            if (window == null) return;

            var appWindow = GetAppWindow(window);
            if (appWindow != null)
            {
                var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = topMost;
                    await _localSettingsService!.SaveSettingAsync(topMostKey, topMost);
                }
            }
        }

        private static Microsoft.UI.Windowing.AppWindow GetAppWindow(Window window)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        }
    }
}
