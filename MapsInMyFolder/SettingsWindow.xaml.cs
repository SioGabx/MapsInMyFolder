using MapsInMyFolder.Commun;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    //    public static class Extension
    //    {
    //        public static void ScrollToElement(this System.Windows.Controls.ScrollViewer scrollViewer, System.Windows.UIElement uIElement)
    //        {
    //            GeneralTransform groupBoxTransform = uIElement.TransformToAncestor(scrollViewer);
    //            Rect rectangle = groupBoxTransform.TransformBounds(new Rect(new Point(0, 0), uIElement.RenderSize));
    //            scrollViewer.ScrollToVerticalOffset(rectangle.Top + scrollViewer.VerticalOffset);
    //        }
    //    }
    /// <summary>
    /// Logique d'interaction pour SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.Title = "MapsInMyFolder - Settings";
            TitleTextBox.Text = "MapsInMyFolder - Settings";
            //int RGBint = Convert.ToInt32("FFD700", 16);
            //byte Red = (byte)((RGBint >> 16) & 255);
            //byte Green = (byte)((RGBint >> 8) & 255);
            //byte Blue = (byte)(RGBint & 255);
            //Color.FromRgb(Red, Green, Blue);

        }

        int DefaultValuesHachCode = 0;
        private void Window_Initialized(object sender, EventArgs e) { }


        void DoIScrollToElement(UIElement element, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Released)
            {
                SettingsScrollViewer.ScrollToElement(element);
            }
            //NameHiddenIdValue layer_startup_idSelectedItem = (NameHiddenIdValue)layer_startup_id.SelectedItem;
            //MessageBox.Show(layer_startup_idSelectedItem.Id.ToString());
            //MessageBox.Show(Commun.Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer).ToString());
        }


        private void MenuItem_Telechargement(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(TelechargementSettingsLabel, e);
        }

        private void MenuItem_Calques(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(CalqueSettingsLabel, e);
        }

        private void MenuItem_Carte(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(CarteSettingsLabel, e);
        }

        private void MenuItem_Avance(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(AvanceSettingsLabel, e);
        }

        private void MenuItem_Update(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(UpdateSettingsLabel, e);
        }

        private void MenuItem_APropos(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(AProposSettingsLabel, e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSettingsWindow();
        }


        void InitSettingsWindow()
        {
            //layer_startup_id
            foreach (Layers layer in Layers.GetLayersList())
            {
                if (string.Equals(layer.class_format, "JPEG", StringComparison.InvariantCultureIgnoreCase))
                {
                    int index = layer_startup_id.Items.Add(new NameHiddenIdValue(layer.class_id, layer.class_name));
                    if (layer.class_id == Settings.layer_startup_id)
                    {
                        layer_startup_id.SelectedIndex = index;
                    }

                }
            }

            //layersSort
            var LayersSortValues = (LayersSort[])Enum.GetValues(typeof(LayersSort));
            for (int LayersSortValuesIndex = 0; LayersSortValuesIndex < LayersSortValues.Length; LayersSortValuesIndex++)
            {
                layersSort.Items.Add(LayersSortValues[LayersSortValuesIndex].ToString().ToLowerInvariant().UcFirst());
                if (LayersSortValues[LayersSortValuesIndex].ToString() == Settings.layers_Sort.ToString())
                {
                    layersSort.SelectedIndex = LayersSortValuesIndex;
                }
            }

            //LayersOrder
            LayersOrder.IsChecked = Settings.Layers_Order == Commun.LayersOrder.ASC;

            //layerpanel_put_non_letter_layername_at_the_end
            layerpanel_put_non_letter_layername_at_the_end.IsChecked = Settings.layerpanel_put_non_letter_layername_at_the_end;

            //layerpanel_displaystyle
            var ListDisplayTypeValues = (ListDisplayType[])Enum.GetValues(typeof(ListDisplayType));
            for (int ListDisplayTypeValuesIndex = 0; ListDisplayTypeValuesIndex < ListDisplayTypeValues.Length; ListDisplayTypeValuesIndex++)
            {
                layerpanel_displaystyle.Items.Add(ListDisplayTypeValues[ListDisplayTypeValuesIndex].ToString().ToLowerInvariant().UcFirst());
                if (ListDisplayTypeValues[ListDisplayTypeValuesIndex].ToString() == Settings.layerpanel_displaystyle.ToString())
                {
                    layerpanel_displaystyle.SelectedIndex = ListDisplayTypeValuesIndex;
                }
            }

            //tileloader_default_script
            tileloader_default_script.Text = Settings.tileloader_default_script;

            //working_folder
            working_folder.Text = Settings.working_folder;

            //temp_folder
            temp_folder.Text = Settings.temp_folder;

            //max_retry_download
            max_retry_download.Text = Settings.max_retry_download.ToString();

            //max_redirection_download_tile
            max_redirection_download_tile.Text = Settings.max_redirection_download_tile.ToString();

            //tiles_cache_expire_after_x_days
            tiles_cache_expire_after_x_days.Text = Settings.tiles_cache_expire_after_x_days.ToString();

            //max_download_project_in_parralele
            max_download_project_in_parralele.Text = Settings.max_download_project_in_parralele.ToString();

            //max_download_tiles_in_parralele
            max_download_tiles_in_parralele.Text = Settings.max_download_tiles_in_parralele.ToString();

            //waiting_before_start_another_tile_download
            waiting_before_start_another_tile_download.Text = Settings.waiting_before_start_another_tile_download.ToString();

            //user_agent
            user_agent.Text = Settings.user_agent;

            //generate_transparent_tiles_on_error
            generate_transparent_tiles_on_error.IsChecked = Settings.generate_transparent_tiles_on_error;

            //generate_transparent_tiles_on_404
            generate_transparent_tiles_on_404.IsChecked = Settings.generate_transparent_tiles_on_404;

            //generate_transparent_tiles_never
            generate_transparent_tiles_never.IsChecked = (!Settings.generate_transparent_tiles_on_error && !Settings.generate_transparent_tiles_on_404);

            //background_layer_color
            int R = Settings.background_layer_color_R;
            int G = Settings.background_layer_color_G;
            int B = Settings.background_layer_color_B;
            string hex = (((byte)R << 16) | ((byte)G << 8) | ((byte)B << 0)).ToString("X");
            background_layer_color.Text = "#" + hex;

            //background_layer_opacity
            background_layer_opacity.Text = (Settings.background_layer_opacity * 100).ToString() + "%";

            //NO_PIN_starting_location_latitude
            NO_PIN_starting_location_latitude.Text = Settings.NO_PIN_starting_location_latitude.ToString();

            //NO_PIN_starting_location_longitude
            NO_PIN_starting_location_longitude.Text = Settings.NO_PIN_starting_location_longitude.ToString();

            //NO_PIN_starting_location_latitude
            SE_PIN_starting_location_latitude.Text = Settings.SE_PIN_starting_location_latitude.ToString();

            //NO_PIN_starting_location_latitude
            SE_PIN_starting_location_longitude.Text = Settings.SE_PIN_starting_location_longitude.ToString();

            //visibility_pins
            visibility_pins.IsChecked = Settings.visibility_pins == Visibility.Visible;

            //map_defaut_zoom_level
            map_defaut_zoom_level.Text = Settings.map_defaut_zoom_level.ToString();

            //maps_margin_ZoomToBounds
            maps_margin_ZoomToBounds.Text = Settings.maps_margin_ZoomToBounds.ToString();

            //zoom_limite_taille_carte
            zoom_limite_taille_carte.IsChecked = Settings.zoom_limite_taille_carte;

            //map_show_tile_border
            map_show_tile_border.IsChecked = Settings.map_show_tile_border;

            //database_pathname
            database_pathname.Text = Settings.database_pathname;

            //database_pathname
            selection_rectangle_resize_tblr_gap.Text = Settings.selection_rectangle_resize_tblr_gap.ToString();

            //database_pathname
            selection_rectangle_resize_angle_gap.Text = Settings.selection_rectangle_resize_angle_gap.ToString();

            //is_in_debug_mode
            is_in_debug_mode.IsChecked = Settings.is_in_debug_mode;

            //show_layer_devtool
            show_layer_devtool.IsChecked = Settings.show_layer_devtool;

            //show_download_devtool
            show_download_devtool.IsChecked = Settings.show_download_devtool;

            DefaultValuesHachCode = Commun.Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
        }


        private async void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var dialog = Message.SetContentDialog("Voullez-vous vraiment restaurer les paramètres par défaut ? Cette action est irréversible et entrainera un redémarrage de l'application!", "Confirmer", MessageDialogButton.YesCancel);
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    if (System.IO.File.Exists(Settings.SettingsPath()))
                    {
                        System.IO.File.Delete(Settings.SettingsPath());
                    }
                }
                catch (Exception ex)
                {
                    dialog = Message.SetContentDialog("Echec de la suppression du fichier de paramètres. Erreur : " + ex.Message, "Erreur", MessageDialogButton.OK);
                    await dialog.ShowAsync();
                    return;
                }
                DefaultValuesHachCode = Commun.Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
                dialog = Message.SetContentDialog("MapsInMyFolder nécessite de redémarrer...\n Fermeture de l'application.", "Information", MessageDialogButton.OK);
                await dialog.ShowAsync();

                Collectif.RestartApplication();
            }
        }


        public void SaveSettings()
        {
            //layer_startup_id = ;
            NameHiddenIdValue layer_startup_idSelectedItem = (NameHiddenIdValue)layer_startup_id.SelectedItem;
            Settings.layer_startup_id = layer_startup_idSelectedItem.Id;

            //background_layer_opacity = ;
            Settings.background_layer_opacity = Convert.ToDouble(background_layer_opacity.Text.Replace("%", "").Trim()) / 100;

            //background_layer_color_R = ;
            //background_layer_color_G = ;
            //background_layer_color_B =;
            string hexvalue = background_layer_color.Text;
            hexvalue = hexvalue.Replace("#", "");
            int RGBint = Convert.ToInt32(hexvalue, 16);
            byte Red = (byte)((RGBint >> 16) & 255);
            byte Green = (byte)((RGBint >> 8) & 255);
            byte Blue = (byte)(RGBint & 255);

            Settings.background_layer_color_R = Red;
            Settings.background_layer_color_G = Green;
            Settings.background_layer_color_B = Blue;

            //working_folder = ;
            Settings.working_folder = working_folder.Text;

            //temp_folder = ;
            Settings.temp_folder = temp_folder.Text;

            //max_retry_download = ;
            Settings.max_retry_download = Convert.ToInt32(max_retry_download.Text);

            //max_redirection_download_tile = ;
            Settings.max_redirection_download_tile = Convert.ToInt32(max_redirection_download_tile.Text);

            //tiles_cache_expire_after_x_days = ;
            Settings.tiles_cache_expire_after_x_days = Convert.ToInt32(tiles_cache_expire_after_x_days.Text);

            //max_download_project_in_parralele = ;
            Settings.max_download_project_in_parralele = Convert.ToInt32(max_download_project_in_parralele.Text);

            //max_download_tiles_in_parralele = ;
            Settings.max_download_tiles_in_parralele = Convert.ToInt32(max_download_tiles_in_parralele.Text);

            //waiting_before_start_another_tile_download = ;
            Settings.waiting_before_start_another_tile_download = Convert.ToInt32(waiting_before_start_another_tile_download.Text);

            //generate_transparent_tiles_on_404 = ;
            Settings.generate_transparent_tiles_on_404 = generate_transparent_tiles_on_404.IsChecked ?? false;

            //generate_transparent_tiles_on_error = ;
            Settings.generate_transparent_tiles_on_error = generate_transparent_tiles_on_error.IsChecked ?? false;

            //is_in_debug_mode = ;
            Settings.is_in_debug_mode = is_in_debug_mode.IsChecked ?? false;

            //show_layer_devtool = ;
            Settings.show_layer_devtool = show_layer_devtool.IsChecked ?? false;

            //show_download_devtool = ;
            Settings.show_download_devtool = show_download_devtool.IsChecked ?? false;

            //layerpanel_website_IsVisible = ;
            //layerpanel_livepreview = ;

            //layerpanel_put_non_letter_layername_at_the_end = ;
            Settings.layerpanel_put_non_letter_layername_at_the_end = layerpanel_put_non_letter_layername_at_the_end.IsChecked ?? false;

            //layerpanel_displaystyle = ;
            string layerpanel_displaystylevalue = layerpanel_displaystyle.SelectedValue.ToString().ToUpperInvariant();
            Settings.layerpanel_displaystyle = (ListDisplayType)Enum.Parse(typeof(ListDisplayType), layerpanel_displaystylevalue);

            //NO_PIN_starting_location_latitude = ;
            Settings.NO_PIN_starting_location_latitude = Convert.ToDouble(NO_PIN_starting_location_latitude.Text);

            //NO_PIN_starting_location_longitude = ;
            Settings.NO_PIN_starting_location_longitude = Convert.ToDouble(NO_PIN_starting_location_longitude.Text);

            //SE_PIN_starting_location_latitude = ;
            Settings.SE_PIN_starting_location_latitude = Convert.ToDouble(SE_PIN_starting_location_latitude.Text);

            //SE_PIN_starting_location_longitude = ;
            Settings.SE_PIN_starting_location_longitude = Convert.ToDouble(SE_PIN_starting_location_longitude.Text);

            //map_defaut_zoom_level = ;
            Settings.map_defaut_zoom_level = Convert.ToInt32(map_defaut_zoom_level.Text);

            //zoom_limite_taille_carte = ;
            Settings.zoom_limite_taille_carte = zoom_limite_taille_carte.IsChecked ?? false;

            //tileloader_default_script = ;
            Settings.tileloader_default_script = tileloader_default_script.Text;

            //user_agent = ;
            Settings.user_agent = user_agent.Text;

            //default_database_pathname = ;
            Settings.database_pathname = database_pathname.Text;

            //layers_Sort = ;
            string layers_Sortvalue = layersSort.SelectedValue.ToString().ToUpperInvariant();
            Settings.layers_Sort = (LayersSort)Enum.Parse(typeof(LayersSort), layers_Sortvalue);

            //Layers_Order = ;
            if (LayersOrder.IsChecked == true)
            {
                Settings.Layers_Order = Commun.LayersOrder.ASC;
            }
            else
            {
                Settings.Layers_Order = Commun.LayersOrder.DESC;
            }


            //visibility_pins = ;
            if (visibility_pins.IsChecked ?? false)
            {
                Settings.visibility_pins = Visibility.Visible;
            }
            else
            {
                Settings.visibility_pins = Visibility.Hidden;
            }

            //selection_rectangle_resize_tblr_gap = ;
            Settings.selection_rectangle_resize_tblr_gap = Convert.ToInt32(selection_rectangle_resize_tblr_gap.Text);

            //selection_rectangle_resize_angle_gap = ;
            Settings.selection_rectangle_resize_angle_gap = Convert.ToInt32(selection_rectangle_resize_angle_gap.Text);

            //maps_margin_ZoomToBounds = ;
            Settings.maps_margin_ZoomToBounds = Convert.ToInt32(maps_margin_ZoomToBounds.Text);

            //disable_selection_rectangle_moving = ;
            //map_show_tile_border = ;
            Settings.map_show_tile_border = map_show_tile_border.IsChecked ?? false;


            string actualSettingsPath = Settings.SettingsPath();
            string newSettingsPath = Settings.SettingsPath(true);
            if (actualSettingsPath != newSettingsPath)
            {
                if (System.IO.File.Exists(actualSettingsPath))
                {
                    System.IO.File.Copy(actualSettingsPath, newSettingsPath);
                }
            }

            //register here values to files
            Settings.SaveSettings();
        }

        private static void RefreshAllPanels()
        {
            MainWindow._instance.Init();
            MainWindow._instance.MainPage.MapLoad();
            MainWindow._instance.MainPage.Init_layer_panel();
            MainWindow._instance.MainPage.ReloadPage();
            MainWindow._instance.MainPage.SearchLayerStart();
            MainWindow._instance.MainPage.Init_download_panel();
            MainWindow._instance.MainPage.Set_current_layer(Commun.Settings.layer_startup_id);
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int ValuesHachCode = Commun.Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
            ContentDialogResult result = ContentDialogResult.None;
            if (DefaultValuesHachCode != ValuesHachCode)
            {
                e.Cancel = true;
                var dialog = Message.SetContentDialog("Voullez-vous enregistrer vos modifications ? Les modifications non enregistrée seront perdues.", "Confirmer", MessageDialogButton.YesNo);
                try
                {
                    result = await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
            if (result == ContentDialogResult.Primary)
            {
                //enregistrement
                SaveSettings();

                //Want to restart ?
                var dialog = Message.SetContentDialog("Un redémarrage de l'application est recommandé. Voullez-vous redemarrer MapsInMyFolder ?\nSans redémarrage, la redéfinition de certains parametres peut entrainer certaines instabilitées.", "Information", MessageDialogButton.YesNo);
                ContentDialogResult result2 = await dialog.ShowAsync();

                if (result2 == ContentDialogResult.Primary)
                {
                    Application.Current.Shutdown();
                }
                else if (result2 == ContentDialogResult.Secondary)
                {
                    //if user dont want to restart
                    DefaultValuesHachCode = ValuesHachCode;
                    RefreshAllPanels();
                    this.Close();
                    return;
                }
                else
                {
                    //if user ESCAPE
                    return;
                }

            }
            else if (result == ContentDialogResult.Secondary)
            {
                Debug.WriteLine(result.ToString());
                DefaultValuesHachCode = ValuesHachCode;
                this.Close();
            }
            else
            {
                //if user ESCAPE
                return;
            }


        }

        private void FilterDigitTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBox((TextBox)sender, FilterDigitTextBox_TextChanged);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.S && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                //CTRL + S
                SaveSettings();
                RefreshAllPanels();
                DefaultValuesHachCode = Commun.Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
            }
        }
    }
}
