using Delete_Newline.Services;
using Delete_Newline.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
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
        ViewModel.SetHotkeyTextBox(TextBox_Hotkey);
        ViewModel.SetDummyFocusButton(DummyFocusButton);
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

    private void TextBox_Hotkey_GotFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.StartHotkeyRegistration();
    }

    private void TextBox_Hotkey_LostFocus(object sender, RoutedEventArgs e)
    {
        ViewModel.EndHotkeyRegistration();
    }
}
