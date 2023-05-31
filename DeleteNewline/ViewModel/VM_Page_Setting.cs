using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.CodeDom;
using System.Windows;

namespace DeleteNewline.ViewModel
{
    class VM_Page_Setting : ObservableObject
    {
        bool _isChecked_checkBox_topMost = Settings.Default.topMost;
        bool _isChecked_checkBox_notification = Settings.Default.notification;
        bool _ischecked_checkBox_deleteMultipleSpace = Settings.Default.deleteMultipleSpace;

        public bool isChecked_checkBox_topMost
        {
            get => _isChecked_checkBox_topMost;
            set => _isChecked_checkBox_topMost = value;
        }

        public bool isChecked_checkBox_notification
        {
            get => _isChecked_checkBox_notification;
            set => _isChecked_checkBox_notification = value;
        }

        public bool ischecked_checkBox_deleteMultipleSpace
        {
            get => _ischecked_checkBox_deleteMultipleSpace;
            set => _ischecked_checkBox_deleteMultipleSpace = value;
        }
    }
}
