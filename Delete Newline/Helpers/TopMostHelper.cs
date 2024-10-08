using Microsoft.UI.Xaml;

namespace Delete_Newline.Helpers
{
    public static class TopMostHelper
    {
        public static void SetWindowTopMost(Window window, bool topMost)
        {
            if (window == null) return;

            var appWindow = GetAppWindow(window);
            if (appWindow != null)
            {
                var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsAlwaysOnTop = topMost;
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
