using DeleteNewline.ViewModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeleteNewline
{
    public partial class Page_Setting
    {
        public Page_Setting(ViewModel_Setting vm_setting_) 
        {
            InitializeComponent();
            DataContext = vm_setting_;
        }
    }
}
