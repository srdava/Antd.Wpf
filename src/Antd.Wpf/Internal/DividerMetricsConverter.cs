using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Antd.Wpf.Internal;

internal sealed class DividerMetricsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double fontSize = (double)value;
        if (Equals(parameter, "Height")) { return fontSize * 0.9; }
        if (Equals(parameter, "Offset"))
        {
            var transform = new TranslateTransform(0, fontSize * -0.06);
            transform.Freeze();
            return transform;
        }
        if (Equals(parameter, "LineHeight")) { return fontSize * 22 / 14; }
        return new Thickness(fontSize, 0, fontSize, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
