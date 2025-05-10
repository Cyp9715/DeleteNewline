using Microsoft.UI.Xaml;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Delete_Newline.Activation;
using Delete_Newline.Contracts.Services;
using Delete_Newline.Core.Contracts.Services;
using Delete_Newline.Core.Services;
using Delete_Newline.Models;
using Delete_Newline.Services;
using Delete_Newline.ViewModels;
using Delete_Newline.Views;

using WinUIEx;
using System.Threading;
using Windows.ApplicationModel;
using System.Runtime.InteropServices;

namespace Delete_Newline;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "Cyp:DeleteNewlineMutex";
    private const string WindowTitle = "Delete Newline";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;
    private const uint WM_SYSCOMMAND = 0x0112;
    private const int SC_RESTORE = 0xF120;

    public IHost Host
    {
        get;
    }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        // Check if another instance is already running
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (createdNew is false)
        {
            // Find the existing window by title
            IntPtr existingWindow = FindWindow(null, WindowTitle);
            if (existingWindow != IntPtr.Zero)
            {
                // Check if window is minimized
                if (IsIconic(existingWindow))
                {
                    // Restore the window if it's minimized
                    ShowWindow(existingWindow, SW_RESTORE);
                }
                // Check if window is hidden (in tray)
                else if (IsWindowVisible(existingWindow) is false)
                {
                    // Send restore message to the window
                    PostMessage(existingWindow, WM_SYSCOMMAND, (IntPtr)SC_RESTORE, IntPtr.Zero);
                    ShowWindow(existingWindow, SW_SHOW);
                }
                // Bring the window to foreground
                SetForegroundWindow(existingWindow);
            }
            
            Environment.Exit(0);
            return;
        }

        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Services
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<INavigationViewService, NavigationViewService>();
                services.AddSingleton<ILocalizationService, LocalizationService>();

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Core Services
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IFilePickerService, FilePickerService>();

                // Services, not need Interface.
                services.AddSingleton<SettingsService>();
                services.AddSingleton<HotkeyCollectSaveService>();
                services.AddSingleton<HotkeyRegisterService>();
                services.AddSingleton<WndProcService>();
                services.AddSingleton<NotificationService>();
                services.AddSingleton<TrayIconService>();
                services.AddSingleton<TopMostService>();
                services.AddSingleton<InAppNotificationService>();

                // Views and ViewModels
                services.AddSingleton<ShellViewModel>();
                services.AddTransient<ShellPage>();

                services.AddSingleton<SettingsViewModel>();
                services.AddTransient<SettingsPage>();

                services.AddSingleton<HotkeyCollectViewModel>();
                services.AddTransient<HotkeyCollectPage>();
                services.AddSingleton<HotkeyViewModel>();
                services.AddTransient<HotkeyPage>();

                services.AddSingleton<OCRViewModel>();
                services.AddTransient<OCRPage>();

                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            }).Build();

        UnhandledException += App_UnhandledException;
    }

    private static MainWindow? _mainWindow;
    public static MainWindow MainWindow
    {
        get
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
            }
            return _mainWindow;
        }
    }

    public static UIElement? AppTitlebar { get; set; }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.

        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        // or write log.
        // System.IO.File.AppendAllText("error.log", $"Unhandled exception: {e.Exception}\n");
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await App.GetService<IActivationService>().ActivateAsync(args);

        MainWindow.StartOnTray();
    }
}
