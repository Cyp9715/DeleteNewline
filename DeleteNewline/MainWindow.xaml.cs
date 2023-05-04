using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using GlobalHook;
using WinForms = System.Windows.Forms;
using Windows;
using System.Windows.Controls;

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Threading.Mutex singleton = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");
        HookImplement hookImplement = new HookImplement();

        MainWindow? mainWindow;
        WinForms.NotifyIcon? notifyIcon;
        Page_InputText page_inputText = new Page_InputText();

        IDataObject? idataObj;
        bool completelyExit = false;


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
            hookImplement.InstallGlobalHook();

            // Init tray
            SetNotifyIcon();
            SetContextMenu();

            // Init NavigationView selection
            NavigationView.SelectedItem = NavigationViewItem_InputText;
            Frame_main.Content = page_inputText;
        }

        private void ContextMenu_Action_Exit(object? sender, EventArgs e)
        {
            completelyExit = true;
            mainWindow.Close();
        }

        private void SetContextMenu()
        {
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, ContextMenu_Action_Exit);
        }

        private void SetNotifyIcon()
        {
            notifyIcon = new WinForms.NotifyIcon();
            
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
                mainWindow.Close();
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Settings.Default.mainWindowSize_width = e.NewSize.Width;
            Settings.Default.mainWindowSize_height = e.NewSize.Height;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(completelyExit == false)
            {
                e.Cancel = true;
                mainWindow.Visibility = Visibility.Hidden;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void NavigationView_PaneOpening(ModernWpf.Controls.NavigationView sender, object args)
        {
            Column_0.Width = new GridLength(300);
        }

        private void NavigationView_PaneClosing(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewPaneClosingEventArgs args)
        {
            Column_0.Width = new GridLength(40);
        }

        private void NavigationView_ItemInvoked(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                
            }
            else
            {
                switch (sender.MenuItems.IndexOf(args.InvokedItemContainer))
                {
                    case 0:
                        Frame_main.Content = page_inputText;
                        break;

                    case 1:
                        //MessageBox.Show("1");
                        break;
                }
            }
        }
    }
}
