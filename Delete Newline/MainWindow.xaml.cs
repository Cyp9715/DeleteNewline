using Delete_Newline.Services;
using Delete_Newline.ViewModels;
using Delete_Newline.Views;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;
using Delete_Newline.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Delete_Newline;

public sealed partial class MainWindow : WindowEx
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;

    public static IntPtr hwnd;
    private WindowMessageMonitor _messageMonitor;
    private UIElement? _currentContentForPointerHandler;
    private bool _pointerPressedHandlerAttached = false;

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

        // Subscribe to Activated event
        this.Activated += MainWindow_Activated;

        hwnd = WindowNative.GetWindowHandle(this);
        
        // Set the window title
        this.Title = "Delete Newline";

        _messageMonitor = new WindowMessageMonitor(this);
        _messageMonitor.WindowMessageReceived += MessageMonitor_WindowMessageReceived!;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // Ignore if handler is already attached or if the window is being deactivated
        if (_pointerPressedHandlerAttached || args.WindowActivationState == WindowActivationState.Deactivated)
        {
            return;
        }

        if (this.Content is UIElement rootElement)
        {
            // If a handler was previously attached, remove it (for safety)
            if (_currentContentForPointerHandler != null)
            {
                _currentContentForPointerHandler.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(MainWindow_PointerPressed));
            }
            
            rootElement.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(MainWindow_PointerPressed), true); // handledEventsToo = true
            _currentContentForPointerHandler = rootElement;
            _pointerPressedHandlerAttached = true;
        }
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
        if (App.GetService<SettingsFileService>().ReadSetting<bool>(SettingsViewModel.DefaultStartOnTray) == false)
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

    private void MainWindow_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(sender as UIElement);
        if (pointerPoint == null) return;

        var properties = pointerPoint.Properties;
        var navigationService = App.GetService<INavigationService>();

        if (properties.IsXButton1Pressed)
        {
            if (navigationService.CanGoBack)
            {
                navigationService.GoBack();
                e.Handled = true;
            }
        }
        else if (properties.IsXButton2Pressed)
        {
            var frame = navigationService.Frame;
            if (frame != null && frame.CanGoForward)
            {
                frame.GoForward();
                e.Handled = true;
            }
        }
    }
}