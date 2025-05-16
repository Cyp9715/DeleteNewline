using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Microsoft.UI.Xaml;

namespace Delete_Newline.Converters;

public class BooleanToFailedBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isFailed && isFailed)
        {
            return new SolidColorBrush(Colors.Red);
        }
        
        // Return default theme brush or a specific fallback
        // For TextBlock Foreground, SystemControlForegroundBaseHighBrush is a common default.
        // Or use a ThemeResource if defined elsewhere for this specific TextBlock.
        // Here, we'll try to use the theme resource provided in the original XAML.
        if (Application.Current.Resources.TryGetValue("SystemChromeGrayColor", out var brush) && brush is SolidColorBrush)
        {
            return brush;
        }
        return new SolidColorBrush(Colors.Gray); // Fallback if resource not found
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
} 