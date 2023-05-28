using System;
using System.Threading;
using System.Windows;
using GlobalHook;

using WinForms = System.Windows.Forms;

namespace DeleteNewline
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>Page_InputText
    public partial class MainWindow
    {
        System.Threading.Mutex singleton = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");

        internal static MainWindow? mainWindow;
        WinForms.NotifyIcon notifyIcon = new WinForms.NotifyIcon();
        Page_InputText page_inputText = new Page_InputText();
        Page_Setting page_setting = new Page_Setting();

        public MainWindow()
        {
            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // Prevent multiple run.
            PreventMultipleRun();

            // Init Window Setting.
            mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Width = Settings.Default.mainWindowSize_width;
            mainWindow.Height = Settings.Default.mainWindowSize_height;

            // Init GlobalHook
            HookImplement.InstallGlobalHook();

            // Init tray
            SetNotifyIcon();
            SetContextMenu();

            // Init NavigationView selection
            navigationView_side.SelectedItem = NavigationViewItem_InputText;
            frame_main.Content = page_inputText;

            // Init User Settings.
            page_setting.InitializationDefaultPage();
        }

        private void ContextMenu_Action_Exit(object? sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void SetContextMenu()
        {
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, ContextMenu_Action_Exit);
        }

        private void SetNotifyIcon()
        {
            notifyIcon.Icon = Resource.favicon;
            notifyIcon.Text = "DeleteNewline";

            notifyIcon.DoubleClick += delegate(object? sender, EventArgs eventArgs)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
            };

            notifyIcon.Visible = true;
        }

        private void PreventMultipleRun()
        {
            if (!singleton.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Already run DeleteNewline!");
                Application.Current.Shutdown();
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Settings.Default.mainWindowSize_width = e.NewSize.Width;
            Settings.Default.mainWindowSize_height = e.NewSize.Height;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            mainWindow.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void NavigationView_PaneOpening(ModernWpf.Controls.NavigationView sender, object args)
        {
            ColumnDefinition_mainWindow_0.Width = new GridLength(300);
        }

        private void NavigationView_PaneClosing(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewPaneClosingEventArgs args)
        {
            ColumnDefinition_mainWindow_0.Width = new GridLength(40);
        }

        private void NavigationView_ItemInvoked(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                frame_main.Content = page_setting;
                HookImplement.UnInstallGlobalHook();
                Windows.Notification.Send("Enter the settings page", "GlobalHooks have been removed.");
            }
            else
            {
                switch (sender.MenuItems.IndexOf(args.InvokedItemContainer))
                {
                    case 0:
                        HookImplement.InstallGlobalHook();
                        frame_main.Content = page_inputText;
                        break;
                }
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Maximized:
                    if (Settings.Default.topMost == true)
                    {
                        mainWindow.Topmost = true;
                    }
                    break;
                case WindowState.Normal:
                    if (Settings.Default.topMost == true)
                    {
                        mainWindow.Topmost = true;
                    }
                    break;
            }
        }
    }
}
