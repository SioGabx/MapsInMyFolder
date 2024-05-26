using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
