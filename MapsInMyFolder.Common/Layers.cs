using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsInMyFolder.Commun
{
    public class Layers
    {
        public int Id { get; set; }
        public bool IsFavorite { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
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


        public Layers(int Id, bool IsFavorite, string Name, string Description, string Category, string Country, string Identifier, string TileUrl, string SiteName, string SiteUrl, int? MinZoom, int? MaxZoom, string TilesFormat, string Style, int? TilesSize, string Script, string Visibility, LayersSpecialsOptions SpecialsOptions, string BoundaryRectangles, int Version, bool IsAtScale)
        {
            this.Id = Id;
            this.IsFavorite = IsFavorite;
            this.Name = Name;
            this.Description = Description;
            this.Category = Category;
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

            public static Layers Copy(Layers layer) {
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
    }
}
