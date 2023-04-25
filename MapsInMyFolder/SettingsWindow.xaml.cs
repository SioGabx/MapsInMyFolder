using MapsInMyFolder.Commun;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MapsInMyFolder
{
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

            tileloader_default_script.TextArea.Caret.CaretBrush = Collectif.HexValueToSolidColorBrush("#f18712");//rgb(241 135 18)
            tileloader_template_script.TextArea.Caret.CaretBrush = Collectif.HexValueToSolidColorBrush("#f18712");//rgb(241 135 18)
            tileloader_default_script.TextArea.Caret.PositionChanged += (_, _) => Collectif.TextEditorCursorPositionChanged(tileloader_default_script, SettingsGrid, SettingsScrollViewer, 100); ;
            tileloader_template_script.TextArea.Caret.PositionChanged += (_, _) => Collectif.TextEditorCursorPositionChanged(tileloader_template_script, SettingsGrid, SettingsScrollViewer, 100); ;
            ScrollViewerHelper.SetFixMouseWheel(Collectif.GetDescendantByType(tileloader_default_script, typeof(ScrollViewer)) as ScrollViewer, true);
            ScrollViewerHelper.SetFixMouseWheel(Collectif.GetDescendantByType(tileloader_template_script, typeof(ScrollViewer)) as ScrollViewer, true);

            MenuItem IndentermenuItem_tileloader_default_script = new MenuItem();
            IndentermenuItem_tileloader_default_script.Header = "Indenter";
            IndentermenuItem_tileloader_default_script.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE12F", Foreground = Collectif.HexValueToSolidColorBrush("#888989") };
            IndentermenuItem_tileloader_default_script.Click += (sender, e) => Collectif.IndenterCode(sender, e, tileloader_default_script);
            tileloader_default_script.ContextMenu.Items.Add(IndentermenuItem_tileloader_default_script);

            MenuItem IndentermenuItem_tileloader_template_script = new MenuItem();
            IndentermenuItem_tileloader_template_script.Header = "Indenter";
            IndentermenuItem_tileloader_template_script.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE12F", Foreground = Collectif.HexValueToSolidColorBrush("#888989") };
            IndentermenuItem_tileloader_template_script.Click += (sender, e) => Collectif.IndenterCode(sender, e, tileloader_template_script);
            tileloader_template_script.ContextMenu.Items.Add(IndentermenuItem_tileloader_template_script);


            tileloader_default_script.TextArea.Options.ConvertTabsToSpaces = true;
            tileloader_default_script.TextArea.Options.IndentationSize = 4;
            tileloader_template_script.TextArea.Options.ConvertTabsToSpaces = true;
            tileloader_template_script.TextArea.Options.IndentationSize = 4;
        }

        int DefaultValuesHachCode = 0;
        private void Window_Initialized(object sender, EventArgs e) { }


        void DoIScrollToElement(UIElement element, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Released)
            {
                SettingsScrollViewer.ScrollToElement(element);
            }
        }

        private void ScrollMenuItem_Telechargement(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(TelechargementSettingsLabel, e);
        }

        private void ScrollMenuItem_Calque(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(CalqueSettingsLabel, e);
        }

        private void ScrollMenuItem_Carte(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(CarteSettingsLabel, e);
        }

        private void ScrollMenuItem_Avance(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(AvanceSettingsLabel, e);
        }

        private void ScrollMenuItem_Update(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(UpdateSettingsLabel, e);
        }

        private void ScrollMenuItem_APropos(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DoIScrollToElement(AProposSettingsLabel, e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitSettingsWindow();
        }

        void InitSettingsWindow()
        {
            foreach (Layers layer in Layers.GetLayersList())
            {
                if (layer.class_id == -1)
                {
                    continue;
                }
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
            layersSort.ItemSource = new List<string>()
            {
                "ID ASC",
                "ID DESC",
                "NOM ASC",
                "NOM DESC",
                "DESCRIPTION ASC",
                "DESCRIPTION DESC",
                "CATEGORIE ASC",
                "CATEGORIE DESC",
                "FORMAT ASC",
                "FORMAT DESC",
                "SITE ASC",
                "SITE DESC",
                "PAYS ASC",
                "PAYS DESC"
            };
            layersSort.SelectedItems = Settings.layers_Sort.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            //layerpanel_put_non_letter_layername_at_the_end
            layerpanel_put_non_letter_layername_at_the_end.IsChecked = Settings.layerpanel_put_non_letter_layername_at_the_end;

            //layerpanel_favorite_at_top
            layerpanel_favorite_at_top.IsChecked = Settings.layerpanel_favorite_at_top;

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

            tileloader_default_script.Text = Settings.tileloader_default_script;
            tileloader_template_script.Text = Settings.tileloader_template_script;
            working_folder.Text = Settings.working_folder;
            temp_folder.Text = Settings.temp_folder;
            max_retry_download.Text = Settings.max_retry_download.ToString();
            max_redirection_download_tile.Text = Settings.max_redirection_download_tile.ToString();
            tiles_cache_expire_after_x_days.Text = Settings.tiles_cache_expire_after_x_days.ToString();
            http_client_timeout_in_seconds.Text = Settings.http_client_timeout_in_seconds.ToString();
            max_download_project_in_parralele.Text = Settings.max_download_project_in_parralele.ToString();
            max_download_tiles_in_parralele.Text = Settings.max_download_tiles_in_parralele.ToString();
            waiting_before_start_another_tile_download.Text = Settings.waiting_before_start_another_tile_download.ToString();
            user_agent.Text = Settings.user_agent;
            generate_transparent_tiles_on_error.IsChecked = Settings.generate_transparent_tiles_on_error;
            generate_transparent_tiles_on_404.IsChecked = Settings.generate_transparent_tiles_on_404;
            generate_transparent_tiles_never.IsChecked = (!Settings.generate_transparent_tiles_on_error && !Settings.generate_transparent_tiles_on_404);
            int R = Settings.background_layer_color_R;
            int G = Settings.background_layer_color_G;
            int B = Settings.background_layer_color_B;
            string hex = (((byte)R << 16) | ((byte)G << 8) | ((byte)B << 0)).ToString("X");
            background_layer_color.Text = "#" + hex;
            background_layer_opacity.Text = (Settings.background_layer_opacity * 100).ToString() + "%";
            NO_PIN_starting_location_latitude.Text = Settings.NO_PIN_starting_location_latitude.ToString();
            NO_PIN_starting_location_longitude.Text = Settings.NO_PIN_starting_location_longitude.ToString();
            SE_PIN_starting_location_latitude.Text = Settings.SE_PIN_starting_location_latitude.ToString();
            SE_PIN_starting_location_longitude.Text = Settings.SE_PIN_starting_location_longitude.ToString();
            visibility_pins.IsChecked = Settings.visibility_pins == Visibility.Visible;
            map_defaut_zoom_level.Text = Settings.map_defaut_zoom_level.ToString();
            zoom_limite_taille_carte.IsChecked = Settings.zoom_limite_taille_carte;
            map_show_tile_border.IsChecked = Settings.map_show_tile_border;
            database_pathname.Text = Settings.database_pathname;
            selection_rectangle_resize_tblr_gap.Text = Settings.selection_rectangle_resize_tblr_gap.ToString();
            selection_rectangle_resize_angle_gap.Text = Settings.selection_rectangle_resize_angle_gap.ToString();
            github_repository_url.Text = Settings.github_repository_url;
            github_database_name.Text = Settings.github_database_name;
            is_in_debug_mode.IsChecked = Settings.is_in_debug_mode;
            show_layer_devtool.IsChecked = Settings.show_layer_devtool;
            show_download_devtool.IsChecked = Settings.show_download_devtool;
            nettoyer_cache_browser_au_demarrage.IsChecked = Settings.nettoyer_cache_browser_au_demarrage;
            nettoyer_cache_layers_au_demarrage.IsChecked = Settings.nettoyer_cache_layers_au_demarrage;
            search_application_update_on_startup.IsChecked = Settings.search_application_update_on_startup;
            search_database_update_on_startup.IsChecked = Settings.search_database_update_on_startup;
            PaysComboBox.ItemSource = Country.getList();
            PaysComboBox.SelectedItems = Country.getListFromEnglishName(Settings.filter_layers_based_on_country.Split(';', StringSplitOptions.RemoveEmptyEntries));

            DefaultValuesHachCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
            SettingsVersionInformation.Content = Update.GetActualProductVersionFormatedString();
            UpdateLastUpdateSearch();

            SettingsScrollViewer.ScrollToTop();
        }

        public void UpdateLastUpdateSearch()
        {
            string LastUpdateCheckDateTimeTick = Collectif.FilterDigitOnly(XMLParser.Cache.Read("LastUpdateCheck"), null, false, false);
            string LastUpdateCheckDate = "/";
            if (!string.IsNullOrEmpty(LastUpdateCheckDateTimeTick))
            {
                LastUpdateCheckDate = new DateTime(Convert.ToInt64(LastUpdateCheckDateTimeTick)).ToString("dd MMMM yyyy à H:mm:ss", CultureInfo.InstalledUICulture);
            }
            Debug.WriteLine(LastUpdateCheckDateTimeTick);
            searchForUpdatesLastUpdateCheck.Content = LastUpdateCheckDate;
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
                DefaultValuesHachCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
                dialog = Message.SetContentDialog("MapsInMyFolder nécessite de redémarrer...\n Fermeture de l'application.", "Information", MessageDialogButton.OK);
                await dialog.ShowAsync();

                Collectif.RestartApplication();
            }
        }
        public void SaveSettings()
        {
            //layer_startup_id = ;
            NameHiddenIdValue layer_startup_idSelectedItem = (NameHiddenIdValue)layer_startup_id.SelectedItem;
            if (layer_startup_idSelectedItem != null)
            {
                Settings.layer_startup_id = layer_startup_idSelectedItem.Id;
            }

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

            Settings.working_folder = working_folder.Text;
            Settings.temp_folder = temp_folder.Text;
            Settings.max_retry_download = Convert.ToInt32(max_retry_download.Text);
            Settings.max_redirection_download_tile = Convert.ToInt32(max_redirection_download_tile.Text);
            Settings.tiles_cache_expire_after_x_days = Convert.ToInt32(tiles_cache_expire_after_x_days.Text);
            Settings.http_client_timeout_in_seconds = Convert.ToInt32(http_client_timeout_in_seconds.Text);
            Settings.max_download_project_in_parralele = Convert.ToInt32(max_download_project_in_parralele.Text);
            Settings.max_download_tiles_in_parralele = Convert.ToInt32(max_download_tiles_in_parralele.Text);
            Settings.waiting_before_start_another_tile_download = Convert.ToInt32(waiting_before_start_another_tile_download.Text);
            Settings.generate_transparent_tiles_on_404 = generate_transparent_tiles_on_404.IsChecked ?? false;
            Settings.generate_transparent_tiles_on_error = generate_transparent_tiles_on_error.IsChecked ?? false;
            Settings.is_in_debug_mode = is_in_debug_mode.IsChecked ?? false;
            Settings.show_layer_devtool = show_layer_devtool.IsChecked ?? false;
            Settings.show_download_devtool = show_download_devtool.IsChecked ?? false;
            Settings.nettoyer_cache_browser_au_demarrage = nettoyer_cache_browser_au_demarrage.IsChecked ?? false;
            Settings.nettoyer_cache_layers_au_demarrage = nettoyer_cache_layers_au_demarrage.IsChecked ?? false;
            Settings.layerpanel_put_non_letter_layername_at_the_end = layerpanel_put_non_letter_layername_at_the_end.IsChecked ?? false;
            Settings.layerpanel_favorite_at_top = layerpanel_favorite_at_top.IsChecked ?? false;
            Settings.filter_layers_based_on_country = string.Join(';', PaysComboBox.SelectedValues("EnglishName"));

            string layerpanel_displaystylevalue = layerpanel_displaystyle.SelectedValue.ToString().ToUpperInvariant();
            Settings.layerpanel_displaystyle = (ListDisplayType)Enum.Parse(typeof(ListDisplayType), layerpanel_displaystylevalue);
            Settings.NO_PIN_starting_location_latitude = Convert.ToDouble(NO_PIN_starting_location_latitude.Text);
            Settings.NO_PIN_starting_location_longitude = Convert.ToDouble(NO_PIN_starting_location_longitude.Text);
            Settings.SE_PIN_starting_location_latitude = Convert.ToDouble(SE_PIN_starting_location_latitude.Text);
            Settings.SE_PIN_starting_location_longitude = Convert.ToDouble(SE_PIN_starting_location_longitude.Text);
            Settings.map_defaut_zoom_level = Convert.ToInt32(map_defaut_zoom_level.Text);
            Settings.zoom_limite_taille_carte = zoom_limite_taille_carte.IsChecked ?? false;
            Settings.tileloader_default_script = tileloader_default_script.Text;
            Settings.tileloader_template_script = tileloader_template_script.Text;
            Settings.user_agent = user_agent.Text;
            Settings.database_pathname = database_pathname.Text;

            Settings.search_application_update_on_startup = search_application_update_on_startup.IsChecked ?? false;
            Settings.search_database_update_on_startup = search_database_update_on_startup.IsChecked ?? false;
            Settings.layers_Sort = string.Join(',', layersSort.SelectedValues());

            if (visibility_pins.IsChecked ?? false)
            {
                Settings.visibility_pins = Visibility.Visible;
            }
            else
            {
                Settings.visibility_pins = Visibility.Hidden;
            }
            Settings.selection_rectangle_resize_tblr_gap = Convert.ToInt32(selection_rectangle_resize_tblr_gap.Text);
            Settings.selection_rectangle_resize_angle_gap = Convert.ToInt32(selection_rectangle_resize_angle_gap.Text);
            Settings.map_show_tile_border = map_show_tile_border.IsChecked ?? false;
            Settings.github_repository_url = github_repository_url.Text;
            Settings.github_database_name = github_database_name.Text;

            string actualSettingsPath = Settings.SettingsPath();
            string newSettingsPath = Settings.SettingsPath(true);
            if (actualSettingsPath != newSettingsPath)
            {
                if (System.IO.File.Exists(actualSettingsPath))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newSettingsPath));
                    System.IO.File.Copy(actualSettingsPath, newSettingsPath);
                }
            }

            Settings.SaveSettings();
        }
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int ValuesHachCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
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
                    MainWindow.RefreshAllPanels();
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
            Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue((TextBox)sender, -1);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.S && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                //CTRL + S
                SaveSettings();
                MainWindow.RefreshAllPanels();
                DefaultValuesHachCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
            }
        }

        private async void searchForUpdates_Click(object sender, RoutedEventArgs e)
        {
            searchForUpdates.IsEnabled = false;
            searchForUpdatesLastUpdateCheck.Content = "Recherche en cours...";
            Debug.WriteLine("Nouvelle mise à jour disponible ? : " + await Commun.Update.CheckIfNewerVersionAvailableOnGithub());
            Database.CheckIfNewerVersionAvailable();
            searchForUpdates.IsEnabled = true;
            UpdateLastUpdateSearch();
        }

        private void DisableRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void SettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            List<(StackPanel, Grid)> ListOfSubMenu = new List<(StackPanel, Grid)>()
            {
                (CalqueSettings, MenuItem_Calque),
                (TelechargementSettings, MenuItem_Telechargement),
                (CarteSettings, MenuItem_Carte),
                (AvanceSettings, MenuItem_Avance),
                (UpdateSettings, MenuItem_Update),
                (AProposSettings, MenuItem_APropos)
            };
            ListOfSubMenu.Reverse();
            const int Margin = 100;
            foreach ((StackPanel SettingsPanel, Grid MenuLabel) item in ListOfSubMenu)
            {
                var UIElementPosition = item.SettingsPanel.TranslatePoint(new Point(0, 0), SettingsScrollViewer).Y;
                if (UIElementPosition - Margin <= 0 && UIElementPosition > -(item.SettingsPanel.ActualHeight - Margin))
                {
                    item.MenuLabel.Style = this.Resources["GridInViewNormalStyle"] as Style;
                }
                else
                {
                    item.MenuLabel.Style = this.Resources["GridSelectNormalStyle"] as Style;
                }
            }
        }

        private void databaseExport_Click(object sender, RoutedEventArgs e)
        {

            SaveFileDialog DatabasesaveFileDialog = new SaveFileDialog();
            DatabasesaveFileDialog.Filter = "SQL database |*.db|Text|*.txt";
            DatabasesaveFileDialog.DefaultExt = "db";
            DatabasesaveFileDialog.FileName = "exported_database.db";
            DatabasesaveFileDialog.CheckPathExists = true;
            DatabasesaveFileDialog.AddExtension = true;
            DatabasesaveFileDialog.RestoreDirectory = true;
            DatabasesaveFileDialog.ValidateNames = true;
            DatabasesaveFileDialog.Title = "Export de la base de donnée";
            if (DatabasesaveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Database.Export(DatabasesaveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    Message.NoReturnBoxAsync("Une erreur s'est produite lors de l'export du fichier. " + ex.Message, "Erreur");
                }

            }
            if (System.IO.File.Exists(DatabasesaveFileDialog.FileName))
            {
                Process.Start("explorer.exe", "/select,\"" + DatabasesaveFileDialog.FileName + "\"");
            }
            else
            {
                Message.NoReturnBoxAsync("Une erreur inconnue s'est produite lors de l'export du fichier", "Erreur");
            }

        }
    }
}
