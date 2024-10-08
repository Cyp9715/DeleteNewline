using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Delete_Newline.Views;

public sealed partial class KeybindCollectPage : Page
{
    public KeybindCollectViewModel ViewModel
    {
        get;
    }

    public KeybindCollectPage()
    {
        ViewModel = App.GetService<KeybindCollectViewModel>();
        InitializeComponent();
    }
}
