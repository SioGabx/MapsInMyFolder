using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsInMyFolder.Commun
{
    public class Layers
    {
        public int class_id;
        public bool class_favorite;
        public string class_name;
        public string class_description;
        public string class_categorie;
        public string class_identifiant;
        public string class_tile_url;
        public string class_site;
        public string class_site_url;
        public int class_min_zoom;
        public int class_max_zoom;
        public string class_format;
        public int class_tiles_size;
        public string class_tilecomputationscript;
        public SpecialsOptions class_specialsoptions;

        public class SpecialsOptions
        {
            public string BackgroundColor { get; set; }
            public string PBFJsonStyle { get; set; }
        }

        public Layers(int class_id, bool class_favorite, string class_name, string class_description, string class_categorie, string class_identifiant, string class_tile_url, string class_site, string class_site_url, int class_min_zoom, int class_max_zoom, string class_format, int class_tiles_size, string class_tilecomputationscript, SpecialsOptions class_specialsoptions)
        {
            this.class_id = class_id;
            this.class_favorite = class_favorite;
            this.class_name = class_name;
            this.class_description = class_description;
            this.class_categorie = class_categorie;
            this.class_identifiant = class_identifiant;
            this.class_tile_url = class_tile_url;
            this.class_site = class_site;
            this.class_site_url = class_site_url;
            this.class_min_zoom = class_min_zoom;
            this.class_max_zoom = class_max_zoom;
            this.class_format = class_format;
            this.class_tiles_size = class_tiles_size;
            this.class_tilecomputationscript = class_tilecomputationscript;
            this.class_specialsoptions = class_specialsoptions;
        }

        public static Layers Empty(int LayerId = -1)
        {
            return new Layers(LayerId, false, "", "Une erreur s'est produite dans la lecture des données. \n Données de secours fournie par OpenStreetMap.", "", "", "http://tile.openstreetmap.org/{z}/{x}/{y}.png", "", "", 0, 19, "jpeg", 256, "function getTile(args){return args;}", new SpecialsOptions());
        }

        public static class Convert
        {
            public static Layers CurentLayerToLayer()
            {
                return GetLayerById(Curent.Layer.class_id);
            }

            public static void ToCurentLayer(Layers layer)
            {
                Curent.Layer.class_id = layer.class_id;
                Curent.Layer.class_tiles_size = layer.class_tiles_size;
                Curent.Layer.class_name = layer.class_name;
                Curent.Layer.class_description = layer.class_description;

                Curent.Layer.class_categorie = layer.class_categorie;
                Curent.Layer.class_identifiant = layer.class_identifiant;
                Curent.Layer.class_tile_url = layer.class_tile_url;
                Curent.Layer.class_site = layer.class_site;

                Curent.Layer.class_site_url = layer.class_site_url;
                Curent.Layer.class_min_zoom = layer.class_min_zoom;
                Curent.Layer.class_max_zoom = layer.class_max_zoom;
                Curent.Layer.class_format = layer.class_format;

                Curent.Layer.class_tilecomputationscript = layer.class_tilecomputationscript;
                Curent.Layer.class_specialsoptions = layer.class_specialsoptions;
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

        public static List<Layers> GetLayersList()
        {
            List<Layers> layers = new List<Layers>();
            foreach (Dictionary<int, Layers> individualdictionnary in Layers_Dictionary_List)
            {
                layers.Add(individualdictionnary.Values.First());
            }
            return layers;
        }
    }
}
