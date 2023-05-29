using CefSharp;
using MapsInMyFolder.Commun;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        void DB_Download_Load()
        {
            Debug.WriteLine("Loading downloads");
            if (Database.DB_Download_Init() == -1)
            {
                return;
            }
            DownloadSettings.Clear();
            string SelectCommandText = "SELECT * FROM 'DOWNLOADS' ORDER BY 'TIMESTAMP' ASC";
            using var sqlite_datareader = Database.ExecuteExecuteReaderSQLCommand(SelectCommandText);

            while (sqlite_datareader.Read())
            {
                try
                {
                    int DB_Download_LAYER_ID = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("LAYER_ID"));
                    Layers layers = Layers.GetLayerById(DB_Download_LAYER_ID) ?? Layers.Empty();
                    int DB_Download_ID = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("ID"));
                    int DB_Download_ZOOM = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("ZOOM"));
                    int DB_Download_NBR_TILES = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("NBR_TILES"));
                    int RESIZEWIDTH = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("RESIZEWIDTH"));
                    int RESIZEHEIGHT = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("RESIZEHEIGHT"));
                    int DB_Download_QUALITY = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("QUALITY"));
                    string DB_Download_TIMESTAMP = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("TIMESTAMP"));
                    string DB_Download_TEMP_DIRECTORY = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("TEMP_DIRECTORY"));
                    string DB_Download_SAVE_DIRECTORY = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("SAVE_DIRECTORY"));
                    string DB_Download_COLORINTERPRETATION = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("COLORINTERPRETATION"));
                    string DB_Download_SCALEINFO = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("SCALEINFO"));
                    string DB_Download_FILE_NAME = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("FILE_NAME"));
                    string DB_Download_STATE = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("STATE"));
                    string DB_Download_INFOS = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("INFOS")).Trim();
                    double DB_Download_NO_LAT = sqlite_datareader.GetDouble(sqlite_datareader.GetOrdinal("NO_LAT"));
                    double DB_Download_NO_LONG = sqlite_datareader.GetDouble(sqlite_datareader.GetOrdinal("NO_LONG"));
                    double DB_Download_SE_LAT = sqlite_datareader.GetDouble(sqlite_datareader.GetOrdinal("SE_LAT"));
                    double DB_Download_SE_LONG = sqlite_datareader.GetDouble(sqlite_datareader.GetOrdinal("SE_LONG"));
                    string final_saveformat = System.IO.Path.GetExtension(DB_Download_FILE_NAME);
                    int downloadid = DB_Download_ID * -1;
                    string format = layers.class_format;
                    string filetempname = "file_id=" + downloadid + "." + final_saveformat;
                    Dictionary<string, double> location = new Dictionary<string, double>
                    {
                        { "NO_Latitude", DB_Download_NO_LAT },
                        { "NO_Longitude", DB_Download_NO_LONG },
                        { "SE_Latitude", DB_Download_SE_LAT },
                        { "SE_Longitude", DB_Download_SE_LONG }
                    };

                    NetVips.Enums.Interpretation COLORINTERPRETATION = (NetVips.Enums.Interpretation)Enum.Parse(typeof(NetVips.Enums.Interpretation), DB_Download_COLORINTERPRETATION);


                    ScaleInfo SCALEINFO = System.Text.Json.JsonSerializer.Deserialize<ScaleInfo>(DB_Download_SCALEINFO, new System.Text.Json.JsonSerializerOptions() { IncludeFields = true });

                    List<TilesUrl> urls = null;
                    CancellationTokenSource tokenSource2 = new CancellationTokenSource();
                    CancellationToken ct = tokenSource2.Token;
                    Status engine_status;
                    string Download_INFOS = Languages.Current["downloadStateCanceled"];
                    switch (DB_Download_STATE)
                    {
                        case "error":
                            engine_status = Status.error;
                            Download_INFOS = DB_Download_INFOS;
                            break;
                        case "cancel":
                        case "progress":
                        case "waitfordownloading":
                        case "assemblage":
                        case "rognage":
                        case "enregistrement":
                            engine_status = Status.cancel;
                            break;
                        case "success":
                        case "cleanup":
                            engine_status = Status.success;
                            Download_INFOS = Languages.Current["downloadStateDownloaded"];
                            break;
                        case "no_data":
                            engine_status = Status.no_data;
                            break;
                        case "deleted":
                            // engine_status = Status.enregistrement;
                            engine_status = Status.deleted;
                            if (string.IsNullOrEmpty(DB_Download_INFOS))
                            {
                                Download_INFOS = Languages.Current["downloadStateDeleted"];
                            }
                            else
                            {
                                Download_INFOS = DB_Download_INFOS;
                            }
                            break;
                        default:
                            engine_status = Status.cancel;
                            break;
                    }

                    if (engine_status != Status.deleted && engine_status == Status.success)
                    {
                        if (!System.IO.File.Exists(DB_Download_SAVE_DIRECTORY + DB_Download_FILE_NAME))
                        {
                            engine_status = Status.deleted;
                            Download_INFOS = Languages.Current["downloadStateNotFound"];
                        }
                    }
                    DownloadSettings engine = new DownloadSettings(downloadid, DB_Download_ID, DB_Download_LAYER_ID, urls, tokenSource2, ct, format, final_saveformat, DB_Download_ZOOM, DB_Download_TEMP_DIRECTORY, DB_Download_SAVE_DIRECTORY, DB_Download_FILE_NAME, filetempname, location, RESIZEWIDTH, RESIZEHEIGHT, new TileLoader(), COLORINTERPRETATION, SCALEINFO, DB_Download_NBR_TILES, layers.class_tile_url, layers.class_identifier, engine_status, layers.class_tiles_size, quality: DB_Download_QUALITY);
                    DownloadSettings.Add(engine, downloadid);
                    string commande_add = "add_download(" + downloadid + @",""" + engine_status.ToString() + @""",""" + DB_Download_FILE_NAME + @""",0," + DB_Download_NBR_TILES + @",""" + Download_INFOS + @""",""" + DB_Download_TIMESTAMP + @""");";
                    if (engine_status == Status.error)
                    {
                        commande_add += "updateprogress(" + downloadid + @", ""100"");";
                    }

                    download_panel_browser.ExecuteScriptAsyncWhenPageLoaded(commande_add);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("fonction download DB_Layer_Read : " + ex.Message);
                }
            }
        }

        public void Init_download_panel()
        {
            string resource_data = Collectif.ReadResourceString("HTML/download_panel.html");
            resource_data = Languages.ReplaceInString(resource_data);
            download_panel_browser.LoadHtml(resource_data);
            if (download_panel_browser is null) { return; }
            try
            {
                download_panel_browser.JavascriptObjectRepository.Register("download_Csharp_call_from_js", new Download_Csharp_call_from_js());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            try
            {
                download_panel_browser.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"download_Csharp_call_from_js\");");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CefSharp.BindObjectAsync(\"download_Csharp_call_from_js\");" + ex.Message);
            }
            DB_Download_Load();
            if (Settings.show_download_devtool)
            {
                download_panel_browser.ShowDevTools();
            }
        }

        private void Download_panel_open_overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Download_panel_close();
        }

        public void Download_panel_close()
        {
            MainWindow._instance.open_download_panel_titlebar_button.Opacity = 1;
            MainWindow._instance.open_download_panel_titlebar_button.IsHitTestVisible = true;
            DoubleAnimation hide_anim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond / 1.5))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
            hide_anim.Completed += hide_anim_Completed;
            download_panel.BeginAnimation(OpacityProperty, hide_anim); 

            void hide_anim_Completed(object sender, EventArgs e)
            {
                download_panel.Visibility = Visibility.Hidden;
                hide_anim.Completed -= hide_anim_Completed;
            }
        }

        public void Download_panel_open()
        {
            download_panel.Opacity = 0;
            MainWindow._instance.open_download_panel_titlebar_button.Opacity = 0.5;
            MainWindow._instance.open_download_panel_titlebar_button.IsHitTestVisible = false;
            download_panel.Visibility = Visibility.Visible;
            DoubleAnimation show_anim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond / 1.5))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
            download_panel.BeginAnimation(OpacityProperty, show_anim);
            download_panel_browser.Focus();
        }
    }

    public class Download_Csharp_call_from_js
    {
        public bool IsFileOk(int id)
        {
            if (id != 0)
            {
                var engine = DownloadSettings.GetEngineById(id);
                if (engine is null) return false;
                if (System.IO.Directory.Exists(engine.saveDirectory))
                {
                    if (System.IO.File.Exists(engine.saveDirectory + engine.fileName))
                    {
                        return true;
                    }
                    else if (engine.state == Status.success)
                    {
                        MainWindow.UpdateDownloadPanel(id, Languages.Current["downloadStateNotFound"], isImportant: true, state: Status.deleted);
                    }
                }
                else
                {
                    Debug.WriteLine("Le chemin n'existe pas : " + engine.saveDirectory);
                }
            }
            return false;
        }

        public void Download_stop(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow.StopingDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void Download_cancel(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow.CancelDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void Download_restart(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow._instance.RestartDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }

        public void Download_restart_from0(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow._instance.RestartDownloadFromZero(id_int);
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
            }
        }
        public void Download_reselect_area(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                var engine = DownloadSettings.GetEngineById(id_int);
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

        public void Download_openfolder(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (IsFileOk(id_int))
            {
                var engine = DownloadSettings.GetEngineById(id_int);
                if (engine is null) return;
                Process.Start("explorer.exe", "/select,\"" + engine.saveDirectory + engine.fileName + "\"");
            }
        }

        public void Download_openfile(double id)
        {
            int id_int = Convert.ToInt32(id);

            if (IsFileOk(id_int))
            {
                var engine = DownloadSettings.GetEngineById(id_int);
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

        public void Download_deletefile(double id)
        {
            int id_int = Convert.ToInt32(id);

            if (IsFileOk(id_int))
            {
                var engine = DownloadSettings.GetEngineById(id_int);
                if (engine is null) return;
                System.IO.File.Delete(engine.saveDirectory + engine.fileName);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow.UpdateDownloadPanel(id_int, Languages.Current["downloadStateDeleted"], "0", true, Status.deleted);
                        Database.DB_Download_Update(id_int, "STATE", nameof(Status.deleted));
                    }
                    catch (Exception ex)
                    {
                        Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                    }
                }, null);
                
                foreach (DownloadSettings eng in DownloadSettings.GetEngineList())
                {
                    if (eng.state == Status.success)
                    {
                        IsFileOk(eng.id);
                    }
                }
            }
        }
        public void Download_opentempfolder(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadSettings.GetEngineById(id_int);

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

        public void Download_delete_db(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadSettings.GetEngineById(id_int);
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

        public void Download_copyloc(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadSettings.GetEngineById(id_int);
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
        public void Download_copypath(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadSettings.GetEngineById(id_int);
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
        public void Download_makecourant(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadSettings.GetEngineById(id_int);
            if (engine is null) return;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    MainPage._instance.layer_browser.GetMainFrame().EvaluateScriptAsync("selectionner_calque_by_id(" + engine.layerid + ")");
                }
                catch (Exception ex)
                {
                    Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                }
            }, null);
        }

        public void Download_cancel_deleted_db(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadSettings.GetEngineById(id_int);
            Status engineinitialstate = engine.state;
            if (engine is null) return;

            List<Status> RunningState = new List<Status>() { Status.progress, Status.enregistrement, Status.rognage, Status.waitfordownloading };

            if (RunningState.Contains(engine.state) || engine.state == Status.pause)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
                {
                    Download_stop(id_int);
                    var result = await Message.SetContentDialog(Languages.GetWithArguments("downloadMessageAskCancelDeleteDownload", engine.fileName), "MapsInMyFolder", MessageDialogButton.YesNo).ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        try
                        {
                            if (RunningState.Contains(engineinitialstate))
                            {
                                Download_cancel(id_int);
                            }

                            MainWindow._instance?.MainPage?.download_panel_browser?.ExecuteScriptAsync("download_js_delete_db(" + id_int.ToString() + ");");
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
                            Download_restart(id_int);
                        }
                    }
                }, null);
            }
            else
            {
                MainWindow._instance?.MainPage?.download_panel_browser?.ExecuteScriptAsync("download_js_delete_db(" + id_int.ToString() + ");");
            }
        }
    }
}
