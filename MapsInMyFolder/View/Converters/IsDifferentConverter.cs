using System;
using System.Globalization;
using System.Windows.Data;

namespace MapsInMyFolder.View.Converters
{
    public class IsDifferentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)new IsEqualConverter().Convert(value, targetType, parameter, culture));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
