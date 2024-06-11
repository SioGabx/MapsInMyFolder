using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace MapsInMyFolder.View.Converters
{
    public class ContainsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null || parameter is null)
            {
                return false;
            }
            if (value is IList valueList)
            {
                return valueList.Contains(parameter);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
