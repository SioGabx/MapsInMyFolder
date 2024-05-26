using System.Collections.Generic;
using System.Data.SQLite;

namespace MapsInMyFolder.Core.Database
{
    public static class Query
    {
        public static int ExecuteNonQuery(this Database database, string Command)
        {
            using (var DbConnection = database.OpenConnection())
            using (var DbCommand = DbConnection.CreateCommand())
            {
                DbCommand.CommandText = Command;
                return DbCommand.ExecuteNonQuery();
            }
        }

        public static SQLiteDataReader ExecuteReader(this Database database, string Command)
        {
            var DbConnection = database.OpenConnection();
            {
                var DbCommand = DbConnection.CreateCommand();
                DbCommand.CommandText = Command;
                return DbCommand.ExecuteReader();
            }
        }

        public static IEnumerable<SQLiteDataReader> ExecuteReaderIEnumerable(this Database database, string Command)
        {
            var Reader = database.ExecuteReader(Command);
            {
                while (Reader.Read())
                {
                    yield return Reader;
                }
            }
        }


    }
}
