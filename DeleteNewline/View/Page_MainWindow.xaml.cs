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
        System.Windows.Forms.NotifyIcon? notifyIcon;
        ViewModel_MainWindow vm_mainWindow;

        public Page_MainWindow(ViewModel_MainWindow vm_mainWindow_)
        {
            InitializeComponent();
            mainWindow = (Page_MainWindow)Application.Current.MainWindow;
            vm_mainWindow = vm_mainWindow_;
            this.DataContext = vm_mainWindow_;

            InitNotifyIcon();
        } 

        private void InitNotifyIcon()
        {
            notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = Resource.favicon,
                Text = GlobalVariables.programName,
                Visible = true
            };

            // 더블클릭시 화면 Visible.
            notifyIcon.DoubleClick += (sender, eventArgs) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (mainWindow != null)
                    {
                        mainWindow.Visibility = Visibility.Visible;
                        mainWindow.WindowState = WindowState.Normal;
                    }
                });
            };

            // notifyIcon 내부 ContentMenu 설정
            notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Exit DeleteNewline", null, vm_mainWindow.Action_ContextMenu_Exit);
        }
    }
}
