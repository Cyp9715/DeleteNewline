using Delete_Newline.Services;
using Microsoft.UI.Dispatching;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using WinUIEx;

namespace Delete_Newline;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;

    public static IntPtr hwnd;
    public readonly WndProcService wndProcService;

    public MainWindow()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _settings = new UISettings();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged;

        hwnd = WindowNative.GetWindowHandle(this);
        wndProcService = new WndProcService(hwnd);
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Helpers.TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }
}
