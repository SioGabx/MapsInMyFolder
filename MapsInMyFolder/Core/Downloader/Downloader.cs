using CefSharp;
using MapsInMyFolder.Commun;
using NetVips;
using Newtonsoft.Json;
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
using System.Windows.Shell;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    public class DownloadOptions
    {
        public int id_layer;
        public string save_path;
        public string format;
        public string filename;
        public string identifier;
        public string name;
        public int tile_size;
        public int quality;
        public int zoom;
        public string urlbase;
        public MapControl.Location NO_PIN_Location;
        public MapControl.Location SE_PIN_Location;
        public int resizeWidth;
        public int resizeHeignt;
        public Enums.Interpretation interpretation;
        public ScaleInfo scaleInfo;

        public DownloadOptions(int id_layer, string save_path, string format, string filename, string identifier, string name, int tile_size, int zoom, int quality, string urlbase, MapControl.Location NO_PIN_Location, MapControl.Location SE_PIN_Location, int resizeWidth, int resizeHeignt, Enums.Interpretation interpretation, ScaleInfo scaleInfo)
        {
            this.id_layer = id_layer;
            this.save_path = save_path;
            this.format = format;
            this.filename = filename;
            this.identifier = identifier;
            this.name = name;
            this.tile_size = tile_size;
            this.zoom = zoom;
            this.quality = quality;
            this.urlbase = urlbase;
            this.NO_PIN_Location = NO_PIN_Location;
            this.SE_PIN_Location = SE_PIN_Location;
            this.resizeWidth = resizeWidth;
            this.resizeHeignt = resizeHeignt;
            this.interpretation = interpretation;
            this.scaleInfo = scaleInfo;
        }
    }

    public partial class Downloader
    {
        public static void CheckifMultipleDownloadInProgress()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                if (Settings.max_download_project_in_parralele == 1)
                {
                    Taskbar.ProgressState = TaskbarItemProgressState.Normal;
                }
                else
                {
                    int nbr_engine_progress = DownloadEngine.GetEngineList().Where(engine => !(engine.state == Status.cancel || engine.state == Status.pause || engine.state == Status.error || engine.state == Status.success || engine.state == Status.deleted)).Count();

                    if (nbr_engine_progress > 1 && Taskbar.ProgressState != TaskbarItemProgressState.Indeterminate)
                    {
                        Taskbar.ProgressState = TaskbarItemProgressState.Indeterminate;
                    }
                    else if (nbr_engine_progress <= 1 && Taskbar.ProgressState != TaskbarItemProgressState.Normal)
                    {
                        Taskbar.ProgressValue = 0;
                        Taskbar.ProgressState = TaskbarItemProgressState.Normal;
                    }
                }
            }, null);
        }


        public static async void CheckIfReadyToStartDownload()
        {
            int maxSimultaneousDownloads = Settings.max_download_project_in_parralele;
            int numDownloadsStarted = 0;
            await Task.Delay(500);
            foreach (DownloadEngine engine in DownloadEngine.GetEngineList())
            {
                if (numDownloadsStarted >= maxSimultaneousDownloads)
                    break;

                if (engine.state == Status.waitfordownloading)
                {
                    if (Network.IsNetworkAvailable())
                    {
                        //start download
                        RestartDownload(engine.id);
                        numDownloadsStarted++;
                    }
                    else
                    {
                        //No internet connection available.downloadStateInternetConnexionWaiting
                        UpdateDownloadPanel(engine.id, Languages.Current["downloadStateInternetConnexionWaiting"], "", true, Status.progress);
                    }
                }
            }

            Debug.WriteLine("-------------");
            Debug.WriteLine(GetListOfIdsAndStates());
            Debug.WriteLine("-------------");

            Network.IsNetworkAvailable();
        }

        private static string GetListOfIdsAndStates()
        {
            StringBuilder sb = new StringBuilder();
            foreach (DownloadEngine engine in DownloadEngine.GetEngineList())
            {
                sb.AppendLine($"{engine.id} : {engine.state}");
            }
            return sb.ToString();
        }


        public static void PrepareDownloadBeforeStart(DownloadOptions download_Options)
        {
            CheckifMultipleDownloadInProgress();
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.MainPage.DownloadPanelOpen();
            mainWindow.MainPage.download_panel_browser?.ExecuteScriptAsync("document.getElementById(\"main\").scrollIntoView({ behavior: \"smooth\", block: \"start\", inline: \"nearest\"})");
            DownloadOptions download_Options_edited = download_Options;
            download_Options_edited.id_layer = Layers.Current.Id;
            download_Options_edited.identifier = Layers.Current.Identifier;
            download_Options_edited.name = Layers.Current.Name;
            download_Options_edited.tile_size = Layers.Current.TilesSize ?? 256;
            download_Options_edited.urlbase = Layers.Current.TileUrl;
            StartDownload(download_Options_edited);
        }


        private static void StartDownload(DownloadOptions download_Options)
        {
            int downloadId = DownloadEngine.GetId();
            string format = Layers.Current.TilesFormat;
            string finalSaveFormat = download_Options.format;
            int zoom = download_Options.zoom;
            int quality = download_Options.quality;
            string fileTempName = "file_id=" + downloadId + "." + finalSaveFormat;
            string filename = Path.HasExtension(download_Options.filename) ? download_Options.filename : download_Options.filename + "." + finalSaveFormat;
            string saveDirectory = download_Options.save_path.Replace(filename, "");
            string identifier = download_Options.identifier;
            string layername = download_Options.name;
            int tileSize = download_Options.tile_size;
            string saveTempDirectory = Collectif.GetSaveTempDirectory(layername, identifier, zoom, Settings.temp_folder);
            string urlbase = download_Options.urlbase;
            int MaxDownloadTilesInParralele = Layers.Current.SpecialsOptions.MaxDownloadTilesInParralele;
            int WaitingBeforeStartAnotherTile = Layers.Current.SpecialsOptions.WaitingBeforeStartAnotherTile;


            if (urlbase.Trim() != "" && tileSize != 0)
            {
                var NO_tile = Collectif.CoordonneesToTile(download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, zoom);
                var SE_tile = Collectif.CoordonneesToTile(download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, zoom);
                int latTileNumber = Math.Abs(SE_tile.X - NO_tile.X) + 1;
                int longTileNumber = Math.Abs(SE_tile.Y - NO_tile.Y) + 1;
                int nbrOfTiles = latTileNumber * longTileNumber;

                if (latTileNumber * tileSize >= 65500 || longTileNumber * tileSize >= 65500)
                {
                    Message.NoReturnBoxAsync("Unable to download the area, the generated image cannot exceed 65000 pixels in width/height.", Languages.Current["dialogTitleOperationFailed"]);
                    return;
                }

                Dictionary<string, double> location = new Dictionary<string, double>
                {
                    { "NO_Latitude", download_Options.NO_PIN_Location.Latitude },
                    { "NO_Longitude", download_Options.NO_PIN_Location.Longitude },
                    { "SE_Latitude", download_Options.SE_PIN_Location.Latitude },
                    { "SE_Longitude", download_Options.SE_PIN_Location.Longitude }
                };
                string engineVarContexte = JsonConvert.SerializeObject(Javascript.Functions.DumpVars(Layers.Current.Id));
                IEnumerable<TileProperty> urls = GetUrl.GetListOfUrlFromLocation(location, zoom, urlbase, Layers.Current.Id, downloadId, engineVarContexte);
                CancellationTokenSource tokenSource2 = new CancellationTokenSource();
                CancellationToken ct = tokenSource2.Token;
                string timestamp = Convert.ToString(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds());
                IEnumerable<HttpStatusCode> ErrorsToIgnore = StatusCode.GetListFromString(Layers.Current.SpecialsOptions.ErrorsToIgnore);
                string jsonScaleInfo = JsonConvert.SerializeObject(download_Options.scaleInfo);

                Debug.WriteLine(engineVarContexte);
                int dbid = Database.DB_Download_Write(Status.waitfordownloading, filename, nbrOfTiles, zoom, download_Options.NO_PIN_Location.Latitude, download_Options.NO_PIN_Location.Longitude, download_Options.SE_PIN_Location.Latitude, download_Options.SE_PIN_Location.Longitude, download_Options.id_layer, saveTempDirectory, saveDirectory, timestamp, quality, download_Options.resizeWidth, download_Options.resizeHeignt, download_Options.interpretation.ToString(), jsonScaleInfo, engineVarContexte);

                DownloadEngine engine = new DownloadEngine(downloadId, dbid, Layers.Current.Id, urls, tokenSource2, ct, format, finalSaveFormat, zoom, saveTempDirectory, saveDirectory, filename, fileTempName, location, download_Options.resizeWidth, download_Options.resizeHeignt, download_Options.interpretation, download_Options.scaleInfo, ErrorsToIgnore, engineVarContexte, nbrOfTiles, urlbase, identifier, Status.waitfordownloading, tileSize, nbrOfTiles, quality, MaxDownloadTilesInParralele, WaitingBeforeStartAnotherTile);
                DownloadEngine.Add(engine, downloadId);

                Status status;
                string info;

                if (Network.IsNetworkAvailable())
                {
                    info = $"{Languages.Current["downloadStateWaiting"]} (0/{nbrOfTiles})";
                    status = Status.progress;
                }
                else
                {
                    info = Languages.Current["downloadStateInternetConnexionWaiting"];
                    status = Status.noconnection;
                }

                string commandAdd = $"add_download({downloadId}, '{status}', '{Collectif.HTMLEntities(filename)}', 0, {nbrOfTiles}, '{Collectif.HTMLEntities(info)}', 'recent')";
                ExecuteScriptInDownloadPanel(commandAdd);

                CheckIfReadyToStartDownload();
            }
            else
            {
                Message.NoReturnBoxAsync("Impossible de télécharger la carte car une erreur s'est produite, vérifiez la base de données...", "Erreur");
            }
        }

        private static void AbordAndCancelWithTokenDownload(int engineId)
        {
            DownloadEngine engine = DownloadEngine.GetEngineById(engineId);
            CancellationTokenSource canceltocken = engine.cancellationTokenSource;
            if (!canceltocken.IsCancellationRequested)
            {
                canceltocken.Cancel();
            }
            CheckIfReadyToStartDownload();
        }

        public static void StopingDownload(int engineId)
        {
            DownloadEngine engine = DownloadEngine.GetEngineById(engineId);
            string info = $"{engine.nbrOfTiles - engine.nbrOfTilesWaitingForDownloading}/{engine.nbrOfTiles}";
            UpdateDownloadPanel(engineId, $"{Languages.Current["downloadStatePaused"]} ({info})", "", true, Status.pause);
            engine.state = Status.pause;
            AbordAndCancelWithTokenDownload(engineId);
        }

        public static void CancelDownload(int engineId)
        {
            DownloadEngine engine = DownloadEngine.GetEngineById(engineId);
            UpdateDownloadPanel(engineId, Languages.Current["downloadStateCanceled"], "", true, Status.cancel);
            engine.state = Status.cancel;
            AbordAndCancelWithTokenDownload(engineId);
        }

        public static void RestartDownloadFromStart(int engineId)
        {
            DownloadEngine engine = DownloadEngine.GetEngineById(engineId);
            engine.nbrOfTilesWaitingForDownloading = engine.nbrOfTiles;
            RestartDownload(engineId);
        }

        private static bool CheckNetworkAvailable(int engineId)
        {
            if (!Network.IsNetworkAvailable())
            {
                UpdateDownloadPanel(engineId, Languages.Current["downloadStateInternetConnexionWaiting"], state: Status.noconnection);
                return false;
            }
            return true;
        }

        public static void RestartDownload(int engineId)
        {
            CheckNetworkAvailable(engineId);
            DownloadEngine engine = DownloadEngine.GetEngineById(engineId);
            engine.state = Status.waitfordownloading;
            CheckifMultipleDownloadInProgress();

            string info = $"{engine.nbrOfTiles - engine.nbrOfTilesWaitingForDownloading}/{engine.nbrOfTiles}";
            UpdateDownloadPanel(engineId, $"{Languages.Current["downloadStateRestarting"]} ({info})", "", true, Status.progress);

            if (engine.urls is null || !engine.urls.Any())
            {
                UpdateDownloadPanel(engineId, Languages.Current["downloadStateGeneratingURL"], "", true, Status.progress);
                engine.urls = GetUrl.GetListOfUrlFromLocation(engine.location, engine.zoom, engine.urlBase, engine.layerid, engine.id, engine.varContext);
            }

            foreach (var url in engine.urls)
            {
                url.status = Status.waitfordownloading;
            }
            if (engine.cancellationTokenSource != null)
            {
                engine.cancellationTokenSource.Cancel();
                engine.cancellationTokenSource.Dispose();
            }

            engine.cancellationTokenSource = new CancellationTokenSource();
            engine.cancellationToken = engine.cancellationTokenSource.Token;

            if (DownloadEngine.CurrentNumberOfDownload < Settings.max_download_project_in_parralele && CheckNetworkAvailable(engineId))
            {
                UpdateDownloadPanel(engineId, Languages.Current["downloadStateIntegrityCheck"], "0", true, Status.no_data);
                DownloadThisEngine(engine);
            }
            else
            {
                UpdateDownloadPanel(engineId, $"{Languages.Current["downloadStateWaiting"]} ({info})", "", true, Status.progress);
            }
        }

        private async static void DownloadThisEngine(DownloadEngine downloadEngineClassArgs)
        {
            DownloadEngine.CurrentNumberOfDownload++;
            downloadEngineClassArgs.state = Status.progress;
            CheckIfReadyToStartDownload();

            int settingsMaxRetryDownload = Settings.max_retry_download;
            int nbrPass = 0;

            do
            {
                nbrPass++;

                if (nbrPass != 1)
                {
                    UpdateDownloadPanel(downloadEngineClassArgs.id, Languages.GetWithArguments("downloadStateErrorsAttempt", nbrPass, settingsMaxRetryDownload), isImportant: true, state: Status.progress);
                }

                await ParallelDownloadTilesTask(downloadEngineClassArgs);

            } while ((CheckDownloadIsComplete(downloadEngineClassArgs) != 0) && (downloadEngineClassArgs.state == Status.progress) && (nbrPass < settingsMaxRetryDownload));
            int nbrOfTilesWaitingForDownloading = CheckDownloadIsComplete(downloadEngineClassArgs);
            if (nbrOfTilesWaitingForDownloading == 0 || (Settings.generate_transparent_tiles_on_error && downloadEngineClassArgs.state == Status.progress))
            {
                await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Taskbar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                }, null);
                await Assemblage(downloadEngineClassArgs.id);
            }
            else if (nbrPass == settingsMaxRetryDownload)
            {
                UpdateDownloadPanel(downloadEngineClassArgs.id, Languages.GetWithArguments("downloadStateErrorsAttempt", nbrPass, settingsMaxRetryDownload), "100", true, Status.error, "Number of tiles missing = " + nbrOfTilesWaitingForDownloading);
                downloadEngineClassArgs.state = Status.error;
            }

            DownloadEngine.CurrentNumberOfDownload--;
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                Taskbar.ProgressValue = 0;
            }, null);
            CheckifMultipleDownloadInProgress();
            CheckIfReadyToStartDownload();
        }

        private static bool WaitForInternet(DownloadEngine downloadEngineClass)
        {
            CancellationTokenSource cancellationTokenSource = downloadEngineClass.cancellationTokenSource;
            CancellationToken cancellationToken = downloadEngineClass.cancellationToken;

            bool isNetworkAvailable;
            do
            {
                isNetworkAvailable = Network.FastIsNetworkAvailable();

                if (cancellationToken.IsCancellationRequested && cancellationToken.CanBeCanceled)
                {
                    cancellationTokenSource?.Cancel();
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        Taskbar.ProgressValue = 0;
                    }, null);
                    return false;
                }

                if (!isNetworkAvailable)
                {
                    UpdateDownloadPanel(downloadEngineClass.id, Languages.Current["downloadStateInternetConnexionWaiting"], state: Status.noconnection);
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        Taskbar.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                    }, null);
                    Thread.Sleep(500);
                }
            } while (!isNetworkAvailable);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                if (Taskbar.ProgressState == System.Windows.Shell.TaskbarItemProgressState.Paused)
                {
                    CheckifMultipleDownloadInProgress();
                    UpdateDownloadPanel(downloadEngineClass.id, Languages.Current["downloadStateInternetConnexionBack"], state: Status.progress);
                }
            }, null);
            return true;
        }

        private static async Task ParallelDownloadTilesTask(DownloadEngine downloadEngineClass)
        {
            IEnumerable<TileProperty> urls = downloadEngineClass.urls;
            CancellationTokenSource cancellationTokenSource = downloadEngineClass.cancellationTokenSource;
            CancellationToken cancellationToken = downloadEngineClass.cancellationToken;

            await Task.Run(() =>
            {
                try
                {
                    Parallel.ForEach(urls, new ParallelOptions { MaxDegreeOfParallelism = downloadEngineClass.maxDownloadtilesInParralele, CancellationToken = cancellationToken }, url =>
                        {
                            WaitForInternet(downloadEngineClass);
                            DownloadUrlAsync(url).Wait();
                        });
                }
                catch (OperationCanceledException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }, cancellationTokenSource.Token);

        }

        static private int CheckDownloadIsComplete(DownloadEngine downloadEngineClass)
        {
            if (downloadEngineClass.nbrOfTilesWaitingForDownloading != 0)
            {
                return downloadEngineClass.nbrOfTilesWaitingForDownloading;
            }

            UpdateDownloadPanel(downloadEngineClass.id, Languages.Current["downloadStateDownloadCheck"], "0", true, Status.progress);
            Task.Delay(500).Wait();
            int numberOfUrlClassWaitingForDownloading = 0;

            if (downloadEngineClass.state == Status.pause && downloadEngineClass.state == Status.cancel)
            {
                numberOfUrlClassWaitingForDownloading++;
            }
            else
            {
                foreach (TileProperty urlClass in downloadEngineClass.urls)
                {
                    if ((urlClass.status != Status.no_data) || !Settings.generate_transparent_tiles_on_404)
                    {
                        if (urlClass.status == Status.waitfordownloading)
                        {
                            numberOfUrlClassWaitingForDownloading++;
                        }
                        string filename = $"{downloadEngineClass.saveTempDirectory}{urlClass.x}_{urlClass.y}.{downloadEngineClass.format}";

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

            downloadEngineClass.nbrOfTilesWaitingForDownloading = numberOfUrlClassWaitingForDownloading;
            return numberOfUrlClassWaitingForDownloading;
        }

        static void DownloadFinish(int id)
        {
            DownloadEngine currentEngine = DownloadEngine.GetEngineById(id);
            if (currentEngine.state != Status.error)
            {
                currentEngine.state = Status.success;
            }

            currentEngine.urls = null;
            currentEngine.skippedPanelUpdate = 0;
            currentEngine.lastCommand = null;
            currentEngine.lastCommandNotImportant = null;
            currentEngine.cancellationTokenSource.Dispose();
            currentEngine.cancellationTokenSource = null;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                UpdateDownloadPanel(id, Languages.Current["downloadStateCompleted"], "100", true, Status.success);
            }, null);
        }







        public static async Task DownloadUrlAsync(TileProperty tileProperty)
        {
            DownloadEngine download_engine = DownloadEngine.GetEngineById(tileProperty.downloadid);
            string format = download_engine.format;
            string save_temp_directory = download_engine.saveTempDirectory;
            string filename = tileProperty.x + "_" + tileProperty.y + "." + format;
            bool do_download_this_tile = Collectif.CheckIfDownloadIsNeededOrCached(save_temp_directory, filename, Settings.tiles_cache_expire_after_x_days);
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            if (!do_download_this_tile)
            {
                //Existing tile
                tileProperty.status = Status.success;

                await Task.Delay(20);
                InternalUpdateProgressBar(download_engine);
                return;
            }
            try
            {
                
                TileLoader.HttpResponse httpResponse = await TileLoader.GetImageAsync(tileProperty, download_engine.layerid, download_engine.tileSize, download_engine.format, save_temp_directory).ConfigureAwait(false);
                if (httpResponse?.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    using var contentStream = Collectif.ByteArrayToStream(httpResponse.Buffer);
                    if (httpResponse.Buffer?.Length > 0)
                    {
                        using FileStream fileStream = new FileStream(save_temp_directory + filename, FileMode.CreateNew);
                        await contentStream.CopyToAsync(fileStream);
                        if (httpResponse.ResponseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            tileProperty.status = Status.success;
                        }
                    }
                    else
                    {
                        tileProperty.status = Status.error;
                    }
                }
                else
                {
                    Thread.Sleep(200);
                    if (httpResponse?.ResponseMessage?.StatusCode != null && download_engine.AlloweRequestErrors.Contains(httpResponse.ResponseMessage.StatusCode))
                    {
                        tileProperty.status = Status.no_data;
                    }
                    else
                    {
                        if (httpResponse?.ResponseMessage?.StatusCode == HttpStatusCode.NotFound && (Settings.generate_transparent_tiles_on_404 || Settings.generate_transparent_tiles_on_error))
                        {
                            tileProperty.status = Status.no_data;
                        }
                        else if (!Network.IsNetworkAvailable())
                        {
                            tileProperty.status = Status.waitfordownloading;
                        }
                        else if (Settings.generate_transparent_tiles_on_error)
                        {
                            tileProperty.status = Status.no_data;
                        }
                        else
                        {
                            tileProperty.status = Status.error;
                        }
                    }

                }
            }
            catch (Exception a)
            {
                Debug.WriteLine("Exception Download : " + a.Message);
            }
            finally
            {
                InternalUpdateProgressBar(download_engine);
                if (download_engine.waitingBeforeStartAnotherTile > 0 && download_engine.nbrOfTilesWaitingForDownloading > 0)
                {
                    Thread.Sleep(download_engine.waitingBeforeStartAnotherTile);
                }
            }

        }


    }
}