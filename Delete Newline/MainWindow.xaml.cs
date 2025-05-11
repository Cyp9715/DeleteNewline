using Delete_Newline.Services;
using Delete_Newline.ViewModels;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;

namespace Delete_Newline;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;

    public static IntPtr hwnd;
    private WindowMessageMonitor _messageMonitor;

    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const uint WM_TRAYICON = 0x8000;
    private const int WM_CLOSE = 0x0010;
    private const int WM_COMMAND = 0x0111;

    public MainWindow()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _settings = new UISettings();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged;

        hwnd = WindowNative.GetWindowHandle(this);
        
        // Set the window title
        this.Title = "Delete Newline";

        _messageMonitor = new WindowMessageMonitor(this);
        _messageMonitor.WindowMessageReceived += MessageMonitor_WindowMessageReceived!;
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Helpers.TitleBarHelper.ApplySystemThemeToCaptionButtons();
        });
    }

    public static void StartOnTray()
    {
        if (App.GetService<SettingsService>().ReadSetting<bool>(SettingsViewModel.DefaultStartOnTray) is false)
        {
            App.MainWindow.Show();
            App.MainWindow.Activate();
        }
    }

    /* 
     * TrayIcon Sector.
     */
    [DllImport("user32.dll")]
    private static extern int GetMenuItemID(IntPtr hMenu, int nPos);

    private async void MessageMonitor_WindowMessageReceived(object sender, WindowMessageEventArgs e)
    {
        var trayIconService = App.GetService<TrayIconService>();

        switch (e.Message.MessageId)
        {
            case WM_TRAYICON:
                switch (e.Message.LParam.ToInt32())
                {
                    case WM_LBUTTONDBLCLK:
                        this.Show();
                        this.Activate();
                        break;
                    case WM_RBUTTONDOWN:
                        trayIconService.ShowContextMenu();
                        break;
                }
                break;

            case WM_CLOSE:
                this.Hide();
                e.Handled = true;
                break;

            case WM_COMMAND:
                int commandId = (int)e.Message.WParam & 0xFFFF;
                switch (commandId)
                {
                    case TrayIconService.ID_EXIT:
                        trayIconService.RemoveTrayIcon();
                        this.Close();
                        break;

                    case TrayIconService.ID_NOTIFICATION:
                        var notificationService = App.GetService<NotificationService>();
                        await notificationService.SetEnableNotificationAsync(!notificationService.GetEnableNotification());
                        break;
                }
                break;
        }
    }
}