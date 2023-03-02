using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows;

namespace MapsInMyFolder.Commun
{
    public enum ListDisplayType { BIG, CONDENSED, SMALL, LIST, LIST_ALTERNAT }
    public enum LayersSort { ID, NOM, DESCRIPTION, CATEGORIE, IDENTIFIANT, FORMAT, SITE }
    public enum LayersOrder { ASC, DESC }

    public static class Settings
    {
        public static int layer_startup_id = 0;
        public static double background_layer_opacity = 0.3;
        public static int background_layer_color_R = 230;
        public static int background_layer_color_G = 230;
        public static int background_layer_color_B = 230;
        public static string working_folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"MapsInMyFolder\");
        public static string temp_folder = System.IO.Path.GetTempPath() + @"MapsInMyFolder\";
        public static int max_retry_download = 3;
        public static int max_redirection_download_tile = 5;
        public static int tiles_cache_expire_after_x_days = 1;
        public static int http_client_timeout_in_seconds = 30;
        public static bool nettoyer_cache_browser_au_demarrage = true;
        public static bool nettoyer_cache_layers_au_demarrage = false;
        public static int max_download_project_in_parralele = 1;
        public static int max_download_tiles_in_parralele = 10;
        public static int waiting_before_start_another_tile_download = 0;
        public static bool generate_transparent_tiles_on_404 = true;
        public static bool generate_transparent_tiles_on_error = true;
        public static bool map_view_error_tile = true;
        public static bool is_in_debug_mode = false;
        public static bool show_layer_devtool = false;
        public static bool show_download_devtool = false;
        public static bool layerpanel_website_IsVisible = false;
        public static bool layerpanel_livepreview = true;
        public static bool layerpanel_put_non_letter_layername_at_the_end = true;
        public static bool layerpanel_favorite_at_top = true;
        public static ListDisplayType layerpanel_displaystyle = ListDisplayType.CONDENSED;
        public static double NO_PIN_starting_location_latitude = 48.175224;
        public static double NO_PIN_starting_location_longitude = 6.449794;
        public static double SE_PIN_starting_location_latitude = 48.174423;
        public static double SE_PIN_starting_location_longitude = 6.45085;
        public static int map_defaut_zoom_level = 18;
        public static bool zoom_limite_taille_carte = false;
        public static string tileloader_default_script = "//default script\nfunction getTile(args) {\n   return args;\n}";
        public static string tileloader_template_script = "function getTile(args) {\n   return args;\n}\n\nfunction getPreview(args){\n   return getTile(args);\n}\n\nfunction getPreviewFallback(args){\n   return getTile(args);\n}";
        public static string user_agent = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        public static LayersSort layers_Sort = LayersSort.NOM;
        public static LayersOrder Layers_Order = LayersOrder.ASC;
        public static string database_pathname = "MapsInMyFolder_Database.db";
        public static Visibility visibility_pins = Visibility.Hidden;
        public static double selection_rectangle_resize_tblr_gap = 15;
        public static double selection_rectangle_resize_angle_gap = 20;
        public static int animations_duration_millisecond = 300;
        public static int maps_margin_ZoomToBounds = 100;
        public static bool disable_selection_rectangle_moving = false;
        public static bool map_show_tile_border = false;
        public static string github_repository_url = "https://github.com/SioGabx/MapsInMyFolder";
        public static string github_database_name = "MapsInMyFolder_Database.db";

        public static bool search_application_update_on_startup = true;
        public static bool search_database_update_on_startup = true;

        public static (Type, FieldInfo[]) SettingsGetFields()
        {
            Type type = typeof(Settings);
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            return (type, fields);
        }

        public static string SettingsPath(bool save = false)
        {
            const string regPath = @"SOFTWARE\SioGabx\MapsInMyFolder";
            const string regKey = "settingsPath";

            if (save)
            {
                string value = System.IO.Path.Combine(working_folder, "Settings.xml");
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
                    value = okey.GetValue(regKey)?.ToString();
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
                string value = XMLParser.Settings.Read(fieldName);
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
                        Message.NoReturnBoxAsync("Une mauvaise valeur à été détécté dans les paramètres ! La valeur par défault à été réappliqué \n    - Nom : " + fieldName + "\n    - Valeur : " + value + "\n    - Valeur par defaut : " + fieldValue, "Erreur");
                    }
                    finally
                    {
                        if (IsErrorOnConvert)
                        {
                            XMLParser.Settings.Write(fieldName, fieldValue.ToString());
                        }
                    }
                    type.GetField(fieldName).SetValue(null, ConvertedValue);
                }
            }

        }

        public static void SaveSettings()
        {
            (_, FieldInfo[] fields) = SettingsGetFields();
            foreach (FieldInfo field in fields)
            {
                var fieldValue = field.GetValue(null);
                var fieldName = field.Name;
                XMLParser.Settings.Write(fieldName, fieldValue.ToString());
            }
        }
    }
}
