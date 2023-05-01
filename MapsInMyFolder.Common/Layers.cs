using System;
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
        public string class_categorie { get; set; }
        public string class_pays { get; set; }
        public string class_identifiant { get; set; }
        public string class_tile_url { get; set; }
        public string class_tile_fallback_url { get; set; }
        public string class_site { get; set; }
        public string class_site_url { get; set; }
        public int? class_min_zoom { get; set; }
        public int? class_max_zoom { get; set; }
        public string class_format { get; set; }
        public int? class_tiles_size { get; set; }
        public string class_tilecomputationscript { get; set; }
        public string class_visibility { get; set; }
        public SpecialsOptions class_specialsoptions { get; set; }
        public string class_rectangles { get; set; }
        public int class_version { get; set; }
        public bool class_hasscale { get; set; }


        public Layers(int class_id, bool class_favorite, string class_name, string class_description, string class_categorie, string class_pays, string class_identifiant, string class_tile_url, string class_tile_fallback_url, string class_site, string class_site_url, int? class_min_zoom, int? class_max_zoom, string class_format, int? class_tiles_size, string class_tilecomputationscript, string class_visibility, SpecialsOptions class_specialsoptions, string class_rectangles, int class_version, bool class_hasscale)
        {
            this.class_id = class_id;
            this.class_favorite = class_favorite;
            this.class_name = class_name;
            this.class_description = class_description;
            this.class_categorie = class_categorie;
            this.class_pays = class_pays;
            this.class_identifiant = class_identifiant;
            this.class_tile_url = class_tile_url;
            this.class_tile_fallback_url = class_tile_fallback_url;
            this.class_site = class_site;
            this.class_site_url = class_site_url;
            this.class_min_zoom = class_min_zoom;
            this.class_max_zoom = class_max_zoom;
            this.class_format = class_format;
            this.class_tiles_size = class_tiles_size;
            this.class_tilecomputationscript = class_tilecomputationscript;
            this.class_visibility = class_visibility;
            this.class_specialsoptions = class_specialsoptions;
            this.class_rectangles = class_rectangles;
            this.class_version = class_version;
            this.class_hasscale = class_hasscale;
        }

        public static Layers Curent = Layers.Empty();

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
            public string PBFJsonStyle { get; set; }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(BackgroundColor) && string.IsNullOrWhiteSpace(PBFJsonStyle))
                {
                    return string.Empty;
                }
                else if (!string.IsNullOrWhiteSpace(BackgroundColor))
                {
                    return "{\"BackgroundColor\":\"" + BackgroundColor + "\"}";
                }
                else if (!string.IsNullOrWhiteSpace(PBFJsonStyle))
                {
                    return "{\"PBFJsonStyle\":\"" + System.Text.Json.JsonSerializer.Serialize<string>(PBFJsonStyle) + "\"}";
                }
                else
                {
                    return System.Text.Json.JsonSerializer.Serialize<Layers.SpecialsOptions>(this);
                }


            }
        }

        public static Layers Empty(int LayerId = -1)
        {
            return new Layers(LayerId, false, "", "Une erreur s'est produite dans la lecture des données. \n Données de secours fournie par OpenStreetMap.", "", "", "", "http://tile.openstreetmap.org/{z}/{x}/{y}.png", "FALLBACK_URL", "", "", null, null, "jpeg", 256, "function getTile(args){return args;}", "Visible", new SpecialsOptions(), "", 0, true);
        }

        public static class Convert
        {
            public static Layers CurentLayerToLayer()
            {
                return GetLayerById(Curent.class_id);
            }

            public static void ToCurentLayer(Layers layer)
            {
                Curent = (Layers)layer.MemberwiseClone();
            }
        }

        public static List<Dictionary<int, Layers>> Layers_Dictionary_List = new List<Dictionary<int, Layers>>();

        public static Layers GetLayerById(int id)
        {
            foreach (Dictionary<int, Layers> layer_dic in Layers_Dictionary_List)
            {
                try
                {
                    if (layer_dic.Keys.First() == id)
                    {
                        Layers layer_class = layer_dic[id];
                        return layer_class;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Erreur : l'id de la tache n'existe pas.");
                }
            }
            return null;
        }

        public static bool RemoveLayerById(int id)
        {
            foreach (Dictionary<int, Layers> layer_dic in Layers_Dictionary_List)
            {
                try
                {
                    if (layer_dic.Keys.First() == id)
                    {
                        Layers_Dictionary_List.Remove(layer_dic);
                        return true;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Erreur : l'id de la tache n'existe pas.");
                }
            }
            return false;
        }

        public static IEnumerable<Layers> GetLayersList()
        {
            foreach (Dictionary<int, Layers> individualdictionnary in Layers_Dictionary_List)
            {
                yield return individualdictionnary.Values.First();
            }
        }
    }
}
