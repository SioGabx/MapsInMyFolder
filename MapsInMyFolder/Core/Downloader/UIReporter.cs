using CefSharp;
using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    public partial class Downloader
    {
        public static class Taskbar
        {
            private static MainWindow GetMainWindow()
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    return Application.Current.MainWindow as MainWindow;
                }, DispatcherPriority.Send);
            }

            public static TaskbarItemProgressState ProgressState
            {
                get
                {
                    return GetMainWindow().TaskbarItemInfo.ProgressState;
                }
                set
                {
                    GetMainWindow().TaskbarItemInfo.ProgressState = value;
                }
            }

            public static double ProgressValue
            {
                get
                {
                    return GetMainWindow().TaskbarItemInfo.ProgressValue;
                }
                set
                {
                    GetMainWindow().TaskbarItemInfo.ProgressValue = value;
                }
            }
        }
        public static void ExecuteScriptInDownloadPanel(string script)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow.MainPage.download_panel_browser?.ExecuteScriptAsync(script);
            }, DispatcherPriority.Normal);
        }
        private static void InternalUpdateProgressBar(DownloadEngine download_engine)
        {
            if (download_engine == null)
            {
                return;
            }
            int number_of_url_class_waiting_for_downloading = 0;
            foreach (TileProperty urclass in download_engine?.urls)
            {
                if (urclass.status == Status.waitfordownloading)
                {
                    number_of_url_class_waiting_for_downloading++;
                }
            }
            download_engine.nbrOfTilesWaitingForDownloading = number_of_url_class_waiting_for_downloading;
            double progress = (download_engine.nbrOfTiles - number_of_url_class_waiting_for_downloading) / (double)download_engine.nbrOfTiles;
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
                    if (number_of_url_class_waiting_for_downloading == 0)
                    {
                        CheckifMultipleDownloadInProgress();
                    }
                    Taskbar.ProgressValue = (double)progress;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("porogresbarupsateerrorv1 : " + ex.Message);
                }
            }, null);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                string info = download_engine.nbrOfTiles - number_of_url_class_waiting_for_downloading + "/" + download_engine.nbrOfTiles;
                double progresstxt = (double)progress * 100;
                if (progresstxt > 100)
                {
                    progresstxt = 100;
                }
                UpdateDownloadPanel(download_engine.id, info, Convert.ToString(progresstxt), false, Status.no_data);
            }, null);
        }

        public static async void UpdateDownloadPanel(int id, string info = "", string progress = "", bool isImportant = false, Status state = Status.no_data, string tooltips = null)
        {
            DownloadEngine engine = DownloadEngine.GetEngineById(id);

            if (!string.IsNullOrEmpty(info) && isImportant)
            {
                Debug.WriteLine($">{info}");
            }

            if (engine.state == Status.error)
            {
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

            if (!string.IsNullOrEmpty(tooltips))
            {
                commandExecuted += $"updatetooltips({id}, \"{tooltips}\");";
                isImportant = true;
            }

            if (state != Status.no_data)
            {
                commandExecuted += $"updatestate({id}, \"{state}\");";
                Database.DB_Download_Update(engine.dbid, "STATE", state.ToString());
            }

            if (state == Status.error)
            {
                Database.DB_Download_Update(engine.dbid, "INFOS", Collectif.HTMLEntities(info));
                engine.state = Status.error;
                AbordAndCancelWithTokenDownload(id);
                commandExecuted += $"updateprogress({id}, \"100\");";
            }

            engine.skippedPanelUpdate++;
            int updateRate = (int)Math.Floor(Math.Pow(engine.maxDownloadtilesInParralele / 10, 1.5));

            if (updateRate < 1)
            {
                updateRate = 1;
            }

            if (engine.maxDownloadtilesInParralele - updateRate > 100)
            {
                updateRate = engine.maxDownloadtilesInParralele;
            }

            if (state == Status.no_data && engine.skippedPanelUpdate != updateRate)
            {
                return;
            }

            engine.skippedPanelUpdate--;

            if (!string.IsNullOrEmpty(commandExecuted) && commandExecuted != engine.lastCommand && commandExecuted != engine.lastCommandNotImportant)
            {
                engine.skippedPanelUpdate = 0;

                await Task.Run(async () =>
                {
                    engine.lastCommand = commandExecuted;

                    if (!isImportant)
                    {
                        engine.lastCommandNotImportant = commandExecuted;
                    }
                    ExecuteScriptInDownloadPanel(commandExecuted);

                    if (isImportant)
                    {
                        await Task.Delay(250);
                    }
                });
            }
        }
    }
}
