using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Net;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;
using System.IO;
using CefSharp;
using System.Windows.Threading;
using System.Net.Http;
using NetVips;
using MapsInMyFolder.Commun;

namespace MapsInMyFolder
{
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

        public Download_Options(int id_layer, string save_path, string format, string filename, string identifiant, string name, int tile_size, int zoom, int quality, string urlbase, MapControl.Location NO_PIN_Location, MapControl.Location SE_PIN_Location, int RedimWidth, int RedimHeignt)
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
                                          int nbr_of_tiles = 0,
                                          string urlbase = "",
                                          string identifiant = "",
                                          Status state = Status.waitfordownloading,
                                          int tile_size = 256,
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
                this.tile_size = tile_size;
                this.quality = quality;
                this.RedimWidth = RedimWidth;
                this.RedimHeignt = RedimHeignt;
                this.TileLoaderGenerator = TileLoaderGenerator;


                this.SkippedPanelUpdate = 0;
                this.last_command = String.Empty;
                this.last_command_non_important = String.Empty;
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
            MainWindow._instance.FrameLoad_PrepareDownload();
        }
    }

    public partial class MainWindow : Window
    {
        void CheckifMultipleDownloadInProgress()
        {
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                if (Commun.Settings.max_download_project_in_parralele > 1)
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
           //Network is back
            CheckIfReadyToStartDownload();
        }



        static void CheckIfReadyToStartDownload()
        {
            DebugMode.WriteLine("Checking... From : number : " + DownloadClass.Number_of_download);

            int settings_max_download_simult = Commun.Settings.max_download_project_in_parralele;
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

                                MainWindow._instance.RestartDownload(engine.id);
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

                Debug.WriteLine("CheckIfNetworkAvailable if false then we wait for and raise an event");
                Network.IsNetworkAvailable();
                Debug.WriteLine("Network wathcer finish");
            }
        }

        public void PrepareDownloadBeforeStart(Download_Options download_Options)
        {
            CheckifMultipleDownloadInProgress();
            TaskbarItemInfo.ProgressValue = 0;
            MainPage.Download_panel_open();
            MainPage.download_panel_browser?.ExecuteScriptAsync("document.getElementById(\"main\").scrollIntoView({ behavior: \"smooth\", block: \"start\", inline: \"nearest\"})");
            Download_Options download_Options_edited = download_Options;
            download_Options_edited.id_layer = Curent.Layer.class_id;
            download_Options_edited.identifiant = Curent.Layer.class_identifiant;
            download_Options_edited.name = Curent.Layer.class_name;
            download_Options_edited.tile_size = Curent.Layer.class_tiles_size;
            download_Options_edited.urlbase = Curent.Layer.class_tile_url;
            StartDownload(download_Options_edited);
        }

        void StartDownload(Download_Options download_Options)
        {
            //string check_path_end_with_slash(string path)
            //{
            //    string edited_path = path;
            //    if (!path.EndsWith("\\"))
            //    {
            //        edited_path += "\\";
            //    }
            //    return edited_path;
            //}
            //string Get_save_temp_directory(string identifiant, string style, int zoom)
            //{
            //    string settings_temp_folder = Settings.temp_folder;
            //    Debug.WriteLine(settings_temp_folder);
            //    settings_temp_folder = check_path_end_with_slash(settings_temp_folder);
            //    string chemin = settings_temp_folder + identifiant + "_" + style + "\\" + zoom + "\\";
            //    return chemin;
            //}

            int downloadid = DownloadClass.GetId();
            string format = Curent.Layer.class_format;
            string final_saveformat = download_Options.format;
            int z = download_Options.zoom;
            int quality = download_Options.quality;
            string filetempname = "file_id=" + downloadid + "." + final_saveformat;
            string filename = download_Options.filename;
            if (!System.IO.Path.HasExtension(filename))
            {
                filename = filename + "." + final_saveformat;
            }

            string save_directory = download_Options.save_path.Replace(filename, "");

            string identifiant = download_Options.identifiant;
            string layername = download_Options.name;
            int tile_size = download_Options.tile_size;
            string save_temp_directory = Collectif.GetSaveTempDirectory(layername, identifiant, z, Commun.Settings.temp_folder);

            string urlbase = download_Options.urlbase;
            if (urlbase.Trim() != "" && tile_size != 0)
            {
                List<int> NO_tile = Collectif.CoordonneesToTile(download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, z);
                List<int> SE_tile = Collectif.CoordonneesToTile(download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, z);
                int lat_tile_number = Math.Abs(SE_tile[0] - NO_tile[0]) + 1;
                int long_tile_number = Math.Abs(SE_tile[1] - NO_tile[1]) + 1;
                int nbr_of_tiles = lat_tile_number * long_tile_number;
                if (lat_tile_number * tile_size >= 65500 || long_tile_number * tile_size >= 65500)
                {
                    //MessageBox.Show("Impossible de télécharger la zone, l'image générée ne peux pas faire plus de 65000 pixel de largeur / hauteur");
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

                List<Url_class> urls = Collectif.GetUrl.GetListOfUrlFromLocation(location, z, urlbase, Curent.Layer.class_id, downloadid);
                //foreach(Url_class url in urls)
                //{
                //    Debug.WriteLine("patare : " +url.url);
                //}
                CancellationTokenSource tokenSource2 = new CancellationTokenSource();
                CancellationToken ct = tokenSource2.Token;
                string timestamp = Convert.ToString(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
                int dbid = Database.DB_Download_Write(Status.waitfordownloading, filename, nbr_of_tiles, z, download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, download_Options.id_layer, save_temp_directory, save_directory, timestamp, download_Options.quality, download_Options.RedimWidth, download_Options.RedimHeignt);
                DownloadClass engine = new DownloadClass(downloadid, dbid, Curent.Layer.class_id, urls, tokenSource2, ct, format, final_saveformat, z, save_temp_directory, save_directory, filename, filetempname, location, download_Options.RedimWidth, download_Options.RedimHeignt, new TileGenerator(), nbr_of_tiles, urlbase, identifiant, Status.waitfordownloading, tile_size, nbr_of_tiles, quality);
                //engine.TileLoaderGenerator.Layer = Layers.Convert.CurentLayerToLayer();
                DownloadClass.Add(engine, downloadid);

                Status status;
                string info;
                if (Network.IsNetworkAvailable())
                {
                    info = "En attente... (0/" + nbr_of_tiles + ")";
                    status =  Status.progress;
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
                    //MessageBox.Show("Intégrité rompu de l'engine de téléchargement. Veuillez relancer l'application (erreur fatale)");
                    Message.NoReturnBoxAsync("Intégrité rompu de l'engine de téléchargement. Veuillez relancer l'application (erreur fatale)", "Erreur");
                }
                CheckIfReadyToStartDownload();
            }
            else
            {
                //MessageBox.Show("Impossible de télécharger la carte car une erreur s'est produite, verifier la base de données...");
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
            if (engine.urls is null)
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
            if (DownloadClass.Number_of_download < Commun.Settings.max_download_project_in_parralele)
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
            List<Url_class> urls = download_main_engine_class_args.urls;
            CancellationTokenSource CancellationTokenSource = download_main_engine_class_args.cancellation_token_source;
            CancellationToken CancellationToken = download_main_engine_class_args.cancellation_token;
            if (Commun.Settings.max_download_tiles_in_parralele == 0)
            {
                Commun.Settings.max_download_tiles_in_parralele = 1;
            }
            async Task start_download_task()
            {
                async Task download_task()
                {
                    try
                    {
                        Task t = Task.Run(() =>
                        {
                            DebugMode.WriteLine("Téléchargement en parralele des fichiers");
                            Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = Commun.Settings.max_download_tiles_in_parralele }, url =>
                             {
                                 bool IsNetworkNotAvailable;
                                 do
                                 {
                                     IsNetworkNotAvailable = !Network.FastIsNetworkAvailable();

                                     if (CancellationToken.IsCancellationRequested)
                                     {
                                         if (CancellationToken.CanBeCanceled)
                                         {
                                             CancellationTokenSource.Cancel();
                                             App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                                             {
                                                 TaskbarItemInfo.ProgressValue = 0;
                                             }, null);
                                             return;
                                         }
                                         else
                                         {
                                             Debug.WriteLine("cant be cancel");
                                         }
                                     }
                                 if (IsNetworkNotAvailable)
                                  {
                                         UpdateDownloadPanel(download_main_engine_class_args.id, "En attente d'une connexion internet", state: Status.noconnection);
                                     App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                                     {
                                         TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                                     }, null);
                                     Thread.Sleep(250);
                                 }
                             } while (IsNetworkNotAvailable) ;

                            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                                 {
                                     TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                                 }, null);


                                 DebugMode.WriteLine("start new DownloadUrlAsync");
                                 DownloadUrlAsync(url).Wait();
                                 DebugMode.WriteLine("end new DownloadUrlAsync");
                             });
                        }, CancellationTokenSource.Token);

                        await t;
                        Debug.WriteLine("Parallel DownloadUrlAsync end");
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Téléchargement echec : " + e.Message);
                    }
                }
                try
                {
                    await download_task();
                }
                catch (OperationCanceledException e)
                {
                    Debug.WriteLine("Téléchargement annulé oce : " + e.Message);
                }
                catch (AggregateException e)
                {
                    Debug.WriteLine("Téléchargement annulé ae : " + e.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Téléchargement annulé gex : " + ex.Message);
                }
            }

            int CheckDownloadIsComplete()
            {
                if (download_main_engine_class_args.nbr_of_tiles_waiting_for_downloading == 0)
                {
                    DebugMode.WriteLine("Verification du téléchargement...");
                    UpdateDownloadPanel(download_main_engine_class_args.id, "Verification du téléchargement...", "0", true, Status.progress);
                    Task.Factory.StartNew(() => Thread.Sleep(500));
                    int number_of_url_class_waiting_for_downloading = 0;
                    if (download_main_engine_class_args.state != Status.pause && download_main_engine_class_args.state != Status.cancel)
                    {
                        foreach (Url_class urclass in download_main_engine_class_args.urls)
                        {
                            if ((urclass.status != Status.no_data) || ((Commun.Settings.generate_transparent_tiles_on_404) == false && (Commun.Settings.generate_transparent_tiles_on_error) == false))
                            {
                                if (urclass.status == Status.waitfordownloading)
                                {
                                    number_of_url_class_waiting_for_downloading++;
                                }
                                string filename = download_main_engine_class_args.save_temp_directory + urclass.x + "_" + urclass.y + "." + download_main_engine_class_args.format;
                                if (System.IO.File.Exists(filename))
                                {
                                    FileInfo filinfo = new FileInfo(filename);
                                    if (filinfo.Length == 0)
                                    {
                                        urclass.status = Status.waitfordownloading;
                                        DebugMode.WriteLine("Tile Taille corrompu");
                                        number_of_url_class_waiting_for_downloading++;
                                    }
                                }
                                else
                                {
                                    DebugMode.WriteLine("Le fichier n'existe pas");
                                    urclass.status = Status.waitfordownloading;
                                    number_of_url_class_waiting_for_downloading++;
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
                        number_of_url_class_waiting_for_downloading++;
                    }
                    download_main_engine_class_args.nbr_of_tiles_waiting_for_downloading = number_of_url_class_waiting_for_downloading;
                    return number_of_url_class_waiting_for_downloading;
                }
                return download_main_engine_class_args.nbr_of_tiles_waiting_for_downloading;
            }

            int settings_max_retry_download = Commun.Settings.max_retry_download;
            int nbr_pass = 0;
            do
            {
                //UpdateDownloadPanel(download_main_engine_class_args.id, "Démarage du téléchargement...", "0", true);
                nbr_pass++;
                if (nbr_pass != 1)
                {
                    UpdateDownloadPanel(download_main_engine_class_args.id, "Erreur, tentative de téléchargement " + nbr_pass + "/" + settings_max_retry_download + " ...", isimportant: true, state: Status.progress);
                }
                DebugMode.WriteLine("start_download_task start");
                await start_download_task();
                DebugMode.WriteLine("start_download_task ended");
                DebugMode.WriteLine("check_download_complete() =" + CheckDownloadIsComplete());
            } while ((CheckDownloadIsComplete() != 0) && (download_main_engine_class_args.state == Status.progress) && (nbr_pass < settings_max_retry_download));

            if (nbr_pass == settings_max_retry_download)
            {
                UpdateDownloadPanel(download_main_engine_class_args.id, "Erreur lors du téléchargement (" + settings_max_retry_download.ToString() + " reprises).", "100", true, Status.error);
                download_main_engine_class_args.state = Status.error;
            }

            if (CheckDownloadIsComplete() == 0)
            {
                await Assemblage(download_main_engine_class_args.id);
            }
            DownloadClass.Number_of_download--;
            CheckifMultipleDownloadInProgress();
            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                TaskbarItemInfo.ProgressValue = 0;
            }, null);

            CheckIfReadyToStartDownload();
        }

        static void DownloadFinish(int id)
        {
            DownloadClass curent_engine = DownloadClass.GetEngineById(id);
            if (curent_engine.state != Status.error)
            {
                curent_engine.state = Status.success;
            }

            //try
            //{
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                UpdateDownloadPanel(id, "Terminé.", "100", true, Status.success);
            }, null);
            //}
            //catch (Exception)
            //{
            //    Debug.WriteLine("Erreur UpdateDownloadPanel")
            //}
        }

        public static async void UpdateDownloadPanel(int id, string info = "", string progress = "", bool isimportant = false, Status state = Status.no_data)
        {
            DownloadClass Engine = DownloadClass.GetEngineById(id);

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
            int update_rate = (int)Math.Floor(Math.Pow(Commun.Settings.max_download_tiles_in_parralele / 10, 1.5));
            if (update_rate < 1)
            {
                update_rate = 1;
            }
            if (Commun.Settings.max_download_tiles_in_parralele - update_rate > 100)
            {
                update_rate = Commun.Settings.max_download_tiles_in_parralele;
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
                        if (MainWindow._instance.MainPage.download_panel_browser is null) { return; }
                        await MainWindow._instance.MainPage.download_panel_browser.GetMainFrame().EvaluateScriptAsync(commande_executer);
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

        async Task Assemblage(int id)
        {
            DebugMode.WriteLine("Assemblage");
            UpdateDownloadPanel(id, "Assemblage...  1/2", "0", true, Status.assemblage);
            DownloadClass curent_engine = DownloadClass.GetEngineById(id);
            int settings_tile_size = curent_engine.tile_size;
            List<int> NO_tile = Collectif.CoordonneesToTile(curent_engine.location["NO_Latitude"], curent_engine.location["NO_Longitude"], curent_engine.zoom);
            List<int> SE_tile = Collectif.CoordonneesToTile(curent_engine.location["SE_Latitude"], curent_engine.location["SE_Longitude"], curent_engine.zoom);
            string format = curent_engine.format;
            string save_temp_directory = curent_engine.save_temp_directory;
            string save_directory = curent_engine.save_directory;
            string save_temp_filename = curent_engine.file_temp_name;
            string save_filename = curent_engine.file_name;
            int tile_size = settings_tile_size;
            DebugMode.WriteLine(format + " ***" + save_temp_directory + " " + save_directory + " " + save_temp_filename + " " + save_filename + " " + tile_size);
            int NO_x = NO_tile[0];
            int NO_y = NO_tile[1];
            int SE_x = SE_tile[0];
            int SE_y = SE_tile[1];
            int decalage_x = SE_x - NO_x;
            int decalage_y = SE_y - NO_y;
            Dictionary<string, int> rognage_info = GetRognageValue(curent_engine.location["NO_Latitude"], curent_engine.location["NO_Longitude"], curent_engine.location["SE_Latitude"], curent_engine.location["SE_Longitude"], curent_engine.zoom, tile_size);

            await Task.Run(() =>
            {
                if (curent_engine.state == Status.error) { return; }
                NetVips.Cache.Max = 0;
                NetVips.Cache.MaxFiles = 0;
                NetVips.Cache.MaxMem = 0;
                NetVips.Image image = NetVips.Image.Black((decalage_x * settings_tile_size) + 1, 1);
                List<NetVips.Image> Vertical_Array = new List<NetVips.Image>();
                for (int decalage_boucle_for_y = 0; decalage_boucle_for_y <= decalage_y; decalage_boucle_for_y++)
                {
                    int tuile_x = NO_x;
                    int tuile_y = NO_y + decalage_boucle_for_y;
                    string filename = save_temp_directory + tuile_x + "_" + tuile_y + "." + format;
                    NetVips.Image tempsimage = NetVips.Image.Black(1, 1);
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
                                Debug.WriteLine("Loading " + filename);
                                tempsimage = NetVips.Image.NewFromFile(filename);
                                if (tempsimage.Width != settings_tile_size)
                                {
                                    double shrinkvalue = settings_tile_size / tempsimage.Width;
                                    tempsimage = tempsimage.Resize(shrinkvalue);
                                }
                                //if (tempsimage.Interpretation != Enums.Interpretation.Srgb)
                                //{
                                //    tempsimage.Colourspace(Enums.Interpretation.Srgb);
                                //}
                                if (tempsimage.Bands < 3)
                                {
                                    tempsimage = tempsimage.Colourspace(Enums.Interpretation.Srgb);
                                }
                                if (tempsimage.Bands == 3)
                                {
                                    tempsimage = tempsimage.Bandjoin(255);
                                }

                                /*
                                 * 
                                if page.bands < 3:
        page = page.colourspace("srgb")
    # make sure there's an alpha
    if page.bands == 3:
        page = page.bandjoin(255)
                                 */

                                if (Settings.is_in_debug_mode)
                                {
                                    var text = Image.Text(tuile_x + " / " + tuile_y, dpi: 150);
                                    tempsimage = tempsimage.Composite2(text, Enums.BlendMode.Atop, 0, 0);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                                //curent_engine.state = Status.error;
                                UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage P1", "", true, state: Status.error);
                                return;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Image not exist, generating empty tile : " + filename);
                            try
                            {
                                //tempsimage = NetVips.Image.Black(settings_tile_size, settings_tile_size,4) + new double[] { 255, 255, 255, 255 };
                                tempsimage = NetVips.Image.Black(settings_tile_size, settings_tile_size) + new double[] { 0,0,0,0 };
                               
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Erreur NetVips : " + ex.ToString());
                                //curent_engine.state = Status.error;
                                UpdateDownloadPanel(curent_engine.id, "Erreur fatale lors de l'assemblage P2", "", true, state: Status.error);
                                return;
                            }
                        }
                        Horizontal_Array.Add(tempsimage);
                    }
                    try
                    {

                        Vertical_Array.Add(NetVips.Image.Arrayjoin(Horizontal_Array.ToArray(), background: new double[] { 255, 255, 255, 255 }));
                        //MessageBox.Show(Vertical_Array[Vertical_Array.Count - 1].Xres.ToString());
                       // Vertical_Array[Vertical_Array.Count - 1].Resize()
                    }
                    catch (Exception ex)
                    {
                        UpdateDownloadPanel(id, "Erreur fatale lors de l'assemblage horizontal", "", true, Status.error);
                        Debug.WriteLine(ex.Message);
                        return;
                    }
                    double progress_value = 0;
                    double operation_pourcentage_denominateur = decalage_y * decalage_boucle_for_y;

                    if (operation_pourcentage_denominateur != 0)
                    {
                        progress_value = (double)(100 / decalage_y * decalage_boucle_for_y);
                    }
                    Horizontal_Array.Clear();
                }

                UpdateDownloadPanel(id, "Assemblage...  2/2", "0", true, Status.assemblage);
                Task.Factory.StartNew(() => Thread.Sleep(300));
                try
                {
                    #region fix_dpi_issue_if_vertical_array_is_completly_black
                    double max_res = 0;
                    for (int i = 0; i < Vertical_Array.Count; i++)
                    {
                        if (Vertical_Array[i].Xres > max_res)
                        {
                            max_res = Vertical_Array[i].Xres;
                            Debug.WriteLine(Vertical_Array[i].Xres);
                        }
                    }
                    Debug.WriteLine("max res = " + max_res);

                    for (int i = 0; i < Vertical_Array.Count; i++)
                    {
                        if (Vertical_Array[i].Xres != max_res)
                        {
                            Vertical_Array[i] = NetVips.Image.Black(settings_tile_size, settings_tile_size);
                        }
                        Debug.WriteLine(Vertical_Array[i].Xres);
                    }
                    #endregion
                    image = NetVips.Image.Arrayjoin(Vertical_Array.ToArray(), across: 1);
                    image.WriteToFile(@"C:\Users\franc\AppData\Local\Temp\MapsInMyFolder\Google Maps Panorama_GMAP_PANO\5\debug_final.jpeg");
                }
                catch (Exception ex)
                {
                    UpdateDownloadPanel(id, "Erreur fatale lors de l'assemblage vertical", "", true, Status.error);
                    Debug.WriteLine(ex.Message);
                }
                Vertical_Array.Clear();
                UpdateDownloadPanel(id, "Rognage...", "0", true, Status.rognage);
                NetVips.Image image_rogner = NetVips.Image.Black(rognage_info["width"], rognage_info["height"]);
                image_rogner = image_rogner.Insert(image, -rognage_info["NO_decalage_x"], -rognage_info["NO_decalage_y"]);
                image.Dispose();

                //image_rogner = image_rogner.Colourspace(Enums.Interpretation.Bw);
                //image_rogner = image_rogner.Colourspace(Enums.Interpretation.Grey16);
                try
                {
                    if (curent_engine.RedimWidth != -1 && curent_engine.RedimHeignt != -1)
                    {
                        UpdateDownloadPanel(id, "Redimensionnement...", "0", true, Status.rognage);
                        double hrink = (double)curent_engine.RedimHeignt / (double)rognage_info["height"];
                        double Vrink = (double)curent_engine.RedimWidth / (double)rognage_info["width"];
                        DebugMode.WriteLine("hrink : " + curent_engine.RedimHeignt + "/" + rognage_info["height"] + " = " + hrink.ToString());
                        DebugMode.WriteLine("Vrink : " + curent_engine.RedimWidth + "/" + rognage_info["width"] + " = " + Vrink.ToString());

                        if ((curent_engine.RedimHeignt == Math.Round((double)rognage_info["height"] * Vrink)) || (curent_engine.RedimWidth == Math.Round((double)rognage_info["width"] * hrink)))
                        {
                            DebugMode.WriteLine("Uniform resizing");
                            image_rogner = image_rogner.Resize(hrink);
                        }
                        else
                        {
                            DebugMode.WriteLine("ThumbnailImage resizing");
                            image_rogner = image_rogner.ThumbnailImage(curent_engine.RedimWidth, curent_engine.RedimHeignt, size: Enums.Size.Force);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateDownloadPanel(id, "Erreur redimensionnement du fichier", "", true, Status.error);
                    DebugMode.WriteLine("Erreur redimensionnement du fichier" + ex.Message);
                }

                if (curent_engine.state == Status.error) { return; }
                UpdateDownloadPanel(id, "Enregistrement...", "0", true, Status.enregistrement);
                Thread.Sleep(500);
                var progress = new Progress<int>(percent => UpdateDownloadPanel(id, "", Convert.ToString(percent)));
                image_rogner.SetProgress(progress);
                string image_temps_assemblage_path = save_temp_directory + save_temp_filename;

                if (Directory.Exists(save_temp_directory))
                {
                    if (File.Exists(image_temps_assemblage_path))
                    {
                        System.IO.File.Delete(image_temps_assemblage_path);
                    }
                }
                else
                {
                    Directory.CreateDirectory(save_temp_directory);
                }

                VOption saving_options = new VOption();
                if (curent_engine.final_saveformat == "png")
                {
                    saving_options.Add("Q", curent_engine.quality);
                    saving_options.Add("compression", 100);
                    saving_options.Add("interlace", true);
                }
                else if (curent_engine.final_saveformat == "jpeg")
                {
                    saving_options.Add("Q", curent_engine.quality);
                    saving_options.Add("interlace", true);
                    saving_options.Add("optimize_coding", false);
                }
                else if (curent_engine.final_saveformat == "tiff")
                {
                    saving_options.Add("Q", curent_engine.quality);
                    saving_options.Add("tileWidth", settings_tile_size);
                    saving_options.Add("tileHeight", settings_tile_size);
                    saving_options.Add("compression", "jpeg");
                    saving_options.Add("interlace", true);
                    saving_options.Add("tile", true);
                    saving_options.Add("pyramid", true);
                    saving_options.Add("bigtif", true);

                    ///compression: NetVips.Enums.ForeignTiffCompression.Jpeg, pyramid: true, tile: true, tileWidth: 256, tileHeight: 256
                }
                try
                {
                    image_rogner.WriteToFile(image_temps_assemblage_path, saving_options);
                }
                catch (Exception ex)
                {
                    UpdateDownloadPanel(id, "Erreur enregistrement du fichier", "", true, Status.error);
                    DebugMode.WriteLine("Erreur enregistrement du fichier" + ex.Message);
                }
                DebugMode.WriteLine(image_temps_assemblage_path);
                if (File.Exists(image_temps_assemblage_path))
                {
                    UpdateDownloadPanel(id, "Déplacement..", "", true, Status.progress);
                    System.IO.FileInfo assemblage_image_file_info = new System.IO.FileInfo(image_temps_assemblage_path);
                    if (Directory.Exists(save_directory))
                    {
                        if (File.Exists(save_directory + save_filename))
                        {
                            System.IO.File.Delete(save_directory + save_filename);

                            foreach (DownloadClass eng in DownloadClass.GetEngineList())
                            {
                                if ((eng.save_directory + eng.file_name) == (save_directory + save_filename))
                                {
                                    if (eng.state == Status.success)
                                    {
                                        UpdateDownloadPanel(eng.id, "Remplacé", "0", true, Status.deleted);
                                        Database.DB_Download_Update(eng.dbid, "INFOS", "Remplacé");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(save_directory);
                    }
                    assemblage_image_file_info.MoveTo(save_directory + save_filename);
                }
                else
                {
                    //MessageBox.Show("Une erreur fatale est survenu lors de l'assemblage du fichier \"" + save_filename + "\"");
                    App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        Message.NoReturnBoxAsync("Une erreur fatale est survenu lors de l'assemblage du fichier \"" + save_filename + "\"", "Erreur");
                    }, null);
                }

                UpdateDownloadPanel(id, "Libération des ressources..", "100", false, Status.progress);
                curent_engine.urls.Clear();
                curent_engine.urls = null;
                image_rogner.Dispose();
            });
            DebugMode.WriteLine("End");
            DownloadFinish(id);
            try
            {
                await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    CheckifMultipleDownloadInProgress();
                    //TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    TaskbarItemInfo.ProgressValue = 0;
                }, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction Assemblage : " + ex.Message);
            }
            GC.Collect();
        }

        public async Task DownloadUrlAsync(Url_class url)
        {
            try
            {
                string id = url.x + "/" + url.y;
                DownloadClass download_engine = DownloadClass.GetEngineById(url.downloadid);

                void InternalUpdateProgressBar()
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
                    double progress = (double)(download_engine.nbr_of_tiles - number_of_url_class_waiting_for_downloading) / (double)download_engine.nbr_of_tiles;
                    if (number_of_url_class_waiting_for_downloading == 0)
                    {
                        progress = 100;
                    }
                    DebugMode.WriteLine("Waiting download :" + number_of_url_class_waiting_for_downloading);
                    try
                    {
                        if (System.Windows.Application.Current is null)
                        {
                            Debug.WriteLine("Erreur fatale");

                            return;
                        }
                        App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
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
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("porogresbarupsateerror : " + ex.Message);
                    }
                    try
                    {
                        App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                         {
                             string info = download_engine.nbr_of_tiles - number_of_url_class_waiting_for_downloading + "/" + download_engine.nbr_of_tiles;
                             double progresstxt = (double)progress * 100;
                             if (progresstxt > 100)
                             {
                                 progresstxt = 100;
                             }
                             UpdateDownloadPanel(url.downloadid, info, Convert.ToString(progresstxt), false, Status.no_data);
                         }, null);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("pannel update : " + ex.Message);
                    }
                }

                int z = download_engine.zoom;
                string format = download_engine.format;
                string save_temp_directory = download_engine.save_temp_directory;
                string filename = url.x + "_" + url.y + "." + format;
                DebugMode.WriteLine("Check if download this : " + save_temp_directory + filename);
                Boolean do_download_this_tile = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory, filename, Commun.Settings.tiles_cache_expire_after_x_days);
                System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

                if (do_download_this_tile)
                {
                    try
                    {
                        HttpResponse httpResponse = await TileGeneratorSettings.TileLoaderGenerator.GetImageAsync(download_engine.urlbase, url.x, url.y, url.z, download_engine.layerid, download_engine.format, save_temp_directory, Commun.Settings.tiles_cache_expire_after_x_days).ConfigureAwait(false);
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
                        InternalUpdateProgressBar();
                        if (Commun.Settings.waiting_before_start_another_tile_download > 0 && download_engine.nbr_of_tiles_waiting_for_downloading > 0)
                        {
                            Thread.Sleep(Commun.Settings.waiting_before_start_another_tile_download);
                            DebugMode.WriteLine("Pause...");
                        }
                    }
                }
                else
                {
                    DebugMode.WriteLine("Existing tile");
                    url.status = Status.success;
                    //await Task.Run(() =>
                    //{
                    //    Thread.Sleep(10);
                    //});

                    await Task.Factory.StartNew(() => Thread.Sleep(20));
                    InternalUpdateProgressBar();
                }
            }
            catch (Exception ex)
            {
                string id = url.x + "/" + url.y;
                Debug.WriteLine(id + " Une erreur non prise en charge s'est produite : " + ex.Message);
                url.status = Status.error;
            }
        }

        /// <summary>
        ///    Cette fonction permet de crée une list de string des urls à téléchargé pour le calque.
        /// </summary>
        public static Dictionary<string, int> GetRognageValue(double NO_Latitude, double NO_Longitude, double SE_Latitude, double SE_Longitude, int zoom, int tile_width)
        {
            List<int> GetRognageFromLocation(double Latitude, double Longitude)
            {
                List<int> list_of_tile_number_from_given_lat_and_long = Collectif.CoordonneesToTile(Latitude, Longitude, zoom);

                List<double> CoinsHautGaucheLocationFromTile = Collectif.TileToCoordonnees(list_of_tile_number_from_given_lat_and_long[0], list_of_tile_number_from_given_lat_and_long[1], zoom);
                double longitude_coins_haut_gauche_curent_tileX = CoinsHautGaucheLocationFromTile[0];
                double latitude_coins_haut_gauche_curent_tileY = CoinsHautGaucheLocationFromTile[1];

                List<double> NextCoinsHautGaucheLocationFromTile = Collectif.TileToCoordonnees(list_of_tile_number_from_given_lat_and_long[0] + 1, list_of_tile_number_from_given_lat_and_long[1] + 1, zoom);
                double longitude_coins_haut_gauche_next_tileX = NextCoinsHautGaucheLocationFromTile[0];
                double latitude_coins_haut_gauche_next_tileY = NextCoinsHautGaucheLocationFromTile[1];

                double latitude_decalage = Math.Abs(Latitude - latitude_coins_haut_gauche_curent_tileY) * 100 / Math.Abs(latitude_coins_haut_gauche_curent_tileY - latitude_coins_haut_gauche_next_tileY) / 100;
                double longitude_decalage = Math.Abs(Longitude - longitude_coins_haut_gauche_curent_tileX) * 100 / Math.Abs(longitude_coins_haut_gauche_curent_tileX - longitude_coins_haut_gauche_next_tileX) / 100;
                int decalage_y = Math.Abs(Convert.ToInt32(Math.Round(latitude_decalage * tile_width, 0)));
                int decalage_x = Math.Abs(Convert.ToInt32(Math.Round(longitude_decalage * tile_width, 0)));
                DebugMode.WriteLine("Décalage en px : X = " + decalage_x + " Y = " + decalage_y);
                return new List<int>() { decalage_x, decalage_y };
            }

            List<int> NO_decalage = GetRognageFromLocation(NO_Latitude, NO_Longitude);
            List<int> SE_decalage = GetRognageFromLocation(SE_Latitude, SE_Longitude);
            int NbrtilesInCol = Collectif.CoordonneesToTile(SE_Latitude, SE_Longitude, zoom)[0] - Collectif.CoordonneesToTile(NO_Latitude, NO_Longitude, zoom)[0] + 1;
            int NbrtilesInRow = Collectif.CoordonneesToTile(SE_Latitude, SE_Longitude, zoom)[1] - Collectif.CoordonneesToTile(NO_Latitude, NO_Longitude, zoom)[1] + 1;
            int final_image_width = Math.Abs((NbrtilesInCol * tile_width) - (NO_decalage[0] + (tile_width - SE_decalage[0])));
            int final_image_height = Math.Abs((NbrtilesInRow * tile_width) - (NO_decalage[1] + (tile_width - SE_decalage[1])));
            if (final_image_width < 10 || final_image_height < 10)
            {
                final_image_width = 10;
                final_image_height = 10;
            }

            Dictionary<string, int> return_dictionnary = new Dictionary<string, int>
            {
                {"NO_decalage_x", NO_decalage[0] },
                {"NO_decalage_y", NO_decalage[1] },
                {"SE_decalage_x", SE_decalage[0] },
                {"SE_decalage_y", SE_decalage[1] },
                {"width", final_image_width },
                {"height", final_image_height }
            };
            return return_dictionnary;
        }
    }
}