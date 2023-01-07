using CefSharp;
using MapsInMyFolder.MapControl;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MapsInMyFolder.Commun;

using System.Data.SQLite;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        // int settings_max_download_simult;

        void DB_Download_Load()
        {
            // await Task.Run(() =>
            // {
            //Thread.Sleep(300);
            DebugMode.WriteLine("Loading downloads");
            //App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            //{
            //on load, add each with id = id* -1
            //Download_main_engine_class engine = new Download_main_engine_class(id, urls, tokenSource2, ct, format, z, save_temp_directory, save_directory, filename, filetempname, location, nbr_of_tiles, urlbase, identifiant, style, Status.waitfordownloading, tile_size);
            //download_temp_engine_class.Add(engine, id);
            SQLiteConnection conn = Database.DB_Connection();
            if (conn is null)
            {
                return;
            }
            Database.DB_Download_Init(conn);
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = "SELECT * FROM 'DOWNLOADS' ORDER BY 'TIMESTAMP' ASC";
            sqlite_datareader = sqlite_cmd.ExecuteReader();

            while (sqlite_datareader.Read())
            {
                try
                {
                    int DB_Download_LAYER_ID = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("LAYER_ID"));
                    Layers layers = Layers.GetLayerById(DB_Download_LAYER_ID) ?? Layers.Empty();
                    int DB_Download_ID = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("ID"));
                    int DB_Download_ZOOM = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("ZOOM"));
                    int DB_Download_NBR_TILES = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("NBR_TILES"));
                    int REDIMWIDTH = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("REDIMWIDTH"));
                    int REDIMHEIGHT = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("REDIMHEIGHT"));
                    int DB_Download_QUALITY = sqlite_datareader.GetInt32(sqlite_datareader.GetOrdinal("QUALITY"));
                    string DB_Download_TIMESTAMP = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("TIMESTAMP"));
                    string DB_Download_TEMP_DIRECTORY = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("TEMP_DIRECTORY"));
                    string DB_Download_SAVE_DIRECTORY = sqlite_datareader.GetString(sqlite_datareader.GetOrdinal("SAVE_DIRECTORY"));
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

                    //List<Url_class> urls = Collectif.GetUrl.GetListOfUrlFromLocation(location, DB_Download_ZOOM, layers.class_tile_url, DB_Download_LAYER_ID, downloadid);
                    List<Url_class> urls = null;
                    CancellationTokenSource tokenSource2 = new CancellationTokenSource();
                    CancellationToken ct = tokenSource2.Token;
                    Status engine_status;
                    string Download_INFOS = "Annulé.";
                    switch (DB_Download_STATE)
                    {
                        case "waitfordownloading":
                            // engine_status = Status.waitfordownloading;
                            engine_status = Status.cancel;
                            break;
                        case "error":
                            engine_status = Status.error;
                            Download_INFOS = DB_Download_INFOS;
                            break;
                        case "cancel":
                            engine_status = Status.cancel;
                            break;
                        case "success":
                            engine_status = Status.success;
                            Download_INFOS = "Téléchargé.";
                            break;
                        case "pause":
                            //engine_status = Status.pause;
                            engine_status = Status.cancel;
                            break;
                        case "progress":
                            //engine_status = Status.progress;
                            engine_status = Status.cancel;
                            break;
                        case "no_data":
                            engine_status = Status.no_data;
                            break;
                        case "assemblage":
                            //engine_status = Status.assemblage;
                            engine_status = Status.cancel;
                            break;
                        case "rognage":
                            //engine_status = Status.rognage;
                            engine_status = Status.cancel;
                            break;
                        case "enregistrement":
                            // engine_status = Status.enregistrement;
                            engine_status = Status.cancel;
                            break;
                        case "deleted":
                            // engine_status = Status.enregistrement;
                            engine_status = Status.deleted;
                            if (string.IsNullOrEmpty(DB_Download_INFOS))
                            {
                                Download_INFOS = "Supprimé.";
                            }
                            else
                            {
                                Download_INFOS = DB_Download_INFOS;
                            }

                            break;
                        default:
                            // engine_status = Status.waitfordownloading;
                            engine_status = Status.cancel;
                            break;
                    }

                    if (engine_status != Status.deleted && engine_status == Status.success)
                    {
                        if (!System.IO.File.Exists(DB_Download_SAVE_DIRECTORY + DB_Download_FILE_NAME))
                        {
                            engine_status = Status.deleted;
                            Download_INFOS = "Introuvable.";
                        }
                    }
                    DownloadClass engine = new DownloadClass(downloadid, DB_Download_ID, DB_Download_LAYER_ID, urls, tokenSource2, ct, format, final_saveformat, DB_Download_ZOOM, DB_Download_TEMP_DIRECTORY, DB_Download_SAVE_DIRECTORY, DB_Download_FILE_NAME, filetempname, location, REDIMWIDTH, REDIMHEIGHT, new TileGenerator(), DB_Download_NBR_TILES, layers.class_tile_url, layers.class_identifiant, engine_status, layers.class_tiles_size, quality: DB_Download_QUALITY);
                    DebugMode.WriteLine("Set DownloadClass : layer id" + layers.class_id + " / " + layers.class_name + " tile size is " + layers.class_tiles_size);
                   // engine.TileLoaderGenerator.Layer = Layers.GetLayerById(DB_Download_LAYER_ID);
                    DownloadClass.Add(engine, downloadid);
                    string commande_add = "add_download(" + downloadid + @",""" + engine_status.ToString() + @""",""" + DB_Download_FILE_NAME + @""",0," + DB_Download_NBR_TILES + @",""" + Download_INFOS + @""",""" + DB_Download_TIMESTAMP + @""");";
                    if (engine_status == Status.error)
                    {
                        commande_add += "updateprogress(" + downloadid + @", ""100"");";
                    }

                    download_panel_browser.ExecuteScriptAsyncWhenPageLoaded(commande_add);
                    DebugMode.WriteLine(commande_add);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("fonction DB_Layer_Read : " + ex.Message);
                }
            }

            conn.Close();
            // }, null);
            // 
            //  });
        }

        public void Init_download_panel()
        {
            string resource_data = Collectif.ReadResourceString("html/download_panel.html");
            download_panel_browser.LoadHtml(resource_data);
            //download_panel_browser.Load(Commun.Settings.working_folder + @"\download_panel.html");
            //read file xml
            if (download_panel_browser is null) { return; }
            try
            {
                download_panel_browser.JavascriptObjectRepository.Register("download_Csharp_call_from_js", new Download_Csharp_call_from_js());
            }
            catch (Exception ex)
            {
                DebugMode.WriteLine(ex.Message);
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
            if (Commun.Settings.show_download_devtool)
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
            DoubleAnimation hide_anim = new DoubleAnimation(0d, Commun.Settings.animations_duration / 1.5)
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
            hide_anim.Completed += (s, e) => download_panel.Visibility = Visibility.Hidden;
            download_panel.BeginAnimation(UIElement.OpacityProperty, hide_anim);
        }

        public void Download_panel_open()
        {
            download_panel.Opacity = 0;
            MainWindow._instance.open_download_panel_titlebar_button.Opacity = 0.5;
            MainWindow._instance.open_download_panel_titlebar_button.IsHitTestVisible = false;
            download_panel.Visibility = Visibility.Visible;
            DoubleAnimation show_anim = new DoubleAnimation(1, Commun.Settings.animations_duration / 1.5)
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
            download_panel.BeginAnimation(UIElement.OpacityProperty, show_anim);
            download_panel_browser.Focus();
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Marquer les membres comme étant static", Justification = "Used by CEFSHARP, static isnt a option here")]
    public class Download_Csharp_call_from_js
    {
        public bool IsFileOk(int id)
        {
            if (id != 0)
            {
                var engine = DownloadClass.GetEngineById(id);
                if (engine is null) return false;
                if (System.IO.Directory.Exists(engine.save_directory))
                {
                    if (System.IO.File.Exists(engine.save_directory + engine.file_name))
                    {
                        return true;
                    }
                    else
                    {
                        if (engine.state == Status.success)
                        {
                            MainWindow.UpdateDownloadPanel(id, "Introuvable.", isimportant: true, state: Status.deleted);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Le chemin n'existe pas : " + engine.save_directory);
                }
            }
            return false;
        }

        public void Download_stop(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow.StopingDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        Message.NoReturnBoxAsync(ex.Message, "Erreur");
                    }
                }, null);
            }
        }

        public void Download_cancel(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow.CancelDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        Message.NoReturnBoxAsync(ex.Message, "Erreur");
                    }
                }, null);
            }
        }

        public void Download_restart(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow._instance.RestartDownload(id_int);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        Message.NoReturnBoxAsync(ex.Message, "Erreur");
                    }
                }, null);
            }
        }

        public void Download_restart_from0(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow._instance.RestartDownloadFromZero(id_int);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        Message.NoReturnBoxAsync(ex.Message, "Erreur");
                    }
                }, null);
            }
        }
        public void Download_reselect_area(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (id_int != 0)
            {
                var engine = DownloadClass.GetEngineById(id_int);
                if (engine is null) return;
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
               {
                   try
                   {
                       //double NO_Latitude = Convert.ToDouble(engine.location["NO_Latitude"]);
                       //double NO_Longitude = Convert.ToDouble(engine.location["NO_Longitude"]);
                       //double SE_Latitude = Convert.ToDouble(engine.location["SE_Latitude"]);
                       //double SE_Longitude = Convert.ToDouble(engine.location["SE_Longitude"]);
                       MainPage.MapViewerSetSelection(engine.location, true);
                   }
                   catch (Exception ex)
                   {
                       //MessageBox.Show(ex.Message);
                       Message.NoReturnBoxAsync(ex.Message, "Erreur");
                   }
               }, null);
            }
        }

        public void Download_openfolder(double id)
        {
            int id_int = Convert.ToInt32(id);
            if (IsFileOk(id_int))
            {
                var engine = DownloadClass.GetEngineById(id_int);
                if (engine is null) return;
                Process.Start("explorer.exe", "/select,\"" + engine.save_directory + engine.file_name + "\"");
            }
        }

        public void Download_openfile(double id)
        {
            int id_int = Convert.ToInt32(id);

            if (IsFileOk(id_int))
            {
                var engine = DownloadClass.GetEngineById(id_int);
                if (engine is null) return;
                new Process
                {
                    StartInfo = new ProcessStartInfo(engine.save_directory + engine.file_name)
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
                var engine = DownloadClass.GetEngineById(id_int);
                if (engine is null) return;
                System.IO.File.Delete(engine.save_directory + engine.file_name);

                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    try
                    {
                        MainWindow.UpdateDownloadPanel(id_int, "Supprimé", "0", true, Status.deleted);
                        Database.DB_Download_Update(id_int, "STATE", nameof(Status.deleted));
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                        Message.NoReturnBoxAsync(ex.Message, "Erreur");
                    }
                }, null);

                foreach (DownloadClass eng in DownloadClass.GetEngineList())
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
            var engine = DownloadClass.GetEngineById(id_int);

            if (engine is null) return;
            if (System.IO.Directory.Exists(engine.save_temp_directory))
            {
                Process.Start("explorer.exe", "\"" + engine.save_temp_directory + "\"");
            }
            else
            {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync("Le dossier temporaire n'existe plus. \n\nChemin du dossier : \n" + engine.save_temp_directory, "Erreur");
                }, null);
            }
        }

        public void Download_delete_db(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadClass.GetEngineById(id_int);
            if (engine is null) return;
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    Database.DB_Download_Delete(id_int);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    Message.NoReturnBoxAsync(ex.Message, "Erreur");
                }
            }, null);
        }

        public void Download_copyloc(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadClass.GetEngineById(id_int);
            if (engine is null) return;
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
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
                    //MessageBox.Show(ex.Message);
                    Message.NoReturnBoxAsync(ex.Message, "Erreur");
                }
            }, null);
        }
        public void Download_copypath(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadClass.GetEngineById(id_int);
            if (engine is null) return;
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    string clipboard_path = engine.save_directory + engine.file_name;
                    Clipboard.SetText(clipboard_path);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    Message.NoReturnBoxAsync(ex.Message, "Erreur");
                }
            }, null);
        }
        public void Download_makecourant(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadClass.GetEngineById(id_int);
            if (engine is null) return;
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                try
                {
                    MainPage._instance.Set_current_layer(engine.layerid);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    Message.NoReturnBoxAsync(ex.Message, "Erreur");
                }
            }, null);
        }

        public void Download_cancel_deleted_db(double id)
        {
            int id_int = Convert.ToInt32(id);
            var engine = DownloadClass.GetEngineById(id_int);
            Status engineinitialstate = engine.state;
            if (engine is null) return;

            List<Status> RunningState = new List<Status>() { Status.progress, Status.enregistrement, Status.rognage, Status.waitfordownloading };

            if (RunningState.Contains(engine.state) || engine.state == Status.pause)
            {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
                {
                    Download_stop(id_int);
                    //var result = await dialog.ShowAsync();
                    var result = await Message.SetContentDialog("Voullez-vous annuler et supprimer le téléchargement de " + engine.file_name + " ? ", "Supprimer le téléchargement", MessageDialogButton.YesNo).ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        try
                        {
                            if (MainWindow._instance.MainPage.download_panel_browser is null) { return; }
                            if (RunningState.Contains(engineinitialstate))
                            {
                                Download_cancel(id_int);
                            }

                            MainWindow._instance.MainPage.download_panel_browser.ExecuteScriptAsync("download_js_delete_db(" + id_int.ToString() + ");");
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(ex.Message);
                            Message.NoReturnBoxAsync(ex.Message, "Erreur");
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
                MainWindow._instance.MainPage.download_panel_browser.ExecuteScriptAsync("download_js_delete_db(" + id_int.ToString() + ");");
            }
        }
    }
}
