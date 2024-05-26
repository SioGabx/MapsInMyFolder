using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Database
{
    public static class Manager
    {
        public static void CreateEmpty(this Database database)
        {
            SQLiteConnection.CreateFile(database.Path);
        }

        public static SQLiteConnection OpenConnection(this Database database)
        {
            SQLiteConnection SqlConnection = new SQLiteConnection($"Data Source={database.Path}; Version = 3; New = True; Compress = True; ");
            SqlConnection.Open();
            return SqlConnection;
        }

        public static void CreateTables(this Database database)
        {
            const string Tables = "'ID' INTEGER UNIQUE, 'NAME' TEXT DEFAULT '', 'DESCRIPTION' TEXT DEFAULT '', 'TAGS' TEXT DEFAULT '','COUNTRY' TEXT DEFAULT '', 'IDENTIFIER' TEXT DEFAULT '', 'TILE_URL' TEXT DEFAULT '', 'MIN_ZOOM' INTEGER DEFAULT '', 'MAX_ZOOM' INTEGER DEFAULT '', 'FORMAT' TEXT DEFAULT '', 'SITE' TEXT DEFAULT '', 'SITE_URL' TEXT DEFAULT '', 'STYLE' TEXT DEFAULT '', 'TILE_SIZE' INTEGER DEFAULT '', 'FAVORITE' INTEGER DEFAULT 0, 'SCRIPT' TEXT DEFAULT '','VISIBILITY' TEXT DEFAULT '' ,'SPECIALSOPTIONS' TEXT DEFAULT '','RECTANGLES' TEXT DEFAULT '', 'VERSION' INTEGER DEFAULT 1, 'HAS_SCALE' INTEGER DEFAULT 1";
            var CommandText = $@"
                CREATE TABLE IF NOT EXISTS 'CUSTOMSLAYERS' ({Tables});
                CREATE TABLE IF NOT EXISTS 'LAYERS' ({Tables},PRIMARY KEY('ID' AUTOINCREMENT));
                CREATE TABLE IF NOT EXISTS 'EDITEDLAYERS' ({Tables});
                ";
            database.ExecuteNonQuery(CommandText);
        }




    }
}
