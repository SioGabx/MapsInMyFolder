using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private static async void CreateEmptyDatabase(string database_pathname)
        {
            SQLiteConnection.CreateFile(database_pathname);
            DB_CreateTables(database_pathname);
            RefreshPanels.Invoke(null, EventArgs.Empty);
            await Database.CheckIfNewerVersionAvailable();
        }

        private static async Task<bool> DB_DownloadFile(string database_url, string database_pathname)
        {
            if (string.IsNullOrEmpty(database_url) || string.IsNullOrEmpty(database_pathname))
            {
                return false;
            }
            string NotificationMsg = $"Téléchargement en cours de la base de donnée depuis {new Uri(database_url).Host}...";
            NProgress DatabaseDownloadNotification = new NProgress(NotificationMsg, "MapsInMyFolder", "MainPage", null, 0, false) { };
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
            using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
            {
                sqlite_cmd.CommandText = querry;
                sqlite_cmd.ExecuteNonQuery();
            }

        }

        static public SQLiteDataReader ExecuteExecuteReaderSQLCommand(string querry)
        {
            bool HasError = false;
            do
            {
                try
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
                catch (Exception ex)
                {
                    CheckForMissingCollumns();
                    if (!HasError)
                    {
                        HasError = true;
                    }
                    else
                    {
                        throw ex;
                    }
                }
            } while (HasError);
            return null;
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
            object ScalarValue = sqlite_cmd.ExecuteScalar();
            if (ScalarValue != null && DBNull.Value != ScalarValue)
            {
                return Convert.ToInt32(ScalarValue);
            }
            else
            {
                return -1;
            }
        }
        static public int ExecuteDirectScalarSQLCommand(string querry)
        {
            SQLiteConnection conn = DB_Connection();
            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return -1;
            }
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = querry;
            if (int.TryParse(sqlite_cmd.ExecuteScalar().ToString(), out int result))
            {
                return result;
            }
            else
            {
                return -1;
            }

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

        public static int? GetIntFromOrdinal(this SQLiteDataReader sqlite_datareader, string name)
        {
            try
            {
                int ordinal = sqlite_datareader.GetOrdinal(name);
                if (ordinal == -1)
                {
                    return null;
                }
                if (sqlite_datareader.IsDBNull(ordinal))
                {
                    return null;
                }
                return sqlite_datareader.GetInt32(ordinal);
            }
            catch (Exception)
            {
                return null;
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
            const string commande_arg = "'ID' INTEGER UNIQUE, 'NOM' TEXT DEFAULT '', 'DESCRIPTION' TEXT DEFAULT '', 'CATEGORIE' TEXT DEFAULT '','PAYS' TEXT DEFAULT '', 'IDENTIFIANT' TEXT DEFAULT '', 'TILE_URL' TEXT DEFAULT '','TILE_FALLBACK_URL' TEXT DEFAULT '', 'MIN_ZOOM' INTEGER DEFAULT '', 'MAX_ZOOM' INTEGER DEFAULT '', 'FORMAT' TEXT DEFAULT '', 'SITE' TEXT DEFAULT '', 'SITE_URL' TEXT DEFAULT '', 'TILE_SIZE' INTEGER DEFAULT '', 'FAVORITE' INTEGER DEFAULT 0, 'TILECOMPUTATIONSCRIPT' TEXT DEFAULT '','VISIBILITY' TEXT DEFAULT '' ,'SPECIALSOPTIONS' TEXT DEFAULT '','RECTANGLES' TEXT DEFAULT '', 'VERSION' INTEGER DEFAULT 1, 'HAS_SCALE' INTEGER DEFAULT 1";
            sqlite_cmd.CommandText = $@"
            CREATE TABLE IF NOT EXISTS 'CUSTOMSLAYERS' ({commande_arg});
            CREATE TABLE IF NOT EXISTS 'LAYERS' ({commande_arg},PRIMARY KEY('ID' AUTOINCREMENT));
            CREATE TABLE IF NOT EXISTS 'EDITEDLAYERS' ({commande_arg});
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
                sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS 'DOWNLOADS' ('ID' INTEGER NOT NULL UNIQUE,'STATE' TEXT,'INFOS' TEXT,'FILE_NAME' TEXT,'NBR_TILES' INTEGER,'ZOOM' INTEGER,'NO_LAT' REAL,'NO_LONG' REAL,'SE_LAT' REAL,'SE_LONG' REAL,'LAYER_ID' INTEGER,'TEMP_DIRECTORY' TEXT,'SAVE_DIRECTORY' TEXT,'TIMESTAMP' TEXT,'QUALITY' INTEGER,'REDIMWIDTH' INTEGER,'REDIMHEIGHT' INTEGER,'COLORINTERPRETATION' TEXT,'SCALEINFO' TEXT,PRIMARY KEY('ID' AUTOINCREMENT));";
                sqlite_cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Une erreur s'est produite au niveau de la base de donnée.\n" + e.Message);
            }
        }

        public static int DB_Download_Write(Status STATE, string FILE_NAME, int NBR_TILES, int ZOOM, double NO_LAT, double NO_LONG, double SE_LAT, double SE_LONG, int LAYER_ID, string TEMP_DIRECTORY, string SAVE_DIRECTORY, string TIMESTAMP, int QUALITY, int REDIMWIDTH, int REDIMHEIGHT, string COLORINTERPRETATION, string SCALEINFO)
        {
            try
            {
                SQLiteConnection conn = DB_Connection();
                if (conn is null) { return 0; }
                SQLiteCommand sqlite_cmd = conn.CreateCommand();
                DB_Download_Init(conn);
                sqlite_cmd.CommandText = $"INSERT INTO 'DOWNLOADS'('STATE','INFOS', 'FILE_NAME', 'NBR_TILES', 'ZOOM', 'NO_LAT', 'NO_LONG', 'SE_LAT', 'SE_LONG', 'LAYER_ID', 'TEMP_DIRECTORY', 'SAVE_DIRECTORY','TIMESTAMP','QUALITY','REDIMWIDTH','REDIMHEIGHT', 'COLORINTERPRETATION', 'SCALEINFO') VALUES('{STATE}','','{FILE_NAME}','{NBR_TILES}','{ZOOM}','{NO_LAT}','{NO_LONG}','{SE_LAT}','{SE_LONG}','{LAYER_ID}','{TEMP_DIRECTORY}','{SAVE_DIRECTORY}','{TIMESTAMP}','{QUALITY}','{REDIMWIDTH}','{REDIMHEIGHT}','{COLORINTERPRETATION}','{SCALEINFO}');";
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

        public static async Task<bool> CheckIfNewerVersionAvailable()
        {
            (bool IsNewVersionAvailable, int NewVersionNumber) NewVersion = await CompareVersion();
            if (NewVersion.IsNewVersionAvailable)
            {
                NewUpdateFoundEvent(null, NewVersion.NewVersionNumber);
                return true;
            }
            return false;
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
                Notification ApplicationUpdateNotification = new NText("La mise à jour de la base de donnée à été effectuée avec succès", "MapsInMyFolder", "MainPage")
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
            CheckForMissingCollumns();
            FixEditedLayers();
        }

        public static void FixEditedLayers()
        {
            using (SQLiteDataReader editedlayers_sqlite_datareader = ExecuteExecuteReaderSQLCommand("SELECT * FROM 'EDITEDLAYERS'"))
            {
                StringBuilder SQLExecute = new StringBuilder();
                SQLExecute.Append("BEGIN TRANSACTION;");
                while (editedlayers_sqlite_datareader.Read())
                {
                    int DB_Layer_ID = (int)editedlayers_sqlite_datareader.GetIntFromOrdinal("ID");
                    int EditedDB_VERSION = editedlayers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    string EditedDB_TILECOMPUTATIONSCRIPT = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILECOMPUTATIONSCRIPT");
                    string EditedDB_TILE_URL = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");

                    using (SQLiteDataReader layers_sqlite_datareader = ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'LAYERS' WHERE ID = {DB_Layer_ID}"))
                    {
                        layers_sqlite_datareader.Read();
                        int LastDB_VERSION = layers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                        string LastDB_TILECOMPUTATIONSCRIPT = layers_sqlite_datareader.GetStringFromOrdinal("TILECOMPUTATIONSCRIPT");
                        string LastDB_TILE_URL = layers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
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

        public static void CheckForMissingCollumns()
        {
            Debug.WriteLine("Check for missing collums");
            Dictionary<string, List<string>> map = new Dictionary<string, List<string>>();
            map.Add("LAYERS", new List<string>());
            map.Add("EDITEDLAYERS", new List<string>());
            map.Add("CUSTOMSLAYERS", new List<string>());

            foreach (string key in map.Keys)
            {
                using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"PRAGMA table_info({key})"))
                {
                    while (LayersTables.Read())
                    {
                        map[key].Add(LayersTables.GetStringFromOrdinal("name"));
                    }
                }
            }

            List<string> UniqueValues = new List<string>();
            bool HasSameNumber = true;
            foreach (string key in map.Keys)
            {
                foreach (string key2 in map.Keys)
                {
                    if (map[key].Count != map[key2].Count)
                    {
                        HasSameNumber = false;
                        break;
                    }
                }
                UniqueValues.AddRange(map[key]);
            }

            if (HasSameNumber && UniqueValues.Distinct().Count() == map.First().Value.Count)
            {
                Debug.WriteLine("No missing Collumn in database");
                return;
            }
            StringBuilder SQLExecute = new StringBuilder();
            SQLExecute.Append("BEGIN TRANSACTION;");
            foreach (string key in map.Keys)
            {
                using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"PRAGMA table_info({key})"))
                {
                    while (LayersTables.Read())
                    {
                        string DB_CollumnName = LayersTables.GetStringFromOrdinal("name");
                        string DB_CollumnType = LayersTables.GetStringFromOrdinal("type");
                        string DB_CollumnDefaultValue = LayersTables.GetStringFromOrdinal("dflt_value");
                        if (string.IsNullOrWhiteSpace(DB_CollumnDefaultValue))
                        {
                            DB_CollumnDefaultValue = "NULL";
                        }
                        foreach (string keyTable in map.Keys)
                        {
                            if (!map[keyTable].Contains(DB_CollumnName))
                            {
                                map[keyTable].Add(DB_CollumnName);
                                SQLExecute.Append($"ALTER TABLE {keyTable} ADD COLUMN {DB_CollumnName} {DB_CollumnType} DEFAULT {DB_CollumnDefaultValue};");
                            }
                        }
                    }

                }
            }
            SQLExecute.Append("COMMIT;");
            ExecuteNonQuerySQLCommand(SQLExecute.ToString());
        }

        public static void Export(string FilePath)
        {
            StringBuilder SQLExecute = new StringBuilder();
            SQLExecute.Append("BEGIN TRANSACTION;");

            using (SQLiteDataReader sqlite_datareader = ExecuteExecuteReaderSQLCommand("SELECT sql FROM 'main'.'sqlite_master' WHERE name = 'LAYERS';"))
            {
                sqlite_datareader.Read();
                SQLExecute.AppendLine(sqlite_datareader.GetString(0) + ";");
            }

            Dictionary<string, string> TableCollumsNames = new Dictionary<string, string>();
            using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"PRAGMA table_info(LAYERS)"))
            {
                while (LayersTables.Read())
                {
                    string DB_CollumnName = LayersTables.GetStringFromOrdinal("name");
                    string DB_CollumnType = LayersTables.GetStringFromOrdinal("type");
                    TableCollumsNames.Add(DB_CollumnName, DB_CollumnType);
                }
            }

            foreach (string Table in new string[2] { "LAYERS", "CUSTOMSLAYERS" })
            {

                using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"SELECT * FROM '{Table}';"))
                {
                    while (LayersTables.Read())
                    {
                        int RowId = LayersTables.GetIntFromOrdinal("ID") ?? 0;
                        string Collumns = "";
                        Dictionary<string, string> ValuesList = new Dictionary<string, string>();
                        foreach (string CollumnName in TableCollumsNames.Keys)
                        {
                            if (CollumnName == "ID" && Table != "LAYERS")
                            {
                                continue;
                            }
                            Collumns += CollumnName + ",";
                            string CurrentCollumnType = TableCollumsNames[CollumnName];
                            string Value = "";
                            if (CurrentCollumnType == "TEXT")
                            {
                                Value = LayersTables.GetStringFromOrdinal(CollumnName);
                            }
                            else if (CurrentCollumnType == "INTEGER")
                            {
                                Value = LayersTables.GetIntFromOrdinal(CollumnName).ToString();
                            }
                            if (string.IsNullOrWhiteSpace(Value))
                            {
                                Value = "NULL";
                            }
                            ValuesList.Add(CollumnName, Collectif.HTMLEntities(Value));
                        }

                        using (SQLiteDataReader EditedLayersTables = ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'EDITEDLAYERS' WHERE ID={RowId};"))
                        {
                            while (EditedLayersTables.Read())
                            {
                                bool HasReplacement = false;
                                foreach (string CollumnName in TableCollumsNames.Keys)
                                {
                                    if (CollumnName == "ID" || CollumnName == "VERSION")
                                    {
                                        continue;
                                    }
                                    string CurrentCollumnType = TableCollumsNames[CollumnName];
                                    string Value = "";
                                    if (CurrentCollumnType == "TEXT")
                                    {
                                        Value = EditedLayersTables.GetStringFromOrdinal(CollumnName);
                                    }
                                    else if (CurrentCollumnType == "INTEGER")
                                    {
                                        Value = EditedLayersTables.GetIntFromOrdinal(CollumnName).ToString();
                                    }
                                    if (!string.IsNullOrWhiteSpace(Value))
                                    {
                                        HasReplacement = true;
                                        ValuesList[CollumnName] = Collectif.HTMLEntities(Value);
                                    }
                                }

                                if (HasReplacement)
                                {
                                    string LayersVersion = ValuesList["VERSION"];
                                    if (string.IsNullOrWhiteSpace(LayersVersion))
                                    {
                                        LayersVersion = "0";
                                    }
                                    if (!int.TryParse(LayersVersion, out int IntLayerVersion))
                                    {
                                        IntLayerVersion = 0;
                                    }
                                    ValuesList["VERSION"] = IntLayerVersion++.ToString();
                                }
                            }
                        }
                        string Values = '\'' + string.Join("','", ValuesList.Values) + '\'';
                        Values = Values.Replace("'NULL'", "NULL");
                        string InsertCommand = $"INSERT INTO 'LAYERS' ({Collumns.Trim(',')}) VALUES ({Values});";

                        SQLExecute.AppendLine(InsertCommand);
                    }
                }
            }

            int user_version = ExecuteDirectScalarSQLCommand("PRAGMA user_version;");
            SQLExecute.AppendLine($"PRAGMA user_version={user_version + 1};");
            SQLExecute.AppendLine("COMMIT;");

            if (FilePath.EndsWith(".txt"))
            {
                using (FileStream fs = File.Create(FilePath))
                {
                    string dataasstring = SQLExecute.ToString();
                    byte[] info = new UTF8Encoding(true).GetBytes(dataasstring);
                    fs.Write(info, 0, info.Length);
                    byte[] data = new byte[] { 0x0 };
                    fs.Write(data, 0, data.Length);
                }
            }
            else
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
                using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + FilePath + "; Version = 3; New = True; Compress = True;"))
                {
                    sqlite_conn.Open();
                    using (SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand())
                    {
                        sqlite_cmd.CommandText = SQLExecute.ToString();
                        sqlite_cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
