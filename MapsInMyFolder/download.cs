using CefSharp;
using MapsInMyFolder.Commun;
using NetVips;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MapsInMyFolder
{

    public class RognageInfo
    {
        public (int X, int Y) NO_decalage;
        public (int X, int Y) SE_decalage;
        public int width;
        public int height;
        public RognageInfo((int X, int Y) NO_decalage, (int X, int Y) SE_decalage, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.SE_decalage = SE_decalage;
            this.NO_decalage = NO_decalage;
        }

        public static RognageInfo GetRognageValue(double NO_Latitude, double NO_Longitude, double SE_Latitude, double SE_Longitude, int zoom, int? tile_width)
        {
            int tile_width_NotNull = tile_width ?? 256;
            (int X, int Y) GetRognageFromLocation(double Latitude, double Longitude)
            {
                var list_of_tile_number_from_given_lat_and_long = Collectif.CoordonneesToTile(Latitude, Longitude, zoom);

                var CoinsHautGaucheLocationFromTile = Collectif.TileToCoordonnees(list_of_tile_number_from_given_lat_and_long.X, list_of_tile_number_from_given_lat_and_long.Y, zoom);
                double longitude_coins_haut_gauche_curent_tileX = CoinsHautGaucheLocationFromTile.Longitude;
                double latitude_coins_haut_gauche_curent_tileY = CoinsHautGaucheLocationFromTile.Latitude;

                var NextCoinsHautGaucheLocationFromTile = Collectif.TileToCoordonnees(list_of_tile_number_from_given_lat_and_long.X + 1, list_of_tile_number_from_given_lat_and_long.Y + 1, zoom);
                double longitude_coins_haut_gauche_next_tileX = NextCoinsHautGaucheLocationFromTile.Longitude;
                double latitude_coins_haut_gauche_next_tileY = NextCoinsHautGaucheLocationFromTile.Latitude;

                double longitude_decalage = Math.Abs(Longitude - longitude_coins_haut_gauche_curent_tileX) * 100 / Math.Abs(longitude_coins_haut_gauche_curent_tileX - longitude_coins_haut_gauche_next_tileX) / 100;
                double latitude_decalage = Math.Abs(Latitude - latitude_coins_haut_gauche_curent_tileY) * 100 / Math.Abs(latitude_coins_haut_gauche_curent_tileY - latitude_coins_haut_gauche_next_tileY) / 100;
                int decalage_x = Math.Abs(Convert.ToInt32(Math.Round(longitude_decalage * tile_width_NotNull, 0)));
                int decalage_y = Math.Abs(Convert.ToInt32(Math.Round(latitude_decalage * tile_width_NotNull, 0)));
                return (decalage_x, decalage_y);
            }

            var NO_decalage = GetRognageFromLocation(NO_Latitude, NO_Longitude);
            var SE_decalage = GetRognageFromLocation(SE_Latitude, SE_Longitude);
            int NbrtilesInCol = Collectif.CoordonneesToTile(SE_Latitude, SE_Longitude, zoom).X - Collectif.CoordonneesToTile(NO_Latitude, NO_Longitude, zoom).X + 1;
            int NbrtilesInRow = Collectif.CoordonneesToTile(SE_Latitude, SE_Longitude, zoom).Y - Collectif.CoordonneesToTile(NO_Latitude, NO_Longitude, zoom).Y + 1;
            int final_image_width = Math.Abs((NbrtilesInCol * tile_width_NotNull) - (NO_decalage.X + (tile_width_NotNull - SE_decalage.X)));
            int final_image_height = Math.Abs((NbrtilesInRow * tile_width_NotNull) - (NO_decalage.Y + (tile_width_NotNull - SE_decalage.Y)));
            if (final_image_width < 10 || final_image_height < 10)
            {
                final_image_width = 10;
                final_image_height = 10;
            }

            return new RognageInfo(NO_decalage, SE_decalage, final_image_width, final_image_height);

        }
    }


    public class DownloadOptions
    {
        public int id_layer;
        public string save_path;
        public string format;
        public string filename;
        public string identifiant;
        public string name;
        public int tile_size;
        public int quality;
        public int zoom;
        public string urlbase;
        public MapControl.Location NO_PIN_Location;
        public MapControl.Location SE_PIN_Location;
        public int RedimWidth;
        public int RedimHeignt;
        public Enums.Interpretation interpretation;
        public ScaleInfo scaleInfo;

        public DownloadOptions(int id_layer, string save_path, string format, string filename, string identifiant, string name, int tile_size, int zoom, int quality, string urlbase, MapControl.Location NO_PIN_Location, MapControl.Location SE_PIN_Location, int RedimWidth, int RedimHeignt, Enums.Interpretation interpretation, ScaleInfo scaleInfo)
        {
            this.id_layer = id_layer;
            this.save_path = save_path;
            this.format = format;
            this.filename = filename;
            this.identifiant = identifiant;
            this.name = name;
            this.tile_size = tile_size;
            this.zoom = zoom;
            this.quality = quality;
            this.urlbase = urlbase;
            this.NO_PIN_Location = NO_PIN_Location;
            this.SE_PIN_Location = SE_PIN_Location;
            this.RedimWidth = RedimWidth;
            this.RedimHeignt = RedimHeignt;
            this.interpretation = interpretation;
            this.scaleInfo = scaleInfo;
        }
    }

    public class DownloadSettings
    {
        public int id;
        public int dbid;
        public int layerid;
        public List<TilesUrl> urls;
        public CancellationTokenSource cancellation_token_source;
        public CancellationToken cancellation_token;
        public string format;
        public string final_saveformat;
        public int zoom;
        public Status state;
        public int nbr_of_tiles;
        public int nbr_of_tiles_waiting_for_downloading;
        public string urlbase;
        public string identifiant;
        public string save_temp_directory;
        public string save_directory;
        public string file_name;
        public string file_temp_name;
        public int tile_size;
        public int quality;
        public Dictionary<string, double> location;
        public int RedimWidth;
        public int RedimHeignt;
        public TileLoader TileLoader;
        public Enums.Interpretation interpretation;
        public ScaleInfo scaleInfo;

        public int SkippedPanelUpdate;
        public string last_command;
        public string last_command_non_important;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:Les paramètres CancellationToken doivent venir en dernier", Justification = "it is safe to suppress a warning from this rule to avoid a breaking change and it more readible")]
        public DownloadSettings(int id,
                                          int dbid,
                                          int layerid,
                                          List<TilesUrl> urls,
                                          CancellationTokenSource cancellation_token_source,
                                          CancellationToken cancellation_token,
                                          string format,
                                          string final_saveformat,
                                          int zoom,
                                          string save_temp_directory,
                                          string save_directory,
                                          string file_name,
                                          string file_temp_name,
                                          Dictionary<string, double> location,
                                          int RedimWidth, int RedimHeignt, TileLoader TileLoaderGenerator,
                                          Enums.Interpretation interpretation,
                                          ScaleInfo scaleInfo,
                                          int nbr_of_tiles = 0,
                                          string urlbase = "",
                                          string identifiant = "",
                                          Status state = Status.waitfordownloading,
                                          int? tile_size = null,
                                          int nbr_of_tiles_waiting_for_downloading = 0,
                                          int quality = 100)
        {
            if (id != 0)
            {
                this.file_temp_name = file_temp_name;
                this.file_name = file_name;
                this.state = state;
                this.save_temp_directory = save_temp_directory;
                this.save_directory = save_directory;
                this.format = format;
                this.final_saveformat = final_saveformat;
                this.zoom = zoom;
                this.id = id;
                this.layerid = layerid;
                this.dbid = dbid;
                this.urls = urls;
                this.cancellation_token_source = cancellation_token_source;
                this.cancellation_token = cancellation_token;
                this.nbr_of_tiles = nbr_of_tiles;
                this.nbr_of_tiles_waiting_for_downloading = nbr_of_tiles_waiting_for_downloading;
                this.urlbase = urlbase;
                this.identifiant = identifiant;
                this.location = location;
                this.tile_size = tile_size ?? 256;
                this.quality = quality;
                this.RedimWidth = RedimWidth;
                this.RedimHeignt = RedimHeignt;
                this.TileLoader = TileLoaderGenerator;
                this.interpretation = interpretation;
                this.scaleInfo = scaleInfo;

                SkippedPanelUpdate = 0;
                last_command = String.Empty;
                last_command_non_important = String.Empty;
            }
        }

        public static Dictionary<int, DownloadSettings> DownloadEngineDictionnary { get; set; } = new Dictionary<int, DownloadSettings>();
        public static int CurrentNumberOfDownload { get; set; } = 0;

        public static int Add(DownloadSettings engine, int id)
        {
            DownloadEngineDictionnary.Add(id, engine);
            return DownloadEngineDictionnary.Count;
        }

        public static void Clear()
        {
            DownloadEngineDictionnary.Clear();
        }

        public static int GetId()
        {
            return DownloadEngineDictionnary.Count + 1;
        }

        public static DownloadSettings[] GetEngineList()
        {
            return DownloadEngineDictionnary.Values.ToArray();
        }

        public static DownloadSettings GetEngineById(int id)
        {
            if (DownloadEngineDictionnary.TryGetValue(id, out DownloadSettings engine))
            {
                return engine;
            }
            else
            {
                Console.WriteLine("Erreur : l'ID de la tâche n'existe pas.");
                return null;
            }
        }

    }

    public partial class MainWindow : Window
    {
        void CheckifMultipleDownloadInProgress()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                if (Settings.max_download_project_in_parralele == 1)
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
                else
                {
                    int nbr_engine_progress = 0;
                    foreach (var _ in DownloadSettings.GetEngineList().Where(engine => !(engine.state == Status.cancel || engine.state == Status.pause || engine.state == Status.error || engine.state == Status.success)))
                    {
                        nbr_engine_progress++;
                    }

                    if (nbr_engine_progress > 1 && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                    {
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                    }
                    else if (nbr_engine_progress <= 1 && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Normal)
                    {
                        TaskbarItemInfo.ProgressValue = 0;
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    }
                }
            }, null);
        }


        static void NetworkIsBack()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                _instance.MainPage.RefreshMap();
                _instance.MainPage.LayerTilePreview_RequestUpdate();
            }, null);

            CheckIfReadyToStartDownload();
        }

        static void CheckIfReadyToStartDownload()
        {
            int maxSimultaneousDownloads = Settings.max_download_project_in_parralele;
            int numDownloadsStarted = 0;

            foreach (DownloadSettings engine in DownloadSettings.GetEngineList())
            {
                if (numDownloadsStarted >= maxSimultaneousDownloads)
                    break;

                if (engine.state == Status.waitfordownloading)
                {
                    if (Network.IsNetworkAvailable())
                    {
                        _instance.RestartDownload(engine.id);
                        Debug.WriteLine("Start " + engine.id);
                        numDownloadsStarted++;
                    }
                    else
                    {
                        DebugMode.WriteLine("Aucune connexion internet");
                        UpdateDownloadPanel(engine.id, "En attente de connexion internet", "", true, Status.progress);
                    }
                }
            }

            Debug.WriteLine("-------------");
            Debug.WriteLine(GetListOfIdsAndStates());
            Debug.WriteLine("-------------");

            Network.IsNetworkAvailable();
        }

        static string GetListOfIdsAndStates()
        {
            StringBuilder sb = new StringBuilder();
            foreach (DownloadSettings engine in DownloadSettings.GetEngineList())
            {
                sb.AppendLine($"{engine.id} : {engine.state}");
            }
            return sb.ToString();
        }


        public void PrepareDownloadBeforeStart(DownloadOptions download_Options)
        {
            CheckifMultipleDownloadInProgress();
            MainPage.Download_panel_open();
            MainPage.download_panel_browser?.ExecuteScriptAsync("document.getElementById(\"main\").scrollIntoView({ behavior: \"smooth\", block: \"start\", inline: \"nearest\"})");
            DownloadOptions download_Options_edited = download_Options;
            download_Options_edited.id_layer = Layers.Current.class_id;
            download_Options_edited.identifiant = Layers.Current.class_identifiant;
            download_Options_edited.name = Layers.Current.class_name;
            download_Options_edited.tile_size = Layers.Current.class_tiles_size ?? 256;
            download_Options_edited.urlbase = Layers.Current.class_tile_url;
            StartDownload(download_Options_edited);
        }
        void StartDownload(DownloadOptions download_Options)
        {
            int downloadId = DownloadSettings.GetId();
            string format = Layers.Current.class_format;
            string finalSaveFormat = download_Options.format;
            int zoom = download_Options.zoom;
            int quality = download_Options.quality;
            string fileTempName = "file_id=" + downloadId + "." + finalSaveFormat;
            string filename = Path.HasExtension(download_Options.filename) ? download_Options.filename : download_Options.filename + "." + finalSaveFormat;
            string saveDirectory = download_Options.save_path.Replace(filename, "");
            string identifiant = download_Options.identifiant;
            string layername = download_Options.name;
            int tileSize = download_Options.tile_size;
            string saveTempDirectory = Collectif.GetSaveTempDirectory(layername, identifiant, zoom, Settings.temp_folder);
            string urlbase = download_Options.urlbase;

            if (urlbase.Trim() != "" && tileSize != 0)
            {
                var NO_tile = Collectif.CoordonneesToTile(download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, zoom);
                var SE_tile = Collectif.CoordonneesToTile(download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, zoom);
                int latTileNumber = Math.Abs(SE_tile.X - NO_tile.X) + 1;
                int longTileNumber = Math.Abs(SE_tile.Y - NO_tile.Y) + 1;
                int nbrOfTiles = latTileNumber * longTileNumber;

                if (latTileNumber * tileSize >= 65500 || longTileNumber * tileSize >= 65500)
                {
                    Message.NoReturnBoxAsync("Impossible de télécharger la zone, l'image générée ne peut pas faire plus de 65000 pixels de largeur / hauteur", "Erreur");
                    return;
                }

                Dictionary<string, double> location = new Dictionary<string, double>
        {
            { "NO_Latitude", download_Options.NO_PIN_Location.Latitude },
            { "NO_Longitude", download_Options.NO_PIN_Location.Longitude },
            { "SE_Latitude", download_Options.SE_PIN_Location.Latitude },
            { "SE_Longitude", download_Options.SE_PIN_Location.Longitude }
        };

                List<TilesUrl> urls = Collectif.GetUrl.GetListOfUrlFromLocation(location, zoom, urlbase, Layers.Current.class_id, downloadId);
                CancellationTokenSource tokenSource2 = new CancellationTokenSource();
                CancellationToken ct = tokenSource2.Token;
                string timestamp = Convert.ToString(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());

                string jsonScaleInfo = Newtonsoft.Json.JsonConvert.SerializeObject(download_Options.scaleInfo);
                int dbid = Database.DB_Download_Write(Status.waitfordownloading, filename, nbrOfTiles, zoom, download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, download_Options.id_layer, saveTempDirectory, saveDirectory, timestamp, quality, download_Options.RedimWidth, download_Options.RedimHeignt, download_Options.interpretation.ToString(), jsonScaleInfo);
                DownloadSettings engine = new DownloadSettings(downloadId, dbid, Layers.Current.class_id, urls, tokenSource2, ct, format, finalSaveFormat, zoom, saveTempDirectory, saveDirectory, filename, fileTempName, location, download_Options.RedimWidth, download_Options.RedimHeignt, new TileLoader(), download_Options.interpretation, download_Options.scaleInfo, nbrOfTiles, urlbase, identifiant, Status.waitfordownloading, tileSize, nbrOfTiles, quality);
                DownloadSettings.Add(engine, downloadId);

                Status status;
                string info;

                if (Network.IsNetworkAvailable())
                {
                    info = "En attente... (0/" + nbrOfTiles + ")";
                    status = Status.progress;
                }
                else
                {
                    info = "En attente d'une connexion internet";
                    status = Status.noconnection;
                }

                string commandAdd = $"add_download({downloadId}, '{status}', '{filename}', 0, {nbrOfTiles}, '{info}', 'recent')";
                MainPage.download_panel_browser?.ExecuteScriptAsync(commandAdd);

                CheckIfReadyToStartDownload();
            }
            else
            {
                Message.NoReturnBoxAsync("Impossible de télécharger la carte car une erreur s'est produite, vérifiez la base de données...", "Erreur");
            }
        }

        public static void AbordAndCancelWithTokenDownload(int engineId)
        {
            DownloadSettings engine = DownloadSettings.GetEngineById(engineId);

            if (engine.state != Status.error)
            {
                engine.state = Status.pause;
            }

            //engine.cancellation_token = engine.cancellation_token_source.Token;
            CancellationTokenSource canceltocken = engine.cancellation_token_source;
            canceltocken.Cancel();
            CheckIfReadyToStartDownload();
        }

        public static void StopingDownload(int engineId)
        {
            DownloadSettings engine = DownloadSettings.GetEngineById(engineId);
            string info = $"{engine.nbr_of_tiles - engine.nbr_of_tiles_waiting_for_downloading}/{engine.nbr_of_tiles}";
            UpdateDownloadPanel(engineId, $"En pause... ({info})", "", true, Status.pause);
            AbordAndCancelWithTokenDownload(engineId);
        }

        public static void CancelDownload(int engineId)
        {
            DownloadSettings engine = DownloadSettings.GetEngineById(engineId);
            UpdateDownloadPanel(engineId, "Annulé...", "", true, Status.cancel);
            AbordAndCancelWithTokenDownload(engineId);
        }

        public void RestartDownloadFromZero(int engineId)
        {
            DownloadSettings engine = DownloadSettings.GetEngineById(engineId);
            engine.nbr_of_tiles_waiting_for_downloading = engine.nbr_of_tiles;
            RestartDownload(engineId);
        }

        public bool CheckNetworkAvailable(int engineId, string progress = "0", bool isImportant = true)
        {
            if (!Network.IsNetworkAvailable())
            {
                UpdateDownloadPanel(engineId, "En attente d'une connexion internet", state: Status.noconnection);
                return false;
            }
            return true;
        }

        public void RestartDownload(int engineId)
        {
            CheckNetworkAvailable(engineId);
            DownloadSettings engine = DownloadSettings.GetEngineById(engineId);
            engine.state = Status.waitfordownloading;
            CheckifMultipleDownloadInProgress();

            string info = $"{engine.nbr_of_tiles - engine.nbr_of_tiles_waiting_for_downloading}/{engine.nbr_of_tiles}";
            UpdateDownloadPanel(engineId, $"Reprise... ({info})", "", true, Status.progress);

            if (engine.urls is null || engine.urls.Count == 0)
            {
                UpdateDownloadPanel(engineId, "Génération des URLs...", "", true, Status.progress);
                engine.urls = Collectif.GetUrl.GetListOfUrlFromLocation(engine.location, engine.zoom, engine.urlbase, engine.layerid, engine.id);
            }

            engine.urls.ForEach(url => url.status = Status.waitfordownloading);

            engine.cancellation_token_source?.Cancel();
            engine.cancellation_token_source = new CancellationTokenSource();
            engine.cancellation_token = engine.cancellation_token_source.Token;

            if (DownloadSettings.CurrentNumberOfDownload < Settings.max_download_project_in_parralele && CheckNetworkAvailable(engineId, "0", true))
            {
                UpdateDownloadPanel(engineId, "Vérification de l'intégrité...", "0", true, Status.no_data);
                DownloadThisEngine(engine);
            }
            else
            {
                UpdateDownloadPanel(engineId, $"En attente... ({info})", "", true, Status.progress);
            }
        }

        async void DownloadThisEngine(DownloadSettings downloadEngineClassArgs)
        {
            DownloadSettings.CurrentNumberOfDownload++;
            downloadEngineClassArgs.state = Status.progress;
            CheckIfReadyToStartDownload();

            if (Settings.max_download_tiles_in_parralele == 0)
            {
                Settings.max_download_tiles_in_parralele = 1;
            }

            int settingsMaxRetryDownload = Settings.max_retry_download;
            int nbrPass = 0;

            do
            {
                nbrPass++;

                if (nbrPass != 1)
                {
                    UpdateDownloadPanel(downloadEngineClassArgs.id, $"Erreur, tentative de téléchargement {nbrPass}/{settingsMaxRetryDownload} ...", isImportant: true, state: Status.progress);
                }

                await ParallelDownloadTilesTask(downloadEngineClassArgs);

            } while ((CheckDownloadIsComplete(downloadEngineClassArgs) != 0) && (downloadEngineClassArgs.state == Status.progress) && (nbrPass < settingsMaxRetryDownload));

            if (CheckDownloadIsComplete(downloadEngineClassArgs) == 0 || Settings.generate_transparent_tiles_on_error)
            {
                await Assemblage(downloadEngineClassArgs.id);
            }
            else if (nbrPass == settingsMaxRetryDownload)
            {
                UpdateDownloadPanel(downloadEngineClassArgs.id, $"Erreur lors du téléchargement ({settingsMaxRetryDownload} reprises).", "100", true, Status.error);
                downloadEngineClassArgs.state = Status.error;
            }

            DownloadSettings.CurrentNumberOfDownload--;
            CheckifMultipleDownloadInProgress();

            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                TaskbarItemInfo.ProgressValue = 0;
            }, null);

            CheckIfReadyToStartDownload();
        }

        private void WaitForInternet(DownloadSettings downloadEngineClass)
        {
            CancellationTokenSource cancellationTokenSource = downloadEngineClass.cancellation_token_source;
            CancellationToken cancellationToken = downloadEngineClass.cancellation_token;

            bool isNetworkAvailable;
            do
            {
                isNetworkAvailable = Network.FastIsNetworkAvailable();

                if (cancellationToken.IsCancellationRequested && cancellationToken.CanBeCanceled)
                {
                    cancellationTokenSource?.Cancel();
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        TaskbarItemInfo.ProgressValue = 0;
                    }, null);
                    return;
                }

                if (!isNetworkAvailable)
                {
                    UpdateDownloadPanel(downloadEngineClass.id, "En attente d'une connexion internet", state: Status.noconnection);
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                    }, null);
                    Thread.Sleep(500);
                }
            } while (!isNetworkAvailable);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                if (TaskbarItemInfo.ProgressState == System.Windows.Shell.TaskbarItemProgressState.Paused)
                {
                    CheckifMultipleDownloadInProgress();
                    UpdateDownloadPanel(downloadEngineClass.id, "Connecté ! Reprise du téléchargement", state: Status.progress);
                }
            }, null);
        }

        private async Task ParallelDownloadTilesTask(DownloadSettings downloadEngineClass)
        {
            List<TilesUrl> urls = downloadEngineClass.urls;
            CancellationTokenSource cancellationTokenSource = downloadEngineClass.cancellation_token_source;
            CancellationToken cancellationToken = downloadEngineClass.cancellation_token;

            await Task.Run(() =>
            {
                Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = Settings.max_download_tiles_in_parralele }, url =>
                {
                    WaitForInternet(downloadEngineClass);
                    DownloadUrlAsync(url).Wait();
                });
            }, cancellationTokenSource.Token);
        }

        static private int CheckDownloadIsComplete(DownloadSettings downloadEngineClass)
        {
            if (downloadEngineClass.nbr_of_tiles_waiting_for_downloading != 0)
            {
                return downloadEngineClass.nbr_of_tiles_waiting_for_downloading;
            }

            UpdateDownloadPanel(downloadEngineClass.id, "Vérification du téléchargement...", "0", true, Status.progress);
            Task.Delay(500).Wait();
            int numberOfUrlClassWaitingForDownloading = 0;

            if (downloadEngineClass.state == Status.pause && downloadEngineClass.state == Status.cancel)
            {
                numberOfUrlClassWaitingForDownloading++;
            }
            else
            {
                foreach (TilesUrl urlClass in downloadEngineClass.urls)
                {
                    if ((urlClass.status != Status.no_data) || !Settings.generate_transparent_tiles_on_404)
                    {
                        if (urlClass.status == Status.waitfordownloading)
                        {
                            numberOfUrlClassWaitingForDownloading++;
                        }
                        string filename = $"{downloadEngineClass.save_temp_directory}{urlClass.x}_{urlClass.y}.{downloadEngineClass.format}";

                        if (File.Exists(filename))
                        {
                            FileInfo fileInfo = new FileInfo(filename);
                            if (fileInfo.Length == 0)
                            {
                                //taille corrompu
                                urlClass.status = Status.waitfordownloading;
                                numberOfUrlClassWaitingForDownloading++;
                            }
                        }
                        else
                        {
                            //Le fichier n'existe pas
                            urlClass.status = Status.waitfordownloading;
                            numberOfUrlClassWaitingForDownloading++;
                        }
                    }
                }
            }

            downloadEngineClass.nbr_of_tiles_waiting_for_downloading = numberOfUrlClassWaitingForDownloading;
            return numberOfUrlClassWaitingForDownloading;
        }

        static void DownloadFinish(int id)
        {
            DownloadSettings currentEngine = DownloadSettings.GetEngineById(id);
            if (currentEngine.state != Status.error)
            {
                currentEngine.state = Status.success;
            }

            currentEngine.urls.Clear();
            currentEngine.SkippedPanelUpdate = 0;
            currentEngine.last_command = null;
            currentEngine.last_command_non_important = null;
            currentEngine.cancellation_token_source.Dispose();
            currentEngine.cancellation_token_source = null;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                UpdateDownloadPanel(id, "Terminé.", "100", true, Status.success);
            }, null);
        }

        public static async void UpdateDownloadPanel(int id, string info = "", string progress = "", bool isImportant = false, Status state = Status.no_data)
        {
            DownloadSettings engine = DownloadSettings.GetEngineById(id);

            if (!string.IsNullOrEmpty(info) && isImportant)
            {
                Debug.WriteLine($">{info}");
            }

            if (engine.state == Status.error)
            {
                DebugMode.WriteLine($"Cancel {state} {info}");
                return;
            }

            info = info.Replace("'", "ʼ");

            string commandExecuted = "";

            if (!string.IsNullOrEmpty(info))
            {
                commandExecuted += $"updateinfos({id}, \"{info}\", \"{isImportant}\");";
            }

            if (!string.IsNullOrEmpty(progress))
            {
                commandExecuted += $"updateprogress({id}, \"{progress}\");";
            }

            if (state != Status.no_data)
            {
                commandExecuted += $"updatestate({id}, \"{state}\");";
                Database.DB_Download_Update(engine.dbid, "STATE", state.ToString());
            }

            if (state == Status.error)
            {
                Database.DB_Download_Update(engine.dbid, "INFOS", info);
                engine.state = Status.error;
                AbordAndCancelWithTokenDownload(id);
                commandExecuted += $"updateprogress({id}, \"100\");";
            }

            engine.SkippedPanelUpdate++;
            int updateRate = (int)Math.Floor(Math.Pow(Settings.max_download_tiles_in_parralele / 10, 1.5));

            if (updateRate < 1)
            {
                updateRate = 1;
            }

            if (Settings.max_download_tiles_in_parralele - updateRate > 100)
            {
                updateRate = Settings.max_download_tiles_in_parralele;
            }

            if (state == Status.no_data && engine.SkippedPanelUpdate != updateRate)
            {
                return;
            }

            engine.SkippedPanelUpdate--;

            if (!string.IsNullOrEmpty(commandExecuted) && commandExecuted != engine.last_command && commandExecuted != engine.last_command_non_important)
            {
                engine.SkippedPanelUpdate = 0;

                await Task.Run(async () =>
                {
                    engine.last_command = commandExecuted;

                    if (!isImportant)
                    {
                        engine.last_command_non_important = commandExecuted;
                    }
                    await _instance?.MainPage?.download_panel_browser?.EvaluateScriptAsync(commandExecuted);

                    if (isImportant)
                    {
                        await Task.Delay(250);
                    }
                });
            }
        }

        Image AddGraphicalScale(NetVips.Image image, ScaleInfo scaleInfo)
        {
            if (!scaleInfo.doDrawScale)
            {
                return image;
            }

            int pixelLength = (int)Math.Round(scaleInfo.drawScalePixelLength);
            double[] backgroundColor = { 255d, 255d, 255d, 200d };
            double[] lineFirstPart = { 0d, 0d, 0d };
            double[] lineSecondPart = { 128d, 128d, 128d };
            int height = 20;
            int margin = 5;
            string font = "Segoe UI";
            int fontDPI = 80;
            int lineHeight = 2;

            using var lText = Image.Text("0", font, 50, null, Enums.Align.Centre, true, fontDPI, 0, null, true);
            using var rText = Image.Text($"{scaleInfo.drawScaleEchelle}m", font, 50, null, Enums.Align.Centre, true, fontDPI, 0, null, true);
            int width = pixelLength + lText.Width + rText.Width + margin * 4;

            using var scaleBackground = Image.Black(width, height, 4).NewFromImage(backgroundColor);
            using var scaleBackgroundSrgb = scaleBackground.Copy(interpretation: Enums.Interpretation.Srgb);
            using var scaleBackgroundSrgbWithLText = scaleBackgroundSrgb.Composite2(lText, Enums.BlendMode.Over, margin, (int)Math.Round((double)height / 2 - (double)lText.Height / 2));
            using var scaleBackgroundSrgbWithRText = scaleBackgroundSrgbWithLText.Composite2(rText, Enums.BlendMode.Over, scaleBackgroundSrgb.Width - rText.Width - margin, (int)Math.Round((double)height / 2 - (double)rText.Height / 2));
            using var lineBase = Image.Black(pixelLength, lineHeight);
            using var line = lineBase.NewFromImage(lineFirstPart);
            using var lineBase2 = Image.Black((int)Math.Round((double)pixelLength / 2), lineHeight);
            using var line2 = lineBase2.NewFromImage(lineSecondPart);

            using var finalLine = line.Insert(line2, line.Width - (int)Math.Round((double)pixelLength / 2), 0, false);

            using var scaleBackgroundWidthAllElements = scaleBackgroundSrgbWithRText.Composite2(finalLine, Enums.BlendMode.Atop, 2 * margin + lText.Width, (int)Math.Round((double)height / 2 - (double)finalLine.Height / 2));

            return image.Composite(scaleBackgroundWidthAllElements, Enums.BlendMode.Over, margin, image.Height - (height + margin), Enums.Interpretation.Srgb, false);
        }

        async Task Assemblage(int id)
        {
            UpdateDownloadPanel(id, "Assemblage...  1/2", "0", true, Status.assemblage);
            DownloadSettings currentEngine = DownloadSettings.GetEngineById(id);
            string format = currentEngine.format;
            string saveDirectory = currentEngine.save_directory;
            string saveTempFilename = currentEngine.file_temp_name;
            string saveFilename = currentEngine.file_name;
            int tileSize = currentEngine.tile_size;

            var rognageInfo = RognageInfo.GetRognageValue(
                currentEngine.location["NO_Latitude"],
                currentEngine.location["NO_Longitude"],
                currentEngine.location["SE_Latitude"],
                currentEngine.location["SE_Longitude"],
                currentEngine.zoom,
                tileSize);

            await Task.Run(() =>
            {
                Cache.MaxMem = 0;
                Cache.Trace = false;
                if (currentEngine.state == Status.error)
                {
                    return;
                }

                using var image = EngineTilesToSingleImage(currentEngine);
                if (image == null)
                {
                    return;
                }
                UpdateDownloadPanel(id, "Rognage...", "0", true, Status.rognage);
                Cache.MaxFiles = 0;

                using var imageRognerBase = Image.Black(rognageInfo.width, rognageInfo.height);
                using var imageRogner = imageRognerBase.Insert(image, -rognageInfo.NO_decalage.X, -rognageInfo.NO_decalage.Y);
                using var imageRedime = ResizeImage(currentEngine, imageRogner, rognageInfo.width, rognageInfo.height);
                using var imageWithScale = AddGraphicalScale(imageRedime, currentEngine.scaleInfo);
                if (currentEngine.state == Status.error)
                {
                    return;
                }
                SaveImage(currentEngine, imageWithScale);
                UpdateDownloadPanel(id, "Libération des ressources..", "100", true, Status.cleanup);

                Debug.WriteLine(
                    "NetVips.Cache.Size" + " : " + Cache.Size + "\n" +
                    "NetVips.Cache.Max" + " : " + Cache.Max + "\n" +
                    "NetVips.Cache.MaxMem" + " : " + Cache.MaxMem + "\n" +
                    "NetVips.Cache.MaxFiles" + " : " + Cache.MaxFiles + "\n" +
                    "NetVips.Stats.Mem" + " : " + Stats.Mem + "\n" +
                    "NetVips.Stats.Files" + " : " + Stats.Files + "\n");

                GC.Collect(9999, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            });

            UpdateDownloadPanel(id, "Finalisation...", "100", true, Status.cleanup);
            DownloadFinish(id);
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                CheckifMultipleDownloadInProgress();
                TaskbarItemInfo.ProgressValue = 0;
            }, null);
        }

        static private Image ResizeImage(DownloadSettings currentEngine, NetVips.Image imageRogner, double width, double height)
        {
            try
            {
                if (currentEngine.RedimWidth != -1 && currentEngine.RedimHeignt != -1)
                {
                    UpdateDownloadPanel(currentEngine.id, "Redimensionnement...", "0", true, Status.rognage);
                    double hrink = currentEngine.RedimHeignt / height;
                    double Vrink = currentEngine.RedimWidth / width;

                    if (currentEngine.RedimHeignt == Math.Round(height * Vrink) || currentEngine.RedimWidth == Math.Round(width * hrink))
                    {
                        DebugMode.WriteLine("Uniform resizing");
                        return imageRogner.Resize(hrink);
                    }
                    else
                    {
                        DebugMode.WriteLine("Deform resizing");
                        return imageRogner.ThumbnailImage(currentEngine.RedimWidth, currentEngine.RedimHeignt, size: Enums.Size.Force);
                    }
                }
            }
            catch (Exception)
            {
                UpdateDownloadPanel(currentEngine.id, "Erreur redimensionnement du fichier", "", true, Status.error);
            }
            return imageRogner;
        }

        private Image EngineTilesToSingleImage(DownloadSettings curent_engine)
        {
            string format = curent_engine.format;
            string save_temp_directory = curent_engine.save_temp_directory;
            string save_directory = curent_engine.save_directory;
            string save_temp_filename = curent_engine.file_temp_name;
            string save_filename = curent_engine.file_name;
            int tile_size = curent_engine.tile_size;

            var NO_tile = Collectif.CoordonneesToTile(curent_engine.location["NO_Latitude"], curent_engine.location["NO_Longitude"], curent_engine.zoom);
            var SE_tile = Collectif.CoordonneesToTile(curent_engine.location["SE_Latitude"], curent_engine.location["SE_Longitude"], curent_engine.zoom);
            int NO_x = NO_tile.X;
            int NO_y = NO_tile.Y;
            int SE_x = SE_tile.X;
            int SE_y = SE_tile.Y;
            int decalage_x = SE_x - NO_x;
            int decalage_y = SE_y - NO_y;

            Cache.Max = 0;
            Cache.MaxFiles = 0;
            Cache.MaxMem = 0;
            List<NetVips.Image> verticalArray = new List<NetVips.Image>();

            for (int decalage_boucle_for_y = 0; decalage_boucle_for_y <= decalage_y; decalage_boucle_for_y++)
            {
                int tuile_x = NO_x;
                int tuile_y = NO_y + decalage_boucle_for_y;
                string filename = save_temp_directory + tuile_x + "_" + tuile_y + "." + format;
                NetVips.Image tempsimage = Image.Black(1, 1);
                List<NetVips.Image> horizontalArray = new List<NetVips.Image>();

                for (int decalage_boucle_for_x = 0; decalage_boucle_for_x <= decalage_x; decalage_boucle_for_x++)
                {
                    tuile_x = NO_x + decalage_boucle_for_x;
                    filename = save_temp_directory + tuile_x + "_" + tuile_y + "." + format;
                    FileInfo filinfo = new FileInfo(filename);

                    if (filinfo.Exists && filinfo.Length != 0)
                    {
                        try
                        {
                            tempsimage = Image.NewFromFile(filename);

                            if (tempsimage.Width != tile_size)
                            {
                                double shrinkvalue = tile_size / tempsimage.Width;
                                tempsimage = tempsimage.Resize(shrinkvalue);
                            }

                            if (tempsimage.Bands < 3)
                            {
                                tempsimage = tempsimage.Colourspace(Enums.Interpretation.Srgb);
                            }

                            if (tempsimage.Bands == 3)
                            {
                                tempsimage = tempsimage.Bandjoin(255);
                            }

                            if (Settings.is_in_debug_mode)
                            {
                                var text = Image.Text(tuile_x + " / " + tuile_y, dpi: 150);
                                tempsimage = tempsimage.Composite2(text, Enums.BlendMode.Atop, 0, 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                            UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage P1", "", true, state: Status.error);
                            return null;
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Image not exist, generating empty tile : " + filename);
                        try
                        {
                            tempsimage = Image.Black(tile_size, tile_size) + new double[] { 0, 0, 0, 0 };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                            UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage P2", "", true, state: Status.error);
                            return null;
                        }
                    }

                    horizontalArray.Add(tempsimage);
                }

                Image tempArrayJoinImage;

                try
                {
                    tempArrayJoinImage = Image.Arrayjoin(horizontalArray.ToArray(), background: new double[] { 255, 255, 255, 255 });

                    horizontalArray.DisposeItems();
                    horizontalArray.Clear();
                }
                catch (Exception ex)
                {
                    UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage horizontal", "", true, Status.error);
                    Debug.WriteLine(ex.Message);
                    return null;
                }

                if (curent_engine.interpretation != Enums.Interpretation.Srgb)
                {
                    try
                    {
                        tempArrayJoinImage = tempArrayJoinImage.Colourspace(curent_engine.interpretation);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error while changing color interpretation to " + curent_engine.interpretation.ToString() + "\n" + ex.Message);
                    }
                }

                verticalArray.Add(tempArrayJoinImage);
                double progress_value = 0;
                double operation_pourcentage_denominateur = decalage_y * decalage_boucle_for_y;

                if (operation_pourcentage_denominateur != 0)
                {
                    progress_value = 100 / decalage_y * decalage_boucle_for_y;
                }
            }

            UpdateDownloadPanel(curent_engine.id, "Assemblage...  2/2", "0", true, Status.assemblage);
            Task.Factory.StartNew(() => Thread.Sleep(300));

            NetVips.Image image = Image.Black((decalage_x * tile_size) + 1, 1);

            try
            {
                double max_res = 0;

                for (int i = 0; i < verticalArray.Count; i++)
                {
                    if (verticalArray[i].Xres > max_res)
                    {
                        max_res = verticalArray[i].Xres;
                    }
                }

                for (int i = 0; i < verticalArray.Count; i++)
                {
                    if (verticalArray[i].Xres != max_res)
                    {
                        verticalArray[i] = Image.Black(tile_size, tile_size);
                    }
                }
                Image[] ImagesVerticalArray = verticalArray.ToArray();
                image = Image.Arrayjoin(verticalArray.ToArray(), across: 1);

                ImagesVerticalArray.DisposeItems();
                verticalArray.DisposeItems();
                verticalArray.Clear();
            }
            catch (Exception ex)
            {
                UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage vertical", "", true, Status.error);
                Debug.WriteLine(ex.Message);
            }

            return image;
        }

        void InternalUpdateProgressBar(DownloadSettings download_engine)
        {
            int number_of_url_class_waiting_for_downloading = 0;
            foreach (TilesUrl urclass in download_engine?.urls)
            {
                if (urclass.status == Status.waitfordownloading)
                {
                    number_of_url_class_waiting_for_downloading++;
                }
            }
            download_engine.nbr_of_tiles_waiting_for_downloading = number_of_url_class_waiting_for_downloading;
            double progress = (download_engine.nbr_of_tiles - number_of_url_class_waiting_for_downloading) / (double)download_engine.nbr_of_tiles;
            if (number_of_url_class_waiting_for_downloading == 0)
            {
                progress = 100;
            }
            if (Application.Current is null)
            {
                Debug.WriteLine("InternalUpdateProgressBar Erreur fatale");
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    if (number_of_url_class_waiting_for_downloading != 0)
                    {
                        //CheckifMultipleDownloadInProgress();
                    }
                    else
                    {
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                    }
                    TaskbarItemInfo.ProgressValue = (double)progress;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("porogresbarupsateerrorv1 : " + ex.Message);
                }
            }, null);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                string info = download_engine.nbr_of_tiles - number_of_url_class_waiting_for_downloading + "/" + download_engine.nbr_of_tiles;
                double progresstxt = (double)progress * 100;
                if (progresstxt > 100)
                {
                    progresstxt = 100;
                }
                UpdateDownloadPanel(download_engine.id, info, Convert.ToString(progresstxt), false, Status.no_data);
            }, null);
        }

        public async Task DownloadUrlAsync(TilesUrl url)
        {
            string id = url.x + "/" + url.y;
            DownloadSettings download_engine = DownloadSettings.GetEngineById(url.downloadid);
            int z = download_engine.zoom;
            string format = download_engine.format;
            string save_temp_directory = download_engine.save_temp_directory;
            string filename = url.x + "_" + url.y + "." + format;
            bool do_download_this_tile = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory, filename, Settings.tiles_cache_expire_after_x_days);
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            if (!do_download_this_tile)
            {
                DebugMode.WriteLine("Existing tile");
                url.status = Status.success;

                await Task.Factory.StartNew(() => Thread.Sleep(20));
                InternalUpdateProgressBar(download_engine);
                return;
            }
            try
            {
                HttpResponse httpResponse = await Tiles.Loader.GetImageAsync(download_engine.urlbase, url.x, url.y, url.z, download_engine.layerid, download_engine.format, save_temp_directory).ConfigureAwait(false);
                if (httpResponse?.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    using var contentStream = Collectif.ByteArrayToStream(httpResponse.Buffer);
                    if (httpResponse.Buffer?.Length > 0)
                    {
                        using FileStream fileStream = new FileStream(save_temp_directory + filename, FileMode.CreateNew);
                        await contentStream.CopyToAsync(fileStream);
                        if (httpResponse.ResponseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            url.status = Status.success;
                        }
                    }
                    else
                    {
                        url.status = Status.error;
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    if (httpResponse?.ResponseMessage?.StatusCode == HttpStatusCode.NotFound && (Settings.generate_transparent_tiles_on_404 || Settings.generate_transparent_tiles_on_error))
                    {
                        url.status = Status.no_data;
                    }
                    else if (!Network.IsNetworkAvailable())
                    {
                        url.status = Status.waitfordownloading;
                    }
                    else if (Settings.generate_transparent_tiles_on_error)
                    {
                        url.status = Status.no_data;
                    }
                    else
                    {
                        url.status = Status.error;
                    }
                    Debug.WriteLine($"Download Fail: {url.url}: {(int)(httpResponse?.ResponseMessage?.StatusCode ?? 0)} {httpResponse?.ResponseMessage?.ReasonPhrase}");
                    Debug.WriteLine("url.status is " + url.status);
                }
            }
            catch (Exception a)
            {
                Debug.WriteLine("Exception Download : " + a.Message);
                DebugMode.WriteLine(url.status);
            }
            finally
            {
                InternalUpdateProgressBar(download_engine);
                if (Settings.waiting_before_start_another_tile_download > 0 && download_engine.nbr_of_tiles_waiting_for_downloading > 0)
                {
                    Thread.Sleep(Settings.waiting_before_start_another_tile_download);
                    DebugMode.WriteLine("Pause...");
                }
            }

        }

        private void SaveImage(DownloadSettings currentEngine, NetVips.Image imageRogner)
        {
            int tileSize = currentEngine.tile_size;
            string saveTempDirectory = currentEngine.save_temp_directory;
            string saveDirectory = currentEngine.save_directory;
            string saveTempFilename = currentEngine.file_temp_name;
            string saveFilename = currentEngine.file_name;

            UpdateDownloadPanel(currentEngine.id, "Enregistrement...", "0", true, Status.enregistrement);
            Thread.Sleep(500);
            var progress = new Progress<int>(percent => UpdateDownloadPanel(currentEngine.id, "", Convert.ToString(percent)));
            imageRogner.SetProgress(progress);
            string imageTempsAssemblagePath = Path.Combine(saveTempDirectory, saveTempFilename);

            if (Directory.Exists(saveTempDirectory))
            {
                if (File.Exists(imageTempsAssemblagePath))
                {
                    File.Delete(imageTempsAssemblagePath);
                }
            }
            else
            {
                Directory.CreateDirectory(saveTempDirectory);
            }

            try
            {
                imageRogner.WriteToFile(imageTempsAssemblagePath, Collectif.getSaveVOption(currentEngine.final_saveformat, currentEngine.quality, tileSize));
            }
            catch (Exception ex)
            {
                UpdateDownloadPanel(currentEngine.id, "Erreur enregistrement du fichier", "", true, Status.error);
                Debug.WriteLine("Erreur enregistrement du fichier" + ex.Message);
            }
            if (File.Exists(imageTempsAssemblagePath))
            {
                UpdateDownloadPanel(currentEngine.id, "Déplacement..", "", true, Status.progress);
                string targetFilePath = Path.Combine(saveDirectory, saveFilename);
                if (Directory.Exists(saveDirectory))
                {
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(targetFilePath);
                        foreach (DownloadSettings eng in DownloadSettings.GetEngineList())
                        {
                            string engineFilePath = Path.Combine(eng.save_directory, eng.file_name);
                            if (eng.state == Status.success && engineFilePath == targetFilePath)
                            {
                                UpdateDownloadPanel(eng.id, "Remplacé", "0", true, Status.deleted);
                                Database.DB_Download_Update(eng.dbid, "INFOS", "Remplacé");
                            }
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(saveDirectory);
                }
                string finalFilePath = Path.Combine(saveDirectory, saveFilename);
                if (Directory.Exists(saveDirectory))
                {
                    if (File.Exists(finalFilePath))
                    {
                        File.Delete(finalFilePath);
                    }
                    File.Move(imageTempsAssemblagePath, finalFilePath);
                }
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync("Une erreur fatale est survenue lors de l'assemblage du fichier \"" + saveFilename + "\"", "Erreur");
                }, null);
            }
        }
    }
}