using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class KeybindPage : Page
{
    public KeybindViewModel ViewModel
    {
        get;
    }

    public KeybindPage()
    {
        ViewModel = App.GetService<KeybindViewModel>();
        InitializeComponent();
    }
}
