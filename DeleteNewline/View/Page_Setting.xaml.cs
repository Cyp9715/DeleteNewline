using DeleteNewline.ViewModel;
using GlobalHook;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeleteNewline
{
    public partial class Page_Setting
    {
        Settings appdata;
        ViewModel_Setting vm_setting;

        public Page_Setting(ViewModel_Setting vm_setting_) 
        {
            InitializeComponent();
            appdata = Settings.GetInstance();
            vm_setting = vm_setting_;
            DataContext = vm_setting_;
        }

        private void Button_addRegex_Click(object sender, RoutedEventArgs e)
        {
            vm_setting.additionalRegex.Add(new ViewModel_Setting.GenericParameter_OC("Regex " + vm_setting.gp_count, "Replace " + vm_setting.gp_count, vm_setting.gp_count++));
        }

        private void Button_deleteRegex_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ViewModel_Setting.GenericParameter_OC gp_oc)
            {
                // machineFunction의 index 및 다른 속성에 접근할 수 있다.
                int index = gp_oc.index;

                // 이 정보를 사용하여 작업을 수행한다.
                vm_setting.additionalRegex.Remove(vm_setting.additionalRegex.Where(i => i.index == index).Single());
            }
        }
    }
}
