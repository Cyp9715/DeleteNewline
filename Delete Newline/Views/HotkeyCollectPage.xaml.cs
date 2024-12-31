using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class HotkeyCollectPage : Page
{
    public HotkeyCollectViewModel ViewModel
    {
        get;
    }

    public HotkeyCollectPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HotkeyCollectViewModel>();
        DataContext = ViewModel;
    }
}
