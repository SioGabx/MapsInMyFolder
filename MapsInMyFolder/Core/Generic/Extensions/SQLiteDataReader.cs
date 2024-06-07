using System.Data.SQLite;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class SQLiteDataReaderExtensions
    {
        public static string GetString(this SQLiteDataReader DataReader, string Name)
        {
            var Ordinal = DataReader.GetOrdinal(Name);
            if (DataReader.IsDBNull(Ordinal)) { return string.Empty; }
            return DataReader.GetString(Ordinal).DecodeEntities();
        }
        public static int GetInt(this SQLiteDataReader DataReader, string Name)
        {
            var Ordinal = DataReader.GetOrdinal(Name);
            if (DataReader.IsDBNull(Ordinal)) { return -1; }
            return DataReader.GetInt32(Ordinal);
        }

        public static bool GetBoolean(this SQLiteDataReader DataReader, string Name)
        {
            return DataReader.GetInt(Name) > 0;
        }
    }
}
