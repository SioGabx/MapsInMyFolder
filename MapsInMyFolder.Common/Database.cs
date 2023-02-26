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
        public static event EventHandler<int> NewUpdateFoundEvent;

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
                    CreateEmptyDatabase(database_pathname);
                    return;
                }
            }

        }
        private static void CreateEmptyDatabase(string database_pathname)
        {
            SQLiteConnection.CreateFile(database_pathname);
            DB_CreateTables(database_pathname);
            RefreshPanels.Invoke(null, EventArgs.Empty);
            Database.CheckIfNewerVersionAvailable();
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
            CreateEmptyDatabase(database_pathname);
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

        static public SQLiteDataReader ExecuteExecuteReaderSQLCommand(string querry)
        {
            SQLiteConnection conn = DB_Connection();
            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return null;
            }
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = querry;
            return sqlite_cmd.ExecuteReader();
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

        public static int GetOrdinal(SQLiteDataReader sqlite_datareader, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Invalid Ordinal name");
            }
            int ordinal = sqlite_datareader.GetOrdinal(name);
            if (sqlite_datareader.IsDBNull(ordinal))
            {
                return -1;
            }
            return ordinal;
        }

        public static string GetStringFromOrdinal(this SQLiteDataReader sqlite_datareader, string name)
        {
            try
            {
                int ordinal = GetOrdinal(sqlite_datareader, name);
                if (ordinal == -1)
                {
                    return "";
                }
                if (sqlite_datareader.IsDBNull(ordinal))
                {
                    return "";
                }
                var get_setring = sqlite_datareader.GetString(ordinal);
                if (string.IsNullOrEmpty(get_setring))
                {
                    return "";
                }
                else
                {
                    return Collectif.HTMLEntities(get_setring, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Get : " + name + " - Erreur : " + ex.ToString());
                return "";
            }
        }
        public static int GetIntFromOrdinal(this SQLiteDataReader sqlite_datareader, string name)
        {
            try
            {
                int ordinal = sqlite_datareader.GetOrdinal(name);
                if (ordinal == -1)
                {
                    return 0;
                }
                if (sqlite_datareader.IsDBNull(ordinal))
                {
                    return 0;
                }
                return sqlite_datareader.GetInt32(ordinal);
            }
            catch (Exception)
            {
                return 0;
            }
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
            const string commande_arg = "('ID' INTEGER UNIQUE, 'NOM' TEXT DEFAULT '', 'DESCRIPTION' TEXT DEFAULT '', 'CATEGORIE' TEXT DEFAULT '', 'IDENTIFIANT' TEXT DEFAULT '', 'TILE_URL' TEXT DEFAULT '','TILE_FALLBACK_URL' TEXT DEFAULT '', 'MIN_ZOOM' INTEGER DEFAULT 0, 'MAX_ZOOM' INTEGER DEFAULT 0, 'FORMAT' TEXT DEFAULT 'jpeg', 'SITE' TEXT DEFAULT '', 'SITE_URL' TEXT DEFAULT '', 'TILE_SIZE' INTEGER DEFAULT 256, 'FAVORITE' INTEGER DEFAULT 0, 'TILECOMPUTATIONSCRIPT' TEXT DEFAULT '','VISIBILITY' TEXT DEFAULT 'Visible' ,'SPECIALSOPTIONS' TEXT DEFAULT '', 'VERSION' INTEGER DEFAULT 1 ";
            sqlite_cmd.CommandText = $@"
            CREATE TABLE IF NOT EXISTS 'CUSTOMSLAYERS' {commande_arg});
            CREATE TABLE IF NOT EXISTS 'LAYERS' {commande_arg},PRIMARY KEY('ID' AUTOINCREMENT));
            CREATE TABLE IF NOT EXISTS 'EDITEDLAYERS' {commande_arg});
            ";
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
            (bool IsNewVersionAvailable, int NewVersionNumber) NewVersion = await CompareVersion();
            if (NewVersion.IsNewVersionAvailable)
            {
                NewUpdateFoundEvent(null, NewVersion.NewVersionNumber);
            }
        }

        private static async Task<(bool IsNewVersionAvailable, int NewVersionNumber)> CompareVersion()
        {
            int UserVersion = Database.ExecuteScalarSQLCommand("PRAGMA user_version");
            int LastCheckDatabaseVersion = Convert.ToInt32(XMLParser.Cache.Read("dbVersion"));
            string LastCheckDatabaseSha = XMLParser.Cache.ReadAttribute("dbVersion", "dbSha");

            GitHubFile githubAssets = GetGithubAssets.GetContentAssetsFromGithub(new Uri(Settings.github_repository_url).PathAndQuery, String.Empty, Settings.github_database_name);
            if (githubAssets == null)
            {
                return (false, UserVersion);
            }

            if (LastCheckDatabaseSha == githubAssets.Sha)
            {
                //Latest downloaded version is equal to latest release version
                if (LastCheckDatabaseVersion > UserVersion)
                {
                    return (true, LastCheckDatabaseVersion);
                }
                else
                {
                    return (false, UserVersion);
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
                return (true, GithubDatabaseVersion);
            }
            else
            {
                return (false, UserVersion);
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

        public static void FixEditedLayers()
        {
            using (SQLiteDataReader editedlayers_sqlite_datareader = ExecuteExecuteReaderSQLCommand("SELECT * FROM 'EDITEDLAYERS'"))
            {
                StringBuilder SQLExecute = new StringBuilder();
                SQLExecute.Append("BEGIN TRANSACTION;");
                while (editedlayers_sqlite_datareader.Read())
                {
                    int DB_Layer_ID = editedlayers_sqlite_datareader.GetIntFromOrdinal("ID");
                    int EditedDB_VERSION = editedlayers_sqlite_datareader.GetIntFromOrdinal("VERSION");
                    string EditedDB_TILECOMPUTATIONSCRIPT = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILECOMPUTATIONSCRIPT");
                    string EditedDB_TILE_URL = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");

                    using (SQLiteDataReader layers_sqlite_datareader = ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'LAYERS' WHERE ID = {DB_Layer_ID}"))
                    {
                        layers_sqlite_datareader.Read();
                        int DB_Layer_MIN_ZOOM = layers_sqlite_datareader.GetIntFromOrdinal("MIN_ZOOM");
                        int DB_Layer_MAX_ZOOM = layers_sqlite_datareader.GetIntFromOrdinal("MAX_ZOOM");
                        int DB_Layer_TILE_SIZE = layers_sqlite_datareader.GetIntFromOrdinal("TILE_SIZE");
                        int LastDB_VERSION = layers_sqlite_datareader.GetIntFromOrdinal("VERSION");
                        string LastDB_TILECOMPUTATIONSCRIPT = layers_sqlite_datareader.GetStringFromOrdinal("TILECOMPUTATIONSCRIPT");
                        string LastDB_TILE_URL = layers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");

                        SQLExecute.Append($"UPDATE 'main'.'EDITEDLAYERS' SET 'MIN_ZOOM'='{DB_Layer_MIN_ZOOM}','MAX_ZOOM'='{DB_Layer_MAX_ZOOM}','TILE_SIZE'='{DB_Layer_TILE_SIZE}' WHERE ID = {DB_Layer_ID};");
                        bool VersionCanBeUpdated = true;
                        if (LastDB_VERSION <= EditedDB_VERSION)
                        {
                            VersionCanBeUpdated = false;
                        }
                        else if (LastDB_TILECOMPUTATIONSCRIPT != EditedDB_TILECOMPUTATIONSCRIPT && LastDB_TILE_URL != EditedDB_TILE_URL)
                        {
                            if (string.IsNullOrWhiteSpace(EditedDB_TILECOMPUTATIONSCRIPT) && string.IsNullOrWhiteSpace(EditedDB_TILE_URL))
                            {
                                VersionCanBeUpdated = true;
                            }
                            else
                            {
                                VersionCanBeUpdated = false;
                            }
                        }

                        if (VersionCanBeUpdated)
                        {
                            SQLExecute.Append($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}' WHERE ID = {DB_Layer_ID};");
                        }
                    }
                }
                SQLExecute.Append("COMMIT;");
                Database.ExecuteNonQuerySQLCommand(SQLExecute.ToString());
            }
        }
    }
}
