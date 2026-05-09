using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Delete_Newline.Converters;

public class BooleanToFailedBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isFailed = value is bool b && b;
        return new SolidColorBrush(isFailed ? Colors.Red : Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
