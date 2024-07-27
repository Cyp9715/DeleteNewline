using System;
using System.Data;
using System.Windows;
using DeleteNewline.ViewModel;
using GlobalHook;
using ModernWpf.Controls;

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Page_MainWindow
    {
        public static Page_MainWindow? mainWindow;
        ViewModel_MainWindow vm_mainWindow;
        Settings appdata = Settings.GetInstance();

        public Page_MainWindow(ViewModel_MainWindow vm_mainWindow_)
        {
            InitializeComponent();
            mainWindow = (Page_MainWindow)Application.Current.MainWindow;
            vm_mainWindow = vm_mainWindow_;
            DataContext = vm_mainWindow_;

            InitializeSettings();
        } 

        private void InitializeSettings()
        {
            // Init Content
            mainWindow.NavigationView_Side.SelectedItem = mainWindow.NavigationViewItem_InputText;
            mainWindow.Frame_Main.Content = App.GetService<Page_InputText>();

            // Init Window Setting.
            mainWindow.Width = appdata.mainWindowSize_width;
            mainWindow.Height = appdata.mainWindowSize_height;
            mainWindow.Topmost = appdata.topMost;

            SetNotifyIcon();
            SetContextMenu();
        }

        private System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        private void SetNotifyIcon()
        {
            notifyIcon.Icon = Resource.favicon;
            notifyIcon.Text = Global.programName;

            notifyIcon.DoubleClick += delegate (object? sender, EventArgs eventArgs)
            {
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.WindowState = WindowState.Normal;
            };

            notifyIcon.Visible = true;
        }

        private void SetContextMenu()
        {
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, Action_ContextMenu_Exit);
        }

        private void Action_ContextMenu_Exit(object? sender, EventArgs e)
        {
            appdata.mainWindowSize_width = mainWindow.Width;
            appdata.mainWindowSize_height = mainWindow.Height;
            Settings.Apply(appdata);
            Settings.Save(appdata);

            System.Environment.Exit(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            appdata.mainWindowSize_width = mainWindow.Width;
            appdata.mainWindowSize_height = mainWindow.Height;
            Settings.Apply(appdata);
            Settings.Save(appdata);

            e.Cancel = true;
            mainWindow.Visibility = Visibility.Hidden;
        }

        private void NavigationView_ItemInvoked(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                Frame_Main.Content = App.GetService<Page_Setting>();
            }
            else
            {
                if (args.InvokedItemContainer is ModernWpf.Controls.NavigationViewItem navigationViewItem)
                {
                    Frame_Main.Content = App.GetService<Page_InputText>();
                }
            }
        }
    }
}
