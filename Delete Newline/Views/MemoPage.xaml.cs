using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class MemoPage : Page
{
    public MemoViewModel ViewModel
    {
        get;
    }

    public MemoPage()
    {
        ViewModel = App.GetService<MemoViewModel>();
        InitializeComponent();
    }
}
