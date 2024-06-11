using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace MapsInMyFolder.View.Converters
{
    public class IsEqualMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameters, CultureInfo culture)
        {
            Debug.WriteLine("Check IsEqualMultiConverter");
            for (int i = 0; i + 1 < values.Length; i++)
            {
                object Current = values[i];
                object Next = values[i + 1];
                if (!Current.Equals(Next)) { return false; }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {

            throw new NotSupportedException();

        }
    }
}
