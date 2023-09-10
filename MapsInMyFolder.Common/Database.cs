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

                ContentDialogResult result = await Message.SetContentDialog(Languages.GetWithArguments("databaseMessageNotFoundAskDownload", database_pathname), Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesNoRetry).ShowAsync();

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
            //DB_CreateTables(database_pathname).Dispose();
            SQLiteConnection connection = DB_OpenConnection(database_pathname);

            DB_CreateTables(connection);
            RefreshPanels.Invoke(null, EventArgs.Empty);
            await CheckIfNewerVersionAvailable();
        }

        private static async Task<bool> DB_DownloadFile(string database_url, string database_pathname)
        {
            if (string.IsNullOrEmpty(database_url) || string.IsNullOrEmpty(database_pathname))
            {
                return false;
            }
            string NotificationMsg = Languages.GetWithArguments("databaseNotificationStartDownloading", new Uri(database_url).Host);
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
                        int UserVersion = ExecuteScalarSQLCommand("PRAGMA user_version");
                        XMLParser.Cache.Write("dbVersion", UserVersion.ToString());
                        XMLParser.Cache.WriteAttribute("dbVersion", "dbSha", githubAssets?.Sha);
                        RefreshPanels.Invoke(null, EventArgs.Empty);
                        return;
                    }

                    ContentDialogResult ErrorDetectedWantToAbord = await Message.SetContentDialog(Languages.Current["databaseMessageDownloadingErrorUnknown"], Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.RetryCancel).ShowAsync();
                    if (ErrorDetectedWantToAbord != ContentDialogResult.Primary)
                    {
                        break;
                    }
                }
                else
                {
                    //print network not available
                    ContentDialogResult NoConnexionDetectedWantToAbord = await Message.SetContentDialog(Languages.Current["databaseMessageDownloadingErrorNoConnexion"], Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesNo).ShowAsync();

                    if (NoConnexionDetectedWantToAbord != ContentDialogResult.Primary)
                    {
                        break;
                    }
                }
            }
            CreateEmptyDatabase(database_pathname);
        }

        static public (SQLiteDataReader Reader, SQLiteConnection conn) ExecuteExecuteReaderSQLCommand(string querry)
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
                        return (null, null);
                    }
                    using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
                    {
                        sqlite_cmd.CommandText = querry;
                        return (sqlite_cmd.ExecuteReader(), conn);
                    }
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
            return (null, null);
        }

        static public int ExecuteScalarSQLCommand(string querry)
        {
            using SQLiteConnection conn = DB_Connection();
            return ExecuteScalarSQLCommand(conn, querry);
        }

        static public int ExecuteScalarSQLCommand(SQLiteConnection conn, string querry)
        {

            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return -1;
            }
            using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
            {
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

        }


        static public int ExecuteNonQuerySQLCommand(string querry)
        {
            using (SQLiteConnection conn = DB_Connection())
            {
                return ExecuteNonQuerySQLCommand(conn, querry);
            }
        }

        static public int ExecuteNonQuerySQLCommand(SQLiteConnection conn, string querry)
        {
            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return -1;
            }
            using (SQLiteCommand sqlite_cmd = conn.CreateCommand())
            {
                sqlite_cmd.CommandText = querry;
                return sqlite_cmd.ExecuteNonQuery();
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
                    return null;
                }
                if (sqlite_datareader.IsDBNull(ordinal))
                {
                    return null;
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
                return null;
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
            SQLiteConnection connection = DB_OpenConnection(dbFile);

            DB_CreateTables(connection);
            return connection;
        }

        public static SQLiteConnection DB_OpenConnection(string datasource)
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
            return sqlite_conn;
        }

        public static void DB_CreateTables(SQLiteConnection sqlite_conn)
        {
            if (sqlite_conn == null)
            {
                return;
            }
            using (SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand())
            {
                const string commande_arg = "'ID' INTEGER UNIQUE, 'NAME' TEXT DEFAULT '', 'DESCRIPTION' TEXT DEFAULT '', 'CATEGORY' TEXT DEFAULT '','COUNTRY' TEXT DEFAULT '', 'IDENTIFIER' TEXT DEFAULT '', 'TILE_URL' TEXT DEFAULT '', 'MIN_ZOOM' INTEGER DEFAULT '', 'MAX_ZOOM' INTEGER DEFAULT '', 'FORMAT' TEXT DEFAULT '', 'SITE' TEXT DEFAULT '', 'SITE_URL' TEXT DEFAULT '', 'STYLE' TEXT DEFAULT '', 'TILE_SIZE' INTEGER DEFAULT '', 'FAVORITE' INTEGER DEFAULT 0, 'SCRIPT' TEXT DEFAULT '','VISIBILITY' TEXT DEFAULT '' ,'SPECIALSOPTIONS' TEXT DEFAULT '','RECTANGLES' TEXT DEFAULT '', 'VERSION' INTEGER DEFAULT 1, 'HAS_SCALE' INTEGER DEFAULT 1";
                sqlite_cmd.CommandText = $@"
                CREATE TABLE IF NOT EXISTS 'CUSTOMSLAYERS' ({commande_arg});
                CREATE TABLE IF NOT EXISTS 'LAYERS' ({commande_arg},PRIMARY KEY('ID' AUTOINCREMENT));
                CREATE TABLE IF NOT EXISTS 'EDITEDLAYERS' ({commande_arg});
                ";
                sqlite_cmd.ExecuteNonQuery();
            }
        }

        public static int DB_Download_Init()
        {
            try
            {
                return ExecuteNonQuerySQLCommand("CREATE TABLE IF NOT EXISTS 'DOWNLOADS' ('ID' INTEGER NOT NULL UNIQUE,'STATE' TEXT,'INFOS' TEXT,'FILE_NAME' TEXT,'NBR_TILES' INTEGER,'ZOOM' INTEGER,'NO_LAT' REAL,'NO_LONG' REAL,'SE_LAT' REAL,'SE_LONG' REAL,'LAYER_ID' INTEGER,'TEMP_DIRECTORY' TEXT,'SAVE_DIRECTORY' TEXT,'TIMESTAMP' TEXT,'QUALITY' INTEGER,'RESIZEWIDTH' INTEGER,'RESIZEHEIGHT' INTEGER,'COLORINTERPRETATION' TEXT,'SCALEINFO' TEXT,'VARCONTEXTE' TEXT,PRIMARY KEY('ID' AUTOINCREMENT));");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Une erreur s'est produite au niveau de la base de donnée.\n" + e.Message);
                return -1;
            }
        }

        public static int DB_Download_Write(Status STATE, string FILE_NAME, int NBR_TILES, int ZOOM, double NO_LAT, double NO_LONG, double SE_LAT, double SE_LONG, int LAYER_ID, string TEMP_DIRECTORY, string SAVE_DIRECTORY, string TIMESTAMP, int QUALITY, int RESIZEWIDTH, int RESIZEHEIGHT, string COLORINTERPRETATION, string SCALEINFO, string VARCONTEXTE)
        {
            if (DB_Download_Init() == -1)
            {
                return -1;
            }

            string InsertCommandText = $"INSERT INTO 'DOWNLOADS'('STATE','INFOS', 'FILE_NAME', 'NBR_TILES', 'ZOOM', 'NO_LAT', 'NO_LONG', 'SE_LAT', 'SE_LONG', 'LAYER_ID', 'TEMP_DIRECTORY', 'SAVE_DIRECTORY','TIMESTAMP','QUALITY','RESIZEWIDTH','RESIZEHEIGHT', 'COLORINTERPRETATION', 'SCALEINFO', 'VARCONTEXTE') VALUES('{STATE}','','{Collectif.HTMLEntities(FILE_NAME)}','{NBR_TILES}','{ZOOM}','{NO_LAT}','{NO_LONG}','{SE_LAT}','{SE_LONG}','{LAYER_ID}','{Collectif.HTMLEntities(TEMP_DIRECTORY)}','{Collectif.HTMLEntities(SAVE_DIRECTORY)}','{TIMESTAMP}','{QUALITY}','{RESIZEWIDTH}','{RESIZEHEIGHT}','{COLORINTERPRETATION}','{SCALEINFO}', '{Collectif.HTMLEntities(VARCONTEXTE)}');";
            //Make sur that select last_insert_rowid() is launch just after insert
            var DatabaseConnexion = DB_Connection();
            ExecuteNonQuerySQLCommand(DatabaseConnexion, InsertCommandText);
            return ExecuteScalarSQLCommand(DatabaseConnexion, "select last_insert_rowid() from DOWNLOADS");
        }

        public static void DB_Download_Update(int bdid, string ROW, string value)
        {
            try
            {
                string UpdateCommandText = "UPDATE 'DOWNLOADS' SET '" + ROW + "'='" + value.Replace("\"", "") + "' WHERE ID=" + bdid;
                ExecuteNonQuerySQLCommand(UpdateCommandText);
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
                string DeleteCommandText = "DELETE FROM 'DOWNLOADS' WHERE ID=" + Math.Abs(bdid);
                ExecuteNonQuerySQLCommand(DeleteCommandText);
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
            int UserVersion = ExecuteScalarSQLCommand("PRAGMA user_version");
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
            string databaseFileName = Path.GetFileName(Settings.database_pathname);
            if (string.IsNullOrWhiteSpace(databaseFileName))
            {
                databaseFileName = "database";
            }
            string downloadedDatabasePath = Path.Combine(Settings.temp_folder, "Github" + databaseFileName);
            Collectif.HttpClientDownloadWithProgress client = new Collectif.HttpClientDownloadWithProgress(githubAssets.Download_url, downloadedDatabasePath);
            await client.StartDownload().ConfigureAwait(false);
            int GithubDatabaseVersion;

            using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + downloadedDatabasePath + "; Version = 3; New = True; Compress = True; "))
            {
                sqlite_conn.Open();
                using (SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "PRAGMA user_version";
                    GithubDatabaseVersion = Convert.ToInt32(sqlite_cmd.ExecuteScalar());
                }
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
            string databaseFileName = Path.GetFileName(Settings.database_pathname);
            if (string.IsNullOrWhiteSpace(databaseFileName))
            {
                databaseFileName = "database";
            }
            string downloadedDatabasePath = Path.Combine(Settings.temp_folder, "Github" + databaseFileName);
            bool IsUpdateSuccessful = await DB_DownloadFile(githubAssets.Download_url, downloadedDatabasePath);
            if (IsUpdateSuccessful)
            {
                string ExistingDatabasePath = Path.Combine(Settings.working_folder, Settings.database_pathname);
                if (File.Exists(ExistingDatabasePath)) {
                    string backupFolderPath = Path.Combine(Settings.working_folder, "databaseBackup");
                    Directory.CreateDirectory(backupFolderPath);
                    string dateFormatee = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                    File.Copy(ExistingDatabasePath, Path.Combine(backupFolderPath, $"olddb_{dateFormatee}.db"));
                }


                MergeDatabase(downloadedDatabasePath);
                using (SQLiteConnection sqlite_conn = new SQLiteConnection("Data Source=" + downloadedDatabasePath + "; Version = 3; New = True; Compress = True;"))
                {
                    sqlite_conn.Open();
                    using (SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand())
                    {
                        sqlite_cmd.CommandText = "PRAGMA user_version;";
                        XMLParser.Cache.Write("dbVersion", sqlite_cmd.ExecuteScalar().ToString());
                        XMLParser.Cache.WriteAttribute("dbVersion", "dbSha", githubAssets?.Sha);
                    }
                }
                Notification ApplicationUpdateNotification = new NText(Languages.Current["databaseNotificationUpdateSuccess"], "MapsInMyFolder", "MainPage")
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
                using (SQLiteCommand sqlite_cmd = sqlite_conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "PRAGMA user_version;";
                    string user_version = sqlite_cmd.ExecuteScalar().ToString();
                    Sql.Append($"PRAGMA user_version={user_version};");
                    SQLiteDataReader sqlite_datareader;
                    sqlite_cmd.CommandText = "SELECT sql FROM 'main'.'sqlite_master' WHERE name = 'LAYERS';";
                    using (sqlite_datareader = sqlite_cmd.ExecuteReader())
                    {
                        sqlite_datareader.Read();
                        Sql.Append(string.Concat(sqlite_datareader.GetString(0), ";"));
                    }
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
            using (SQLiteDataReader editedlayers_sqlite_datareader = ExecuteExecuteReaderSQLCommand("SELECT * FROM 'EDITEDLAYERS'").Reader)
            {
                StringBuilder SQLExecute = new StringBuilder();
                SQLExecute.Append("BEGIN TRANSACTION;");
                while (editedlayers_sqlite_datareader.Read())
                {
                    int DB_Layer_ID = (int)editedlayers_sqlite_datareader.GetIntFromOrdinal("ID");
                    int EditedDB_VERSION = editedlayers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    string EditedDB_SCRIPT = editedlayers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    string EditedDB_TILE_URL = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");

                    using (SQLiteDataReader layers_sqlite_datareader = ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'LAYERS' WHERE ID = {DB_Layer_ID}").Reader)
                    {
                        layers_sqlite_datareader.Read();
                        int LastDB_VERSION = layers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                        string LastDB_SCRIPT = layers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                        string LastDB_TILE_URL = layers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                        bool VersionCanBeUpdated = true;
                        if (LastDB_VERSION <= EditedDB_VERSION)
                        {
                            VersionCanBeUpdated = false;
                        }
                        else if (LastDB_SCRIPT != EditedDB_SCRIPT && LastDB_TILE_URL != EditedDB_TILE_URL)
                        {
                            VersionCanBeUpdated = string.IsNullOrWhiteSpace(EditedDB_SCRIPT) && string.IsNullOrWhiteSpace(EditedDB_TILE_URL);
                        }

                        if (VersionCanBeUpdated)
                        {
                            SQLExecute.Append($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}' WHERE ID = {DB_Layer_ID};");
                        }
                    }
                }
                SQLExecute.Append("COMMIT;");
                ExecuteNonQuerySQLCommand(SQLExecute.ToString());
            }
        }

        public static void CheckForMissingCollumns()
        {
            Debug.WriteLine("Check for missing collums");
            Dictionary<string, List<string>> map = new Dictionary<string, List<string>>
            {
                { "LAYERS", new List<string>() },
                { "EDITEDLAYERS", new List<string>() },
                { "CUSTOMSLAYERS", new List<string>() }
            };

            foreach (string key in map.Keys)
            {
                using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"PRAGMA table_info({key})").Reader)
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
                using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"PRAGMA table_info({key})").Reader)
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

            using (SQLiteDataReader sqlite_datareader = ExecuteExecuteReaderSQLCommand("SELECT sql FROM 'main'.'sqlite_master' WHERE name = 'LAYERS';").Reader)
            {
                sqlite_datareader.Read();
                SQLExecute.AppendLine(string.Concat(sqlite_datareader.GetString(0) + ";"));
            }

            Dictionary<string, string> TableCollumsNames = new Dictionary<string, string>();
            using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"PRAGMA table_info(LAYERS)").Reader)
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
                using (SQLiteDataReader LayersTables = ExecuteExecuteReaderSQLCommand($"SELECT * FROM '{Table}';").Reader)
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

                        using (SQLiteDataReader EditedLayersTables = ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'EDITEDLAYERS' WHERE ID={RowId};").Reader)
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

                                    ValuesList["VERSION"] = (IntLayerVersion + 1).ToString();
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

            int user_version = ExecuteScalarSQLCommand("PRAGMA user_version;");
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
