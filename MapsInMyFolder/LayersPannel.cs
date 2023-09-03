using CefSharp;
using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        string last_input;
        public string SearchGetText()
        {
            string searchText = null;
            if (layer_searchbar.Text != Languages.Current["searchLayerPlaceholder"])
            {
                searchText = layer_searchbar.Text.Replace("'", "’").Trim();
            }
            return searchText;
        }

        public async void SearchLayerStart(bool IsIgnoringLastInput = false)
        {
            await Task.Run(async () =>
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    string SearchValue = SearchGetText();
                    if ((last_input != SearchValue || IsIgnoringLastInput) && SearchValue != null && layer_browser?.IsInitialized == true)
                    {
                        last_input = SearchValue;
                        Debug.WriteLine("Search: " + SearchValue);
                        layer_browser?.ExecuteScriptAsync("searchAndUpdatePreview", SearchValue);
                    }
                }));
            });
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
                Debug.WriteLine(ex.Message);
            }
            try
            {
                layer_browser.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"layer_Csharp_call_from_js\");");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
                int LayerId = Layers.Current.class_id;
                string scroll = ", false";
                if (LayerId == -1)
                {
                    LayerId = Settings.layer_startup_id;
                    scroll = "";
                }
                layer_browser.GetMainFrame().EvaluateScriptAsync($"selectionner_calque_by_id({LayerId}{scroll})");
            }
        }

        public static List<Layers> DB_Layer_Read(string query_command)
        {
            List<Layers> layersFavorite = new List<Layers>();
            List<Layers> layersClassicSort = new List<Layers>();
            using SQLiteDataReader sqlite_datareader = Database.ExecuteExecuteReaderSQLCommand(query_command).Reader;

            while (sqlite_datareader.Read())
            {
                try
                {
                    SQLiteDataReader sqlite_datareaderCopy = sqlite_datareader;
                    string GetStringFromOrdinal(string name)
                    {
                        return Database.GetStringFromOrdinal(sqlite_datareaderCopy, name);
                    }
                    int? GetIntFromOrdinal(string name)
                    {
                        return Database.GetIntFromOrdinal(sqlite_datareaderCopy, name);
                    }

                    int? DB_Layer_ID = GetIntFromOrdinal("ID");
                    string DB_Layer_NAME = GetStringFromOrdinal("NAME").RemoveNewLineChar();
                    bool DB_Layer_FAVORITE = Convert.ToBoolean(GetIntFromOrdinal("FAVORITE"));
                    string DB_Layer_DESCRIPTION = GetStringFromOrdinal("DESCRIPTION");
                    string DB_Layer_CATEGORY = GetStringFromOrdinal("CATEGORY").RemoveNewLineChar();
                    string DB_Layer_COUNTRY = GetStringFromOrdinal("COUNTRY").RemoveNewLineChar();
                    string DB_Layer_IDENTIFIER = GetStringFromOrdinal("IDENTIFIER").RemoveNewLineChar();
                    string DB_Layer_TILE_URL = GetStringFromOrdinal("TILE_URL").RemoveNewLineChar();
                    int? DB_Layer_MIN_ZOOM = GetIntFromOrdinal("MIN_ZOOM");
                    int? DB_Layer_MAX_ZOOM = GetIntFromOrdinal("MAX_ZOOM");
                    string DB_Layer_FORMAT = GetStringFromOrdinal("FORMAT");
                    string DB_Layer_SITE = GetStringFromOrdinal("SITE").RemoveNewLineChar();
                    string DB_Layer_SITE_URL = GetStringFromOrdinal("SITE_URL").RemoveNewLineChar();
                    string DB_Layer_STYLE = GetStringFromOrdinal("STYLE");
                    int? DB_Layer_TILE_SIZE = GetIntFromOrdinal("TILE_SIZE");
                    string DB_Layer_SCRIPT = GetStringFromOrdinal("SCRIPT");
                    string DB_Layer_VISIBILITY = GetStringFromOrdinal("VISIBILITY");
                    string DB_Layer_SPECIALSOPTIONS = GetStringFromOrdinal("SPECIALSOPTIONS");
                    string DB_Layer_RECTANGLES = GetStringFromOrdinal("RECTANGLES");
                    int DB_Layer_VERSION = GetIntFromOrdinal("VERSION") ?? 0;
                    bool DB_Layer_HAS_SCALE = Convert.ToBoolean(GetIntFromOrdinal("HAS_SCALE"));

                    bool doCreateSpecialsOptionsClass = true;
                    Layers.SpecialsOptions DeserializeSpecialsOptions = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(DB_Layer_SPECIALSOPTIONS))
                        {
                            DeserializeSpecialsOptions = System.Text.Json.JsonSerializer.Deserialize<Layers.SpecialsOptions>(DB_Layer_SPECIALSOPTIONS);
                            doCreateSpecialsOptionsClass = false;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Invalide JSON inside layer :" + DB_Layer_ID + " named : " + DB_Layer_NAME);
                    }
                    finally
                    {
                        if (doCreateSpecialsOptionsClass)
                        {
                            DeserializeSpecialsOptions = new Layers.SpecialsOptions();
                        }
                    }

                    if (!string.IsNullOrEmpty(DB_Layer_SCRIPT))
                    {
                        DB_Layer_SCRIPT = Collectif.HTMLEntities(DB_Layer_SCRIPT, true);
                    }

                    Layers calque = new Layers((int)DB_Layer_ID, DB_Layer_FAVORITE, DB_Layer_NAME, DB_Layer_DESCRIPTION, DB_Layer_CATEGORY, DB_Layer_COUNTRY, DB_Layer_IDENTIFIER, DB_Layer_TILE_URL, DB_Layer_SITE, DB_Layer_SITE_URL, DB_Layer_MIN_ZOOM, DB_Layer_MAX_ZOOM, DB_Layer_FORMAT, DB_Layer_STYLE, DB_Layer_TILE_SIZE, DB_Layer_SCRIPT, DB_Layer_VISIBILITY, DeserializeSpecialsOptions, DB_Layer_RECTANGLES, DB_Layer_VERSION, DB_Layer_HAS_SCALE);
                    if (DB_Layer_FAVORITE && Settings.layerpanel_favorite_at_top)
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
            return layersFavorite.Concat(layersClassicSort).ToList();
        }

        string DB_Layer_Load()
        {
            string OriginalLayersGetQuery = $"SELECT *,'LAYERS' AS TYPE FROM LAYERS UNION SELECT *,'CUSTOMSLAYERS' FROM CUSTOMSLAYERS ORDER BY {Settings.layers_Sort} NULLS LAST";
            string EditedLayersGetQuery = $"SELECT * FROM EDITEDLAYERS ORDER BY {Settings.layers_Sort} NULLS LAST";
            using (SQLiteConnection conn = Database.DB_Connection())
            {
                if (conn is null)
                {
                    return "<style>p{font-family: \"Segoe UI\";color:#888989;font-size:14px;}</style><p>" + Languages.Current["layerMessageNoDatabase"] + "</p>";
                }
            }

            string baseHTML = DB_Layer_CreateHTML(DB_Layer_Read(OriginalLayersGetQuery), DB_Layer_Read(EditedLayersGetQuery));

            if (Settings.show_layer_devtool)
            {
                layer_browser.ShowDevTools();
            }

            StringBuilder PropertyBuilder = new StringBuilder();
            PropertyBuilder.Append("<script>");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--opacity_preview_background\", {1 - Settings.background_layer_opacity});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_R\", {Settings.background_layer_color_R});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_G\", {Settings.background_layer_color_G});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_B\", {Settings.background_layer_color_B});");
            PropertyBuilder.Append("</script>");

            baseHTML += PropertyBuilder.ToString();

            return baseHTML;

        }

        static string DB_Layer_CreateHTML(List<Layers> layers, List<Layers> editedlayers)
        {
            Layers.Clear();
            StringBuilder generated_layers = new StringBuilder("<ul class=\"");
            generated_layers.Append(Settings.layerpanel_displaystyle.ToString().ToLower());
            generated_layers.AppendLine("\">");

            Dictionary<int, Layers> EditedLayersDictionnary = editedlayers.ToDictionary(l => l.class_id, l => l);

            List<Layers> GenerateHTMLFromLayerList(List<Layers> ListOfLayers, bool DoRejectLayer = true)
            {
                List<Layers> layersRejected = new List<Layers>();

                string[] layer_country_to_keep = Settings.filter_layers_based_on_country.Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (Layers InitialLayerFromList in ListOfLayers)
                {
                    int Initial_ClassVersion = InitialLayerFromList.class_version;
                    Layers LayerWithReplacement = InitialLayerFromList;
                    bool layerHasReplacement = EditedLayersDictionnary.TryGetValue(InitialLayerFromList.class_id, out Layers replacementLayer);
                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    if (layerHasReplacement)
                    {
                        foreach (FieldInfo field in typeof(Layers).GetFields(bindingFlags))
                        {
                            object replacementValue = field.GetValue(replacementLayer);
                            if (replacementValue is null)
                            {
                                continue;
                            }
                            Type replacementValueType = replacementValue.GetType();

                            if (replacementValueType == typeof(string))
                            {
                                string replacementValueTypeToString = replacementValue as string;
                                if (replacementValueTypeToString != null)
                                {
                                    field.SetValue(LayerWithReplacement, replacementValueTypeToString);
                                }
                            }
                            else
                            {
                                field.SetValue(LayerWithReplacement, replacementValue);
                            }
                        }
                    }

                    if (LayerWithReplacement?.class_visibility?.Trim() == "DELETED")
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(LayerWithReplacement.class_script))
                    {
                        LayerWithReplacement.class_script = Settings.tileloader_default_script;
                    }
                    if (string.IsNullOrEmpty(LayerWithReplacement.class_visibility))
                    {
                        LayerWithReplacement.class_visibility = "Visible";
                    }
                    if (LayerWithReplacement.class_category == "/")
                    {
                        LayerWithReplacement.class_category = "";
                    }

                    if (Settings.layerpanel_put_non_letter_layername_at_the_end)
                    {
                        if (DoRejectLayer && (string.IsNullOrEmpty(LayerWithReplacement.class_name) || !Char.IsLetter(LayerWithReplacement.class_name.Trim()[0])))
                        {
                            layersRejected.Add(InitialLayerFromList);
                            continue;
                        }
                    }

                    foreach (FieldInfo field in typeof(Layers).GetFields(bindingFlags))
                    {
                        object actualValue = field.GetValue(LayerWithReplacement);
                        if (actualValue is null)
                        {
                            if (field.FieldType == typeof(string))
                            {
                                field.SetValue(LayerWithReplacement, string.Empty);
                            }
                            else if (field.FieldType == typeof(int))
                            {
                                field.SetValue(LayerWithReplacement, 0);
                            }
                            else
                            {
                                field.SetValue(LayerWithReplacement, null);
                            }

                        }
                    }


                    Layers.Add(Convert.ToInt32(LayerWithReplacement.class_id), LayerWithReplacement);

                    string orangestar = LayerWithReplacement.class_favorite
                        ? @$"class=""star orange"" title=""{Languages.Current["layerContextMenuRemoveFavorite"]}"""
                        : @$"class=""star"" title=""{Languages.Current["layerContextMenuAddFavorite"]}""";

                    string orangelayervisibility;
                    string visibility = "layer";

                    bool CountryFilterThisLayer = true;
                    if (layer_country_to_keep.Contains("Invariant Country") || layer_country_to_keep.Contains("World") || layer_country_to_keep.Contains("*") || layer_country_to_keep.Length == 0)
                    {
                        CountryFilterThisLayer = false;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(LayerWithReplacement.class_country))
                        {
                            CountryFilterThisLayer = false;
                        }
                        string[] layer_country = LayerWithReplacement.class_country.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        if (layer_country.ContainsOneOrMore(layer_country_to_keep) || layer_country.Length == 0 || layer_country.Contains("Invariant Country") || layer_country.Contains("World") || layer_country.Contains("*"))
                        {
                            CountryFilterThisLayer = false;
                        }
                    }

                    if (CountryFilterThisLayer)
                    {
                        visibility += "Filtered";
                        orangelayervisibility = @"class=""eye hidden""";
                    }
                    else
                    {
                        if (LayerWithReplacement.class_visibility == "Hidden")
                        {
                            orangelayervisibility = @$"class=""eye"" title=""{Languages.Current["layerContextMenuShowLayer"]}""";
                            visibility += "Hidden";
                        }
                        else
                        {
                            orangelayervisibility = @$"class=""eye orange"" title=""{Languages.Current["layerContextMenuHideLayer"]}""";
                            visibility += "Visible";
                        }
                    }

                    const string imgbase64 = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"; //1 pixel gif transparent -> disable base 

                    string overideBackgroundColor = string.Empty;
                    if (!string.IsNullOrEmpty(LayerWithReplacement?.class_specialsoptions?.BackgroundColor?.Trim()))
                    {
                        var Color = Collectif.HexValueToSolidColorBrush(LayerWithReplacement.class_specialsoptions.BackgroundColor);
                        overideBackgroundColor = $"background-color:rgba({Color.Color.R},{Color.Color.G},{Color.Color.B},{1 - Settings.background_layer_opacity});";
                    }
                    string supplement_class = " ";
                    if (!Settings.layerpanel_website_IsVisible)
                    {
                        supplement_class += "displaynone";
                    }

                    string WarningMessageDiv = string.Empty;
                    if (Initial_ClassVersion > LayerWithReplacement.class_version)
                    {
                        WarningMessageDiv = $"<div class=\"warning\" title=\"{Languages.Current["layerMessageErrorDetectedClickHere"]}\" onclick=\"show_warning(event, '{LayerWithReplacement.class_id}');\"></div>";
                    }

                    generated_layers.AppendLine(@$"
                <li class=""{visibility}"" id=""{LayerWithReplacement.class_id}"">
                    <div class=""layer_main_div"" style=""background-image:url({imgbase64.Trim()});{overideBackgroundColor}"">
                        <div class=""layer_main_div_background_image""></div>
                        <div class=""layer_content"" data-layer=""{LayerWithReplacement.class_identifier}"" title=""{Collectif.HTMLEntities(LayerWithReplacement.class_description)}"">
                            <div class=""layer_texte"">
                                <p class=""display_name"">{Collectif.HTMLEntities(LayerWithReplacement.class_name)}</p>
                                <p class=""zoom"">[{LayerWithReplacement.class_min_zoom}-{LayerWithReplacement.class_max_zoom}] - {LayerWithReplacement.class_site}</p>
                                <p class=""layer_website{supplement_class}"">{LayerWithReplacement.class_site}</p>
                                <p class=""layer_category{supplement_class}"">{LayerWithReplacement.class_category}</p>
                            </div>
                            <div {orangestar} onclick=""ajouter_aux_favoris(event, this, {LayerWithReplacement.class_id})""></div>
                            <div {orangelayervisibility} onclick=""change_visibility(event, this, {LayerWithReplacement.class_id})""></div>
                            {WarningMessageDiv}
                        </div>
                    </div>
                </li>");
                }
                return layersRejected;
            }

            List<Layers> ListOfLayerRejected = GenerateHTMLFromLayerList(layers);
            GenerateHTMLFromLayerList(ListOfLayerRejected, false);

            generated_layers.AppendLine("</ul>");
            string resource_data = Collectif.ReadResourceString("HTML/layer_panel.html");
            resource_data = Languages.ReplaceInString(resource_data);
            return resource_data.Replace("<!--htmllayerplaceholder-->", generated_layers.ToString());
        }

        public void RefreshMap()
        {
            Set_current_layer(Layers.Current.class_id);
        }

        public void Set_current_layer(int id)
        {
            int layer_startup_id = Settings.layer_startup_id;
            string last_format = string.Empty;

            if (!string.IsNullOrWhiteSpace(Layers.Current.class_format))
            {
                last_format = Layers.Current.class_format;
            }

            Layers layer = Layers.GetLayerById(id);

            if (Layers.Count() == 0)
            {
                Layers.Add(0, Layers.Empty(0));
            }

            if (layer is null || layer_startup_id == 0)
            {
                layer = Layers.GetLayersList().First();
                Settings.layer_startup_id = layer.class_id;
                if (layer_startup_id == 0)
                {
                    layer_startup_id = layer.class_id;
                }
            }

            if (layer is not null)
            {
                MapFigures.DrawFigureOnMapItemsControlFromJsonString(mapviewerRectangles, layer.class_rectangles, mapviewer.ZoomLevel);
                //Clear all layer notifications
                Notification.ListOfNotificationsOnShow.Where(notification => Regex.IsMatch(notification.NotificationId, @"^LayerId_\d+_")).ToList().ForEach(notification => notification.Remove());

                try
                {
                    Layers.Convert.ToCurentLayer(layer);

                    List<string> listoftransparentformat = new List<string> { "png" };
                    if (listoftransparentformat.Contains(layer.class_format))
                    {
                        MapTileLayer_Transparent.TileSource = new TileSource { UriFormat = layer.class_tile_url, LayerID = layer.class_id };
                        MapTileLayer_Transparent.Opacity = 1;

                        if ((!listoftransparentformat.Contains(last_format)) && !string.IsNullOrWhiteSpace(last_format) && layer.class_identifier is not null)
                        {
                            try
                            {
                                Layers StartupLayer = Layers.GetLayerById(layer_startup_id);
                                if (StartupLayer != null)
                                {
                                    UIElement basemap = new MapTileLayer
                                    {
                                        TileSource = new TileSource { UriFormat = StartupLayer?.class_tile_url, LayerID = layer_startup_id },
                                        SourceName = StartupLayer.class_identifier + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                        MaxZoomLevel = StartupLayer.class_max_zoom ?? 0,
                                        MinZoomLevel = StartupLayer.class_min_zoom ?? 0,
                                        Description = ""
                                    };

                                    basemap.Opacity = Settings.background_layer_opacity;
                                    mapviewer.MapLayer = basemap;
                                }
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
                            SourceName = layer.class_identifier + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                            MaxZoomLevel = layer.class_max_zoom ?? 0,
                            MinZoomLevel = layer.class_min_zoom ?? 0,
                            Description = layer.class_description
                        };

                        MapTileLayer_Transparent.TileSource = new TileSource();
                        MapTileLayer_Transparent.Opacity = 0;
                        mapviewer.MapLayer.Opacity = 1;
                        mapviewer.MapLayer = layer_uielement;
                    }

                    if (Settings.zoom_limite_taille_carte)
                    {
                        mapviewer.MinZoomLevel = layer.class_min_zoom < 3 ? 2 : layer.class_min_zoom ?? 0;
                        mapviewer.MaxZoomLevel = layer.class_max_zoom ?? 0;
                    }
                    else
                    {
                        mapviewer.MinZoomLevel = 2;
                        mapviewer.MaxZoomLevel = 24;
                    }

                    if (string.IsNullOrEmpty(layer?.class_specialsoptions?.BackgroundColor?.Trim()))
                    {
                        mapviewer.Background = Collectif.RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B);
                    }
                    else
                    {
                        mapviewer.Background = Collectif.HexValueToSolidColorBrush(layer.class_specialsoptions.BackgroundColor);
                    }
                    Collectif.SetBackgroundOnUIElement(mapviewer, layer?.class_specialsoptions?.BackgroundColor);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Erreur changement de calque" + ex.Message);
                }
            }
        }

        public static long ClearCache(int id, bool showErrors = true)
        {
            if (id == 0) { return 0; }

            Layers layers = Layers.GetLayerById(id);
            if (layers is null) { return 0; }
            long DirectorySize = 0;
            try
            {
                Javascript.EngineDeleteById(id);
                string temp_dir = Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifier);
                if (Directory.Exists(temp_dir))
                {
                    DirectorySize = Collectif.GetDirectorySize(temp_dir);
                    Directory.Delete(temp_dir, true);
                }
            }
            catch (Exception ex)
            {
                if (!showErrors)
                {
                    Message.NoReturnBoxAsync(Languages.GetWithArguments("layerMessageErrorCachesNorClear", ex.Message), Languages.Current["dialogTitleOperationFailed"]);

                }
                Debug.WriteLine("Erreur lors du nettoyage du cache : " + ex.Message);
            }
            return DirectorySize;
        }

        public static async void ShowLayerWarning(int id)
        {
            int EditedDB_VERSION;
            string EditedDB_SCRIPT;
            string EditedDB_TILE_URL;

            int LastDB_VERSION;
            string LastDB_SCRIPT;
            string LastDB_TILE_URL;


            var DatabaseEditedLayerExecutable = Database.ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'EDITEDLAYERS' WHERE ID = {id}");
            using (DatabaseEditedLayerExecutable.conn)
            {
                using (SQLiteDataReader editedlayers_sqlite_datareader = DatabaseEditedLayerExecutable.Reader)
                {
                    if (!editedlayers_sqlite_datareader.Read())
                    {
                        return;
                    }

                    EditedDB_VERSION = editedlayers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    EditedDB_SCRIPT = editedlayers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    EditedDB_TILE_URL = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                }
            }

            var DatabaseLayerExecutable = Database.ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'LAYERS' WHERE ID = {id}");
            using (DatabaseLayerExecutable.conn)
            {
                using (SQLiteDataReader layers_sqlite_datareader = DatabaseLayerExecutable.Reader)
                {
                    layers_sqlite_datareader.Read();
                    LastDB_VERSION = layers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    LastDB_SCRIPT = layers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    if (string.IsNullOrEmpty(LastDB_SCRIPT))
                    {
                        LastDB_SCRIPT = "";
                    }
                    LastDB_TILE_URL = layers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                }
            }

            if (EditedDB_VERSION != LastDB_VERSION)
            {
                bool HasActionToTake = false;
                StackPanel AskMsg = new StackPanel();
                string RemoveSQL = "";

                if (EditedDB_SCRIPT != LastDB_SCRIPT && !string.IsNullOrWhiteSpace(EditedDB_SCRIPT))
                {
                    HasActionToTake = true;
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Languages.Current["layerMessageErrorUpdateScriptChanged"],
                        TextWrapping = TextWrapping.Wrap
                    };
                    AskMsg.Children.Add(textBlock);
                    AskMsg.Children.Add(Collectif.FormatDiffGetScrollViewer(EditedDB_SCRIPT, LastDB_SCRIPT));
                    RemoveSQL += $"'SCRIPT'=NULL";
                }

                if (EditedDB_TILE_URL != LastDB_TILE_URL && !string.IsNullOrWhiteSpace(EditedDB_TILE_URL))
                {
                    HasActionToTake = true;
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Languages.Current["layerMessageErrorUpdateTileURLChanged"],
                        TextWrapping = TextWrapping.Wrap
                    };
                    AskMsg.Children.Add(textBlock);
                    AskMsg.Children.Add(Collectif.FormatDiffGetScrollViewer(EditedDB_TILE_URL, LastDB_TILE_URL));
                    RemoveSQL += $"'TILE_URL'=NULL";
                }

                TextBlock textBlockAsk = new TextBlock
                {
                    Text = Languages.Current["layerMessageErrorUpdateAskFix"],
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.FromOpenTypeWeight(600)
                };
                AskMsg.Children.Add(textBlockAsk);
                ContentDialogResult result = ContentDialogResult.Secondary;
                if (HasActionToTake) { 
                ContentDialog dialog = Message.SetContentDialog(AskMsg, "MapsInMyFolder", MessageDialogButton.YesNoCancel);

                    result = await dialog.ShowAsync();
                }
                if (result == ContentDialogResult.Primary)
                {
                    Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}',{RemoveSQL} WHERE ID = {id};");
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}' WHERE ID = {id};");
                }
                else
                {
                    return;
                }

                _instance.ReloadPage();
                _instance.Set_current_layer(Layers.Current.class_id);
            }
        }

        public static void DBLayerFavorite(int id, bool favBooleanState)
        {
            if (id == 0) { return; }
            int fav_state = favBooleanState ? 1 : 0;

            Database.ExecuteNonQuerySQLCommand($"UPDATE LAYERS SET FAVORITE = {fav_state} WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE EDITEDLAYERS SET FAVORITE = {fav_state} WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE CUSTOMSLAYERS SET FAVORITE = {fav_state} WHERE ID={id}");

            Layers.GetLayerById(id).class_favorite = Convert.ToBoolean(fav_state);
        }

        public static void DBLayerVisibility(int id, string visibility_state)
        {
            if (id == 0) { return; }

            Database.ExecuteNonQuerySQLCommand($"UPDATE LAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE EDITEDLAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE CUSTOMSLAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");

            Layers.GetLayerById(id).class_visibility = visibility_state;
        }

        public void LayerTilePreview_RequestUpdate()
        {
            var bbox = mapviewer.ViewRectToBoundingBox(new Rect(0, 0, mapviewer.ActualWidth, mapviewer.ActualHeight));
            Commun.Map.CurentView.NO_Latitude = bbox.North;
            Commun.Map.CurentView.NO_Longitude = bbox.West;
            Commun.Map.CurentView.SE_Latitude = bbox.South;
            Commun.Map.CurentView.SE_Longitude = bbox.East;

            layer_browser.ExecuteScriptAsyncWhenPageLoaded("UpdatePreview();");
            return;
        }


        public string LayerTilePreview_ReturnUrl(int id)
        {
            Layers layer = Layers.GetLayerById(id);

            if (layer is null)
            {
                return "";
            }

            try
            {
                int layer_startup_id = Settings.layer_startup_id;
                Layers backgroundLayer = Layers.GetLayerById(layer_startup_id);

                int min_zoom = layer.class_min_zoom ?? 0;
                int max_zoom = layer.class_max_zoom ?? 0;
                int back_min_zoom = min_zoom;
                int back_max_zoom = max_zoom;

                if (backgroundLayer is not null)
                {
                    back_min_zoom = backgroundLayer.class_min_zoom ?? 0;
                    back_max_zoom = backgroundLayer.class_max_zoom ?? 0;
                }
                int Zoom = Math.Max(Convert.ToInt32(Math.Round(mapviewer.TargetZoomLevel)) - 1, 0);
                if (Zoom < min_zoom) { Zoom = min_zoom; }
                if (Zoom > max_zoom) { Zoom = max_zoom; }

                if (Zoom < back_min_zoom) { Zoom = Math.Max(back_min_zoom, Zoom); }
                if (Zoom > back_max_zoom) { Zoom = Math.Min(back_max_zoom, Zoom); }

                double Latitude = mapviewer.Center.Latitude;
                double Longitude = mapviewer.Center.Longitude;
                var TileNumber = Collectif.CoordonneesToTile(Latitude, Longitude, Zoom);
                string previewLayerFrontImageUrl = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";
                string previewBackgroundImageUrl = string.Empty;
                if (Javascript.CheckIfFunctionExist(id, Javascript.InvokeFunction.getPreview.ToString(), null))
                {
                    previewLayerFrontImageUrl = Collectif.Replacements(layer.class_tile_url, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), id, Javascript.InvokeFunction.getPreview);
                    previewBackgroundImageUrl = Collectif.Replacements(backgroundLayer?.class_tile_url, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), id, Javascript.InvokeFunction.getPreview);
                    if (backgroundLayer?.class_tile_url == previewBackgroundImageUrl)
                    {
                        previewBackgroundImageUrl = "";
                    }
                }
                else
                {
                    previewLayerFrontImageUrl = Collectif.Replacements(layer.class_tile_url, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), id, Javascript.InvokeFunction.getTile);
                    previewBackgroundImageUrl = Collectif.Replacements(backgroundLayer?.class_tile_url, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), id, Javascript.InvokeFunction.getTile);
                }

                string previewFallbackLayerFrontImageUrl = string.Empty;
                string previewFallbackBackgroundImageUrl = string.Empty;
                if (Javascript.CheckIfFunctionExist(id, Javascript.InvokeFunction.getPreviewFallback.ToString(), null))
                {
                    previewFallbackLayerFrontImageUrl = Collectif.Replacements(layer.class_tile_url, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), id, Javascript.InvokeFunction.getPreviewFallback);
                    previewFallbackBackgroundImageUrl = Collectif.Replacements(backgroundLayer?.class_tile_url, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), id, Javascript.InvokeFunction.getPreviewFallback);
                    if (backgroundLayer?.class_tile_url == previewFallbackBackgroundImageUrl)
                    {
                        previewFallbackBackgroundImageUrl = "";
                    }
                }



                const string base64Before = "[internal]";
                string encodeURL(string url)
                {
                    return HttpUtility.UrlEncode(url.TrimStart(base64Before));
                }

                string previewReferrer = Collectif.AddHttpToUrl(layer?.class_site_url);
                string previewBackgroundReferrer = Collectif.AddHttpToUrl(backgroundLayer?.class_site_url);

                if (previewLayerFrontImageUrl.StartsWith(base64Before))
                {
                    previewLayerFrontImageUrl = $"mapsinmyfolder://get?referrer={encodeURL(previewReferrer)}&url={encodeURL(previewLayerFrontImageUrl)}";
                }
                if (previewFallbackLayerFrontImageUrl.StartsWith(base64Before))
                {
                    previewFallbackLayerFrontImageUrl = $"mapsinmyfolder://get?referrer={encodeURL(previewBackgroundReferrer)}&url={encodeURL(previewFallbackLayerFrontImageUrl)}";
                }

                previewBackgroundImageUrl = previewBackgroundImageUrl.TrimStart(base64Before);
                previewFallbackBackgroundImageUrl = previewFallbackBackgroundImageUrl.TrimStart(base64Before);

                string previewJSON = "{\"preview\":{\"frontImage\":{\"url\":\"" + previewLayerFrontImageUrl + "\",\"referrer\":\"" + previewReferrer + "\"},\"backgroundImage\":{\"url\":\"" + previewBackgroundImageUrl + "\",\"referrer\":\"" + previewBackgroundReferrer + "\"}},\"previewFallback\":{\"frontImage\":{\"url\":\"" + previewFallbackLayerFrontImageUrl + "\",\"referrer\":\"" + previewReferrer + "\"},\"backgroundImage\":{\"url\":\"" + previewFallbackBackgroundImageUrl + "\",\"referrer\":\"" + previewBackgroundReferrer + "\"}}}";

                return previewJSON;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error getPreview exception :" + ex.Message);
            }
            return String.Empty;

        }
    }

    // [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Marquer les membres comme étant static", Justification = "Used by CEFSHARP, static isnt a option here")]
    public class Layer_Csharp_call_from_js
    {
        public void Clear_cache(string listOfId = "0")
        {
            long DirectorySize = 0;
            string[] splittedListOfId = listOfId.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in splittedListOfId)
            {
                int id_int = int.Parse(str.Trim());
                //Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                //{
                DirectorySize += MainPage.ClearCache(id_int);

                //}, null);
                Debug.WriteLine("Clear_cache layer " + id_int);
            }
            if (DirectorySize >= 0)
            {
                string memoryFreed = Collectif.FormatBytes(DirectorySize);
                string cacheCleanedMessage = "";

                if (splittedListOfId.Length == 1)
                {
                    cacheCleanedMessage = Languages.GetWithArguments("layerMessageCacheCleared", Layers.GetLayerById(int.Parse(splittedListOfId[0])).class_name, memoryFreed);
                }
                else
                {
                    cacheCleanedMessage = Languages.GetWithArguments("layerMessageCachesCleared", Layers.GetLayerById(int.Parse(splittedListOfId[0])).class_name, memoryFreed);
                }
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                {
                    Message.NoReturnBoxAsync(cacheCleanedMessage, Languages.Current["dialogTitleOperationSuccess"]);
                }, null);
            }
        }


        public void Layer_favorite(double id = 0, bool isAdding = true)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.DBLayerFavorite(id_int, isAdding);
            }, null);
        }

        public void Layer_visibility(double id = 0, bool isVisible = true)
        {
            //Debug.WriteLine($"Layer_visibility : id={id} & isVisible={isVisible}");
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.DBLayerVisibility(id_int, (isVisible ? "Visible" : "Hidden"));
            }, null);
        }


        public void Layer_edit(double id = 0, double prefilid = -1)
        {
            int id_int = Convert.ToInt32(id);
            int prefilid_int = Convert.ToInt32(prefilid);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow._instance.FrameLoad_CustomOrEditLayers(id_int, prefilid_int);
            }, null);
        }

        public void Layer_set_current(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                //Debug.WriteLine("Layer_set_current " + id);
                MainWindow._instance.MainPage.Set_current_layer(id_int);
            }, null);
        }
        public void Layer_show_warning(double id = 0)
        {
            int id_int = Convert.ToInt32(id);
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainPage.ShowLayerWarning(id_int);
            }, null);
        }

        public void Request_search_update()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, (SendOrPostCallback)delegate
            {
                MainWindow._instance.MainPage.SearchLayerStart(true);
            }, null);
        }

        public string Request_search_string()
        {
            return Application.Current.Dispatcher.Invoke(() => MainWindow._instance.MainPage.SearchGetText(), DispatcherPriority.Send);
        }

        public void Refresh_panel()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                MainWindow._instance.MainPage.ReloadPage();
                MainWindow._instance.MainPage.SearchLayerStart();
            }, null);
        }

        public string Gettilepreviewurlfromid(double id = 0)
        {
            if (!Settings.layerpanel_livepreview)
            {
                return "";
            }
            async Task<string> Gettilepreviewurlfromid_interne(double id)
            {
                int id_int = Convert.ToInt32(id);
                DispatcherOperation op = Application.Current.Dispatcher.BeginInvoke(new Func<string>(() => MainWindow._instance.MainPage.LayerTilePreview_ReturnUrl(id_int)));
                await op;
                return op.Result.ToString();
            }
            return Gettilepreviewurlfromid_interne(id).Result;
        }
    }
}