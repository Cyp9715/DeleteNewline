using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class RegexCollectPage : Page
{
    public RegexCollectViewModel ViewModel
    {
        get;
    }

    public RegexCollectPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<RegexCollectViewModel>();
        DataContext = ViewModel;
    }
}
