using Delete_Newline.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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

    private void HotkeyPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Retrieve the current pointer event data.
        var point = e.GetCurrentPoint(this);

        // XButton1Pressed typically corresponds to the mouse's 'Back' button.
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.XButton1Pressed)
        {
            e.Handled = true;
            Frame.Navigate(typeof(HotkeyCollectPage));
        }
    }
}
