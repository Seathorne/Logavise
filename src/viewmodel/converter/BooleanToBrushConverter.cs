using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Logavise
{
    public class BooleanToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var colors = parameter.ToString().Split('|');
            return (bool)value ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[0]))
                               : new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[1]));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
