using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapsInMyFolder.Commun
{
    public enum ListDisplayType { BIG, CONDENSED, LIST, LIST_ALTERNAT }
    public enum LayersSort { ID, NOM, DESCRIPTION, CATEGORIE, IDENTIFIANT, FORMAT, SITE }
    public enum LayersOrder { ASC, DESC }


    //public class SettingsProperty
    //{
    //    string Name;
    //    string Description;

    //    public SettingsProperty(string Name, string Description)
    //    {
    //        this.Name = Name;
    //        this.Description = Description;
    //    }

    //}



    public static class Settings
    {
        public static int layer_startup_id = 0;                                                 //Selectionne le calque ayant l'id n°x dans la liste xml des calques | Default=0 (choix du 1er dans la liste)
        public static double background_layer_opacity = 0.3;                                    //Defini l'opacité de la carte à l'arrière plan en cas de couche transparente | Default=0.2
        public static int background_layer_color_R = 230;                                    //Defini la couleur du fond de carte.
        public static int background_layer_color_G = 230;                                    //Defini la couleur du fond de carte.
        public static int background_layer_color_B = 230;                                    //Defini la couleur du fond de carte.

        public static string working_folder = @"C:\Users\franc\Desktop\AppData_MapsInMyFolder\";
        public static string temp_folder = System.IO.Path.GetTempPath() + @"MapsInMyFolder\";   //chemin temporaire pour enregistrer les fichiers | Default=System.IO.Path.GetTempPath() + @"MapsInMyFolder\"
        public static int max_retry_download = 3;                                               //Nombre max de passe lors du telechargement d'un calque | Default=3
        public static int max_redirection_download_tile = 5;                                               //Nombre max de passe de redirection d'une tuile
        public static int tiles_cache_expire_after_x_days = 1;                                  //Nombre de jours apres lequel retelecharger les tuiles | Default=1
        public static int max_download_project_in_parralele = 1;                                //Nombre telechargement en para | Default=1
        public static int max_download_tiles_in_parralele = 10;                                 //Nombre telechargement de tuile en para | Default=10
        public static int waiting_before_start_another_tile_download = 0;                        //Nombre de milisecondes entre chaque téléchargement d'une tuile (seulement si max_download_tiles_in_parralele = 1 de préférence)
        public static bool generate_transparent_tiles_on_404 = true;                            //Genere une tuile en cas d'erreur no data | Default=true
        public static bool generate_transparent_tiles_on_error = true;                            //Genere une tuile en cas d'erreur  | Default=true

        public static bool is_in_debug_mode = false;                                                  //Debug : affiche les devtool des deux navigateur       
        public static bool show_layer_devtool = false;                                                  //Debug : affiche les devtool des deux navigateur       
        public static bool show_download_devtool = false;                                                  //Debug : affiche les devtool des deux navigateur       
        public static bool layerpanel_website_IsVisible = false;                                //Affiche les sites web dans le nom des layers
        public static bool layerpanel_livepreview = true;                                      //Charge dans les migniatures des claques une tuile.                   
        public static bool layerpanel_put_non_letter_layername_at_the_end = true;
        public static ListDisplayType layerpanel_displaystyle = ListDisplayType.CONDENSED;                                      //Charge dans les migniatures des claques une tuile.                   
        public static double NO_PIN_starting_location_latitude = 48.175224;
        public static double NO_PIN_starting_location_longitude = 6.449794;
        public static double SE_PIN_starting_location_latitude = 48.174423;
        public static double SE_PIN_starting_location_longitude = 6.45085;
        public static int map_defaut_zoom_level = 18;
        public static bool zoom_limite_taille_carte = false;                                    //Empeche de zoomer au dela de la limite de la carte
        public static string tileloader_default_script = "//default script\nfunction main(args) {\n   return args;\n}"; //todo : https in github 
        public static string user_agent = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        //public static string user_agent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62";

        public static LayersSort layers_Sort = LayersSort.NOM;//LayersSort.ID; 
        public static LayersOrder Layers_Order = LayersOrder.ASC;//LayersSort.ID; 
        public static string database_pathname = "layers_sqlite.db";
        public static Visibility visibility_pins = Visibility.Hidden;
        public static double selection_rectangle_resize_tblr_gap = 15;
        public static double selection_rectangle_resize_angle_gap = 20;
        public static TimeSpan animations_duration = new TimeSpan(0, 0, 0, 00, 300);
        public static int maps_margin_ZoomToBounds = 100;
        public static bool disable_selection_rectangle_moving = false;
        public static bool map_show_tile_border = false;

        //public static Dictionary<string, SettingsProperty> DictionaryCategorieProperty = new Dictionary<string, SettingsProperty>()
        //{
        //    {"Carte", new SettingsProperty("layer_startup_id","Carte affichée au démarage")}
        //};



        public static (Type, FieldInfo[]) SettingsGetFields()
        {
            Type type = typeof(Settings);
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            var returnTurple = (type, fields);
            return returnTurple;
        }


        public static string SettingsPath(bool save = false)
        {
            const string regPath = @"SOFTWARE\SioGabx\MapsInMyFolder";
            const string regKey = "settingsPath";

            if (save)
            {
                string value = System.IO.Path.Combine(Settings.working_folder, "Settings.xml");
                RegistryKey ckey = Registry.CurrentUser.CreateSubKey(regPath);
                ckey.SetValue(regKey, value);
                ckey.Close();
                return value;
            }
            else
            {
                RegistryKey okey = Registry.CurrentUser.OpenSubKey(regPath);
                string value = String.Empty;
                if (okey != null)
                {
                    value = okey.GetValue(regKey).ToString();
                    okey.Close();
                }
                if (string.IsNullOrEmpty(value))
                {
                    return SettingsPath(true);
                }
                else
                {
                    return value;
                }
            }
        }



        public static void LoadSetting()
        {
            (Type type, FieldInfo[] fields) GetFields = SettingsGetFields();
            Type type = GetFields.type;
            foreach (FieldInfo field in GetFields.fields)
            {
                var fieldValue = field.GetValue(null);
                var fieldName = field.Name;
                string value = XMLParser.Read(fieldName);
                bool IsErrorOnConvert = false;

                if (!(value is null))
                {
                    object ConvertedValue;
                    Type value_type = fieldValue.GetType();
                    try
                    {
                        if (value_type.IsEnum)
                        {
                            try
                            {
                                ConvertedValue = Enum.Parse(value_type, value);
                            }
                            catch (Exception)
                            {
                                ConvertedValue = Enum.Parse(value_type, 0.ToString());
                                IsErrorOnConvert = true;
                            }
                        }
                        else
                        {
                            ConvertedValue = Convert.ChangeType(value, value_type);
                        }
                    }
                    catch (Exception)
                    {
                        ConvertedValue = Convert.ChangeType(fieldValue, value_type);
                        IsErrorOnConvert = true;
                        //Message.NoReturnBoxAsync("Une mauvaise valeur à été détécté dans les paramêtres ! La valeur par défault à été réappliqué \n    - Nom : " + fieldName + "\n    - Valeur : " + value + "\n    - Valeur par defaut : " + fieldValue, "Erreur");

                    }
                    finally
                    {
                        if (IsErrorOnConvert)
                        {
                            XMLParser.Write(fieldName, fieldValue.ToString());
                            //await Message.SetContentDialog("Une mauvaise valeur à été détéctée dans les paramêtres ! La valeur par défault à été réappliquée. \n\n    - Nom : " + fieldName + "\n    - Valeur : " + value + "\n    - Valeur par defaut : " + fieldValue + "\n\n→ Un redémarrage de l'application est vivement conseillé !", "Erreur", MessageDialogButton.OK).ShowAsync();
                        }
                    }
                    type.GetField(fieldName).SetValue(null, ConvertedValue);
                }
            }


            //replace PATH variable
            //Dictionary<string, string> variables = new Dictionary<string, string>();
            //variables.Add("WORKINGFOLDER", Settings.working_folder);
            //foreach (KeyValuePair<string, string> pair in variables)
            //{
            //    Settings.selected_database_pathname = selected_database_pathname.Replace("$" + pair.Key + "$", pair.Value);
            //}

        }

        public static void SaveSettings()
        {
            (_, FieldInfo[] fields) = SettingsGetFields();
            foreach (FieldInfo field in fields)
            {
                var fieldValue = field.GetValue(null);
                var fieldName = field.Name;
                XMLParser.Write(fieldName, fieldValue.ToString());
            }

            //XMLParser.Write("visibility_pins", "Hidden");
            //XMLParser.Write("layerpanel_displaystyle", "condensed");
            //XMLParser.Write("NO_PIN_starting_location_latitude", "48.175224");
            //XMLParser.Write("NO_PIN_starting_location_longitude", "6.449794");
            //XMLParser.Write("SE_PIN_starting_location_latitude", "48.174423");
            //XMLParser.Write("SE_PIN_starting_location_longitude", "6.45085");
        }
    }
}
