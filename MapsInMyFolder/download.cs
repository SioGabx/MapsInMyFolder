using CefSharp;
using MapsInMyFolder.Commun;
using NetVips;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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


    public class Download_Options
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

        public Download_Options(int id_layer, string save_path, string format, string filename, string identifiant, string name, int tile_size, int zoom, int quality, string urlbase, MapControl.Location NO_PIN_Location, MapControl.Location SE_PIN_Location, int RedimWidth, int RedimHeignt, Enums.Interpretation interpretation, ScaleInfo scaleInfo)
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

    public class DownloadClass
    {
        public int id;
        public int dbid;
        public int layerid;
        public List<Url_class> urls;
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
        public TileGenerator TileLoaderGenerator;
        public Enums.Interpretation interpretation;
        public ScaleInfo scaleInfo;

        public int SkippedPanelUpdate;
        public string last_command;
        public string last_command_non_important;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1068:Les paramètres CancellationToken doivent venir en dernier", Justification = "it is safe to suppress a warning from this rule to avoid a breaking change and it more readible")]
        public DownloadClass(int id,
                                          int dbid,
                                          int layerid,
                                          List<Url_class> urls,
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
                                          int RedimWidth, int RedimHeignt, TileGenerator TileLoaderGenerator,
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
                this.TileLoaderGenerator = TileLoaderGenerator;
                this.interpretation = interpretation;
                this.scaleInfo = scaleInfo;

                SkippedPanelUpdate = 0;
                last_command = String.Empty;
                last_command_non_important = String.Empty;
            }
        }

        public static List<Dictionary<int, DownloadClass>> Engine_list { get; set; } = new List<Dictionary<int, DownloadClass>>();
        public static int Number_of_download { get; set; } = 0;

        /// <summary>
        /// Ajoute une download_main_engine_class dans la liste des engines accessible
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static int Add(DownloadClass engine, int id)
        {
            int number_of_engine_in_list = Engine_list.Count + 1;
            //Dictionary<int, Download_main_engine_class> temp_dictionnary = new Dictionary<int, Download_main_engine_class>
            //{
            //    { id, engine }
            //};
            //Download_engine_list.engine_list.Add(temp_dictionnary);
            Engine_list.Add(new Dictionary<int, DownloadClass> { { id, engine } });
            return number_of_engine_in_list;
        }

        public static int GetId()
        {
            return Engine_list.Count + 1;
        }

        public static List<DownloadClass> GetEngineList()
        {
            List<DownloadClass> EngineList = new List<DownloadClass>();

            foreach (Dictionary<int, DownloadClass> engine_dic in Engine_list)
            {
                DownloadClass value = engine_dic.Values.First();
                EngineList.Add(value);
                /*
                foreach (Download_main_engine_class val in Download_engine_list.engine_list.Values[0])
                {
                    EngineList.Add(val);
                }*/
            }
            return EngineList;
        }

        /// <summary>
        /// return the engine_class with the id give
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DownloadClass GetEngineById(int id)
        {
            foreach (Dictionary<int, DownloadClass> engine_dic in Engine_list)
            {
                try
                {
                    if (engine_dic.Keys.First() == id)
                    {
                        DownloadClass return_download_main_engine_class = engine_dic[id];
                        return return_download_main_engine_class;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Erreur : l'id de la tache n'existe pas.");
                }
            }
            return null;
        }
    }

    public partial class MainPage : System.Windows.Controls.Page
    {
        private void Start_Download_Click(object sender, RoutedEventArgs e)
        {
            mapSelectable.CleanRectangleLocations();
            MainWindow._instance.FrameLoad_PrepareDownload();
        }
    }

    public partial class MainWindow : Window
    {
        void CheckifMultipleDownloadInProgress()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                if (Settings.max_download_project_in_parralele > 1)
                {
                    int nbr_engine_progress = 0;
                    DownloadClass.GetEngineList().ForEach(engine =>
                    {
                        if (nbr_engine_progress > 1 && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                        {
                            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                            return;
                        }
                        else
                        {
                            if (engine.state == Status.progress)
                            {
                                DebugMode.WriteLine("-------> " + engine.state.ToString());
                                nbr_engine_progress++;
                            }
                        }
                    });

                    if (nbr_engine_progress > 1 && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Indeterminate)
                    {
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                        return;
                    }
                    else
                    {
                        if (nbr_engine_progress < 2 && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Normal)
                        {
                            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                        }
                    }
                }
                else
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }, null);
        }


        static void CheckIfReadyToStartDownloadAfterNetworkChange()
        {
            CheckIfReadyToStartDownload();
        }



        static void CheckIfReadyToStartDownload()
        {
            int settings_max_download_simult = Settings.max_download_project_in_parralele;
            if (DownloadClass.Number_of_download < settings_max_download_simult)
            {
                string listofid = "";
                int number_of_download_started = 0;
                foreach (DownloadClass engine in DownloadClass.GetEngineList())
                {
                    listofid = listofid + engine.id + " : " + engine.state.ToString() + "\n";
                    if (number_of_download_started < settings_max_download_simult)
                    {
                        if (engine.state == Status.waitfordownloading)
                        {
                            number_of_download_started++;
                            if (Network.IsNetworkAvailable())
                            {

                                _instance.RestartDownload(engine.id);
                                Debug.WriteLine("Start " + engine.id);
                            }
                            else
                            {
                                DebugMode.WriteLine("Aucune connexion internet");
                                UpdateDownloadPanel(engine.id, "En attente de connexion internet", "", true, Status.progress);
                            }
                        }
                    }
                }
                Debug.WriteLine("-------------");
                Debug.WriteLine(listofid);
                Debug.WriteLine("-------------");

                //CheckIfNetworkAvailable if false then we wait for and raise an event
                Network.IsNetworkAvailable();
            }
        }

        public void PrepareDownloadBeforeStart(Download_Options download_Options)
        {
            CheckifMultipleDownloadInProgress();
            MainPage.Download_panel_open();
            MainPage.download_panel_browser?.ExecuteScriptAsync("document.getElementById(\"main\").scrollIntoView({ behavior: \"smooth\", block: \"start\", inline: \"nearest\"})");
            Download_Options download_Options_edited = download_Options;
            download_Options_edited.id_layer = Layers.Curent.class_id;
            download_Options_edited.identifiant = Layers.Curent.class_identifiant;
            download_Options_edited.name = Layers.Curent.class_name;
            download_Options_edited.tile_size = Layers.Curent.class_tiles_size ?? 256;
            download_Options_edited.urlbase = Layers.Curent.class_tile_url;
            StartDownload(download_Options_edited);
        }

        void StartDownload(Download_Options download_Options)
        {
            int downloadid = DownloadClass.GetId();
            string format = Layers.Curent.class_format;
            string final_saveformat = download_Options.format;
            int z = download_Options.zoom;
            int quality = download_Options.quality;
            string filetempname = "file_id=" + downloadid + "." + final_saveformat;
            string filename = download_Options.filename;

            if (!Path.HasExtension(filename))
            {
                filename = filename + "." + final_saveformat;
            }

            string save_directory = download_Options.save_path.Replace(filename, "");

            string identifiant = download_Options.identifiant;
            string layername = download_Options.name;
            int tile_size = download_Options.tile_size;
            string save_temp_directory = Collectif.GetSaveTempDirectory(layername, identifiant, z, Settings.temp_folder);

            string urlbase = download_Options.urlbase;
            if (urlbase.Trim() != "" && tile_size != 0)
            {
                var NO_tile = Collectif.CoordonneesToTile(download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, z);
                var SE_tile = Collectif.CoordonneesToTile(download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, z);
                int lat_tile_number = Math.Abs(SE_tile.X - NO_tile.X) + 1;
                int long_tile_number = Math.Abs(SE_tile.Y - NO_tile.Y) + 1;
                int nbr_of_tiles = lat_tile_number * long_tile_number;
                if (lat_tile_number * tile_size >= 65500 || long_tile_number * tile_size >= 65500)
                {
                    Message.NoReturnBoxAsync("Impossible de télécharger la zone, l'image générée ne peux pas faire plus de 65000 pixel de largeur / hauteur", "Erreur");
                    return;
                }
                Dictionary<string, double> location = new Dictionary<string, double>
                {
                    { "NO_Latitude", download_Options.NO_PIN_Location.Latitude },
                    { "NO_Longitude", download_Options.NO_PIN_Location.Longitude },
                    { "SE_Latitude", download_Options.SE_PIN_Location.Latitude },
                    { "SE_Longitude", download_Options.SE_PIN_Location.Longitude }
                };

                List<Url_class> urls = Collectif.GetUrl.GetListOfUrlFromLocation(location, z, urlbase, Layers.Curent.class_id, downloadid);
                CancellationTokenSource tokenSource2 = new CancellationTokenSource();
                CancellationToken ct = tokenSource2.Token;
                string timestamp = Convert.ToString(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());

                string JsonScaleInfo = Newtonsoft.Json.JsonConvert.SerializeObject(download_Options.scaleInfo);
                int dbid = Database.DB_Download_Write(Status.waitfordownloading, filename, nbr_of_tiles, z, download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, download_Options.id_layer, save_temp_directory, save_directory, timestamp, download_Options.quality, download_Options.RedimWidth, download_Options.RedimHeignt, download_Options.interpretation.ToString(), JsonScaleInfo);
                DownloadClass engine = new DownloadClass(downloadid, dbid, Layers.Curent.class_id, urls, tokenSource2, ct, format, final_saveformat, z, save_temp_directory, save_directory, filename, filetempname, location, download_Options.RedimWidth, download_Options.RedimHeignt, new TileGenerator(), download_Options.interpretation, download_Options.scaleInfo, nbr_of_tiles, urlbase, identifiant, Status.waitfordownloading, tile_size, nbr_of_tiles, quality);
                DownloadClass.Add(engine, downloadid);

                Status status;
                string info;
                if (Network.IsNetworkAvailable())
                {
                    info = "En attente... (0/" + nbr_of_tiles + ")";
                    status = Status.progress;
                }
                else
                {
                    info = "En attente d'une connexion internet";
                    status = Status.noconnection;
                }
                string commande_add = "add_download(" + downloadid + @",""" + status.ToString() + @""",""" + filename + @""",0," + nbr_of_tiles + @",""" + info + @""",""" + "recent" + @""");";
                MainPage.download_panel_browser?.ExecuteScriptAsync(commande_add);
                if (DownloadClass.GetEngineById(downloadid) != engine)
                {
                    Message.NoReturnBoxAsync("Intégrité rompu de l'engine de téléchargement. Veuillez relancer l'application (erreur fatale)", "Erreur");
                }
                CheckIfReadyToStartDownload();
            }
            else
            {
                Message.NoReturnBoxAsync("Impossible de télécharger la carte car une erreur s'est produite, verifier la base de données...", "Erreur");
            }
        }

        public static void AbordAndCancelWithTokenDownload(int engine_id)
        {
            DownloadClass engine = DownloadClass.GetEngineById(engine_id);
            if (engine.state != Status.error)
            {
                engine.state = Status.pause;
            }
            engine.cancellation_token = engine.cancellation_token_source.Token;
            CancellationTokenSource canceltocken = engine.cancellation_token_source;
            canceltocken.Cancel();
            CheckIfReadyToStartDownload();
        }
        public static void StopingDownload(int engine_id)
        {
            DownloadClass engine = DownloadClass.GetEngineById(engine_id);
            string info = engine.nbr_of_tiles - engine.nbr_of_tiles_waiting_for_downloading + "/" + engine.nbr_of_tiles;
            UpdateDownloadPanel(engine_id, "En pause... (" + info + ")", "", true, Status.pause);
            AbordAndCancelWithTokenDownload(engine_id);
        }

        public static void CancelDownload(int engine_id)
        {
            DownloadClass engine = DownloadClass.GetEngineById(engine_id);
            if (engine.state != Status.error)
            {
                engine.state = Status.cancel;
            }
            UpdateDownloadPanel(engine_id, "Annulé...", "", true, Status.cancel);

            AbordAndCancelWithTokenDownload(engine_id);
        }

        public void RestartDownloadFromZero(int engine_id)
        {
            DownloadClass engine = DownloadClass.GetEngineById(engine_id);
            engine.nbr_of_tiles_waiting_for_downloading = engine.nbr_of_tiles;

            RestartDownload(engine_id);
        }

        public void RestartDownload(int engine_id)
        {
            if (!Network.IsNetworkAvailable())
            {
                UpdateDownloadPanel(engine_id, "En attente d'une connexion internet", state: Status.noconnection);
            }
            DownloadClass engine = DownloadClass.GetEngineById(engine_id);
            engine.state = Status.waitfordownloading;

            string info = engine.nbr_of_tiles - engine.nbr_of_tiles_waiting_for_downloading + "/" + engine.nbr_of_tiles;

            UpdateDownloadPanel(engine_id, "Reprise... (" + info + ")", "", true, Status.progress);

            if (engine.urls is null || engine.urls.Count == 0)
            {
                UpdateDownloadPanel(engine_id, "Generation des urls...", "", true, Status.progress);
                engine.urls = Collectif.GetUrl.GetListOfUrlFromLocation(engine.location, engine.zoom, engine.urlbase, engine.layerid, engine.id);
            }
            foreach (Commun.Url_class url in engine.urls)
            {
                url.status = Status.waitfordownloading;
            }
            engine.cancellation_token_source = new CancellationTokenSource();
            engine.cancellation_token = engine.cancellation_token_source.Token;
            if (DownloadClass.Number_of_download < Settings.max_download_project_in_parralele)
            {
                if (Network.IsNetworkAvailable())
                {
                    UpdateDownloadPanel(engine_id, "Verification de l'intégritée...", "0", true, Status.no_data);
                }
                else
                {
                    UpdateDownloadPanel(engine_id, "En attente d'une connexion internet", "0", true, Status.noconnection);
                }
                DownloadThisEngine(engine);
            }
            else
            {
                if (Network.IsNetworkAvailable())
                {
                    UpdateDownloadPanel(engine_id, "En attente... (" + info + ")", "", true, Status.progress);
                }
                else
                {
                    UpdateDownloadPanel(engine_id, "En attente d'une connexion internet", "0", true, Status.noconnection);
                }
            }
        }


        async void DownloadThisEngine(DownloadClass download_main_engine_class_args)
        {
            DownloadClass.Number_of_download++;
            download_main_engine_class_args.state = Status.progress;
            CheckIfReadyToStartDownload();

            if (Settings.max_download_tiles_in_parralele == 0)
            {
                Settings.max_download_tiles_in_parralele = 1;
            }

            int settings_max_retry_download = Settings.max_retry_download;
            int nbr_pass = 0;
            do
            {
                //UpdateDownloadPanel(download_main_engine_class_args.id, "Démarage du téléchargement...", "0", true);
                nbr_pass++;
                if (nbr_pass != 1)
                {
                    UpdateDownloadPanel(download_main_engine_class_args.id, "Erreur, tentative de téléchargement " + nbr_pass + "/" + settings_max_retry_download + " ...", isimportant: true, state: Status.progress);
                }
                await ParallelDownloadTilesTask(download_main_engine_class_args);
                //DebugMode.WriteLine("check_download_complete() =" + CheckDownloadIsComplete(download_main_engine_class_args));
            } while ((CheckDownloadIsComplete(download_main_engine_class_args) != 0) && (download_main_engine_class_args.state == Status.progress) && (nbr_pass < settings_max_retry_download));

            if (CheckDownloadIsComplete(download_main_engine_class_args) == 0 || Settings.generate_transparent_tiles_on_error)
            {

                await Assemblage(download_main_engine_class_args.id);
                //await Assemblage(download_main_engine_class_args.id);
            }
            else if (nbr_pass == settings_max_retry_download)
            {

                UpdateDownloadPanel(download_main_engine_class_args.id, "Erreur lors du téléchargement (" + settings_max_retry_download.ToString() + " reprises).", "100", true, Status.error);
                download_main_engine_class_args.state = Status.error;
            }
            DownloadClass.Number_of_download--;
            CheckifMultipleDownloadInProgress();
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                TaskbarItemInfo.ProgressValue = 0;
            }, null);

            CheckIfReadyToStartDownload();
        }

        private async Task ParallelDownloadTilesTask(DownloadClass DownloadEngineClass)
        {
            List<Url_class> urls = DownloadEngineClass.urls;
            CancellationTokenSource CancellationTokenSource = DownloadEngineClass.cancellation_token_source;
            CancellationToken CancellationToken = DownloadEngineClass.cancellation_token;

            await Task.Run(() =>
            {
                DebugMode.WriteLine("Téléchargement en parralele des fichiers : " + urls.Count());
                Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = Settings.max_download_tiles_in_parralele }, url =>
                {
                    bool IsNetworkNotAvailable;
                    do
                    {
                        IsNetworkNotAvailable = !Network.FastIsNetworkAvailable();

                        if (CancellationToken.IsCancellationRequested && CancellationToken.CanBeCanceled)
                        {
                            CancellationTokenSource.Cancel();
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                            {
                                TaskbarItemInfo.ProgressValue = 0;
                            }, null);
                            return;
                        }
                        if (IsNetworkNotAvailable)
                        {
                            UpdateDownloadPanel(DownloadEngineClass.id, "En attente d'une connexion internet", state: Status.noconnection);
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                            {
                                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                            }, null);
                            Thread.Sleep(250);
                        }
                    } while (IsNetworkNotAvailable);

                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    }, null);


                    DebugMode.WriteLine("start new DownloadUrlAsync");
                    DownloadUrlAsync(url).Wait();
                    DebugMode.WriteLine("end new DownloadUrlAsync");
                });
            }, CancellationTokenSource.Token);
            Debug.WriteLine("Parallel DownloadUrlAsync end");
        }

        static private int CheckDownloadIsComplete(DownloadClass DownloadEngineClass)
        {
            if (DownloadEngineClass.nbr_of_tiles_waiting_for_downloading == 0)
            {
                DebugMode.WriteLine("Verification du téléchargement...");
                UpdateDownloadPanel(DownloadEngineClass.id, "Verification du téléchargement...", "0", true, Status.progress);
                Task.Factory.StartNew(() => Thread.Sleep(500));
                int NumberOfUrlClassWaitingForDownloading = 0;
                if (DownloadEngineClass.state != Status.pause && DownloadEngineClass.state != Status.cancel)
                {
                    foreach (Url_class urclass in DownloadEngineClass.urls)
                    {
                        if ((urclass.status != Status.no_data) || !Settings.generate_transparent_tiles_on_404)
                        {
                            if (urclass.status == Status.waitfordownloading)
                            {
                                NumberOfUrlClassWaitingForDownloading++;
                            }
                            string filename = DownloadEngineClass.save_temp_directory + urclass.x + "_" + urclass.y + "." + DownloadEngineClass.format;
                            if (File.Exists(filename))
                            {
                                FileInfo filinfo = new FileInfo(filename);
                                if (filinfo.Length == 0)
                                {
                                    urclass.status = Status.waitfordownloading;
                                    DebugMode.WriteLine("Tile Taille corrompu");
                                    NumberOfUrlClassWaitingForDownloading++;
                                }
                            }
                            else
                            {
                                DebugMode.WriteLine("Le fichier n'existe pas");
                                urclass.status = Status.waitfordownloading;
                                NumberOfUrlClassWaitingForDownloading++;
                            }
                        }
                        else
                        {
                            DebugMode.WriteLine("Tuile no_data");
                        }
                    }
                }
                else
                {
                    NumberOfUrlClassWaitingForDownloading++;
                }
                DownloadEngineClass.nbr_of_tiles_waiting_for_downloading = NumberOfUrlClassWaitingForDownloading;
                return NumberOfUrlClassWaitingForDownloading;
            }
            return DownloadEngineClass.nbr_of_tiles_waiting_for_downloading;
        }



        static void DownloadFinish(int id)
        {
            DownloadClass curent_engine = DownloadClass.GetEngineById(id);
            if (curent_engine.state != Status.error)
            {
                curent_engine.state = Status.success;
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                UpdateDownloadPanel(id, "Terminé.", "100", true, Status.success);
            }, null);
        }

        public static async void UpdateDownloadPanel(int id, string info = "", string progress = "", bool isimportant = false, Status state = Status.no_data)
        {
            DownloadClass Engine = DownloadClass.GetEngineById(id);
            if (!string.IsNullOrEmpty(info) && isimportant)
            {
                Debug.WriteLine("> " + info);
            }
            if (Engine.state == Status.error)
            {
                DebugMode.WriteLine("Cancel " + state.ToString() + " " + info);
                return;
            }
            info = info.Replace("'", "ʼ");

            string commande_executer = "";
            if (info != "" && info is not null)
            {
                commande_executer += "updateinfos(" + id + @", """ + info + @""", """ + isimportant + @""");";
            }
            if (progress != "")
            {
                commande_executer += "updateprogress(" + id + @", """ + progress + @""");";
            }
            if (state != Status.no_data)
            {
                commande_executer += "updatestate(" + id + @", """ + state.ToString() + @""");";
                Database.DB_Download_Update(Engine.dbid, "STATE", state.ToString());
            }
            if (state == Status.error)
            {
                Database.DB_Download_Update(Engine.dbid, "INFOS", info);
                Engine.state = Status.error;
                AbordAndCancelWithTokenDownload(id);
                commande_executer += "updateprogress(" + id + @", ""100"");";
            }

            Engine.SkippedPanelUpdate++;
            int update_rate = (int)Math.Floor(Math.Pow(Settings.max_download_tiles_in_parralele / 10, 1.5));
            if (update_rate < 1)
            {
                update_rate = 1;
            }
            if (Settings.max_download_tiles_in_parralele - update_rate > 100)
            {
                update_rate = Settings.max_download_tiles_in_parralele;
            }
            if (state == Status.no_data && Engine.SkippedPanelUpdate != update_rate) { return; }
            Engine.SkippedPanelUpdate--;
            if (!string.IsNullOrEmpty(commande_executer) && commande_executer != Engine.last_command && commande_executer != Engine.last_command_non_important)
            {
                Engine.SkippedPanelUpdate = 0;
                try
                {
                    await Task.Run(async () =>
                    {
                        Engine.last_command = commande_executer;
                        if (!isimportant)
                        {
                            Engine.last_command_non_important = commande_executer;
                        }
                        if (_instance.MainPage.download_panel_browser is null) { return; }
                        await _instance.MainPage.download_panel_browser.GetMainFrame().EvaluateScriptAsync(commande_executer);
                        if (isimportant)
                        {
                            Thread.Sleep(250);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Erreur UpdateDownloadPanel :" + ex.Message);
                }
            }
        }

        NetVips.Image AddGraphicalScale(NetVips.Image image, ScaleInfo scaleInfo)
        {
            if (scaleInfo.doDrawScale)
            {
                int PixelLength = (int)Math.Round(scaleInfo.drawScalePixelLength);
                var BackgroundColor = new double[] { 255d, 255d, 255d, 200d };
                var LineFirstPart = new double[] { 0d, 0d, 0d };
                var LineSecondPart = new double[] { 128d, 128d, 128d };
                int height = 20;
                int margin = 5;
                string font = "Segoe UI";
                int fontDPI = 80;
                int LineHeight = 2;

                using NetVips.Image Ltext = NetVips.Image.Text("0", font, 50, null, NetVips.Enums.Align.Centre, true, fontDPI, 0, null, true);
                using NetVips.Image Rtext = NetVips.Image.Text(scaleInfo.drawScaleEchelle.ToString() + "m", font, 50, null, NetVips.Enums.Align.Centre, true, fontDPI, 0, null, true);
                int width = PixelLength + Ltext.Width + Rtext.Width + margin * 4;

                using NetVips.Image ScaleBackground = NetVips.Image.Black(width, height, 4).NewFromImage(BackgroundColor);
                using NetVips.Image ScaleBackgroundSrgb = ScaleBackground.Copy(interpretation: Enums.Interpretation.Srgb);
                using NetVips.Image ScaleBackgroundSrgbWithLtext = ScaleBackgroundSrgb.Composite2(Ltext, NetVips.Enums.BlendMode.Over, margin, (int)Math.Round((double)height / 2 - (double)Ltext.Height / 2));
                using Image ScaleBackgroundSrgbWithRtext = ScaleBackgroundSrgbWithLtext.Composite2(Rtext, NetVips.Enums.BlendMode.Over, ScaleBackgroundSrgb.Width - Rtext.Width - margin, (int)Math.Round((double)height / 2 - (double)Rtext.Height / 2));
                using Image LineBase = NetVips.Image.Black(PixelLength, LineHeight);
                using Image Line = LineBase.NewFromImage(LineFirstPart);
                using Image LineBase2 = NetVips.Image.Black((int)Math.Round((double)PixelLength / 2), LineHeight);
                using Image Line2 = LineBase2.NewFromImage(LineSecondPart);

                using Image FinalLine = Line.Insert(Line2, Line.Width - (int)Math.Round((double)PixelLength / 2), 0, false);

                using Image ScaleBackgroundWidthAllElements = ScaleBackgroundSrgbWithRtext.Composite2(FinalLine, NetVips.Enums.BlendMode.Atop, 2 * margin + Ltext.Width, (int)Math.Round((double)height / 2 - (double)FinalLine.Height / 2));

                NetVips.Image ImageWidthScale = image.Composite(ScaleBackgroundWidthAllElements, NetVips.Enums.BlendMode.Over, margin, image.Height - (height + margin), Enums.Interpretation.Srgb, false);
                Debug.WriteLine("End Here");
                return ImageWidthScale;
            }
            else
            {
                return image;
            }
        }


        async Task Assemblage(int id)
        {
            UpdateDownloadPanel(id, "Assemblage...  1/2", "0", true, Status.assemblage);
            DownloadClass curent_engine = DownloadClass.GetEngineById(id);
            string format = curent_engine.format;
            string save_directory = curent_engine.save_directory;
            string save_temp_filename = curent_engine.file_temp_name;
            string save_filename = curent_engine.file_name;
            int tile_size = curent_engine.tile_size;

            var rognage_info = RognageInfo.GetRognageValue(curent_engine.location["NO_Latitude"], curent_engine.location["NO_Longitude"], curent_engine.location["SE_Latitude"], curent_engine.location["SE_Longitude"], curent_engine.zoom, tile_size);

            await Task.Run(() =>
            {
                NetVips.Cache.MaxMem = 0;
                NetVips.Cache.Trace = false;
                if (curent_engine.state == Status.error) { return; }

                using NetVips.Image image = GetImageFromTiles(curent_engine);
                if (image == null) { return; }
                UpdateDownloadPanel(id, "Rognage...", "0", true, Status.rognage);
                Cache.MaxFiles = 0;

                using (NetVips.Image ImageRognerBase = Image.Black(rognage_info.width, rognage_info.height))
                using (NetVips.Image ImageRogner = ImageRognerBase.Insert(image, -rognage_info.NO_decalage.X, -rognage_info.NO_decalage.Y))
                using (NetVips.Image ImageRedime = RedimImage(curent_engine, ImageRogner, rognage_info.width, rognage_info.height))
                using (NetVips.Image ImageWithScale = AddGraphicalScale(ImageRedime, curent_engine.scaleInfo))
                {
                    if (curent_engine.state == Status.error) { return; }
                    SaveImage(curent_engine, ImageWithScale);
                    UpdateDownloadPanel(id, "Libération des ressources..", "100", true, Status.cleanup);
                }

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


        static private NetVips.Image RedimImage(DownloadClass curent_engine, NetVips.Image image_rogner, double width, double height)
        {
            try
            {
                if (curent_engine.RedimWidth != -1 && curent_engine.RedimHeignt != -1)
                {
                    UpdateDownloadPanel(curent_engine.id, "Redimensionnement...", "0", true, Status.rognage);
                    double hrink = curent_engine.RedimHeignt / height;
                    double Vrink = curent_engine.RedimWidth / width;

                    if ((curent_engine.RedimHeignt == Math.Round(height * Vrink)) || (curent_engine.RedimWidth == Math.Round(width * hrink)))
                    {
                        DebugMode.WriteLine("Uniform resizing");
                        return image_rogner.Resize(hrink);
                    }
                    else
                    {
                        DebugMode.WriteLine("Deform resizing");
                        return image_rogner.ThumbnailImage(curent_engine.RedimWidth, curent_engine.RedimHeignt, size: Enums.Size.Force);
                    }
                }
            }
            catch (Exception)
            {
                UpdateDownloadPanel(curent_engine.id, "Erreur redimensionnement du fichier", "", true, Status.error);
            }
            return image_rogner;
        }

        private void SaveImage(DownloadClass curent_engine, NetVips.Image image_rogner)
        {
            int tile_size = curent_engine.tile_size;
            string save_temp_directory = curent_engine.save_temp_directory;
            string save_directory = curent_engine.save_directory;
            string save_temp_filename = curent_engine.file_temp_name;
            string save_filename = curent_engine.file_name;

            UpdateDownloadPanel(curent_engine.id, "Enregistrement...", "0", true, Status.enregistrement);
            Thread.Sleep(500);
            var progress = new Progress<int>(percent => UpdateDownloadPanel(curent_engine.id, "", Convert.ToString(percent)));
            image_rogner.SetProgress(progress);
            string image_temps_assemblage_path = save_temp_directory + save_temp_filename;

            if (Directory.Exists(save_temp_directory))
            {
                if (File.Exists(image_temps_assemblage_path))
                {
                    File.Delete(image_temps_assemblage_path);
                }
            }
            else
            {
                Directory.CreateDirectory(save_temp_directory);
            }

            try
            {
                image_rogner.WriteToFile(image_temps_assemblage_path, Collectif.getSaveVOption(curent_engine.final_saveformat, curent_engine.quality, tile_size));
                //image_rogner.Jpegsave(image_temps_assemblage_path, 100, null, false, false, false, false, false, null, Enums.ForeignSubsample.Off, null, true, null, null);
            }
            catch (Exception ex)
            {
                UpdateDownloadPanel(curent_engine.id, "Erreur enregistrement du fichier", "", true, Status.error);
                Debug.WriteLine("Erreur enregistrement du fichier" + ex.Message);
            }
            if (File.Exists(image_temps_assemblage_path))
            {
                UpdateDownloadPanel(curent_engine.id, "Déplacement..", "", true, Status.progress);
                System.IO.FileInfo assemblage_image_file_info = new System.IO.FileInfo(image_temps_assemblage_path);
                if (Directory.Exists(save_directory))
                {
                    string targetFilePath = save_directory + save_filename;
                    if (File.Exists(save_directory + save_filename))
                    {
                        File.Delete(save_directory + save_filename);
                        foreach (DownloadClass eng in DownloadClass.GetEngineList())
                        {
                            string engineFilePath = eng.save_directory + eng.file_name;
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
                    Directory.CreateDirectory(save_directory);
                }
                string FinalFilePath = save_directory + save_filename;
                if (Directory.Exists(save_directory))
                {
                    if (File.Exists(FinalFilePath))
                    {
                        File.Delete(FinalFilePath);
                    }
                    assemblage_image_file_info.MoveTo(FinalFilePath);
                }

            }
            else
            {
                //MessageBox.Show("Une erreur fatale est survenu lors de l'assemblage du fichier \"" + save_filename + "\"");
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync("Une erreur fatale est survenu lors de l'assemblage du fichier \"" + save_filename + "\"", "Erreur");
                }, null);
            }
        }

        private NetVips.Image GetImageFromTiles(DownloadClass curent_engine)
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
            List<NetVips.Image> Vertical_Array = new List<NetVips.Image>();
            for (int decalage_boucle_for_y = 0; decalage_boucle_for_y <= decalage_y; decalage_boucle_for_y++)
            {
                int tuile_x = NO_x;
                int tuile_y = NO_y + decalage_boucle_for_y;
                string filename = save_temp_directory + tuile_x + "_" + tuile_y + "." + format;
                NetVips.Image tempsimage = Image.Black(1, 1);
                List<NetVips.Image> Horizontal_Array = new List<NetVips.Image>();
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
                            //tempsimage = NetVips.Image.Black(settings_tile_size, settings_tile_size,4) + new double[] { 255, 255, 255, 255 };
                            tempsimage = Image.Black(tile_size, tile_size) + new double[] { 0, 0, 0, 0 };
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                            UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage P2", "", true, state: Status.error);
                            return null;
                        }
                    }
                    Horizontal_Array.Add(tempsimage);
                }

                NetVips.Image tempArrayJoinImage;
                try
                {
                    tempArrayJoinImage = Image.Arrayjoin(Horizontal_Array.ToArray(), background: new double[] { 255, 255, 255, 255 });
                    foreach (NetVips.Image imgtodispose in Horizontal_Array)
                    {
                        imgtodispose.Dispose();
                    }
                    Horizontal_Array.Clear();
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

                Vertical_Array.Add(tempArrayJoinImage);
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
                #region fix_dpi_issue_if_vertical_array_is_completly_black
                double max_res = 0;
                for (int i = 0; i < Vertical_Array.Count; i++)
                {
                    if (Vertical_Array[i].Xres > max_res)
                    {
                        max_res = Vertical_Array[i].Xres;
                    }
                }

                for (int i = 0; i < Vertical_Array.Count; i++)
                {
                    if (Vertical_Array[i].Xres != max_res)
                    {
                        Vertical_Array[i] = Image.Black(tile_size, tile_size);
                    }
                }
                #endregion
                image = Image.Arrayjoin(Vertical_Array.ToArray(), across: 1);
                //NetVips.Image image = Image.Arrayjoin(Vertical_Array.ToArray(), across: 1);

                Vertical_Array.Clear();
                //return image;



            }
            catch (Exception ex)
            {
                UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage vertical", "", true, Status.error);
                Debug.WriteLine(ex.Message);
            }
            //Vertical_Array.Clear();
            return image;
            //return Image.Black((decalage_x * tile_size) + 1, 1);
        }


        void InternalUpdateProgressBar(DownloadClass download_engine)
        {
            int number_of_url_class_waiting_for_downloading = 0;
            foreach (Url_class urclass in download_engine.urls)
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
                        CheckifMultipleDownloadInProgress();
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

        public async Task DownloadUrlAsync(Url_class url)
        {
            string id = url.x + "/" + url.y;
            DownloadClass download_engine = DownloadClass.GetEngineById(url.downloadid);
            int z = download_engine.zoom;
            string format = download_engine.format;
            string save_temp_directory = download_engine.save_temp_directory;
            string filename = url.x + "_" + url.y + "." + format;
            Boolean do_download_this_tile = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory, filename, Settings.tiles_cache_expire_after_x_days);
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
                HttpResponse httpResponse = await TileGeneratorSettings.TileLoaderGenerator.GetImageAsync(download_engine.urlbase, url.x, url.y, url.z, download_engine.layerid, download_engine.format, save_temp_directory).ConfigureAwait(false);
                if (httpResponse.ResponseMessage.IsSuccessStatusCode)
                {
                    var contentStream = Collectif.ByteArrayToStream(httpResponse.Buffer);
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
                    if (httpResponse.ResponseMessage.StatusCode == HttpStatusCode.NotFound && (Settings.generate_transparent_tiles_on_404 || Settings.generate_transparent_tiles_on_error))
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
                    Debug.WriteLine($"Download Fail: {url.url}: {(int)httpResponse.ResponseMessage.StatusCode} {httpResponse.ResponseMessage.ReasonPhrase}");
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
    }
}