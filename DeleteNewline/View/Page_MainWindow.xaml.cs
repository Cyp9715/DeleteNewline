using DeleteNewline.ViewModel;

namespace DeleteNewline.View
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
