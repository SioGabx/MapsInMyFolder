using MapsInMyFolder.Commun;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace MapsInMyFolder
{
    public class Layers
    {
        public int Id { get; set; }
        public bool IsFavorite { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public string Country { get; set; }
        public string Identifier { get; set; }
        public string TileUrl { get; set; }
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }
        public int? MinZoom { get; set; }
        public int? MaxZoom { get; set; }
        public string TilesFormat { get; set; }
        public bool TilesFormatHasTransparency = false;
        public string Style { get; set; }
        public int? TilesSize { get; set; }
        public string Script { get; set; }
        public string Visibility { get; set; }
        public LayersSpecialsOptions SpecialsOptions { get; set; }
        public string BoundaryRectangles { get; set; }
        public int Version { get; set; }
        public bool DoShowWarningLegacyVersionNewerThanEdited = false;
        public bool IsAtScale { get; set; }


        public Layers(int Id, bool IsFavorite, string Name, string Description, string Tag, string Country, string Identifier, string TileUrl, string SiteName, string SiteUrl, int? MinZoom, int? MaxZoom, string TilesFormat, string Style, int? TilesSize, string Script, string Visibility, LayersSpecialsOptions SpecialsOptions, string BoundaryRectangles, int Version, bool IsAtScale)
        {
            this.Id = Id;
            this.IsFavorite = IsFavorite;
            this.Name = Name;
            this.Description = Description;
            this.Tag = Tag;
            this.Country = Country;
            this.Identifier = Identifier;
            this.TileUrl = TileUrl;
            this.SiteName = SiteName;
            this.SiteUrl = SiteUrl;
            this.MinZoom = MinZoom;
            this.MaxZoom = MaxZoom;
            this.TilesFormat = TilesFormat;
            this.Style = Style;
            this.TilesSize = TilesSize;
            this.Script = Script;
            this.Visibility = Visibility;
            this.SpecialsOptions = SpecialsOptions;
            this.BoundaryRectangles = BoundaryRectangles;
            this.Version = Version;
            this.IsAtScale = IsAtScale;
        }

        public static Layers Current { get; set; } = Empty();

        public static int StartupLayerId { get; set; } = Settings.layer_startup_id;

        //(int)Layers.ReservedId.EditorTempLayer
        public enum ReservedId
        {
            GenericTempLayer = -1,
            EditorTempLayer = -2
        }



        public class LayersSpecialsOptions
        {
            private string _BackgroundColor;
            public string BackgroundColor
            {
                get
                {
                    return this._BackgroundColor;
                }
                set
                {
                    if (!value.StartsWith("#") && !string.IsNullOrWhiteSpace(value))
                    {
                        this._BackgroundColor = "#" + value;
                    }
                    else
                    {
                        this._BackgroundColor = value;
                    }
                }
            }


            private string _ErrorsToIgnore;
            public string ErrorsToIgnore
            {
                get
                {
                    return this._ErrorsToIgnore;
                }
                set
                {
                    this._ErrorsToIgnore = value;
                }
            }

            private int _MaxDownloadtilesInParralele;
            public int MaxDownloadTilesInParralele
            {
                get
                {
                    return this._MaxDownloadtilesInParralele;
                }
                set
                {
                    this._MaxDownloadtilesInParralele = Math.Max(value, 0);
                }
            }

            private int _WaitingBeforeStartAnotherTile;
            public int WaitingBeforeStartAnotherTile
            {
                get
                {
                    return this._WaitingBeforeStartAnotherTile;
                }
                set
                {
                    this._WaitingBeforeStartAnotherTile = value;
                }
            }

            public override string ToString()
            {
                return System.Text.Json.JsonSerializer.Serialize(this);
            }
        }

        public static Layers Empty(int LayerId = -1)
        {
            return new Layers(LayerId, false, "", "An error occurred while reading the data. \n Backup data provided by OpenStreetMap.", "", "", "", "http://tile.openstreetmap.org/{z}/{x}/{y}.png", "", "", null, null, "jpeg", "", 256, "function getTile(args){return args;}", "Visible", new LayersSpecialsOptions(), "", 0, true);
        }

        public static class Convert
        {
            public static Layers CurentLayerToLayer()
            {
                return GetLayerById(Current.Id);
            }

            public static void ToCurentLayer(Layers layer)
            {
                Current = Copy(layer);
            }

            public static Layers Copy(Layers layer)
            {
                return (Layers)layer.MemberwiseClone();
            }
        }



        public static void SetCurrentLayer(int id)
        {
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
                }) ?? AllLayers.First();

                Layers.StartupLayerId = SelectedDefaultLayer.Id;
            }
            Layers layer = GetLayerById(id) ?? GetLayerById(StartupLayerId) ?? GetLayersList().First();
            if (layer is not null)
            {
                Convert.ToCurentLayer(layer);
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

        public static void SetAsFavorite(int id, bool favBooleanState)
        {
            if (id == 0) { return; }
            int fav_state = favBooleanState ? 1 : 0;

            Database.ExecuteNonQuerySQLCommand($"UPDATE LAYERS SET FAVORITE = {fav_state} WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE EDITEDLAYERS SET FAVORITE = {fav_state} WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE CUSTOMSLAYERS SET FAVORITE = {fav_state} WHERE ID={id}");

            Layers.GetLayerById(id).IsFavorite = System.Convert.ToBoolean(fav_state);
        }

        public static void SetVisibility(int id, string visibility_state)
        {
            if (id == 0) { return; }

            Database.ExecuteNonQuerySQLCommand($"UPDATE LAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE EDITEDLAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");
            Database.ExecuteNonQuerySQLCommand($"UPDATE CUSTOMSLAYERS SET VISIBILITY = '{visibility_state}' WHERE ID={id}");

            Layers.GetLayerById(id).Visibility = visibility_state;
        }

        private static Dictionary<int, Layers> LayersDictionary { get; set; } = new Dictionary<int, Layers>();

        public static Layers GetLayerById(int id)
        {
            if (LayersDictionary.TryGetValue(id, out Layers layer))
            {
                return layer;
            }
            return null;
        }

        public static bool RemoveLayerById(int id)
        {
            if (LayersDictionary.ContainsKey(id))
            {
                LayersDictionary.Remove(id);
                return true;
            }
            return false;
        }

        public static IEnumerable<Layers> GetLayersList()
        {
            return LayersDictionary.Values;
        }

        public static void Add(int key, Layers layer)
        {
            RemoveLayerById(key);
            LayersDictionary.Add(key, layer);
        }
        public static void Clear()
        {
            LayersDictionary.Clear();
        }

        public static int Count()
        {
            return GetLayersList().Count();
        }

        public static Dictionary<string, string> GetValuesForSaving(Layers layers)
        {
            return new Dictionary<string, string>()
            {
                //Key need to be the same as the db
                {"NAME", Collectif.HTMLEntities(layers.Name)},
                {"DESCRIPTION", Collectif.HTMLEntities(layers.Description)},
                {"CATEGORY", Collectif.HTMLEntities(layers.Tag)},
                {"COUNTRY", Collectif.HTMLEntities(layers.Country)},
                {"IDENTIFIER", Collectif.HTMLEntities(layers.Identifier)},
                {"TILE_URL", Collectif.HTMLEntities(layers.TileUrl)},
                {"MIN_ZOOM", layers.MinZoom.ToString()},
                {"MAX_ZOOM", layers.MaxZoom.ToString()},
                {"FORMAT", Collectif.HTMLEntities(layers.TilesFormat)},
                {"SITE", Collectif.HTMLEntities(layers.SiteName)},
                {"SITE_URL", Collectif.HTMLEntities(layers.SiteUrl)},
                {"STYLE", Collectif.HTMLEntities(layers.Style)},
                {"TILE_SIZE", layers.TilesSize.ToString()},
                {"VISIBILITY", layers.Visibility.ToString()},
                {"SCRIPT", Collectif.HTMLEntities(layers.Script)},
                {"RECTANGLES", Collectif.HTMLEntities(layers.BoundaryRectangles)},
                {"SPECIALSOPTIONS", layers.SpecialsOptions.ToString()},
                {"HAS_SCALE", (layers.IsAtScale ? 1 : 0).ToString()}
            };
        }

        public static bool Load()
        {
            string OriginalLayersGetQuery = $"SELECT *,'LAYERS' AS TYPE FROM LAYERS UNION SELECT *,'CUSTOMSLAYERS' FROM CUSTOMSLAYERS ORDER BY {Settings.layers_Sort} NULLS LAST";
            string EditedLayersGetQuery = $"SELECT * FROM EDITEDLAYERS ORDER BY {Settings.layers_Sort} NULLS LAST";
            if (Database.DB_IsConnectionNull())
            {
                return false;
            }

            List<Layers> legacyLayers = LayerReadInDatabase(OriginalLayersGetQuery);
            List<Layers> editedLayers = LayerReadInDatabase(EditedLayersGetQuery);
            Layers.LayersMergeLegacyWithEdited(legacyLayers, editedLayers);
            return true;
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


        public static Layers GetLayerFromSQLiteDataReader(SQLiteDataReader sQLiteDataReader)
        {
            string GetStringFromOrdinal(string name)
            {
                return Database.GetStringFromOrdinal(sQLiteDataReader, name);
            }
            int? GetIntFromOrdinal(string name)
            {
                return Database.GetIntFromOrdinal(sQLiteDataReader, name);
            }

            int? DB_Layer_ID = GetIntFromOrdinal("ID");
            string DB_Layer_NAME = GetStringFromOrdinal("NAME").RemoveNewLineChar();
            bool DB_Layer_FAVORITE = System.Convert.ToBoolean(GetIntFromOrdinal("FAVORITE"));
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
            bool DB_Layer_HAS_SCALE = System.Convert.ToBoolean(GetIntFromOrdinal("HAS_SCALE"));

            bool doCreateSpecialsOptionsClass = true;
            LayersSpecialsOptions DeserializeSpecialsOptions = null;
            try
            {
                if (!string.IsNullOrEmpty(DB_Layer_SPECIALSOPTIONS))
                {
                    DeserializeSpecialsOptions = System.Text.Json.JsonSerializer.Deserialize<Layers.LayersSpecialsOptions>(DB_Layer_SPECIALSOPTIONS);
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
                    DeserializeSpecialsOptions = new Layers.LayersSpecialsOptions();
                }
            }

            if (!string.IsNullOrEmpty(DB_Layer_SCRIPT))
            {
                DB_Layer_SCRIPT = Collectif.HTMLEntities(DB_Layer_SCRIPT, true);
            }

            return new Layers((int)DB_Layer_ID, DB_Layer_FAVORITE, DB_Layer_NAME, DB_Layer_DESCRIPTION, DB_Layer_CATEGORY, DB_Layer_COUNTRY, DB_Layer_IDENTIFIER, DB_Layer_TILE_URL, DB_Layer_SITE, DB_Layer_SITE_URL, DB_Layer_MIN_ZOOM, DB_Layer_MAX_ZOOM, DB_Layer_FORMAT, DB_Layer_STYLE, DB_Layer_TILE_SIZE, DB_Layer_SCRIPT, DB_Layer_VISIBILITY, DeserializeSpecialsOptions, DB_Layer_RECTANGLES, DB_Layer_VERSION, DB_Layer_HAS_SCALE);
        }

        private static void LayersMergeLegacyWithEdited(List<Layers> legacyLayers, List<Layers> editedLayers)
        {
            Layers.Clear();
            Dictionary<int, Layers> editedLayersDictionnary = editedLayers.ToDictionary(l => l.Id, l => l);
            foreach (Layers legacyLayer in legacyLayers)
            {
                int legacyLayerVersion = legacyLayer.Version;
                bool legacyLayerHasReplacement = editedLayersDictionnary.TryGetValue(legacyLayer.Id, out Layers replacementLayer);

                BindingFlags fieldsBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                Layers legacyLayerWithReplacements = legacyLayer;
                if (legacyLayerHasReplacement)
                {
                    foreach (FieldInfo field in typeof(Layers).GetFields(fieldsBindingFlags))
                    {
                        object replacementValue = field.GetValue(replacementLayer);
                        if (replacementValue is null)
                        {
                            continue;
                        }
                        Type replacementValueType = replacementValue.GetType();

                        if (replacementValueType == typeof(string))
                        {
                            if (replacementValue is string replacementValueTypeToString)
                            {
                                field.SetValue(legacyLayerWithReplacements, replacementValueTypeToString);
                            }
                        }
                        else
                        {
                            field.SetValue(legacyLayerWithReplacements, replacementValue);
                        }
                    }


                    if (legacyLayerVersion > replacementLayer.Version)
                    {
                        legacyLayerWithReplacements.DoShowWarningLegacyVersionNewerThanEdited = true;
                    }
                }

                if (legacyLayerWithReplacements?.Visibility?.Trim() == "DELETED")
                {
                    continue;
                }

                if (string.IsNullOrEmpty(legacyLayerWithReplacements.Script))
                {
                    legacyLayerWithReplacements.Script = Settings.tileloader_default_script;
                }
                if (string.IsNullOrEmpty(legacyLayerWithReplacements.Visibility))
                {
                    legacyLayerWithReplacements.Visibility = "Visible";
                }
                if (legacyLayerWithReplacements.Tag == "/")
                {
                    legacyLayerWithReplacements.Tag = "";
                }
                legacyLayerWithReplacements.Country = legacyLayerWithReplacements.Country?.Replace("*", "World");
                List<string> listOfAllFormatsAcceptedWithTransparency = new List<string> { "png" };
                if (!string.IsNullOrWhiteSpace(legacyLayerWithReplacements.TilesFormat) && listOfAllFormatsAcceptedWithTransparency.Contains(legacyLayerWithReplacements.TilesFormat))
                {
                    legacyLayerWithReplacements.TilesFormatHasTransparency = true;
                }

                //make sure there is no null values inside the layer
                foreach (FieldInfo field in typeof(Layers).GetFields(fieldsBindingFlags))
                {
                    object actualValue = field.GetValue(legacyLayerWithReplacements);
                    if (actualValue is null)
                    {
                        if (field.FieldType == typeof(string))
                        {
                            field.SetValue(legacyLayerWithReplacements, string.Empty);
                        }
                        else if (field.FieldType == typeof(int))
                        {
                            field.SetValue(legacyLayerWithReplacements, 0);
                        }
                        else
                        {
                            field.SetValue(legacyLayerWithReplacements, null);
                        }
                    }
                }

                Layers.Add(System.Convert.ToInt32(legacyLayerWithReplacements.Id), legacyLayerWithReplacements);
            }
        }

        public static string PreviewGetUrl(int id, double TargetZoomLevel, double Latitude, double Longitude)
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
                int Zoom = Math.Max(System.Convert.ToInt32(Math.Round(TargetZoomLevel)) - 1, 0);
                if (Zoom < min_zoom) { Zoom = min_zoom; }
                if (Zoom > max_zoom) { Zoom = max_zoom; }

                if (Zoom < back_min_zoom) { Zoom = Math.Max(back_min_zoom, Zoom); }
                if (Zoom > back_max_zoom) { Zoom = Math.Min(back_max_zoom, Zoom); }

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
            return string.Empty;
        }
    }
}
