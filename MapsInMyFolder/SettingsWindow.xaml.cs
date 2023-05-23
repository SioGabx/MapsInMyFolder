using ICSharpCode.AvalonEdit;
using MapsInMyFolder.Commun;
using Microsoft.Win32;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            TitleTextBox.Text = this.Title = "MapsInMyFolder - Settings";

            SetCaretBrush(tileloader_default_script);
            SetCaretBrush(tileloader_template_script);
            SetTextEditorPositionChangedHandler(tileloader_default_script, 100);
            SetTextEditorPositionChangedHandler(tileloader_template_script, 100);
            SetFixMouseWheelBehavior(tileloader_default_script);
            SetFixMouseWheelBehavior(tileloader_template_script);

            AddContextMenuItems(tileloader_default_script, "Indenter");
            AddContextMenuItems(tileloader_template_script, "Indenter");

            SetTextAreaOptions(tileloader_default_script, true, 4);
            SetTextAreaOptions(tileloader_template_script, true, 4);
        }

        private void SetCaretBrush(TextEditor textEditor)
        {
            textEditor.TextArea.Caret.CaretBrush = Collectif.HexValueToSolidColorBrush("#f18712");
        }

        private void SetTextEditorPositionChangedHandler(TextEditor textEditor, int scrollOffset)
        {
            textEditor.TextArea.Caret.PositionChanged += (_, _) => Collectif.TextEditorCursorPositionChanged(textEditor, SettingsGrid, SettingsScrollViewer, scrollOffset);
        }

        private void SetFixMouseWheelBehavior(TextEditor textEditor)
        {
            ScrollViewerHelper.SetFixMouseWheel(Collectif.GetDescendantByType(textEditor, typeof(ScrollViewer)) as ScrollViewer, true);
        }

        private void AddContextMenuItems(TextEditor textEditor, string header)
        {
            MenuItem indenterMenuItem = new MenuItem();
            indenterMenuItem.Header = header;
            indenterMenuItem.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE12F", Foreground = Collectif.HexValueToSolidColorBrush("#888989") };
            indenterMenuItem.Click += (sender, e) => Collectif.IndenterCode(sender, e, textEditor);
            textEditor.ContextMenu.Items.Add(indenterMenuItem);
        }

        private void SetTextAreaOptions(TextEditor textEditor, bool convertTabsToSpaces, int indentationSize)
        {
            textEditor.TextArea.Options.ConvertTabsToSpaces = convertTabsToSpaces;
            textEditor.TextArea.Options.IndentationSize = indentationSize;
        }

        int DefaultValuesHachCode;
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
                if (layer.class_id != -1 && string.Equals(layer.class_format, "JPEG", StringComparison.InvariantCultureIgnoreCase))
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
            var listDisplayTypes = (ListDisplayType[])Enum.GetValues(typeof(ListDisplayType));
            foreach (var displayType in listDisplayTypes)
            {
                layerpanel_displaystyle.Items.Add(displayType.ToString().ToLowerInvariant().UcFirst());
                if (displayType.ToString() == Settings.layerpanel_displaystyle.ToString())
                {
                    layerpanel_displaystyle.SelectedIndex = Array.IndexOf(listDisplayTypes, displayType);
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

            //search_engine
            SearchEngines[] SearchEngines = (SearchEngines[])Enum.GetValues(typeof(SearchEngines));
            foreach (var searchEngine in SearchEngines)
            {
                search_engine.Items.Add(searchEngine.ToString());
                if (searchEngine.ToString() == Settings.search_engine.ToString())
                {
                    search_engine.SelectedIndex = Array.IndexOf(SearchEngines, searchEngine);
                }
            }

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
            string lastUpdateCheckDateTimeTick = Collectif.FilterDigitOnly(XMLParser.Cache.Read("LastUpdateCheck"), null, false, false);
            string lastUpdateCheckDate = "/";

            if (!string.IsNullOrEmpty(lastUpdateCheckDateTimeTick))
            {
                long tick = Convert.ToInt64(lastUpdateCheckDateTimeTick);
                DateTime lastUpdateCheckDateTime = new DateTime(tick);
                lastUpdateCheckDate = lastUpdateCheckDateTime.ToString("dd MMMM yyyy à H:mm:ss", CultureInfo.InstalledUICulture);
            }

            Debug.WriteLine(lastUpdateCheckDateTimeTick);
            searchForUpdatesLastUpdateCheck.Content = lastUpdateCheckDate;
        }


        private async void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = Message.SetContentDialog("Voulez-vous vraiment restaurer les paramètres par défaut ? Cette action est irréversible et entraînera un redémarrage de l'application!", "Confirmer", MessageDialogButton.YesCancel);
            ContentDialogResult result = await confirmDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    string settingsPath = Settings.SettingsPath();

                    if (System.IO.File.Exists(settingsPath))
                    {
                        System.IO.File.Delete(settingsPath);
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = Message.SetContentDialog("Échec de la suppression du fichier de paramètres. Erreur : " + ex.Message, "Erreur", MessageDialogButton.OK);
                    await errorDialog.ShowAsync();
                    return;
                }

                DefaultValuesHachCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);

                var infoDialog = Message.SetContentDialog("MapsInMyFolder nécessite de redémarrer...\nFermeture de l'application.", "Information", MessageDialogButton.OK);
                await infoDialog.ShowAsync();

                Collectif.RestartApplication();
            }
        }

        public void SaveSettings()
        {
            NameHiddenIdValue layerStartupIdSelectedItem = (NameHiddenIdValue)layer_startup_id.SelectedItem;
            if (layerStartupIdSelectedItem != null)
            {
                Settings.layer_startup_id = layerStartupIdSelectedItem.Id;
            }

            Settings.background_layer_opacity = Convert.ToDouble(background_layer_opacity.Text.Replace("%", "").Trim()) / 100;

            string hexValue = background_layer_color.Text;
            hexValue = hexValue.Replace("#", "");
            int rgbInt = Convert.ToInt32(hexValue, 16);
            byte red = (byte)((rgbInt >> 16) & 255);
            byte green = (byte)((rgbInt >> 8) & 255);
            byte blue = (byte)(rgbInt & 255);

            Settings.background_layer_color_R = red;
            Settings.background_layer_color_G = green;
            Settings.background_layer_color_B = blue;

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

            string layerpanelDisplayStyleValue = layerpanel_displaystyle.SelectedValue.ToString().ToUpperInvariant();
            Settings.layerpanel_displaystyle = (ListDisplayType)Enum.Parse(typeof(ListDisplayType), layerpanelDisplayStyleValue);
            Settings.NO_PIN_starting_location_latitude = Convert.ToDouble(NO_PIN_starting_location_latitude.Text);
            Settings.NO_PIN_starting_location_longitude = Convert.ToDouble(NO_PIN_starting_location_longitude.Text);
            Settings.SE_PIN_starting_location_latitude = Convert.ToDouble(SE_PIN_starting_location_latitude.Text);
            Settings.SE_PIN_starting_location_longitude = Convert.ToDouble(SE_PIN_starting_location_longitude.Text);
            Settings.map_defaut_zoom_level = Convert.ToInt32(map_defaut_zoom_level.Text);
            Settings.zoom_limite_taille_carte = zoom_limite_taille_carte.IsChecked ?? false;

            string selectedSearchEngine = search_engine.SelectedValue.ToString();
            Settings.search_engine = (SearchEngines)Enum.Parse(typeof(SearchEngines), selectedSearchEngine);


            Settings.tileloader_default_script = tileloader_default_script.Text;
            Settings.tileloader_template_script = tileloader_template_script.Text;
            Settings.user_agent = user_agent.Text;
            Settings.database_pathname = database_pathname.Text;

            Settings.search_application_update_on_startup = search_application_update_on_startup.IsChecked ?? false;
            Settings.search_database_update_on_startup = search_database_update_on_startup.IsChecked ?? false;
            Settings.layers_Sort = string.Join(',', layersSort.SelectedValues());

            Settings.visibility_pins = (visibility_pins.IsChecked ?? false) ? Visibility.Visible : Visibility.Hidden;

            Settings.selection_rectangle_resize_tblr_gap = Convert.ToInt32(selection_rectangle_resize_tblr_gap.Text);
            Settings.selection_rectangle_resize_angle_gap = Convert.ToInt32(selection_rectangle_resize_angle_gap.Text);
            Settings.map_show_tile_border = map_show_tile_border.IsChecked ?? false;
            Settings.github_repository_url = github_repository_url.Text;
            Settings.github_database_name = github_database_name.Text;

            string actualSettingsPath = Settings.SettingsPath();
            string newSettingsPath = Settings.SettingsPath(true);
            if (actualSettingsPath != newSettingsPath && System.IO.File.Exists(actualSettingsPath))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(newSettingsPath));
                System.IO.File.Copy(actualSettingsPath, newSettingsPath);
            }

            Settings.SaveSettings();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            int valuesHashCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
            ContentDialogResult result = ContentDialogResult.None;

            if (DefaultValuesHachCode != valuesHashCode)
            {
                e.Cancel = true;
                var dialog = Message.SetContentDialog("Voulez-vous enregistrer vos modifications ? Les modifications non enregistrées seront perdues.", "Confirmer", MessageDialogButton.YesNo);

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
                // Enregistrement
                SaveSettings();

                // Redémarrer ?
                var dialog = Message.SetContentDialog("Un redémarrage de l'application est recommandé. Voulez-vous redémarrer MapsInMyFolder ?\nSans redémarrage, la redéfinition de certains paramètres peut entraîner certaines instabilités.", "Information", MessageDialogButton.YesNo);
                ContentDialogResult result2 = await dialog.ShowAsync();

                if (result2 == ContentDialogResult.Primary)
                {
                    Application.Current.Shutdown();
                }
                else if (result2 == ContentDialogResult.Secondary)
                {
                    // Si l'utilisateur ne souhaite pas redémarrer
                    DefaultValuesHachCode = valuesHashCode;
                    MainWindow.RefreshAllPanels();
                    this.Close();
                    return;
                }
                else
                {
                    // Si l'utilisateur appuie sur ÉCHAP
                    return;
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                DefaultValuesHachCode = valuesHashCode;
                this.Close();
            }
            else
            {
                // Si l'utilisateur appuie sur ÉCHAP
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
                // CTRL + S
                SaveSettings();
                MainWindow.RefreshAllPanels();
                DefaultValuesHachCode = Collectif.CheckIfInputValueHaveChange(SettingsScrollViewer);
            }
        }

        private async void searchForUpdates_Click(object sender, RoutedEventArgs e)
        {
            searchForUpdates.IsEnabled = false;
            searchForUpdatesLastUpdateCheck.Content = "Recherche en cours...";
            bool isNewerDatabaseVersionAvailableOnGithub = await Database.CheckIfNewerVersionAvailable();
            bool isNewerApplicationVersionAvailableOnGithub = await Update.CheckIfNewerVersionAvailableOnGithub();
            searchForUpdates.IsEnabled = true;
            UpdateLastUpdateSearch();

            if (isNewerApplicationVersionAvailableOnGithub)
            {
                Notification.ListOfNotificationsOnShow.ToList().ForEach(notification => notification.Remove());
                Update.StartUpdating();
            }
        }

        private void DisableRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void SettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            List<(StackPanel SettingsPanel, Grid MenuLabel)> ListOfSubMenu = new List<(StackPanel, Grid)>()
    {
        (CalqueSettings, MenuItem_Calque),
        (TelechargementSettings, MenuItem_Telechargement),
        (CarteSettings, MenuItem_Carte),
        (AvanceSettings, MenuItem_Avance),
        (UpdateSettings, MenuItem_Update),
        (AProposSettings, MenuItem_APropos)
    };

            const int Margin = 100;
            foreach ((StackPanel SettingsPanel, Grid MenuLabel) item in ListOfSubMenu)
            {
                var UIElementPosition = item.SettingsPanel.TranslatePoint(new Point(0, 0), SettingsScrollViewer).Y;

                if (UIElementPosition - Margin <= 0 && UIElementPosition > -(item.SettingsPanel.ActualHeight - Margin))
                    item.MenuLabel.Style = (Style)this.Resources["GridInViewNormalStyle"];
                else
                    item.MenuLabel.Style = (Style)this.Resources["GridSelectNormalStyle"];
            }
        }

        private void databaseExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog DatabasesaveFileDialog = new SaveFileDialog
            {
                Filter = "SQL database |*.db|Text|*.txt",
                DefaultExt = "db",
                FileName = "exported_database.db",
                CheckPathExists = true,
                AddExtension = true,
                RestoreDirectory = true,
                ValidateNames = true,
                Title = "Export de la base de donnée"
            };

            if (DatabasesaveFileDialog.ShowDialog() == true)
            {
                try
                {
                    Database.Export(DatabasesaveFileDialog.FileName);
                    Process.Start("explorer.exe", "/select,\"" + DatabasesaveFileDialog.FileName + "\"");
                }
                catch (Exception ex)
                {
                    Message.NoReturnBoxAsync("Une erreur s'est produite lors de l'export du fichier. " + ex.Message, "Erreur");
                }
            }
            else
            {
                Message.NoReturnBoxAsync("Une erreur inconnue s'est produite lors de l'export du fichier", "Erreur");
            }
        }

        private void openAvancedEditor_Click(object sender, RoutedEventArgs e)
        {
            DataGridEditor DataGridEditor = new DataGridEditor();
            DataGridEditor.Show();
        }

    }
}
