using DeleteNewline.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IHost Host
        {
            get;
        }

        public static T GetService<T>()
            where T : class
        {
            if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
            {
                throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
            }

            return service;
        }

        App()
        {
            Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddSingleton<ViewModel_MainWindow>();
                services.AddSingleton<ViewModel_InputText>();
                services.AddSingleton<ViewModel_Setting>();

                services.AddSingleton<Page_MainWindow>();
                services.AddSingleton<Page_InputText>();
                services.AddSingleton<Page_Setting>();
            }).Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = GetService<Page_MainWindow>();

            this.MainWindow = mainWindow;

            // Delete Newline 은 시작과 동시에 Hidden.
            //mainWindow.Show();
        }

    }
}
