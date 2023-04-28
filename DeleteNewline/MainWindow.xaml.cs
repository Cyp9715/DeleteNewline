using System;
using System.Windows;
using System.Windows.Input;
using GlobalHook;
using WinForms = System.Windows.Forms;

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindow? mainWindow;
        IDataObject? idata;
        HookImplement hookImplement = new HookImplement();
        WinForms.NotifyIcon? notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            // Init Window Setting.
            mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Width = Settings.Default.mainWindowSize_width;
            mainWindow.Height = Settings.Default.mainWindowSize_height;

            // Init GlobalHook
            hookImplement.InstallGlobalHook(GlobalHook_Executation);

            // Init Tray Icon
            SetNotifyIcon();
            SetContextMenu();
        }

        private void ContextMenu_Action_Exit(object? sender, EventArgs e)
        {
            if (mainWindow is not null)
            {
                mainWindow.Close();
            }
        }

        private void SetContextMenu()
        {
            if(notifyIcon is not null)
            {
                notifyIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
                notifyIcon.ContextMenuStrip.Items.Add("Exit", null, ContextMenu_Action_Exit);
            }
        }

        private void SetNotifyIcon()
        {
            notifyIcon = new WinForms.NotifyIcon();

            if (mainWindow is not null)
            {
                notifyIcon.Icon = new System.Drawing.Icon("./Resource/favicon.ico");
                notifyIcon.Text = "DeleteNewline";

                notifyIcon.DoubleClick += delegate(object? sender, EventArgs eventArgs)
                {
                    mainWindow.Show();
                    mainWindow.WindowState = WindowState.Normal;
                };
            }
        }

        private void WindowToTray()
        {
            if(notifyIcon is not null)
            {
                mainWindow!.Visibility = Visibility.Hidden;
                notifyIcon.Visible = true;
            }
        }

        private void TrayToWindow()
        {
            if(notifyIcon is not null)
            {
                notifyIcon.Visible = false;
            }
        }

        private bool CheckDataForm()
        {
            idata = Clipboard.GetDataObject();
            if (idata.GetDataPresent(DataFormats.Text) == false)
            {
                return false;
            }

            return true;
        }

        private void DeleteNewline()
        { 
            if(idata is not null)
            {
                string text = (string)idata.GetData(DataFormats.Text);
                string deleteNewline = text.Replace("\r\n", " ");
                Clipboard.SetDataObject(deleteNewline);
            }
        }

        private void AddAlertMsg()
        {
            TextBox_Main.AppendText("\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.AppendText(" ================== Only text form can be entered =================\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.ScrollToEnd();
        }

        private void GlobalHook_Executation()
        {
            VirtualInput.InputImplement.PressKeyboard_Copy();

            if (CheckDataForm() == false)
            {
                AddAlertMsg();
                return;
            }
            else
            {
                DeleteNewline();
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Settings.Default.mainWindowSize_width = e.NewSize.Width;
            Settings.Default.mainWindowSize_height = e.NewSize.Height;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
        }

        private void TextBox_Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
                if (CheckDataForm() == false)
                {
                    AddAlertMsg();
                    return;
                }
                else
                {
                    DeleteNewline();
                }
            }
        }

        private void MenuItem_TopMost_Checked(object sender, RoutedEventArgs e)
        {
            if(mainWindow is not null)
            {
                mainWindow.Topmost = true;
            }
        }

        private void MenuItem_TopMost_Unchecked(object sender, RoutedEventArgs e)
        {
            if(mainWindow is not null)
            {
                mainWindow.Topmost = false;
            }
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            if (CheckDataForm() == false)
            {
                AddAlertMsg();
                return;
            }
            else
            {
                if (idata is not null)
                {
                    originalText = (string)idata.GetData(DataFormats.Text);
                }
                TextBox_Main.AppendText(originalText);
                DeleteNewline();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if(mainWindow is not null)
            {
                switch (mainWindow.WindowState)
                {
                    case WindowState.Minimized:
                        WindowToTray();
                        break;

                    case WindowState.Normal:
                        TrayToWindow();
                        break;
                }
            } 
        }
    }
}
