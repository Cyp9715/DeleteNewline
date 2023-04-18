using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

using GlobalHook;

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

        public MainWindow()
        {
            InitializeComponent();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Width = Settings.Default.mainWindowSize_width;
            mainWindow.Height = Settings.Default.mainWindowSize_height;

            hookImplement.InstallGlobalHook(GlobalHook_Executation);
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
            if (CheckDataForm() == false)
            {
                AddAlertMsg();
                return;
            }

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
                DeleteNewline();
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
            // To add text to textbox.
            if(CheckDataForm() == false)
            {
                AddAlertMsg();
                return;
            }

            if(idata is not null)
            {
                TextBox_Main.AppendText((string)idata.GetData(DataFormats.Text));
            }
            DeleteNewline();
        }

        private void GlobalHook_Executation()
        {
            if(CheckDataForm() == true)
            {
                DeleteNewline();
            }
        }
    }
}
