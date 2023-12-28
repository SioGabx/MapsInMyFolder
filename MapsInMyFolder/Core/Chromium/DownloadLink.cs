using CefSharp;
using MapsInMyFolder.Commun;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Marquer les membres comme étant static", Justification = "Used by CEFSHARP")]
    public class DownloadLink
    {
        public bool IsFileOk(int id)
        {
            if (id != 0)
            {
                var engine = DownloadEngine.GetEngineById(id);
                if (engine is null) return false;
                if (System.IO.Directory.Exists(engine.saveDirectory))
                {
                    if (System.IO.File.Exists(engine.saveDirectory + engine.fileName))
                    {
                        return true;
                    }
                    else if (engine.state == Status.success)
                    {
                        Downloader.UpdateDownloadPanel(id, Languages.Current["downloadStateNotFound"], isImportant: true, state: Status.deleted);
                    }
                }
                else
                {
                    Debug.WriteLine("Le chemin n'existe pas : " + engine.saveDirectory);
                }
            }
            return false;
        }

        public void DownloadStop(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        Downloader.StopingDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void DownloadCancel(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        Downloader.CancelDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void DownloadRestart(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        Downloader.RestartDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void DownloadRestartFromStart(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        Downloader.RestartDownloadFromStart(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }
        public void DownloadReselectArea(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                var engine = DownloadEngine.GetEngineById(id_int);
                if (engine is null) return;
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainPage._instance.MapViewerSetSelection(engine.location, true);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void DownloadOpenFolder(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (IsFileOk(id_int))
            {
                var engine = DownloadEngine.GetEngineById(id_int);
                if (engine is null) return;
                Process.Start("explorer.exe", "/select,\"" + engine.saveDirectory + engine.fileName + "\"");
            }
        }

        public void DownloadOpenFile(double id)
        {
            int id_int = Convert.ToInt32(id);

            if (IsFileOk(id_int))
            {
                var engine = DownloadEngine.GetEngineById(id_int);
                if (engine is null) return;
                new Process
                {
                    StartInfo = new ProcessStartInfo(engine.saveDirectory + engine.fileName)
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
        }

        public void DownloadDeleteFile(double id)
        {
            int id_int = Convert.ToInt32(id);

            if (IsFileOk(id_int))
            {
                var engine = DownloadEngine.GetEngineById(id_int);
                if (engine is null) return;
                System.IO.File.Delete(engine.saveDirectory + engine.fileName);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        Downloader.UpdateDownloadPanel(id_int, Languages.Current["downloadStateDeleted"], "0", true, Status.deleted);
                        Database.DB_Download_Update(id_int, "STATE", nameof(Status.deleted));
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);

                foreach (DownloadEngine eng in DownloadEngine.GetEngineList())
                {
                    if (eng.state == Status.success)
                    {
                        IsFileOk(eng.id);
                    }
                }
            }
        }
        public void DownloadOpenTempFolder(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadEngine.GetEngineById(id_int);

            if (engine is null) return;
            if (System.IO.Directory.Exists(engine.saveTempDirectory))
            {
                Process.Start("explorer.exe", "\"" + engine.saveTempDirectory + "\"");
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync(Languages.GetWithArguments("downloadMessageErrorTempFolderNotFound", engine.saveTempDirectory), Languages.Current["dialogTitleOperationFailed"]);
                }, null);
            }
        }

        public void DownloadDeleteInDatabase(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadEngine.GetEngineById(id_int);
            if (engine is null) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    Database.DB_Download_Delete(id_int);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                }
            }, null);
        }

        public void DownloadCopyLocations(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadEngine.GetEngineById(id_int);
            if (engine is null) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    double NO_Latitude = Math.Round(Convert.ToDouble(engine.location["NO_Latitude"]), 6);
                    double NO_Longitude = Math.Round(Convert.ToDouble(engine.location["NO_Longitude"]), 6);
                    double SE_Latitude = Math.Round(Convert.ToDouble(engine.location["SE_Latitude"]), 6);
                    double SE_Longitude = Math.Round(Convert.ToDouble(engine.location["SE_Longitude"]), 6);

                    string clipboard_loc = NO_Latitude + "," + NO_Longitude + "," + SE_Latitude + "," + SE_Longitude;
                    Clipboard.SetText(clipboard_loc);
                }
                catch (Exception ex)
                {
                    Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                }
            }, null);
        }
        public void DownloadCopyFilePath(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadEngine.GetEngineById(id_int);
            if (engine is null) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    string clipboard_path = engine.saveDirectory + engine.fileName;
                    Clipboard.SetText(clipboard_path);
                }
                catch (Exception ex)
                {
                    Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                }
            }, null);
        }
        public void DownloadSetLayerAsCurrent(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadEngine.GetEngineById(id_int);
            if (engine is null) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    MainPage._instance.LayerPanel.LayerBrowser.GetMainFrame().EvaluateScriptAsync("selectionner_calque_by_id(" + engine.layerid + ")");
                }
                catch (Exception ex)
                {
                    Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                }
            }, null);
        }

        public void DownloadCancelAndDeletedInDatabase(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadEngine.GetEngineById(id_int);
            Status engineinitialstate = engine.state;
            if (engine is null) return;

            List<Status> RunningState = new List<Status>() { Status.progress, Status.enregistrement, Status.rognage, Status.waitfordownloading };

            if (RunningState.Contains(engine.state) || engine.state == Status.pause)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
                {
                    DownloadStop(id_int);
                    var dialogConfirmCancelDeleteDownloadResult = await Message.ShowContentDialog(Languages.GetWithArguments("downloadMessageAskCancelDeleteDownload", engine.fileName), "MapsInMyFolder", MessageDialogButton.YesNo);
                    if (dialogConfirmCancelDeleteDownloadResult == ContentDialogResult.Primary)
                    {
                        try
                        {
                            if (RunningState.Contains(engineinitialstate))
                            {
                                DownloadCancel(id_int);
                            }

                            MainWindow.Instance?.MainPage?.download_panel_browser?.ExecuteScriptAsync("download_js_delete_db(" + id_int.ToString() + ");");
                        }
                        catch (Exception ex)
                        {
                            Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                        }
                    }
                    else
                    {
                        if (RunningState.Contains(engineinitialstate))
                        {
                            DownloadRestart(id_int);
                        }
                    }
                }, null);
            }
            else
            {
                MainWindow.Instance?.MainPage?.download_panel_browser?.ExecuteScriptAsync("download_js_delete_db(" + id_int.ToString() + ");");
            }
        }
    }
}

