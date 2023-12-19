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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;

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

        public void InitLayerPanel()
        {
            if (layer_browser is null) { return; }
            try
            {
                layer_browser.JavascriptObjectRepository.Register("LayerCEFSharpLink", new LayerCEFSharpLink());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            try
            {
                layer_browser.ExecuteScriptAsync("CefSharp.BindObjectAsync(\"LayerCEFSharpLink\");");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void ReloadPage()
        {
            layer_browser.LoadHtml(LayersLoad(), "http://siogabx.fr");
        }


        private void Layer_browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                layer_browser.GetMainFrame().EvaluateScriptAsync(LayerGetDefaultSelectByIdScript());
            }
        }

        public static string LayerGetDefaultSelectByIdScript()
        {
            int LayerId = Layers.Current.Id;
            string scroll = ", true";
            if (LayerId == -1)
            {
                LayerId = Layers.StartupLayerId;
                scroll = "";
            }
            return $"lastelement = document.getElementById({LayerId}); selectionner_calque_by_id({LayerId}{scroll})";
        }



        public static List<Layers> LayerReadInDatabase(string query_command)
        {
            List<Layers> layersFavorite = new List<Layers>();
            List<Layers> layersClassicSort = new List<Layers>();
            using SQLiteDataReader sqlite_datareader = Database.ExecuteExecuteReaderSQLCommand(query_command).Reader;

            while (sqlite_datareader.Read())
            {
                try
                {
                    var calque = Layers.GetLayerFromSQLiteDataReader(sqlite_datareader);
                    if (calque.IsFavorite && Settings.layerpanel_favorite_at_top)
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

        string LayersLoad()
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

            List<Layers> legacyLayers = LayerReadInDatabase(OriginalLayersGetQuery);
            List<Layers> editedLayers = LayerReadInDatabase(EditedLayersGetQuery);
            Layers.LayersMergeLegacyWithEdited(legacyLayers, editedLayers);
            string baseHTML = LayersCreateHTML();

            if (Settings.show_layer_devtool)
            {
                layer_browser.ShowDevTools();
            }

            StringBuilder PropertyBuilder = new StringBuilder();
            PropertyBuilder.Append("<script>");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--opacity_preview_background-image\", {Settings.background_layer_opacity});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_R\", {Settings.background_layer_color_R});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_G\", {Settings.background_layer_color_G});");
            PropertyBuilder.Append($"document.body.style.setProperty(\"--background_layer_color_B\", {Settings.background_layer_color_B});");
            PropertyBuilder.Append("</script>");

            baseHTML += PropertyBuilder.ToString();

            return baseHTML;

        }


        static string LayersCreateHTML()
        {

            StringBuilder generated_layers = new StringBuilder("<ul class=\"");
            generated_layers.Append(Settings.layerpanel_displaystyle.ToString().ToLower());
            generated_layers.AppendLine("\">");

            List<Layers> layersRejectedAtFirstIteration = new List<Layers>();
            string[] layersSpecificsCountryToKeep = Settings.filter_layers_based_on_country.Split(';', StringSplitOptions.RemoveEmptyEntries);
            for (int iterationOfLayerTreatments = 0; iterationOfLayerTreatments <= 1; iterationOfLayerTreatments++)
            {
                bool isFirstIterationDoRejectLayer = iterationOfLayerTreatments == 0;
                IEnumerable<Layers> EnumerableLayers;

                if (isFirstIterationDoRejectLayer)
                {
                    EnumerableLayers = Layers.GetLayersList();
                }
                else
                {
                    EnumerableLayers = layersRejectedAtFirstIteration;
                }

                foreach (Layers layer in EnumerableLayers)
                {
                    if (isFirstIterationDoRejectLayer && Settings.layerpanel_put_non_letter_layername_at_the_end)
                    {
                        if (string.IsNullOrEmpty(layer.Name) || !Char.IsLetter(layer.Name.Trim()[0]))
                        {
                            layersRejectedAtFirstIteration.Add(layer);
                            continue;
                        }
                    }

                    bool LayerShouldBeCountryFiltered = true;


                    string[] layerCountrySpecificsAttributes = layer.Country.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    string[] GlobeSpecificAttributes = new string[] { "Invariant Country", "World", "*" };

                    bool isGlobeSpecificLayer = layerCountrySpecificsAttributes.ContainsOneOrMore(GlobeSpecificAttributes);

                    if (layersSpecificsCountryToKeep.Length == 0 || layerCountrySpecificsAttributes.Length == 0 || string.IsNullOrWhiteSpace(layer.Country))
                    {
                        LayerShouldBeCountryFiltered = false;
                    }
                    else
                    {
                        if (layersSpecificsCountryToKeep.ContainsOneOrMore(GlobeSpecificAttributes) && isGlobeSpecificLayer)
                        {
                            LayerShouldBeCountryFiltered = false;
                        }
                        if (layerCountrySpecificsAttributes.ContainsOneOrMore(layersSpecificsCountryToKeep))
                        {
                            LayerShouldBeCountryFiltered = false;
                        }
                    }

                    bool ShowCountry = true;
                    string CountryHTML = string.Empty;
                    if (ShowCountry)
                    {
                        CountryHTML = " - " + layer.Country;
                    }

                    string layerVisibilityHTML;
                    string layerFiltered = "layer";
                    if (LayerShouldBeCountryFiltered)
                    {
                        layerFiltered += "Filtered";
                        layerVisibilityHTML = @"class=""eye hidden""";
                        continue;
                    }
                    else
                    {
                        if (layer.Visibility == "Hidden")
                        {
                            layerVisibilityHTML = @$"class=""eye"" title=""{Languages.Current["layerContextMenuShowLayer"]}""";
                            layerFiltered += "Hidden";
                        }
                        else
                        {
                            layerVisibilityHTML = @$"class=""eye orange"" title=""{Languages.Current["layerContextMenuHideLayer"]}""";
                            layerFiltered += "Visible";
                        }
                    }
                    string layerFavoriteHTML = layer.IsFavorite
                       ? @$"class=""star orange"" title=""{Languages.Current["layerContextMenuRemoveFavorite"]}"""
                       : @$"class=""star"" title=""{Languages.Current["layerContextMenuAddFavorite"]}""";



                    string overideBackgroundColor = string.Empty;
                    if (!string.IsNullOrEmpty(layer?.SpecialsOptions?.BackgroundColor?.Trim()))
                    {
                        var Color = Collectif.HexValueToSolidColorBrush(layer.SpecialsOptions.BackgroundColor);
                        overideBackgroundColor = $"background-color:rgba({Color.Color.R},{Color.Color.G},{Color.Color.B},{1 - Settings.background_layer_opacity});";
                    }
                    string supplement_class = string.Empty;
                    if (!Settings.layerpanel_website_IsVisible)
                    {
                        supplement_class = string.Concat(" ", "displaynone");
                    }

                    string WarningMessageDiv = string.Empty;
                    if (layer.DoShowWarningLegacyVersionNewerThanEdited)
                    {
                        WarningMessageDiv = $"<div class=\"warning\" title=\"{Languages.Current["layerMessageErrorDetectedClickHere"]}\" onclick=\"show_warning(event, '{layer.Id}');\"></div>";
                    }

                    generated_layers.AppendLine(@$"
                <li class=""{layerFiltered}"" id=""{layer.Id}"">
                    <div class=""layer_main_div"" style=""{overideBackgroundColor}"">
                        <div class=""layer_main_div_preview_images"">
                            <div class=""layer_main_div_background_image""></div>
                            <div class=""layer_main_div_front_image""></div>
                        </div>
                        <div class=""layer_content"" data-layer=""{layer.Identifier}"" title=""{Collectif.HTMLEntities(layer.Description)}"">
                            <div class=""layer_texte"">
                                <p class=""display_name"">{Collectif.HTMLEntities(layer.Name)}</p>
                                <p class=""zoom"">[{layer.MinZoom}-{layer.MaxZoom}]{CountryHTML} - {layer.SiteName}</p>
                                <p class=""layer_website{supplement_class}"">{layer.SiteName}</p>
                                <p class=""layer_category{supplement_class}"">{layer.Category}</p>
                            </div>
                            <div {layerFavoriteHTML} onclick=""ajouter_aux_favoris(event, this, {layer.Id})""></div>
                            <div {layerVisibilityHTML} onclick=""change_visibility(event, this, {layer.Id})""></div>
                            {WarningMessageDiv}
                        </div>
                    </div>
                </li>");
                }
            }

            generated_layers.AppendLine("</ul>");
            generated_layers.AppendLine($"<script>{LayerGetDefaultSelectByIdScript()}</script>");
            string resource_data = Collectif.ReadResourceString("HTML/layer_panel.html");
            resource_data = Languages.ReplaceInString(resource_data);
            return resource_data.Replace("<!--htmllayerplaceholder-->", generated_layers.ToString());
        }

        public void RefreshMap()
        {
            SetCurrentLayer(Layers.Current.Id);
        }



        public void SetCurrentLayer(int id)
        {

            bool lastLayerHasTransparency = Layers.Current.TilesFormatHasTransparency;
            if (Layers.Count() == 0)
            {
                Layers.Add(0, Layers.Empty(0));
            }

            if (Layers.StartupLayerId == 0)
            {
                IEnumerable<Layers> AllLayers = Layers.GetLayersList();
                Layers SelectedDefaultLayer = AllLayers.FirstOrDefault(layer =>
                {
                    bool isJpeg = layer.TilesFormat == "jpeg" || layer.TilesFormat == "jpg";
                    bool hasUrl = !string.IsNullOrWhiteSpace(layer.TileUrl);
                    return isJpeg && hasUrl;
                }, AllLayers.First());
                Layers.StartupLayerId = SelectedDefaultLayer.Id;
            }
            Layers layer = Layers.GetLayerById(id) ?? Layers.GetLayerById(Layers.StartupLayerId);

            if (layer is null)
            {
                layer = Layers.GetLayersList().First();
            }


            if (layer is not null)
            {
                MapFigures.DrawFigureOnMapItemsControlFromJsonString(mapviewerRectangles, layer.BoundaryRectangles, mapviewer.ZoomLevel);
                //Clear all layer notifications
                Notification.ListOfNotificationsOnShow.Where(notification => Regex.IsMatch(notification.NotificationId, @"^LayerId_\d+_")).ToList().ForEach(notification => notification.Remove());

                try
                {
                    Layers.Convert.ToCurentLayer(layer);

                    if (layer.TilesFormatHasTransparency)
                    {
                        MapTileLayer_Transparent.TileSource = new TileSource { UriFormat = layer.TileUrl, LayerID = layer.Id };
                        MapTileLayer_Transparent.Opacity = 1;

                        if (layer.Identifier is not null)
                        {
                            Layers StartupLayer = Layers.GetLayerById(Layers.StartupLayerId);
                            if (StartupLayer != null)
                            {
                                UIElement basemap = new MapTileLayer
                                {
                                    TileSource = new TileSource { UriFormat = StartupLayer?.TileUrl, LayerID = Layers.StartupLayerId },
                                    SourceName = StartupLayer.Identifier + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                    MaxZoomLevel = StartupLayer.MaxZoom ?? 0,
                                    MinZoomLevel = StartupLayer.MinZoom ?? 0,
                                    Description = "",
                                    Opacity = Settings.background_layer_opacity
                                };
                                mapviewer.MapLayer = basemap;
                            }
                        }
                    }
                    else
                    {
                        UIElement layer_uielement = new MapTileLayer
                        {
                            TileSource = new TileSource { UriFormat = layer.TileUrl, LayerID = layer.Id },
                            SourceName = layer.Identifier + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                            MaxZoomLevel = layer.MaxZoom ?? 0,
                            MinZoomLevel = layer.MinZoom ?? 0,
                            Description = layer.Description
                        };

                        MapTileLayer_Transparent.TileSource = new TileSource();
                        MapTileLayer_Transparent.Opacity = 0;
                        mapviewer.MapLayer.Opacity = 1;
                        mapviewer.MapLayer = layer_uielement;
                    }

                    if (Settings.zoom_limite_taille_carte)
                    {
                        mapviewer.MinZoomLevel = layer.MinZoom < 3 ? 2 : layer.MinZoom ?? 0;
                        mapviewer.MaxZoomLevel = layer.MaxZoom ?? 0;
                    }
                    else
                    {
                        mapviewer.MinZoomLevel = 2;
                        mapviewer.MaxZoomLevel = 24;
                    }

                    if (string.IsNullOrEmpty(layer?.SpecialsOptions?.BackgroundColor?.Trim()))
                    {
                        mapviewer.Background = Collectif.RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B);
                    }
                    else
                    {
                        mapviewer.Background = Collectif.HexValueToSolidColorBrush(layer.SpecialsOptions.BackgroundColor);
                    }
                    Collectif.SetBackgroundOnUIElement(mapviewer, layer?.SpecialsOptions?.BackgroundColor);

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
                string temp_dir = Collectif.GetSaveTempDirectory(layers.Name, layers.Identifier);
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
                if (HasActionToTake)
                {
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
                _instance.SetCurrentLayer(Layers.Current.Id);
            }
        }

        public static void DBLayerFavorite(int id, bool favBooleanState)
        {
            if (id == 0) { return; }
            int fav_state = favBooleanState ? 1 : 0;

            Database.ExecuteNonQuerySQLCommand($"UPDATE LAYERS SET FAVORITE = {fav_state} WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE EDITEDLAYERS SET FAVORITE = {fav_state} WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE CUSTOMSLAYERS SET FAVORITE = {fav_state} WHERE ID={id}");

            Layers.GetLayerById(id).IsFavorite = Convert.ToBoolean(fav_state);
        }

        public static void DBLayerVisibility(int id, string visibility_state)
        {
            if (id == 0) { return; }

            Database.ExecuteNonQuerySQLCommand($"UPDATE LAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE EDITEDLAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE CUSTOMSLAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");

            Layers.GetLayerById(id).Visibility = visibility_state;
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
                int layer_startup_id = Layers.StartupLayerId;
                Layers backgroundLayer = Layers.GetLayerById(layer_startup_id);

                int min_zoom = layer.MinZoom ?? 0;
                int max_zoom = layer.MaxZoom ?? 0;
                int back_min_zoom = min_zoom;
                int back_max_zoom = max_zoom;

                if (backgroundLayer is not null)
                {
                    back_min_zoom = backgroundLayer.MinZoom ?? 0;
                    back_max_zoom = backgroundLayer.MaxZoom ?? 0;
                }
                int Zoom = Math.Max(Convert.ToInt32(Math.Round(mapviewer.TargetZoomLevel)) - 1, 0);
                if (Zoom < min_zoom) { Zoom = min_zoom; }
                if (Zoom > max_zoom) { Zoom = max_zoom; }

                if (Zoom < back_min_zoom) { Zoom = Math.Max(back_min_zoom, Zoom); }
                if (Zoom > back_max_zoom) { Zoom = Math.Min(back_max_zoom, Zoom); }

                double Latitude = mapviewer.Center.Latitude;
                double Longitude = mapviewer.Center.Longitude;
                var TileNumber = Collectif.CoordonneesToTile(Latitude, Longitude, Zoom);

                bool CheckIfFunctionExist(int layerId, Javascript.InvokeFunction invokeFunction)
                {
                    return Javascript.CheckIfFunctionExist(layerId, invokeFunction.ToString(), null);
                }
                string GetReplacement(int layerId, string tileUrl, Javascript.InvokeFunction invokeFunction)
                {
                    return Collectif.Replacements(tileUrl, TileNumber.X.ToString(), TileNumber.Y.ToString(), Zoom.ToString(), layerId, invokeFunction);
                }

                string previewLayerFrontImageUrl = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";
                string previewBackgroundImageUrl = string.Empty;
                string previewFallbackLayerFrontImageUrl = string.Empty;
                string previewFallbackBackgroundImageUrl = string.Empty;

                if (CheckIfFunctionExist(id, Javascript.InvokeFunction.getPreview))
                {
                    previewLayerFrontImageUrl = GetReplacement(id, layer.TileUrl, Javascript.InvokeFunction.getPreview);
                }
                else
                {
                    previewLayerFrontImageUrl = GetReplacement(id, layer.TileUrl, Javascript.InvokeFunction.getTile);
                }

                if (CheckIfFunctionExist(id, Javascript.InvokeFunction.getPreviewFallback))
                {
                    previewFallbackLayerFrontImageUrl = GetReplacement(id, layer.TileUrl, Javascript.InvokeFunction.getPreviewFallback);
                }

                if (layer.TilesFormatHasTransparency && backgroundLayer is not null)
                {
                    if (CheckIfFunctionExist(backgroundLayer.Id, Javascript.InvokeFunction.getPreview))
                    {
                        previewBackgroundImageUrl = GetReplacement(backgroundLayer.Id, backgroundLayer.TileUrl, Javascript.InvokeFunction.getPreview);
                    }
                    else
                    {
                        previewBackgroundImageUrl = GetReplacement(backgroundLayer.Id, backgroundLayer.TileUrl, Javascript.InvokeFunction.getTile);
                    }
                    if (backgroundLayer?.TileUrl == previewBackgroundImageUrl)
                    {
                        previewBackgroundImageUrl = "";
                    }

                    if (CheckIfFunctionExist(backgroundLayer.Id, Javascript.InvokeFunction.getPreviewFallback))
                    {
                        previewFallbackBackgroundImageUrl = GetReplacement(backgroundLayer.Id, backgroundLayer?.TileUrl, Javascript.InvokeFunction.getPreviewFallback);
                        if (backgroundLayer?.TileUrl == previewFallbackBackgroundImageUrl)
                        {
                            previewFallbackBackgroundImageUrl = "";
                        }
                    }
                }




                string EncodeURL(string url)
                {
                    return HttpUtility.UrlEncode(url);
                }

                bool UseReferrerForPreviews = true;

                string previewReferrer = Collectif.AddHttpToUrl(layer?.SiteUrl);
                string previewBackgroundReferrer = Collectif.AddHttpToUrl(backgroundLayer?.SiteUrl);

                if (UseReferrerForPreviews)
                {
                    if (!string.IsNullOrWhiteSpace(previewReferrer))
                    {
                        previewLayerFrontImageUrl = $"mapsinmyfolder://get?referrer={EncodeURL(previewReferrer)}&url={EncodeURL(previewLayerFrontImageUrl)}";
                    }
                    if (!string.IsNullOrWhiteSpace(previewBackgroundReferrer))
                    {
                        previewFallbackLayerFrontImageUrl = $"mapsinmyfolder://get?referrer={EncodeURL(previewBackgroundReferrer)}&url={EncodeURL(previewFallbackLayerFrontImageUrl)}";
                    }
                }
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


}