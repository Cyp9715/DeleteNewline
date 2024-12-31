using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class HotkeyPage : Page
{
    public HotkeyViewModel ViewModel
    {
        get;
    }

    public HotkeyPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HotkeyViewModel>();
        DataContext = ViewModel;
    }
}
