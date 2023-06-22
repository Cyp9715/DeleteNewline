using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Threading;
using GlobalHook;
using System.Windows;
using WinForms = System.Windows.Forms;


namespace DeleteNewline
{
    // Application InitialSetting.
    class InitialSetting
    {
        System.Threading.Mutex singleton = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");
        WinForms.NotifyIcon notifyIcon = new WinForms.NotifyIcon();

        Page_InputText page_inputText = new Page_InputText();
        Page_Setting page_setting = new Page_Setting();

        Settings appdata = Settings.Default;
        MainWindow mainWindow;

        public InitialSetting(MainWindow mainWindow_)
        {
            PreventMultipleRun();
            mainWindow = mainWindow_;

            // Init Window Setting.
            mainWindow.Width = appdata.mainWindowSize_width;
            mainWindow.Height = appdata.mainWindowSize_height;
            mainWindow.Topmost = appdata.topMost;

            // Init GlobalHook
            HookImplement.InstallGlobalHook();

            // Init tray
            SetNotifyIcon();
            SetContextMenu();

            // Init Content
            mainWindow.navigationView_side.SelectedItem = mainWindow.NavigationViewItem_InputText;
            mainWindow.frame_main.Content = page_inputText;

            Windows.Startup.Unregistered();
        }

        private void PreventMultipleRun()
        {
            if (!singleton.WaitOne(TimeSpan.Zero, true))
            {
                string alertMsg = "Already run " + Global.programName + "!";
                MessageBox.Show(alertMsg);
                Application.Current.Shutdown();
            }
        }

        private void SetNotifyIcon()
        {
            notifyIcon.Icon = Resource.favicon;
            notifyIcon.Text = Global.programName;

            notifyIcon.DoubleClick += delegate (object? sender, EventArgs eventArgs)
            {
                mainWindow.Show();
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
            appdata.Save();

            System.Environment.Exit(0);
        }
    }
}
