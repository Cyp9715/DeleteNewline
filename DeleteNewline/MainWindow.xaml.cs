using System;
using System.IO;
using System.Text;
using System.Threading;
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
        System.Threading.Mutex singleton = new Mutex(true, "260bf0b2-4dae-4146-9c0b-f794ad868790");
        HookImplement hookImplement = new HookImplement();
        WinForms.NotifyIcon? notifyIcon;
        MainWindow? mainWindow;
        IDataObject? idataObj;

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
            hookImplement.InstallGlobalHook(GlobalHook_Executation);

            // Init tray
            SetNotifyIcon();
            SetContextMenu();
        }

        private void ContextMenu_Action_Exit(object? sender, EventArgs e)
        {
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

            string iconPath = "./Resource/favicon.ico";
            if (File.Exists(iconPath) == true)
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                // 해당 메세지 발생시 TrayIcon 에 들어가지 않는 문제 발생.
                MessageBox.Show("Can't find : " + iconPath + "\r\n");
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

        private StringBuilder DeleteClipboardNewline(ref IDataObject idata, bool splitPeriod = false)
        {
            StringBuilder stringBuilder;

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

            return stringBuilder;
        }

        private void AddAlertMessageInTextBox()
        {
            TextBox_Main.AppendText("\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.AppendText(" ================== Only text form can be entered =================\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.ScrollToEnd();
        }

        private void GlobalHook_Executation()
        {
            string notifyHeader = String.Empty;
            string notifyContent = String.Empty;
            int limitLen = 100;

            VirtualInput.InputImplement.PressKeyboard_Copy();

            if (GetClipboardData_Text(ref idataObj) == true)
            {
                notifyHeader = "SUCCESS";

                string deletedText = DeleteClipboardNewline(ref idataObj).ToString();
                Clipboard.SetDataObject(deletedText);

                if (deletedText.Length > limitLen)
                {
                    notifyContent = deletedText.Substring(0, limitLen) + " ...";
                }
                else
                {
                    notifyContent = deletedText;
                }
            }
            else
            {
                notifyHeader = "ERROR";
                notifyContent = "CLIPBOARD FORM IS NOT TEXT";
            }

            Notification.Send(notifyHeader, notifyContent);
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

            if(GetClipboardData_Text(ref idataObj) == true)
            {
                originalText = (string)idataObj.GetData(DataFormats.Text);
                TextBox_Main.AppendText(originalText);
                Clipboard.SetDataObject(DeleteClipboardNewline(ref idataObj).ToString());
            }
            else
            {
                AddAlertMessageInTextBox();
                return;
            }
        }

        private void TextBox_Main_KeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.V || Keyboard.IsKeyDown(Key.V) && e.Key == Key.LeftCtrl)
            {
                if(GetClipboardData_Text(ref idataObj) == true)
                {
                    Clipboard.SetDataObject(DeleteClipboardNewline(ref idataObj).ToString());
                }
                else
                {
                    AddAlertMessageInTextBox();
                    return;
                }
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
