using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MapsInMyFolder.View.Converters
{
    public class IsEqualMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameters, CultureInfo culture)
        {
            for (int i = 0; i + 1 < values.Length; i++)
            {
                object Current = values[i];
                object Next = values[i + 1];
                if (Current != Next) { return false; }
            }
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {

            throw new NotSupportedException();

        }
    }
}
