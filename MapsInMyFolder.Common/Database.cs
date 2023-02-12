using ModernWpf.Controls;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace MapsInMyFolder.Commun
{
    public static class Database
    {
        public static event EventHandler RefreshPanels;
        //public static string selected_database_pathname = "";
        // public static string database_default_path_url = "";
        // public static string default_database_pathname = "";
        //public static string working_folder = "";
        public static async void DB_Download(bool force_download = false)
        {
            //https://api.github.com/repos/SioGabx/MapsInMyFolder/releases
            //https://api.github.com/repos/SioGabx/MapsInMyFolder/releases/latest
            try
            {
                ContentDialogResult result;
                string database_pathname = String.Empty;
                while (true)
                {
                    database_pathname = Path.Combine(Settings.working_folder, Settings.database_pathname);
                    if (File.Exists(database_pathname) && !force_download)
                    {
                        //Si le fichier existe et que force dowbload == false alors return
                        return;
                    }
                    var dialog = Message.SetContentDialog("La base de données n'as pas été trouvée à partir du chemin \"" + database_pathname + "\".\nVoullez-vous en crée une nouvelle depuis la ressource en ligne ?", "Confirmer", MessageDialogButton.YesNoRetry);
                    result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Secondary)
                    {
                        SQLiteConnection.CreateFile(database_pathname);
                        DB_CreateTables(database_pathname);
                        RefreshPanels.Invoke(null, EventArgs.Empty);
                        return;
                    }
                    else if (result == ContentDialogResult.Primary)
                    {
                        break;
                    }
                }


                if (!Directory.Exists(Settings.working_folder))
                {
                    Directory.CreateDirectory(Settings.working_folder);
                }

                //Ask if we want to download remote database or create empty

                while (true)
                {
                    if (Network.IsNetworkAvailable())
                    {
                        GitHubFile githubAssets = GetGithubAssets.GetContentAssetsFromGithub(new Uri(Settings.github_repository_url).PathAndQuery, String.Empty, Settings.github_database_name);
                        if (!(githubAssets is null))
                        {
                            string database_url = githubAssets.Download_url;
                            Debug.WriteLine("database_github_url : " + database_url);
                            HttpResponse response = await Collectif.ByteDownloadUri(new Uri(database_url), 0, true);
                            if (response?.Buffer != null && response.ResponseMessage.IsSuccessStatusCode)
                            {
                                byte[] arrBytes = response.Buffer;
                                File.WriteAllBytes(database_pathname, arrBytes);
                                RefreshPanels.Invoke(null, EventArgs.Empty);
                                return;
                            }
                            else
                            {
                                var dialog = Message.SetContentDialog("Une erreur s'est produite lors du téléchargement.\nError StatusCode :" + response.ResponseMessage.StatusCode + ".\nVoullez-vous reessayer ou annuler et crée une database vide ?", "Confirmer", MessageDialogButton.RetryCancel);
                                ContentDialogResult result2 = ContentDialogResult.None;
                                try
                                {
                                    result2 = await dialog.ShowAsync();
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.ToString());
                                }

                                if (result2 != ContentDialogResult.Primary)
                                {
                                    break;
                                }
                            }
                        }

                        var dialog2 = Message.SetContentDialog("Une erreur s'est produite lors du téléchargement.\nVoullez-vous reessayer ou annuler et crée une database vide ?", "Confirmer", MessageDialogButton.RetryCancel);
                        ContentDialogResult result3 = ContentDialogResult.None;
                        try
                        {
                            result3 = await dialog2.ShowAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                        if (result3 != ContentDialogResult.Primary)
                        {
                            break;
                        }
                    }
                    else
                    {
                        //print network not available
                        var dialog = Message.SetContentDialog("Impossible de télécharger la dernière base de données en ligne car vous n'êtes pas connecté à internet. Voullez-vous reessayer ?", "Confirmer", MessageDialogButton.YesNo);
                        ContentDialogResult result2 = ContentDialogResult.None;
                        try
                        {
                            result2 = await dialog.ShowAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }

                        if (result2 != ContentDialogResult.Primary)
                        {
                            break;
                        }
                    }
                }



                SQLiteConnection.CreateFile(database_pathname);
                DB_CreateTables(database_pathname);
                RefreshPanels.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Layer_Download : " + ex.Message);
            }
        }

        static public void ExecuteNonQuerySQLCommand(string querry)
        {
            SQLiteConnection conn = DB_Connection();
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
            SQLiteConnection conn = DB_Connection();
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
            // Create a new database connection:
            string dbFile = Path.Combine(Settings.working_folder, Settings.database_pathname);
            if (File.Exists(dbFile))
            {
                FileInfo filinfo = new FileInfo(dbFile);
                if (filinfo.Length == 0)
                {
                    Debug.WriteLine("DB Taille corrompu");
                    DB_Download(true);
                    return null;
                }
            }
            else
            {
                Debug.WriteLine("DB Le fichier n'existe pas");
                DB_Download();
                return null;
            }
            return DB_CreateTables(dbFile);
        }


        public static SQLiteConnection DB_CreateTables(string datasource)
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + datasource + "; Version = 3; New = True; Compress = True; ");
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
            //
            const string commande_arg = "('ID' INTEGER UNIQUE, 'NOM' TEXT, 'DESCRIPTION' TEXT, 'CATEGORIE' TEXT, 'IDENTIFIANT' TEXT, 'TILE_URL' TEXT,'TILE_FALLBACK_URL' TEXT, 'MIN_ZOOM' INTEGER DEFAULT 0, 'MAX_ZOOM' INTEGER DEFAULT 0, 'FORMAT' TEXT, 'SITE' TEXT, 'SITE_URL' TEXT, 'TILE_SIZE' INTEGER DEFAULT 256, 'FAVORITE' INTEGER DEFAULT 0, 'TILECOMPUTATIONSCRIPT' TEXT DEFAULT '','VISIBILITY' TEXT DEFAULT 'Visible' ,'SPECIALSOPTIONS' TEXT DEFAULT '', 'VERSION' INTEGER DEFAULT 1 ";
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
                sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'DOWNLOADS' ('ID' INTEGER NOT NULL UNIQUE,'STATE' TEXT,'INFOS' TEXT,'FILE_NAME' TEXT,'NBR_TILES' INTEGER,'ZOOM' INTEGER,'NO_LAT' REAL,'NO_LONG' REAL,'SE_LAT' REAL,'SE_LONG' REAL,'LAYER_ID' INTEGER,'TEMP_DIRECTORY' TEXT,'SAVE_DIRECTORY' TEXT,'TIMESTAMP' TEXT,'QUALITY' INTEGER,'REDIMWIDTH' INTEGER,'REDIMHEIGHT' INTEGER,'COLORINTERPRETATION' TEXT,PRIMARY KEY('ID' AUTOINCREMENT));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                //MessageBox.Show("Une erreur s'est produite au niveau de la base de donnée.\n" + e.Message);  
                Debug.WriteLine("Une erreur s'est produite au niveau de la base de donnée.\n" + e.Message);
            }
        }

        public static int DB_Download_Write(Status STATE, string FILE_NAME, int NBR_TILES, int ZOOM, double NO_LAT, double NO_LONG, double SE_LAT, double SE_LONG, int LAYER_ID, string TEMP_DIRECTORY, string SAVE_DIRECTORY, string TIMESTAMP, int QUALITY, int REDIMWIDTH, int REDIMHEIGHT, string COLORINTERPRETATION)
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
                sqlite_cmd.CommandText = "INSERT INTO 'DOWNLOADS'('STATE','INFOS', 'FILE_NAME', 'NBR_TILES', 'ZOOM', 'NO_LAT', 'NO_LONG', 'SE_LAT', 'SE_LONG', 'LAYER_ID', 'TEMP_DIRECTORY', 'SAVE_DIRECTORY','TIMESTAMP','QUALITY','REDIMWIDTH','REDIMHEIGHT', 'COLORINTERPRETATION') VALUES('" + STATE + "','','" + FILE_NAME + "','" + NBR_TILES + "','" + ZOOM + "','" + NO_LAT + "','" + NO_LONG + "','" + SE_LAT + "','" + SE_LONG + "','" + LAYER_ID + "','" + TEMP_DIRECTORY + "','" + SAVE_DIRECTORY + "','" + TIMESTAMP + "','" + QUALITY + "','" + REDIMWIDTH + "','" + REDIMHEIGHT + "','" + COLORINTERPRETATION + "');";
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
