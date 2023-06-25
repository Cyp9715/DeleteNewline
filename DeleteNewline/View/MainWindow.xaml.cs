using System;
using System.Windows;
using DeleteNewline.ViewModel;
using GlobalHook;

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static MainWindow? mainWindow;
        ViewModel_MainWindow vm_mainWindow;
        Settings appdata = DeleteNewline.Settings.Default;

        public MainWindow()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            vm_mainWindow = new ViewModel_MainWindow(mainWindow);
            DataContext = vm_mainWindow;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            appdata.mainWindowSize_width = mainWindow.Width;
            appdata.mainWindowSize_height = mainWindow.Height;
            appdata.Save();

            e.Cancel = true;
            mainWindow.Visibility = Visibility.Hidden;
        }

        private void NavigationView_PaneOpening(ModernWpf.Controls.NavigationView sender, object args)
        {
            ColumnDefinition_mainWindow_0.Width = new GridLength(navigationView_side.OpenPaneLength - 20);
        }

        private void NavigationView_PaneClosing(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewPaneClosingEventArgs args)
        {
            ColumnDefinition_mainWindow_0.Width = new GridLength(40);
        }

        private void NavigationView_ItemInvoked(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                frame_main.Content = Page_Setting.instance;
                HookImplement.UnInstallGlobalHook();
                Windows.Notification.Send("Enter the settings page", "To use keyBind, go to Input Text page.");
            }
            else
            {
                switch (sender.MenuItems.IndexOf(args.InvokedItemContainer))
                {
                    case 0:
                        HookImplement.InstallGlobalHook();
                        frame_main.Content = Page_InputText.instance;
                        break;
                }
            }
        }
    }
}
