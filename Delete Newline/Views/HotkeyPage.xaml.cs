using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class HotKeyPage : Page
{
    public HotKeyViewModel ViewModel
    {
        get;
    }

    public HotKeyPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HotKeyViewModel>();
        DataContext = ViewModel;
    }
}
