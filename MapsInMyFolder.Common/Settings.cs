using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MapsInMyFolder.Commun
{
    public enum ListDisplayType { BIG, CONDENSED, LIST, LIST_ALTERNAT }
    public enum Layers_Sort { ID, DISPLAY_NAME, DESCRIPTION, CATEGORIE, IDENTIFIANT, FORMAT, SITE }
    public enum Layers_Order { ASC, DESC }


    public static class Settings
    {
        public static int layer_startup_id = 0;                                                 //Selectionne le calque ayant l'id n°x dans la liste xml des calques | Default=0 (choix du 1er dans la liste)
        public static double background_layer_opacity = 0.3;                                    //Defini l'opacité de la carte à l'arrière plan en cas de couche transparente | Default=0.2
        public static int background_layer_color_R = 255;                                    //Defini la couleur du fond de carte.
        public static int background_layer_color_G = 255;                                    //Defini la couleur du fond de carte.
        public static int background_layer_color_B = 255;                                    //Defini la couleur du fond de carte.

        public static string working_folder = @"C:\Users\franc\Desktop\MapsInMyFolder_Project\AppData_MapsInMyFolder";
        public static string temp_folder = System.IO.Path.GetTempPath() + @"MapsInMyFolder\";   //chemin temporaire pour enregistrer les fichiers | Default=System.IO.Path.GetTempPath() + @"MapsInMyFolder\"
        public static int max_retry_download = 3;                                               //Nombre max de passe lors du telechargement d'un calque | Default=3
        public static int max_redirection_download_tile = 5;                                               //Nombre max de passe de redirection d'une tuile
        public static int tiles_cache_expire_after_x_days = 1;                                  //Nombre de jours apres lequel retelecharger les tuiles | Default=1
        public static int max_download_project_in_parralele = 1;                                //Nombre telechargement en para | Default=1
        public static int max_download_tiles_in_parralele = 10;                                 //Nombre telechargement de tuile en para | Default=10
        public static int waiting_before_start_another_tile_download = 0;                        //Nombre de milisecondes entre chaque téléchargement d'une tuile (seulement si max_download_tiles_in_parralele = 1 de préférence)
        public static bool generate_transparent_tiles_on_404 = true;                            //Genere une tuile en cas d'erreur no data | Default=true
        public static bool generate_transparent_tiles_on_error = true;                            //Genere une tuile en cas d'erreur  | Default=true
        
        public static bool is_in_debug_mode = true;                                                  //Debug : affiche les devtool des deux navigateur       
        public static bool show_layer_devtool = false;                                                  //Debug : affiche les devtool des deux navigateur       
        public static bool show_download_devtool = false;                                                  //Debug : affiche les devtool des deux navigateur       
        public static bool layerpanel_website_IsVisible = false;                                //Affiche les sites web dans le nom des layers
        public static bool layerpanel_livepreview = true;                                      //Charge dans les migniatures des claques une tuile.                   
        public static bool layerpanel_put_non_letter_layername_at_the_end = true;
        public static ListDisplayType layerpanel_compactstyle = ListDisplayType.CONDENSED;                                      //Charge dans les migniatures des claques une tuile.                   
        public static double NO_PIN_starting_location_latitude = 48.175224;
        public static double NO_PIN_starting_location_longitude = 6.449794;
        public static double SE_PIN_starting_location_latitude = 48.174423;
        public static double SE_PIN_starting_location_longitude = 6.45085;
        // public static Location NO_PIN_starting_location = new Location(NO_PIN_starting_location_latitude, NO_PIN_starting_location_longitude);           //Carré de séléction de départ. TODO : Mettre par defaut la dernière selection réalisée
        // public static Location SE_PIN_starting_location = new Location(SE_PIN_starting_location_latitude, SE_PIN_starting_location_longitude);          //Carré de séléction de départ. TODO : Mettre par defaut la dernière selection réalisée
        public static int map_defaut_zoom_level = 18;
        public static bool zoom_limite_taille_carte = false;                                    //Empeche de zoomer au dela de la limite de la carte
        public static string database_default_path_url = @"C:\Users\franc\Desktop\MapsInMyFolder_Project\layers_sqlite.db"; //todo : https in github 
        public static string tileloader_default_script = "//default script\nfunction main(args) {\n   return args;\n}"; //todo : https in github 
        public static string user_agent = @"Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        //public static string user_agent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.114 Safari/537.36 Edg/103.0.1264.62";

        public static string default_database_pathname = Settings.working_folder + @"\layers_sqlite.db";
        public static Layers_Sort layers_Sort = Layers_Sort.DISPLAY_NAME;//Layers_Sort.ID; 
        public static Layers_Order Layers_Order = Layers_Order.ASC;//Layers_Sort.ID; 
        public static string selected_database_pathname = @"C:\Users\franc\Desktop\MapsInMyFolder_Project\AppData_MapsInMyFolder\layers_sqlite.db";
        public static Visibility visibility_pins = Visibility.Hidden;
        public static double selection_rectangle_resize_tblr_gap = 15;
        public static double selection_rectangle_resize_angle_gap = 20;
        public static TimeSpan animations_duration = new TimeSpan(0, 0, 0, 00, 300);
        public static int maps_margin_ZoomToBounds = 100;
        public static bool disable_selection_rectangle_moving = false;
        public static bool map_show_tile_border = false;


        public static void LoadSettingsAsync()
        {
            Type type = typeof(Settings);
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                var fieldValue = field.GetValue(null);
                //Debug.WriteLine(fieldValue.GetType());
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
        }



        public static void SaveSettings()
        {
            XMLParser.Write("visibility_pins", "Hidden");
            XMLParser.Write("layerpanel_compactstyle", "condensed");
            XMLParser.Write("NO_PIN_starting_location_latitude", "48.175224");
            XMLParser.Write("NO_PIN_starting_location_longitude", "6.449794");
            XMLParser.Write("SE_PIN_starting_location_latitude", "48.174423");
            XMLParser.Write("SE_PIN_starting_location_longitude", "6.45085");
        }

 
    }



    



}
