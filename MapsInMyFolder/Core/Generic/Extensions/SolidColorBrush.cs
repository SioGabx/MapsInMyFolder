using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class SolidColorBrushExtensions
    {
        public static SolidColorBrush ConvertHexValueToSolidColorBrush(this string HexString, string FallbackHex = "")
        {
            if (!string.IsNullOrWhiteSpace(HexString))
            {

                HexString = HexString.Trim('#');


                if (int.TryParse(HexString, System.Globalization.NumberStyles.HexNumber, null, out _))
                {
                    var brushConverter = new BrushConverter();
                    if (HexString.Length == 3)
                    {
                        string expandedHex = string.Concat(HexString[0], HexString[0], HexString[1], HexString[1], HexString[2], HexString[2]);
                        return (SolidColorBrush)brushConverter.ConvertFrom($"#{expandedHex}");
                    }

                    if (HexString.Length == 6 || HexString.Length == 8)
                    {
                        return (SolidColorBrush)brushConverter.ConvertFrom($"#{HexString}");
                    }
                }
            }

            return string.IsNullOrWhiteSpace(FallbackHex) ? null : ConvertHexValueToSolidColorBrush(FallbackHex);
        }
    }
}
