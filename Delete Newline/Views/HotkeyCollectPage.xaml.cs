using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class HotKeyCollectPage : Page
{
    public HotKeyCollectViewModel ViewModel
    {
        get;
    }

    public HotKeyCollectPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<HotKeyCollectViewModel>();
        DataContext = ViewModel;
    }
}
