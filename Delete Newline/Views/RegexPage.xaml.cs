using Delete_Newline.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Delete_Newline.Views;

public sealed partial class RegexPage : Page
{
    public RegexViewModel ViewModel
    {
        get;
    }

    public RegexPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<RegexViewModel>();
        DataContext = ViewModel;
    }

    private void TextBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this);

        // Check if Shift key is pressed
        bool shiftPressed = (e.KeyModifiers & Windows.System.VirtualKeyModifiers.Shift) == Windows.System.VirtualKeyModifiers.Shift;
        if (shiftPressed)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Get the mouse wheel delta
                var delta = pointer.Properties.MouseWheelDelta;

                // Get the ScrollViewer from the TextBox
                var scrollViewer = GetScrollViewer(textBox);
                if (scrollViewer != null)
                {
                    // Adjust horizontal offset based on wheel delta
                    // Negative delta means scrolling right, positive means scrolling left
                    double newOffset = scrollViewer.HorizontalOffset - (delta / 1.0);  // Adjust the divisor to control scroll speed
                    scrollViewer.ChangeView(newOffset, null, null);
                    
                    // Mark the event as handled so it doesn't trigger vertical scrolling
                    e.Handled = true;
                }
            }
        }
    }

    // Helper method to get the ScrollViewer from a TextBox
    private ScrollViewer? GetScrollViewer(DependencyObject depObj)
    {
        if (depObj == null)
            return null;

        // Check if the object itself is a ScrollViewer
        if (depObj is ScrollViewer scrollViewer)
            return scrollViewer;

        // If not, look for a ScrollViewer in the child elements
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
            
            ScrollViewer? result = GetScrollViewer(child);
            if (result != null)
                return result;
        }

        return null;
    }
}
