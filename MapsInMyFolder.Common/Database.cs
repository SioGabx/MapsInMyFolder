using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{
    public static class Database
    {
        //public static string selected_database_pathname = "";
       // public static string database_default_path_url = "";
       // public static string default_database_pathname = "";
        //public static string working_folder = "";
        public static void DB_Download(bool force_download = false)
        {
            //staticc

            //url :
            //todo : dowload db here and check for personnal setting db path
            //todo : if db illisible alors forcer le retélechargement apres confirmation user or create  from scratch
            //download
            try
            {
                string database_pathname = Path.Combine(Settings.working_folder, Settings.database_pathname);
                if (File.Exists(database_pathname) && !force_download)
                {
                    return;
                }
                if (!Directory.Exists(Commun.Settings.working_folder))
                {
                    Directory.CreateDirectory(Commun.Settings.working_folder);
                }
                const string database_default_path_url = @"C:\Users\franc\Desktop\MapsInMyFolder_Project\layers_sqlite.db";
                if (File.Exists(database_default_path_url))
                {
                    System.IO.File.Copy(database_default_path_url, database_pathname, true);
                }
                else
                {
                    Debug.WriteLine("Base de données introuvable", "Erreur");
                    //MessageBox.Show("Base de données introuvable");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Layer_Download : " + ex.Message);
            }
        }

        static public void ExecuteNonQuerySQLCommand(string querry)
        {
            SQLiteConnection conn = Database.DB_Connection();
            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return;
            }
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = querry;
            sqlite_cmd.ExecuteNonQuery();
        }

        static public int ExecuteScalarSQLCommand(string querry)
        {
            SQLiteConnection conn = Database.DB_Connection();
            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return -1;
            }
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = querry;
            int RowCount = Convert.ToInt32(sqlite_cmd.ExecuteScalar());
            return RowCount;
        }

        //CONNEXION A LA BASE DE DONNEES
        public static SQLiteConnection DB_Connection()
        {
            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            string dbFile = Path.Combine(Settings.working_folder, Settings.database_pathname);
            if (System.IO.File.Exists(dbFile))
            {
                FileInfo filinfo = new FileInfo(dbFile);
                if (filinfo.Length == 0)
                {
                    Debug.WriteLine("DB Taille corrompu");
                    DB_Download();
                    return null;
                }
            }
            else
            {
                Debug.WriteLine("DB Le fichier n'existe pas");
                DB_Download();
                return null;
            }
            sqlite_conn = new SQLiteConnection("Data Source=" + dbFile + "; Version = 3; New = True; Compress = True; ");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception)
            {
                return null;
            }
            SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
            const string commande_arg = "('ID' INTEGER UNIQUE, 'NOM' TEXT, 'DESCRIPTION' TEXT, 'CATEGORIE' TEXT, 'IDENTIFIANT' TEXT, 'TILE_URL' TEXT, 'MIN_ZOOM' INTEGER DEFAULT 0, 'MAX_ZOOM' INTEGER DEFAULT 0, 'FORMAT' TEXT, 'SITE' TEXT, 'SITE_URL' TEXT, 'TILE_SIZE' INTEGER DEFAULT 256, 'FAVORITE' INTEGER DEFAULT 0, 'TILECOMPUTATIONSCRIPT' TEXT DEFAULT '', 'SPECIALSOPTIONS' TEXT DEFAULT ''";
            sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'CUSTOMSLAYERS' " + commande_arg + ");";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'LAYERS' " + commande_arg + ",PRIMARY KEY('ID' AUTOINCREMENT))";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'EDITEDLAYERS' " + commande_arg + ");";
            sqlite_cmd.ExecuteNonQuery();
            return sqlite_conn;
        }

        public static void DB_Download_Init(SQLiteConnection conn)
        {
            try
            {
                SQLiteCommand sqlite_cmd = conn?.CreateCommand();
                if (conn is null) { return; }
                sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'DOWNLOADS' ('ID' INTEGER NOT NULL UNIQUE,'STATE' TEXT,'INFOS' TEXT,'FILE_NAME' TEXT,'NBR_TILES' INTEGER,'ZOOM' INTEGER,'NO_LAT' REAL,'NO_LONG' REAL,'SE_LAT' REAL,'SE_LONG' REAL,'LAYER_ID' INTEGER,'TEMP_DIRECTORY' TEXT,'SAVE_DIRECTORY' TEXT,'TIMESTAMP' TEXT,'QUALITY' INTEGER,'REDIMWIDTH' INTEGER,'REDIMHEIGHT' INTEGER,'SPECIALSOPTIONS' TEXT,PRIMARY KEY('ID' AUTOINCREMENT));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                //MessageBox.Show("Une erreur s'est produite au niveau de la base de donnée.\n" + e.Message);  
                Debug.WriteLine("Une erreur s'est produite au niveau de la base de donnée.\n" + e.Message);
            }
        }

        public static int DB_Download_Write(Status STATE, string FILE_NAME, int NBR_TILES, int ZOOM, double NO_LAT, double NO_LONG, double SE_LAT, double SE_LONG, int LAYER_ID, string TEMP_DIRECTORY, string SAVE_DIRECTORY, string TIMESTAMP, int QUALITY, int REDIMWIDTH, int REDIMHEIGHT)
        {
            //staticc
            //CREATE TABLE IF NOT EXISTS some_table (id INTEGER PRIMARY KEY AUTOINCREMENT,

            // CREATE TABLE IF NOT EXISTS "DOWNLOADS" ("ID" INTEGER NOT NULL UNIQUE,"STATE" TEXT,"FILE_NAME" TEXT,"NBR_TILES" INTEGER,"ZOOM" INTEGER,"NO_LAT" REAL,"NO_LONG" REAL,"SE_LAT" REAL,"SE_LONG" REAL,"LAYER_ID" INTEGER,"TEMP_DIRECTORY" TEXT,"SAVE_DIRECTORY" TEXT,PRIMARY KEY("ID" AUTOINCREMENT));
            //INSERT INTO "DOWNLOADS" ("STATE","FILE_NAME","NBR_TILES","ZOOM","NO_LAT","NO_LONG","SE_LAT","SE_LONG","LAYER_ID","TEMP_DIRECTORY","SAVE_DIRECTORY") VALUES (\"" + STATE + \"",\"" + FILE_NAME + \"",\"" + NBR_TILES + \"",\"" + ZOOM + \"",\"" + NO_LAT + \"",\"" + NO_LONG + \"",\"" + SE_LAT + \"",\"" + SE_LONG + \"","LAYER_ID + \"",\"" + TEMP_DIRECTORY + \"",\"" + SAVE_DIRECTORY + \"")

            try
            {
                SQLiteConnection conn = DB_Connection();
                if (conn is null) { return 0; }
                SQLiteCommand sqlite_cmd = conn.CreateCommand();
                DB_Download_Init(conn);
                sqlite_cmd.CommandText = "INSERT INTO 'DOWNLOADS'('STATE','INFOS', 'FILE_NAME', 'NBR_TILES', 'ZOOM', 'NO_LAT', 'NO_LONG', 'SE_LAT', 'SE_LONG', 'LAYER_ID', 'TEMP_DIRECTORY', 'SAVE_DIRECTORY','TIMESTAMP','QUALITY','REDIMWIDTH','REDIMHEIGHT') VALUES('" + STATE + "','','" + FILE_NAME + "','" + NBR_TILES + "','" + ZOOM + "','" + NO_LAT + "','" + NO_LONG + "','" + SE_LAT + "','" + SE_LONG + "','" + LAYER_ID + "','" + TEMP_DIRECTORY + "','" + SAVE_DIRECTORY + "','" + TIMESTAMP + "','" + QUALITY + "','" + REDIMWIDTH + "','" + REDIMHEIGHT + "');";
                sqlite_cmd.ExecuteNonQuery();

                //sqlite_cmd.

                sqlite_cmd.CommandText = "select last_insert_rowid()";
                Int64 LastRowID64 = (Int64)sqlite_cmd.ExecuteScalar();
                int LastRowID = (int)LastRowID64;

                conn.Close();
                return LastRowID;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Download_Write : " + ex.Message);
            }

            return 0;
        }

        public static void DB_Download_Update(int bdid, string ROW, string value)
        {
            try
            {
                SQLiteConnection conn = DB_Connection();
                if (conn is null) { return; }
                DB_Download_Init(conn);
                SQLiteCommand sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = "UPDATE 'DOWNLOADS' SET '" + ROW + "'='" + value.Replace("\"", "") + "' WHERE ID=" + bdid;
                sqlite_cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Download_Update : " + ex.Message);
            }
        }

        public static void DB_Download_Delete(int bdid)
        {
            try
            {
                SQLiteConnection conn = DB_Connection();
                DB_Download_Init(conn);
                SQLiteCommand sqlite_cmd = conn.CreateCommand();
                //DELETE FROM "main"."DOWNLOADS" WHERE _rowid_ IN ('11');
                sqlite_cmd.CommandText = "DELETE FROM 'DOWNLOADS' WHERE ID=" + Math.Abs(bdid);
                sqlite_cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Download_Delete : " + ex.Message);
            }
        }

    }
}
