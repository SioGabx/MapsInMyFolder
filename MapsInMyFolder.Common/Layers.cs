using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MapsInMyFolder.Commun
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

        public static Layers Current = Empty();
        public static int StartupLayerId = Settings.layer_startup_id;

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
        public static void LayersMergeLegacyWithEdited(List<Layers> legacyLayers, List<Layers> editedLayers)
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

    }
}
