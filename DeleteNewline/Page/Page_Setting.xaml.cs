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

namespace DeleteNewline.Page
{
    /// <summary>
    /// Page_Setting.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Setting
    {
        public Page_Setting()
        {
            InitializeComponent();
        }

        bool topMost_origin = Settings.Default.topMost;


        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBox_TopMost.IsChecked != null)
            {
                Settings.Default.topMost = (bool)CheckBox_TopMost.IsChecked;

                if (CheckBox_TopMost.IsChecked == true)
                {
                    DeleteNewline.MainWindow.mainWindow.Topmost = true;
                }
                else
                {
                    DeleteNewline.MainWindow.mainWindow.Topmost = false;
                }
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if(Settings.Default.topMost == true) 
            {
                CheckBox_TopMost.IsChecked = true;
            }
            else
            {
                CheckBox_TopMost.IsChecked = false;
            }
        }
    }
}
