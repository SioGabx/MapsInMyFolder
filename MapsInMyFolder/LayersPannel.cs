using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using CefSharp;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using System.Data.SQLite;
using System.Timers;
using System.Windows.Input;
using System.Threading.Tasks;
using MapsInMyFolder.Commun;
using System.Windows.Controls;
using ModernWpf.Controls;
using System.Text.Json;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        string last_input = "";
        public async void SearchLayerStart(bool IsIgnoringLastInput = false)
        {
            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                if ((last_input != layer_searchbar.Text.Trim() || IsIgnoringLastInput) && layer_searchbar.Text != "Rechercher un calque, un site...")
                {
                    layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
                    if (layer_browser.IsLoaded)
                    {
                        last_input = layer_searchbar.Text.Trim();
                        await layer_browser.GetMainFrame().EvaluateScriptAsync("search(\"" + layer_searchbar.Text.Replace("\"", "") + "\")");
                        DebugMode.WriteLine("Search");
                    }
                }
            }, null);
        }

        public void Init_layer_panel()
        {
            if (layer_browser is null) { return; }
            try
            {
                //layer_browser.JavascriptObjectRepository.UnRegisterAll();
                layer_browser.JavascriptObjectRepository.Register("layer_Csharp_call_from_js", new Layer_Csharp_call_from_js());
            }
            catch (Exception ex)
            {
                DebugMode.WriteLine(ex.Message);
            }
            try
            {
                layer_browser.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"layer_Csharp_call_from_js\");");
                layer_browser.ExecuteScriptAsync("StartObserving();");
            }
            catch (Exception ex)
            {
                DebugMode.WriteLine(ex.Message);
            }
        }

        public void ReloadPage()
        {
            layer_browser.LoadHtml(DB_Layer_Load(), "http://siogabx.fr");
        }


        private void Layer_browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                layer_browser.GetMainFrame().EvaluateScriptAsync("selectionner_calque_by_id(" + Settings.layer_startup_id + ")");
            }
        }

        static List<Layers> DB_Layer_Read(SQLiteConnection conn, string query_command)
        {
            List<Layers> layersFavorite = new List<Layers>();
            List<Layers> layersClassicSort = new List<Layers>();
            SQLiteDataReader sqlite_datareader;
            SQLiteCommand sqlite_cmd = conn.CreateCommand();
            sqlite_cmd.CommandText = query_command;
            sqlite_datareader = sqlite_cmd.ExecuteReader();

            while (sqlite_datareader.Read())
            {
                try
                {
                    int GetOrdinal(string name)
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

                    string GetStringFromOrdinal(string name)
                    {
                        try
                        {
                            int ordinal = GetOrdinal(name);
                            if (ordinal == -1)
                            {
                                return "";
                            }
                            string get_setring = sqlite_datareader.GetString(ordinal);
                            if (string.IsNullOrEmpty(get_setring))
                            {
                                return "";
                            }
                            else
                            {
                                return Collectif.HTMLEntities(get_setring, true);
                            }
                        }
                        catch (Exception)
                        {
                            return "";
                        }
                    }
                    int GetIntFromOrdinal(string name)
                    {
                        int ordinal = GetOrdinal(name);
                        if (ordinal == -1)
                        {
                            return 0;
                        }
                        return sqlite_datareader.GetInt32(ordinal);
                    }

                    int DB_Layer_ID = GetIntFromOrdinal("ID");
                    string DB_Layer_NOM = GetStringFromOrdinal("NOM");
                    bool DB_Layer_FAVORITE = Convert.ToBoolean(GetIntFromOrdinal("FAVORITE"));
                    string DB_Layer_DESCRIPTION = GetStringFromOrdinal("DESCRIPTION");
                    string DB_Layer_CATEGORIE = GetStringFromOrdinal("CATEGORIE");
                    string DB_Layer_IDENTIFIANT = GetStringFromOrdinal("IDENTIFIANT");
                    string DB_Layer_TILE_URL = GetStringFromOrdinal("TILE_URL");
                    int DB_Layer_MIN_ZOOM = GetIntFromOrdinal("MIN_ZOOM");
                    int DB_Layer_MAX_ZOOM = GetIntFromOrdinal("MAX_ZOOM");
                    string DB_Layer_FORMAT = GetStringFromOrdinal("FORMAT");
                    string DB_Layer_SITE = GetStringFromOrdinal("SITE");
                    string DB_Layer_SITE_URL = GetStringFromOrdinal("SITE_URL");
                    string DB_Layer_STYLE = GetStringFromOrdinal("STYLE");
                    int DB_Layer_TILE_SIZE = GetIntFromOrdinal("TILE_SIZE");
                    string DB_Layer_TILECOMPUTATIONSCRIPT = GetStringFromOrdinal("TILECOMPUTATIONSCRIPT");
                    string DB_Layer_SPECIALSOPTIONS = GetStringFromOrdinal("SPECIALSOPTIONS");

                    bool doCreateSpecialsOptionsClass = true;
                    Layers.SpecialsOptions DeserializeSpecialsOptions = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(DB_Layer_SPECIALSOPTIONS))
                        {
                            DeserializeSpecialsOptions = JsonSerializer.Deserialize<Layers.SpecialsOptions>(DB_Layer_SPECIALSOPTIONS);
                            doCreateSpecialsOptionsClass = false;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Invalide JSON");
                    }
                    finally
                    {
                        if (doCreateSpecialsOptionsClass)
                        {
                            DeserializeSpecialsOptions = new Layers.SpecialsOptions();
                        }
                    }
                    if (string.IsNullOrEmpty(DB_Layer_TILECOMPUTATIONSCRIPT))
                    {
                        DB_Layer_TILECOMPUTATIONSCRIPT = Commun.Settings.tileloader_default_script;
                    }
                    DB_Layer_TILECOMPUTATIONSCRIPT = Collectif.HTMLEntities(DB_Layer_TILECOMPUTATIONSCRIPT, true);
                    Debug.WriteLine("Layer " + DB_Layer_NOM + " : Tilesize = " + DB_Layer_TILE_SIZE + " id = " + DB_Layer_ID);
                    Layers calque = new Layers(DB_Layer_ID, DB_Layer_FAVORITE, DB_Layer_NOM, DB_Layer_DESCRIPTION, DB_Layer_CATEGORIE, DB_Layer_IDENTIFIANT, DB_Layer_TILE_URL, DB_Layer_SITE, DB_Layer_SITE_URL, DB_Layer_MIN_ZOOM, DB_Layer_MAX_ZOOM, DB_Layer_FORMAT, DB_Layer_TILE_SIZE, DB_Layer_TILECOMPUTATIONSCRIPT, DeserializeSpecialsOptions);
                    Debug.WriteLine("Layer " + DB_Layer_NOM + " : Tilesize = " + DB_Layer_TILE_SIZE);
                    if (DB_Layer_FAVORITE)
                    {
                        layersFavorite.Add(calque);
                    }
                    else
                    {
                        layersClassicSort.Add(calque);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("fonction DB_Layer_Read : " + ex.Message);
                }
            }
            List<Layers> layers = layersFavorite.Concat(layersClassicSort).ToList();
            return layers;
        }

        string DB_Layer_Load()
        {
            string query = "SELECT *,'LAYERS' AS TYPE FROM LAYERS UNION SELECT *,'CUSTOMSLAYERS' FROM CUSTOMSLAYERS ORDER BY " + Commun.Settings.layers_Sort.ToString() + " " + Commun.Settings.Layers_Order.ToString() + " NULLS LAST";
            string query2 = "SELECT * FROM EDITEDLAYERS ORDER BY " + Commun.Settings.layers_Sort.ToString() + " " + Commun.Settings.Layers_Order.ToString() + " NULLS LAST";
            SQLiteConnection conn = Database.DB_Connection();
            if (conn is null)
            {
                return "<style>p{font-family: \"Segoe UI\";color:#888989;font-size:14px;}</style><p>Aucune base de données trouvée. Veuillez relancer l'application.</p><p>Si le problème persiste, veuillez réessayer ultérieurement</p>";
            }

            string baseHTML = DB_Layer_CreateHTML(DB_Layer_Read(conn, query), DB_Layer_Read(conn, query2));

            conn.Close();
            string finalHTML = baseHTML;
            if (Commun.Settings.show_layer_devtool)
            {
                layer_browser.ShowDevTools();
            }

            string injection = @"<script>
                document.body.style.setProperty(""--opacity_preview_background"", " + (1 - Commun.Settings.background_layer_opacity) + @");
                document.body.style.setProperty(""--background_layer_color_R"", " + Commun.Settings.background_layer_color_R + @");
                document.body.style.setProperty(""--background_layer_color_G"", " + Commun.Settings.background_layer_color_G + @");
                document.body.style.setProperty(""--background_layer_color_B"", " + Commun.Settings.background_layer_color_B + @");
               </script>";

            finalHTML += injection;

            return finalHTML;
        }
        static string DB_Layer_CreateHTML(List<Layers> layers, List<Layers> editedlayers)
        {
            Layers.Layers_Dictionary_List.Clear();
            string generated_layers = String.Empty;
            generated_layers += "<ul class=\"" + Commun.Settings.layerpanel_displaystyle.ToString().ToLower() + "\">";
            Dictionary<int, Layers> EditedLayersDictionnary = new Dictionary<int, Layers>();
            foreach (Layers individual_editedlayer in editedlayers)
            {
                EditedLayersDictionnary.Add(individual_editedlayer.class_id, individual_editedlayer);
            }
            List<Layers> GenerateHTMLFromLayerList(List<Layers> ListOfLayers, bool DoRejectLayer = true)
            {
                List<Layers> layersRejected = new List<Layers>();
                foreach (Layers individual_layer in ListOfLayers)
                {
                    Layers individual_layer_with_replacement;
                    bool hasValue = EditedLayersDictionnary.TryGetValue(individual_layer.class_id, out Layers value);
                    if (hasValue)
                    {
                        individual_layer_with_replacement = value;
                    }
                    else
                    {
                        individual_layer_with_replacement = individual_layer;
                    }

                    if (Commun.Settings.layerpanel_put_non_letter_layername_at_the_end)
                    {
                        if (DoRejectLayer && (string.IsNullOrEmpty(individual_layer_with_replacement.class_name) || !Char.IsLetter(individual_layer_with_replacement.class_name.Trim()[0])))
                        {
                            layersRejected.Add(individual_layer);
                            continue;
                        }
                    }
                    Dictionary<int, Layers> temp_Layers_dictionnary = new Dictionary<int, Layers> { { Convert.ToInt32(individual_layer_with_replacement.class_id), individual_layer_with_replacement } };

                    Layers.Layers_Dictionary_List.Add(temp_Layers_dictionnary);

                    string orangestar = "";
                    if (individual_layer_with_replacement.class_favorite)
                    {
                        orangestar = @"class=""star orange"" title=""Supprimer le calque des favoris""";
                    }
                    else
                    {
                        orangestar = @"class=""star"" title=""Ajouter le calque aux favoris""";
                    }

                    const string imgbase64 = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"; //1 pixel gif transparent -> disable base 

                    string supplement_class = "";
                    if (!Commun.Settings.layerpanel_website_IsVisible)
                    {
                        supplement_class += " displaynone";
                    }
                    generated_layers = generated_layers + @"<li class=""inview"" id=""" + individual_layer_with_replacement.class_id + @""">
          <div class=""layer_main_div"" style=""background-image:url(" + imgbase64.Trim() + @")"" id=""" + individual_layer_with_replacement.class_id + @""">
             <div class=""layer_main_div_background_image""></div>
            <div class=""layer_content"" data-layer=""" + individual_layer_with_replacement.class_identifiant + @""" title=""Sélectionner ce calque"" onclick=""selectionner_ce_calque(event, this," + individual_layer.class_id + @")"">
                  <div class=""layer_texte"" title=""" + Collectif.HTMLEntities(individual_layer_with_replacement.class_description) + @""">
                      <p class=""display_name"">" + Collectif.HTMLEntities(individual_layer_with_replacement.class_name) + @"</p>
                      <p class=""zoom"">[" + individual_layer_with_replacement.class_min_zoom + "-" + individual_layer_with_replacement.class_max_zoom + @"]</p>
                      <p class=""layer_website" + supplement_class + @""">" + individual_layer_with_replacement.class_site + @"</p>
                      <p class=""layer_categorie" + supplement_class + @""">" + individual_layer_with_replacement.class_categorie + @"</p>
                  </div>
                  <div " + orangestar + @" onclick=""ajouter_aux_favoris(event, this," + individual_layer_with_replacement.class_id + @")""></div>
              </div>
          </div>
      </li>";
                }
                return layersRejected;
            }

            List<Layers> ListOfLayerRejected = GenerateHTMLFromLayerList(layers);
            GenerateHTMLFromLayerList(ListOfLayerRejected, false);

            generated_layers += "</ul>";
            string resource_data = Collectif.ReadResourceString("html/layer_panel.html");
            string final_generated_layers = resource_data.Replace("<!--htmllayerplaceholder-->", generated_layers);
            return final_generated_layers;
        }

        public void Set_current_layer(int id)
        {
            int layer_startup_id = Commun.Settings.layer_startup_id;
            DebugMode.WriteLine("Set layer");
            string last_format = "";
            if (Curent.Layer.class_format is not null && Curent.Layer.class_format.Trim() != "")
            {
                last_format = Curent.Layer.class_format;
            }

            Layers layer = Layers.GetLayerById(id);
            if (Layers.Layers_Dictionary_List.Count == 0)
            {
                layer = Layers.Empty(0);
                Dictionary<int, Layers> x = new Dictionary<int, Layers> { { 0, layer } };
                Layers.Layers_Dictionary_List.Add(x);
                DebugMode.WriteLine("Ajout layer vide");
            }
            if (layer is null || layer_startup_id == 0)
            {
                layer = Layers.GetLayerById(Layers.Layers_Dictionary_List[0].Keys.First());
                Commun.Settings.layer_startup_id = layer.class_id;
                if (layer_startup_id == 0)
                {
                    layer_startup_id = layer.class_id;
                }
            }
            if (layer is not null)
            {
                try
                {
                    Layers.Convert.ToCurentLayer(layer);
                    Debug.WriteLine("Set curent layer " + layer.class_name + " = " + layer.class_tiles_size);
                    if (layer.class_format == "png")
                    {
                        MapTileLayer_Transparent.TileSource = new TileSource { UriFormat = layer.class_tile_url, LayerID = layer.class_id };
                        MapTileLayer_Transparent.Opacity = 1;

                        if (last_format != "png" && last_format.Trim() != "" && layer.class_identifiant is not null)
                        {
                            try
                            {
                                UIElement basemap = new MapTileLayer
                                {
                                    TileSource = new TileSource { UriFormat = Layers.GetLayerById(layer_startup_id).class_tile_url, LayerID = layer_startup_id },
                                    SourceName = Layers.GetLayerById(layer_startup_id).class_identifiant + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                    MaxZoomLevel = Layers.GetLayerById(layer_startup_id).class_max_zoom,
                                    MinZoomLevel = Layers.GetLayerById(layer_startup_id).class_min_zoom,
                                    Description = "© [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)"
                                };

                                basemap.Opacity = Commun.Settings.background_layer_opacity;
                                mapviewer.MapLayer = basemap;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Erreur changement de calque : " + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        UIElement layer_uielement = new MapTileLayer
                        {
                            TileSource = new TileSource { UriFormat = layer.class_tile_url, LayerID = layer.class_id },
                            SourceName = layer.class_identifiant + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                            MaxZoomLevel = layer.class_max_zoom,
                            MinZoomLevel = layer.class_min_zoom,

                            Description = layer.class_description
                        };

                        MapTileLayer_Transparent.TileSource = new TileSource();
                        MapTileLayer_Transparent.Opacity = 0;
                        mapviewer.MapLayer.Opacity = 1;
                        mapviewer.MapLayer = layer_uielement;
                    }

                    if (Commun.Settings.zoom_limite_taille_carte)
                    {
                        if (layer.class_min_zoom < 3)
                        {
                            mapviewer.MinZoomLevel = 2;
                        }
                        else
                        {
                            mapviewer.MinZoomLevel = layer.class_min_zoom;
                        }
                        mapviewer.MaxZoomLevel = layer.class_max_zoom;
                    }
                    else
                    {
                        mapviewer.MinZoomLevel = 2;
                        mapviewer.MaxZoomLevel = 22;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Erreur changement de calque" + ex.Message);
                }
            }
        }

        public static void ClearCache(int id, bool ShowMessageBox = true)
        {
            if (id == 0) { return; }

            Layers layers = Layers.GetLayerById(id);
            if (layers is null) { return; }
            try
            {
                Javascript.EngineDeleteById(id);
                string temp_dir = Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifiant);
                Debug.WriteLine("Cache path : " + temp_dir);
                if (Directory.Exists(temp_dir))
                {
                    Directory.Delete(temp_dir, true);
                }
                //TODO : Fixe error : L'objet Visual spécifié est déjà un enfant d'un autre objet Visual ou la racine d'une classe CompositionTarget.
                //Generate dinamicly and not set 

                if (ShowMessageBox)
                {
                    Message.NoReturnBoxAsync("Le cache du calque \"" + layers.class_name + "\" à été vidé", "Opération réussie");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur lors du nettoyage du cache : " + ex.Message);
            }
        }

        public static void DBLayerFavorite(int id, int fav_state)
        {
            if (id == 0) { return; }
            try
            {
                SQLiteConnection conn = Database.DB_Connection();
                if (conn is null)
                {
                    DebugMode.WriteLine("Connection to bdd is null");
                    return;
                }
                SQLiteCommand sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = "UPDATE LAYERS SET FAVORITE = " + fav_state + " WHERE ID=" + id;
                sqlite_cmd.ExecuteNonQuery();

                sqlite_cmd.CommandText = "UPDATE EDITEDLAYERS SET FAVORITE = " + fav_state + " WHERE ID=" + id;
                sqlite_cmd.ExecuteNonQuery();

                sqlite_cmd.CommandText = "UPDATE CUSTOMSLAYERS SET FAVORITE = " + fav_state + " WHERE ID=" + id;
                sqlite_cmd.ExecuteNonQuery();
                conn.Close();

                Layers.GetLayerById(id).class_favorite = Convert.ToBoolean(fav_state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("fonction DB_Layer_Favorite : " + ex.Message);
            }
        }

        public void LayerTilePreview_RequestUpdate()
        {
            layer_browser.ExecuteScriptAsyncWhenPageLoaded("UpdatePreview();");
            return;
        }

        public static void LayerEditOpenWindow(int id = -1)
        {
            MainWindow._instance.FrameLoad_CustomOrEditLayers(id);
        }

        public string LayerTilePreview_ReturnUrl(int id)
        {
            Layers layer = Layers.GetLayerById(id);

            if (layer is not null)
            {
                try
                {
                    int layer_startup_id = Commun.Settings.layer_startup_id;
                    if (layer_startup_id == 0)
                    {
                        layer_startup_id = Layers.GetLayerById(Layers.Layers_Dictionary_List[0].Keys.First()).class_id;
                    }

                    string class_tile_url_p1 = layer.class_tile_url;
                    int min_zoom = layer.class_min_zoom;
                    int max_zoom = layer.class_max_zoom;

                    Layers StartingLayer = Layers.GetLayerById(layer_startup_id);

                    int back_min_zoom = min_zoom;
                    int back_max_zoom = max_zoom;
                    if (StartingLayer is not null)
                    {
                        back_min_zoom = StartingLayer.class_min_zoom;
                        back_max_zoom = StartingLayer.class_max_zoom;
                    }

                    double Latitude = mapviewer.Center.Latitude;
                    double Longitude = mapviewer.Center.Longitude;
                    int Zoom = Convert.ToInt32(Math.Round(mapviewer.TargetZoomLevel));
                    if (Zoom < min_zoom) { Zoom = min_zoom; }
                    if (Zoom > max_zoom) { Zoom = max_zoom; }

                    if (Zoom < back_min_zoom) { Zoom = Math.Max(back_min_zoom, Zoom); }
                    if (Zoom > back_max_zoom) { Zoom = Math.Min(back_max_zoom, Zoom); }

                    List<int> TileNumber = Collectif.CoordonneesToTile(Latitude, Longitude, Zoom);
                    //string Replacements(string origin)
                    //{
                    //    string origin_result = origin;
                    //    origin_result = origin_result.Replace("{x}", TileNumber[0].ToString());
                    //    origin_result = origin_result.Replace("{y}", TileNumber[1].ToString());
                    //    origin_result = origin_result.Replace("{z}", Zoom.ToString());
                    //    origin_result = origin_result.Replace(" ", "%20");
                    //    return origin_result;
                    //}
                    class_tile_url_p1 = Collectif.Replacements(class_tile_url_p1, TileNumber[0].ToString(), TileNumber[1].ToString(), Zoom.ToString(), id);

                    string class_tile_url_p2 = "";
                    if (StartingLayer is not null)
                    {
                        class_tile_url_p2 = StartingLayer.class_tile_url;
                    }
                    class_tile_url_p2 = Collectif.Replacements(class_tile_url_p2, TileNumber[0].ToString(), TileNumber[1].ToString(), Zoom.ToString(), id);
                    return class_tile_url_p1 + " " + class_tile_url_p2;
                }
                catch (Exception ex)
                {
                    DebugMode.WriteLine("error exception " + ex.Message);
                }
            }
            else
            {
                DebugMode.WriteLine("layer is null");
                return "";
            }
            DebugMode.WriteLine("????");
            return "";
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Marquer les membres comme étant static", Justification = "Used by CEFSHARP, static isnt a option here")]
    public class Layer_Csharp_call_from_js
    {
        public void Layer_favorite_add(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.DBLayerFavorite(id_int, 1);
            }, null);
            DebugMode.WriteLine("Adding layer" + id_int + " to favorite");
        }

        public void Clear_cache(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.ClearCache(id_int);
            }, null);
            DebugMode.WriteLine("Clear_cache layer " + id_int);
        }

        public void Layer_favorite_remove(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.DBLayerFavorite(id_int, 0);
            }, null);
            DebugMode.WriteLine("Removing layer" + id_int + " to favorite");
        }

        public void Layer_edit(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.LayerEditOpenWindow(id_int);
            }, null);
            DebugMode.WriteLine("Editing layer" + id_int + " to favorite");
        }

        public void Layer_set_current(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow._instance.MainPage.Set_current_layer(id_int);
            }, null);
        }
        public void Request_search_update()
        {
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow._instance.MainPage.SearchLayerStart(true);
            }, null);
        }

        public void Refresh_panel()
        {
            App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow._instance.MainPage.ReloadPage();
                MainWindow._instance.MainPage.SearchLayerStart();
            }, null);
        }

        public string Gettilepreviewurlfromid(double id = 0)
        {
            if (!Commun.Settings.layerpanel_livepreview)
            {
                return "";
            }
            async Task<string> Gettilepreviewurlfromid_interne(double id)
            {
                int id_int = Convert.ToInt32(id);
                DispatcherOperation op = App.Current.Dispatcher.BeginInvoke(new Func<string>(() => MainWindow._instance.MainPage.LayerTilePreview_ReturnUrl(id_int)));
                await op;
                return op.Result.ToString();
            }
            return Gettilepreviewurlfromid_interne(id).Result;
        }
    }
}