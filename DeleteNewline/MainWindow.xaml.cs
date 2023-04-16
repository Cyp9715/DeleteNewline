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

namespace DeleteNewline
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindow mainWindow;
        IDataObject idata;

        public MainWindow()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
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

            string text = (string)idata.GetData(DataFormats.Text);
            string deleteNewline = text.Replace("\r\n", " ");
            Clipboard.SetDataObject(deleteNewline);
        }

        private void AddAlertMsg()
        {
            TextBox_Main.AppendText("\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.AppendText(" ================== Only text form can be entered =================\r\n");
            TextBox_Main.AppendText(" ========================================================\r\n");
            TextBox_Main.ScrollToEnd();
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
            mainWindow.Topmost = true;
        }

        private void MenuItem_TopMost_Unchecked(object sender, RoutedEventArgs e)
        {
            mainWindow.Topmost = false;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            // To add text to textbox.
            if(CheckDataForm() == false)
            {
                AddAlertMsg();
                return;
            }

            TextBox_Main.AppendText((string)idata.GetData(DataFormats.Text));
            DeleteNewline();
        }
    }
}
