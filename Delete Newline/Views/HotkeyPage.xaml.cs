using Delete_Newline.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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

    private void HotKeyPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Retrieve the current pointer event data.
        var point = e.GetCurrentPoint(this);

        // XButton1Pressed typically corresponds to the mouse's 'Back' button.
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.XButton1Pressed)
        {
            e.Handled = true;
            Frame.Navigate(typeof(HotKeyCollectPage));
        }
    }
}
