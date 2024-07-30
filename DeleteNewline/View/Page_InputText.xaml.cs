using CommunityToolkit.Mvvm.Input;
using DeleteNewline.ViewModel;
using System;
using System.Windows;
using System.Windows.Input;

namespace DeleteNewline
{
    public partial class Page_InputText
    {
        public Page_InputText(ViewModel_InputText? vm_inputText_)
        {
            InitializeComponent();
            DataContext = vm_inputText_;
        }
    }
}
