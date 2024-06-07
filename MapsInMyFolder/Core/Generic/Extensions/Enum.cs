using System;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class EnumExtensions
    {
        public static T ConvertToEnum<T>(this string Value) where T : IConvertible//enum
        {
            if (string.IsNullOrEmpty(Value)) return default(T);
            return (T)Enum.Parse(typeof(T), Value, true);
        }
    }
}
