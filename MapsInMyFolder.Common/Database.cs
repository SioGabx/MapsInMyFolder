using ModernWpf.Controls;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapsInMyFolder.Commun
{
    public static class Database
    {
        public static event EventHandler RefreshPanels;
        public static event EventHandler NewUpdateFoundEvent;

        private static string GetDatabasePath()
        {
            return Path.Combine(Settings.working_folder, Settings.database_pathname);
        }

        public static async Task DB_AskDownload(bool force_download = false)
        {
            string database_pathname = GetDatabasePath();

            while (true)
            {
                if (File.Exists(database_pathname) && !force_download)
                {
                    //Si le fichier existe et que (force_download == false) alors return
                    RefreshPanels.Invoke(null, EventArgs.Empty);
                    return;
                }

                ContentDialogResult result = await Message.SetContentDialog($"La base de données n'as pas été trouvée à partir du chemin \"{database_pathname}\".\nVoullez-vous en crée une nouvelle depuis la ressource en ligne ?", "Confirmer", MessageDialogButton.YesNoRetry).ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    DB_Download();
                    break;
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    SQLiteConnection.CreateFile(database_pathname);
                    DB_CreateTables(database_pathname);
                    RefreshPanels.Invoke(null, EventArgs.Empty);
                    return;
                }
            }

        }

        private static async Task<bool> DB_DownloadFile(string database_url, string database_pathname)
        {
            if (string.IsNullOrEmpty(database_url) || string.IsNullOrEmpty(database_pathname))
            {
                return false;
            }
            string NotificationMsg = $"Téléchargement en cours de la base de donnée depuis {new Uri(database_url).Host}...";
            NProgress DatabaseDownloadNotification = new NProgress(NotificationMsg, "MapsInMyFolder", null, 0, false) { };
            DatabaseDownloadNotification.Register();

            Collectif.HttpClientDownloadWithProgress client = new Collectif.HttpClientDownloadWithProgress(database_url, database_pathname);
            client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DatabaseDownloadNotification.Text(NotificationMsg + $" {progressPercentage}% - {Collectif.FormatBytes(totalBytesDownloaded)}/{Collectif.FormatBytes((long)totalFileSize)}");
                    DatabaseDownloadNotification.SetProgress((double)progressPercentage);
                });
            };

            bool IsDownloadSuccess = true;
            try
            {
                await client.StartDownload();
            }
            catch (Exception ex)
            {
                IsDownloadSuccess = false;
                Debug.WriteLine("DB_DownloadFile error :" + ex.ToString());
            }

            if (IsDownloadSuccess)
            {
                RefreshPanels.Invoke(null, EventArgs.Empty);
            }
            else
            {
                DatabaseDownloadNotification.Remove();
            }
            return IsDownloadSuccess;
        }

        public static async void DB_Download()
        {
            string database_pathname = GetDatabasePath();
            if (!Directory.Exists(Settings.working_folder))
            {
                Directory.CreateDirectory(Settings.working_folder);
            }

            while (true)
            {
                if (Network.IsNetworkAvailable())
                {
                    GitHubFile githubAssets = GetGithubAssets.GetContentAssetsFromGithub(new Uri(Settings.github_repository_url).PathAndQuery, String.Empty, Settings.github_database_name);
                    if (!(githubAssets is null) && await DB_DownloadFile(githubAssets?.Download_url, database_pathname))
                    {
                        int UserVersion = Database.ExecuteScalarSQLCommand("PRAGMA user_version");
                        XMLParser.Cache.Write("dbVersion", UserVersion.ToString());
                        XMLParser.Cache.WriteAttribute("dbVersion", "dbSha", githubAssets?.Sha);
                        RefreshPanels.Invoke(null, EventArgs.Empty);
                        return;
                    }

                    ContentDialogResult ErrorDetectedWantToAbord = await Message.SetContentDialog("Une erreur s'est produite lors du téléchargement.\nVoullez-vous reessayer ou annuler (et donc créer une database vide) ?", "Confirmer", MessageDialogButton.RetryCancel).ShowAsync();
                    if (ErrorDetectedWantToAbord != ContentDialogResult.Primary)
                    {
                        break;
                    }
                }
                else
                {
                    //print network not available
                    ContentDialogResult NoConnexionDetectedWantToAbord = await Message.SetContentDialog("Impossible de télécharger la dernière base de données en ligne car vous n'êtes pas connecté à internet. Voullez-vous reessayer ?", "Confirmer", MessageDialogButton.YesNo).ShowAsync();

                    if (NoConnexionDetectedWantToAbord != ContentDialogResult.Primary)
                    {
                        break;
                    }
                }
            }
            SQLiteConnection.CreateFile(database_pathname);
            DB_CreateTables(database_pathname);
            RefreshPanels.Invoke(null, EventArgs.Empty);
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
            return Convert.ToInt32(sqlite_cmd.ExecuteScalar());
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
                    DB_AskDownload(true).ConfigureAwait(true);
                    return null;
                }
            }
            else
            {
                Debug.WriteLine("DB Le fichier n'existe pas");
                DB_AskDownload().ConfigureAwait(true);
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
                sqlite_cmd.CommandText = "DELETE FROM 'DOWNLOADS' WHERE ID=" + Math.Abs(bdid);
                sqlite_cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Download_Delete : " + ex.Message);
            }
        }

        public static async void CheckIfNewerVersionAvailable()
        {
            if (await CompareVersion())
            {
                NewUpdateFoundEvent(null, EventArgs.Empty);
            }
        }

        public static async void StartUpdating()
        {
            GitHubFile githubAssets = GetGithubAssets.GetContentAssetsFromGithub(new Uri(Settings.github_repository_url).PathAndQuery, String.Empty, Settings.github_database_name);
            string downloadedDatabasePath = Path.Combine(Settings.temp_folder, "Github" + Settings.github_database_name);
            bool IsUpdateSuccessful = await DB_DownloadFile(githubAssets.Download_url, downloadedDatabasePath);
            if (IsUpdateSuccessful)
            {
                MergeDatabase(downloadedDatabasePath);
                using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + downloadedDatabasePath + "; Version = 3; New = True; Compress = True;"))
                {
                    sqlite_conn.Open();
                    SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
                    sqlite_cmd.CommandText = "PRAGMA user_version;";
                    XMLParser.Cache.Write("dbVersion", sqlite_cmd.ExecuteScalar().ToString());
                    XMLParser.Cache.WriteAttribute("dbVersion", "dbSha", githubAssets?.Sha);
                }
                Notification ApplicationUpdateNotification = new NText("La mise à jour de la base de donnée à été effectuée avec succès", "MapsInMyFolder")
                {
                    NotificationId = "DatabaseUpdateNotification",
                    DisappearAfterAMoment = true,
                    IsPersistant = true,
                };
                ApplicationUpdateNotification.Register();
                RefreshPanels.Invoke(null, EventArgs.Empty);
            }
        }

        public static void MergeDatabase(string downloadedDatabasePath)
        {
            StringBuilder Sql = new StringBuilder();
            Sql.Append("DROP TABLE IF EXISTS main.'LAYERS';");
            using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + downloadedDatabasePath + "; Version = 3; New = True; Compress = True;"))
            {
                sqlite_conn.Open();
                SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = "PRAGMA user_version;";
                string user_version = sqlite_cmd.ExecuteScalar().ToString();
                Sql.Append($"PRAGMA user_version={user_version};");

                SQLiteDataReader sqlite_datareader;
                sqlite_cmd.CommandText = "SELECT sql FROM 'main'.'sqlite_master' WHERE name = 'LAYERS';";
                using (sqlite_datareader = sqlite_cmd.ExecuteReader())
                {
                    sqlite_datareader.Read();
                    Sql.Append(sqlite_datareader.GetString(0) + ";");
                }
            }
            Sql.Append($"ATTACH '{downloadedDatabasePath}' AS githubdatabase;");
            Sql.Append("INSERT INTO main.LAYERS SELECT * FROM githubdatabase.LAYERS;");
            Sql.Append("DETACH githubdatabase;");
            ExecuteNonQuerySQLCommand(Sql.ToString());
        }

        private static async Task<bool> CompareVersion()
        {
            int UserVersion = Database.ExecuteScalarSQLCommand("PRAGMA user_version");
            int LastCheckDatabaseVersion = Convert.ToInt32(XMLParser.Cache.Read("dbVersion"));
            string LastCheckDatabaseSha = XMLParser.Cache.ReadAttribute("dbVersion", "dbSha");

            GitHubFile githubAssets = GetGithubAssets.GetContentAssetsFromGithub(new Uri(Settings.github_repository_url).PathAndQuery, String.Empty, Settings.github_database_name);

            if (githubAssets == null)
            {
                return false;
            }

            if (LastCheckDatabaseSha == githubAssets.Sha)
            {
                //Latest downloaded version is equal to latest release version
                if (LastCheckDatabaseVersion > UserVersion)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            //Download database from github
            string downloadedDatabasePath = Path.Combine(Settings.temp_folder, "Github" + Settings.github_database_name);
            Collectif.HttpClientDownloadWithProgress client = new Collectif.HttpClientDownloadWithProgress(githubAssets.Download_url, downloadedDatabasePath);
            await client.StartDownload().ConfigureAwait(false);

            int GithubDatabaseVersion;
            using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + downloadedDatabasePath + "; Version = 3; New = True; Compress = True; "))
            {
                sqlite_conn.Open();
                SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = "PRAGMA user_version";
                GithubDatabaseVersion = Convert.ToInt32(sqlite_cmd.ExecuteScalar());
            }

            XMLParser.Cache.Write("dbVersion", GithubDatabaseVersion.ToString());
            XMLParser.Cache.WriteAttribute("dbVersion", "dbSha", githubAssets.Sha);

            if (GithubDatabaseVersion > UserVersion)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
