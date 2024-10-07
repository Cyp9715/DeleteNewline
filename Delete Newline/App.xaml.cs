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

namespace Delete_Newline;

public partial class App : Application
{
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
        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Services
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<INavigationViewService, NavigationViewService>();
                services.AddSingleton<ILocalizationService, LocalizationService>();

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<INotificationService, NotificationService>();

                // Core Services
                services.AddSingleton<IFileService, FileService>();

                // Views and ViewModels
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();

                services.AddTransient<MemoViewModel>();
                services.AddTransient<MemoPage>();

                services.AddTransient<ShellViewModel>();
                services.AddTransient<ShellPage>();

                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            }).Build();

        UnhandledException += App_UnhandledException;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar { get; set; }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.

        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        // 또는 파일에 로그 작성
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
    }
}
