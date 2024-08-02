using DeleteNewline.ViewModel;

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
