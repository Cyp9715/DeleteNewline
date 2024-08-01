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
        public Page_MainWindow(ViewModel_MainWindow vm_mainWindow)
        {
            InitializeComponent();
            DataContext = vm_mainWindow;
        } 
    }
}
