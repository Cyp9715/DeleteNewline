using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using GlobalHook;
using WindowNotification;
using WinForms = System.Windows.Forms;

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindow? mainWindow;
        WinForms.NotifyIcon? notifyIcon;
        HookImplement hookImplement = new HookImplement();
        IDataObject? idata;

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
            mainWindow.Close();
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

            string iconPath = "./Resource/favicon.ico";
            if (File.Exists(iconPath) == true)
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }

            notifyIcon.Text = "DeleteNewline";

            notifyIcon.DoubleClick += delegate(object? sender, EventArgs eventArgs)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
            };
        }

        private void WindowToTray()
        {
            mainWindow.Visibility = Visibility.Hidden;
            notifyIcon.Visible = true;
        }

        private void TrayToWindow()
        {
            notifyIcon.Visible = false;
        }

        private bool GetClipboardData_Text(ref IDataObject idata)
        {
            idata = Clipboard.GetDataObject();

            if (idata.GetDataPresent(DataFormats.Text) == false)
            {
                return false;
            }

            return true;
        }

        private StringBuilder DeleteNewline(bool splitPeriod = false)
        {
            StringBuilder stringBuilder;

            if (GetClipboardData_Text(ref idata) == false)
            {
                AddAlertMsg();
                return new StringBuilder("Error");
            }
            else
            {
                string clipboardText = (string)idata.GetData(DataFormats.Text);
                stringBuilder = new StringBuilder(clipboardText);

                if(splitPeriod == false)
                {
                    stringBuilder.Replace("\r\n", " ");
                }
                else
                {
                    stringBuilder.Replace("\r\n", " ");
                    stringBuilder.Replace(". ", ".\r\n");
                }
                Clipboard.SetDataObject(stringBuilder.ToString());

                return stringBuilder;
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

            string deletedText = DeleteNewline().ToString();
            string lenLimitText = String.Empty;
            int limitLen = 100;

            if(deletedText.Length > limitLen) 
            {
                lenLimitText = deletedText.Substring(0, limitLen) + " ...";
            }
            else
            {
                lenLimitText = deletedText;
            }

            Notification.Send("Successfully deleted newline!", lenLimitText);
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

        private void MenuItem_TopMost_Checked(object sender, RoutedEventArgs e)
        {
            mainWindow.Topmost = true;
        }

        private void MenuItem_TopMost_Unchecked(object sender, RoutedEventArgs e)
        {
            mainWindow.Topmost = false;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            string originalText = String.Empty;

            originalText = (string)idata.GetData(DataFormats.Text);
            TextBox_Main.AppendText(originalText);
            DeleteNewline();
        }

        private void TextBox_Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
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
