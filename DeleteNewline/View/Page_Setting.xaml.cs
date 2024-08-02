using CommunityToolkit.Mvvm.Input;
using DeleteNewline.ViewModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeleteNewline.View
{
    public partial class Page_Setting
    {
        public Page_Setting(ViewModel_Setting vm_setting) 
        {
            InitializeComponent();
            DataContext = vm_setting;
        }
    }
}
