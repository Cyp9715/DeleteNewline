using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Delete_Newline.Converters;

public class BooleanToFailedBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FailedBrush = new(Colors.Red);
    private static readonly SolidColorBrush DefaultBrush = new(Colors.Gray);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isFailed && isFailed ? FailedBrush : DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
