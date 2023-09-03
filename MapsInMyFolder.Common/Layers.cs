using System.Collections.Generic;
using System.Linq;

namespace MapsInMyFolder.Commun
{
    public class Layers
    {
        public int class_id { get; set; }
        public bool class_favorite { get; set; }
        public string class_name { get; set; }
        public string class_description { get; set; }
        public string class_category { get; set; }
        public string class_country { get; set; }
        public string class_identifier { get; set; }
        public string class_tile_url { get; set; }
        public string class_site { get; set; }
        public string class_site_url { get; set; }
        public int? class_min_zoom { get; set; }
        public int? class_max_zoom { get; set; }
        public string class_format { get; set; }
        public string class_style { get; set; }
        public int? class_tiles_size { get; set; }
        public string class_script { get; set; }
        public string class_visibility { get; set; }
        public SpecialsOptions class_specialsoptions { get; set; }
        public string class_rectangles { get; set; }
        public int class_version { get; set; }
        public bool class_hasscale { get; set; }


        public Layers(int class_id, bool class_favorite, string class_name, string class_description, string class_category, string class_country, string class_identifier, string class_tile_url, string class_site, string class_site_url, int? class_min_zoom, int? class_max_zoom, string class_format, string class_style, int? class_tiles_size, string class_script, string class_visibility, SpecialsOptions class_specialsoptions, string class_rectangles, int class_version, bool class_hasscale)
        {
            this.class_id = class_id;
            this.class_favorite = class_favorite;
            this.class_name = class_name;
            this.class_description = class_description;
            this.class_category = class_category;
            this.class_country = class_country;
            this.class_identifier = class_identifier;
            this.class_tile_url = class_tile_url;
            this.class_site = class_site;
            this.class_site_url = class_site_url;
            this.class_min_zoom = class_min_zoom;
            this.class_max_zoom = class_max_zoom;
            this.class_format = class_format;
            this.class_style = class_style;
            this.class_tiles_size = class_tiles_size;
            this.class_script = class_script;
            this.class_visibility = class_visibility;
            this.class_specialsoptions = class_specialsoptions;
            this.class_rectangles = class_rectangles;
            this.class_version = class_version;
            this.class_hasscale = class_hasscale;
        }

        public static Layers Current = Empty();

        public class SpecialsOptions
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


            public override string ToString()
            {
                return System.Text.Json.JsonSerializer.Serialize(this);
            }
        }

        public static Layers Empty(int LayerId = -1)
        {
            return new Layers(LayerId, false, "", "An error occurred while reading the data. \n Backup data provided by OpenStreetMap.", "", "", "", "http://tile.openstreetmap.org/{z}/{x}/{y}.png", "", "", null, null, "jpeg", "", 256, "function getTile(args){return args;}", "Visible", new SpecialsOptions(), "", 0, true);
        }

        public static class Convert
        {
            public static Layers CurentLayerToLayer()
            {
                return GetLayerById(Current.class_id);
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
