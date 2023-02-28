﻿using MapsInMyFolder.MapControl;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MapsInMyFolder.Commun;
using System.Text.Json;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using System.IO;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour CustomOrEditLayersPage.xaml
    /// </summary>
    public partial class CustomOrEditLayersPage : System.Windows.Controls.Page
    {
        public CustomOrEditLayersPage()
        {
            InitializeComponent();
            LayerId = Settings.layer_startup_id;
        }

        public int LayerId { get; set; }
        private const int InternalEditorId = -2;
        private int DefaultValuesHachCode = 0;
        public void Init_CustomOrEditLayersWindow(int prefilLayerId)
        {
            if (prefilLayerId == -1)
            {
                prefilLayerId = LayerId;
            }
            Javascript.JavascriptInstance.Logs = String.Empty;
            TextboxLayerScriptConsole.Text = String.Empty;
            Javascript.ClearVar(-1);
            Javascript.ClearVar(-2);
            GenerateTempLayerInDicList();
            mapviewerappercu.Background = Collectif.RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B);
            mapviewerappercu.Center = MainPage._instance.mapviewer.Center;
            mapviewerappercu.ZoomLevel = MainPage._instance.mapviewer.ZoomLevel;

            Init_LayerEditableTextbox(prefilLayerId);
            SetAppercuLayers();

            if (Database.ExecuteScalarSQLCommand("SELECT COUNT(*) FROM 'main'.'EDITEDLAYERS' WHERE ID = " + LayerId) == 0)
            {
                ResetInfoLayerClikableLabel.IsEnabled = false;
                ResetInfoLayerClikableLabel.Opacity = 0.6;
            }

            //Javascript JavascriptLogInstance = Javascript.JavascriptInstance;
            Javascript.LogsChanged += (o, e) => SetTextboxLayerScriptConsoleText(e.Logs);
            var keyeventHandler = new KeyEventHandler(TextboxLayerScriptConsoleSender_KeyDown);
            TextboxLayerScriptConsoleSender.AddHandler(PreviewKeyDownEvent, keyeventHandler, handledEventsToo: true);


            TextboxLayerScript.TextArea.Caret.CaretBrush = Collectif.HexValueToSolidColorBrush("#f18712");//rgb(241 135 18)
            TextboxLayerScript.TextArea.Caret.PositionChanged += (_, _) => Collectif.TextEditorCursorPositionChanged(TextboxLayerScript, EditeurGrid, EditeurScrollBar, 75);
            ScrollViewerHelper.SetFixMouseWheel(Collectif.GetDescendantByType(TextboxLayerScript, typeof(ScrollViewer)) as ScrollViewer, true);
        }

      


        public void SetTextboxLayerScriptConsoleText(string text)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                TextboxLayerScriptConsole.Text = text;
                TextboxLayerScriptConsole.ScrollToEnd();
            }), DispatcherPriority.ContextIdle);
        }

        static void GenerateTempLayerInDicList()
        {
            Layers.RemoveLayerById(-2);
            Layers MoinsUnEditLayer = Layers.Empty();
            MoinsUnEditLayer.class_id = InternalEditorId;
            Dictionary<int, Layers> DicLayers = new Dictionary<int, Layers>
            {
                { -2, MoinsUnEditLayer }
            };
            Layers.Layers_Dictionary_List.Add(DicLayers);
        }

        void Init_LayerEditableTextbox(int prefilLayerId)
        {
            List<string> Categories = new List<string>();
            List<string> Site = new List<string>();
            List<string> SiteUrl = new List<string>();
            foreach (MapsInMyFolder.Commun.Layers layer in Layers.GetLayersList())
            {
                if (layer.class_id < 0)
                {
                    continue;
                }
                string class_categorie = layer.class_categorie.Trim();
                if (!Categories.Contains(class_categorie))
                {
                    Categories.Add(class_categorie);
                    TextboxLayerCategories.Items.Add(class_categorie);
                    //Debug.WriteLine("Adding " + class_categorie + " From " + layer.class_id);
                }
                string class_site = layer.class_site.Trim();
                if (!Site.Contains(class_site))
                {
                    Site.Add(class_site);
                    TextboxLayerSite.Items.Add(class_site);
                }
                string class_site_url = layer.class_site_url.Trim();
                if (!SiteUrl.Contains(class_site_url))
                {
                    SiteUrl.Add(class_site_url);
                    TextboxLayerSiteUrl.Items.Add(class_site_url);
                }
            }
            Categories.Clear();
            Site.Clear();
            SiteUrl.Clear();
            System.ComponentModel.ListSortDirection listSortDirection = System.ComponentModel.ListSortDirection.Ascending;
            if (Settings.Layers_Order == LayersOrder.DESC)
            {
                listSortDirection = System.ComponentModel.ListSortDirection.Descending;
            }


            TextboxLayerCategories.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", listSortDirection));
            TextboxLayerSite.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", listSortDirection));
            TextboxLayerSiteUrl.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", listSortDirection));
            Layers LayerInEditMode = Layers.GetLayerById(prefilLayerId);
            TextboxLayerCategories.IsEditable = true;
            TextboxLayerSite.IsEditable = true;
            TextboxLayerSiteUrl.IsEditable = true;
            if (LayerInEditMode is null)
            {
                TextboxLayerCategories.Text = "";
                TextboxLayerSite.Text = "";
                TextboxLayerSiteUrl.Text = "";
                return;
            }
            TextboxLayerName.Text = LayerInEditMode.class_name;
            if (LayerId > 0 && !string.IsNullOrEmpty(LayerInEditMode.class_name.Trim()))
            {
                CalqueType.Content = String.Concat("Calque - ", LayerInEditMode.class_name);
            }
            else if (LayerId != prefilLayerId)
            {
                CalqueType.Content = "Nouveau calque (basé sur le calque N°" + prefilLayerId + ")";
            }
            TextboxLayerCategories.Text = LayerInEditMode.class_categorie;
            TextboxLayerSiteUrl.Text = LayerInEditMode.class_site_url;
            TextboxLayerFormat.Text = LayerInEditMode.class_format.ToUpperInvariant();
            if (LayerInEditMode.class_format.EndsWith("jpeg") || LayerInEditMode.class_format.EndsWith("jpg"))
            {
                TextboxLayerFormat.SelectedIndex = 0;
            }
            else if (LayerInEditMode.class_format.EndsWith("png"))
            {
                TextboxLayerFormat.SelectedIndex = 1;
            }
            else if (LayerInEditMode.class_format.EndsWith("pbf"))
            {
                TextboxLayerFormat.SelectedIndex = 2;
            }
            else
            {
                TextboxLayerFormat.SelectedIndex = TextboxLayerFormat.Items.Add(LayerInEditMode.class_format.ToUpperInvariant());
            }
            DoShowSpecialOptionPBFJsonStyle();

            string tileSize = LayerInEditMode.class_tiles_size.ToString();
            if (string.IsNullOrEmpty(tileSize.Trim()))
            {
                tileSize = "256";
            }

            TextBoxSetValueAndLock(TextboxLayerIdentifiant, LayerInEditMode.class_identifiant);
            TextBoxSetValueAndLock(TextboxLayerDescriptif, LayerInEditMode.class_description);
            TextBoxSetValueAndLock(TextboxLayerScript, LayerInEditMode.class_tilecomputationscript);
            TextBoxSetValueAndLock(TextboxLayerName, LayerInEditMode.class_name);
            TextBoxSetValueAndLock(TextBoxLayerMinZoom, LayerInEditMode.class_min_zoom.ToString());
            TextBoxSetValueAndLock(TextBoxLayerMaxZoom, LayerInEditMode.class_max_zoom.ToString());
            TextBoxSetValueAndLock(TextboxLayerTileUrl, LayerInEditMode.class_tile_url);
            TextBoxSetValueAndLock(TextboxLayerTileWidth, tileSize);
            TextBoxSetValueAndLock(TextboxSpecialOptionBackgroundColor, LayerInEditMode.class_specialsoptions.BackgroundColor?.TrimEnd('#'));
            TextBoxSetValueAndLock(TextboxSpecialOptionPBFJsonStyle, LayerInEditMode.class_specialsoptions.PBFJsonStyle);
            DefaultValuesHachCode = Collectif.CheckIfInputValueHaveChange(EditeurStackPanel);

            Collectif.setBackgroundOnUIElement(mapviewerappercu, LayerInEditMode?.class_specialsoptions?.BackgroundColor);
            MenuItem IndentermenuItem = new MenuItem();
            IndentermenuItem.Header = "Indenter";
            IndentermenuItem.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE12F", Foreground = Collectif.HexValueToSolidColorBrush("#888989") };
            IndentermenuItem.Click += (sender, e) => IndenterCode(sender, e, TextboxLayerScript);
            TextboxLayerScript.ContextMenu.Items.Add(IndentermenuItem); 
            
            MenuItem templateMenuItem = new MenuItem();
            templateMenuItem.Header = "Script template";
            templateMenuItem.Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE15C", Foreground = Collectif.HexValueToSolidColorBrush("#888989") };
            templateMenuItem.Click += (sender, e) => putScriptTemplate(sender, e, TextboxLayerScript);
            TextboxLayerScript.ContextMenu.Items.Add(templateMenuItem);
                        
            TextboxLayerScript.TextArea.Options.ConvertTabsToSpaces = true;
            TextboxLayerScript.TextArea.Options.IndentationSize = 4;
        }

        void putScriptTemplate(object sender, EventArgs e, ICSharpCode.AvalonEdit.TextEditor textBox)
        {
            Collectif.InsertTextAtCaretPosition(textBox, Settings.tileloader_template_script);
            DoWeNeedToUpdateMoinsUnLayer();
        }

        void IndenterCode(object sender, EventArgs e, ICSharpCode.AvalonEdit.TextEditor textBox)
        {
            Collectif.IndenterCode(sender, e, textBox);
            DoWeNeedToUpdateMoinsUnLayer();
        }


        static void TextBoxSetValueAndLock(TextBox textBox, string value)
        {
            if (textBox is null) { return; }
            textBox.Text = value;
            Collectif.LockPreviousUndo(textBox);
        }

        static void TextBoxSetValueAndLock(ICSharpCode.AvalonEdit.TextEditor textBox, string value)
        {
            if (textBox is null) { return; }
            textBox.Text = value;
            //Collectif.LockPreviousUndo(textBox);
        }

        void SetAppercuLayers(string url = "")
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    if (!string.IsNullOrEmpty(TextboxLayerTileUrl.Text))
                    {
                        url = TextboxLayerTileUrl.Text;
                    }
                }
                if (string.IsNullOrEmpty(TextBoxLayerMinZoom.Text))
                {
                    mapviewerappercu.MinZoomLevel = 0;
                }
                else
                {
                    int MinZoomLevel = Convert.ToInt32(TextBoxLayerMinZoom.Text);
                    if (MinZoomLevel < 0)
                    {
                        MinZoomLevel = 0;
                    }
                    else if (MinZoomLevel > 22)
                    {
                        MinZoomLevel = 22;
                    }
                    mapviewerappercu.MinZoomLevel = MinZoomLevel;
                }
                if (string.IsNullOrEmpty(TextBoxLayerMaxZoom.Text))
                {
                    mapviewerappercu.MaxZoomLevel = 20;
                }
                else
                {
                    int MaxZoomLevel = Convert.ToInt32(TextBoxLayerMaxZoom.Text);
                    if (MaxZoomLevel < 1)
                    {
                        MaxZoomLevel = 1;
                    }
                    else if (MaxZoomLevel > 25)
                    {
                        MaxZoomLevel = 25;
                    }
                    mapviewerappercu.MaxZoomLevel = MaxZoomLevel;
                }
                if (!string.IsNullOrEmpty(url))
                {
                    UpdateMoinsUnLayer();
                    MapTileLayer_Transparent.TileSource = new TileSource { UriFormat = url, LayerID = InternalEditorId };
                }
                UIElement basemap;
                try
                {
                    if (BackgroundSwitch.IsOn)
                    {
                        Layers Layer = Layers.GetLayerById(Settings.layer_startup_id) ?? Layers.Empty();
                        basemap = new MapTileLayer
                        {
                            TileSource = new TileSource { UriFormat = Layer.class_tile_url, LayerID = Layer.class_id },
                            SourceName = Layer.class_identifiant,
                            MaxZoomLevel = Layer.class_max_zoom,
                            MinZoomLevel = Layer.class_min_zoom,
                            Description = "© [OpenStreetMap contributors](http://www.openstreetmap.org/copyright)"
                        };
                    }
                    else
                    {
                        basemap = new MapTileLayer();
                    }
                }
                catch (Exception ex)
                {
                    basemap = new MapTileLayer();
                    DebugMode.WriteLine(ex.Message);
                }
                basemap.Opacity = Settings.background_layer_opacity;
                mapviewerappercu.MapLayer = basemap;
            }
            catch (Exception ex)
            {
                DebugMode.WriteLine(ex.Message);
            }
        }

        private void TextboxLayerTileUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                SetAppercuLayers(TextboxLayerTileUrl.Text);
            }, null);
        }

        private void BackgroundSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SetAppercuLayers();
        }


        private void TextboxLayerTileWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBox(TextboxLayerTileWidth, TextboxLayerTileWidth_TextChanged, 4096);
        }

        private void TextBoxLayerMinZoom_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBoxLayerMinZoom, TextBoxLayerMinZoom_TextChanged, 4096))
            {
                SetAppercuLayers();
            }
        }

        private void TextBoxLayerMaxZoom_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBoxLayerMaxZoom, TextBoxLayerMaxZoom_TextChanged, 4096))
            {
                SetAppercuLayers();
            }
        }

        Boolean HasErrorZoomLevelMinZoom = false;
        Boolean HasErrorZoomLevelMaxZoom = false;
        void DisableButtonOnError()
        {
            if (!string.IsNullOrEmpty(TextBoxLayerMaxZoom.Text) && !string.IsNullOrEmpty(TextBoxLayerMinZoom.Text))
            {
                if (!HasErrorZoomLevelMinZoom && Convert.ToInt32(TextBoxLayerMaxZoom.Text) <= Convert.ToInt32(TextBoxLayerMinZoom.Text))
                {
                    //Le zoom minimum ne peux pas être supérieur ou égal au zoom maximum !;
                    Message.NoReturnBoxAsync("Le zoom minimum ne peux pas être supérieur ou égal au zoom maximum !", "Erreur");
                    HasErrorZoomLevelMinZoom = true;
                }
                else
                {
                    HasErrorZoomLevelMinZoom = false;
                }
                if (!HasErrorZoomLevelMinZoom && Convert.ToInt32(TextBoxLayerMinZoom.Text) >= Convert.ToInt32(TextBoxLayerMaxZoom.Text))
                {
                    //Le zoom maximum ne peux pas être inférieur ou égal au zoom minimum !
                    Message.NoReturnBoxAsync("Le zoom maximum ne peux pas être inférieur ou égal au zoom minimum !", "Erreur");
                    HasErrorZoomLevelMaxZoom = true;
                }
                else
                {
                    HasErrorZoomLevelMaxZoom = false;
                }
            }
            if (!HasErrorZoomLevelMaxZoom && !HasErrorZoomLevelMinZoom)
            {
                SaveEditButton.IsEnabled = true;
                SaveEditButton.Opacity = 1;
            }
            else
            {
                SaveEditButton.IsEnabled = false;
                SaveEditButton.Opacity = 0.5;
            }
        }

        static string GetComboBoxValue(ComboBox cb_element)
        {
            string return_value = string.Empty;
            try
            {
                object select_val = cb_element.SelectedValue;
                if (select_val is not null)
                {
                    return_value = select_val.ToString();
                }
            }
            catch (Exception)
            {
                return_value = string.Empty;
            }

            if (string.IsNullOrEmpty(return_value))
            {
                try
                {
                    return_value = cb_element.Text;
                }
                catch (Exception)
                {
                    return_value = string.Empty;
                }
            }
            if (string.IsNullOrEmpty(return_value))
            {
                return string.Empty;
            }
            else
            {
                return return_value;
            }
        }

        static int GetIntValueFromTextBox(TextBox textBox)
        {
            int number = 0;
            try
            {
                string text = textBox.Text;

                if (!string.IsNullOrEmpty(text))
                {
                    number = Convert.ToInt32(text);
                }
            }
            catch (Exception)
            {
                number = 0;
            }
            return number;
        }

        private void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            DisposeElementOnLeave();
            UpdateMoinsUnLayer();
            DebugMode.WriteLine("Saving...");

            Layers layers = Layers.GetLayerById(-2);
            if (layers is null)
            {
                Message.NoReturnBoxAsync("Une erreur s'est produite lors de l'enregistrement, veuillez réessayer.");
            }
            string NOM = Collectif.HTMLEntities(layers.class_name);
            string DESCRIPTION = Collectif.HTMLEntities(layers.class_description);
            string CATEGORIE = Collectif.HTMLEntities(layers.class_categorie);
            string IDENTIFIANT = Collectif.HTMLEntities(layers.class_identifiant);
            string TILE_URL = Collectif.HTMLEntities(layers.class_tile_url);
            int MIN_ZOOM = layers.class_min_zoom;
            int MAX_ZOOM = layers.class_max_zoom;
            string FORMAT = Collectif.HTMLEntities(layers.class_format);
            string SITE = Collectif.HTMLEntities(layers.class_site);
            string SITE_URL = Collectif.HTMLEntities(layers.class_site_url);
            int TILE_SIZE = layers.class_tiles_size;
            string TILECOMPUTATIONSCRIPT = Collectif.HTMLEntities(layers.class_tilecomputationscript);
            string SPECIALSOPTIONS = JsonSerializer.Serialize<Layers.SpecialsOptions>(layers.class_specialsoptions);
            string TILE_FALLBACK_URL = "";
            SQLiteConnection conn = Database.DB_Connection();

            string getSavingStringOptimalValue(string formValue, string layerValue)
            {
                formValue = formValue.Trim();
                layerValue = layerValue.Trim();
                if (formValue == layerValue || formValue == Collectif.HTMLEntities(layerValue))
                {
                    return null;
                }
                else
                {
                    return formValue;
                }
            }


            try
            {
                //check both : with HTMLEntities and not -> set a function
                //set int ? 

                Layers DB_Layer = MainPage.DB_Layer_Read(conn, $"SELECT * FROM LAYERS WHERE ID='{LayerId}'")[0];
                NOM = getSavingStringOptimalValue(NOM, DB_Layer.class_name);
                DESCRIPTION = getSavingStringOptimalValue(DESCRIPTION, DB_Layer.class_description);
                CATEGORIE = getSavingStringOptimalValue(CATEGORIE, DB_Layer.class_categorie);
                IDENTIFIANT = getSavingStringOptimalValue(IDENTIFIANT, DB_Layer.class_identifiant);
                TILE_URL = getSavingStringOptimalValue(TILE_URL, DB_Layer.class_tile_url);
                FORMAT = getSavingStringOptimalValue(FORMAT, DB_Layer.class_format);
                SITE = getSavingStringOptimalValue(SITE, DB_Layer.class_site);
                SITE_URL = getSavingStringOptimalValue(SITE_URL, DB_Layer.class_site_url);
                TILECOMPUTATIONSCRIPT = getSavingStringOptimalValue(TILECOMPUTATIONSCRIPT, DB_Layer.class_tilecomputationscript);
                SPECIALSOPTIONS = (SPECIALSOPTIONS == JsonSerializer.Serialize<Layers.SpecialsOptions>(DB_Layer.class_specialsoptions)) ? null : SPECIALSOPTIONS;
                TILE_FALLBACK_URL = getSavingStringOptimalValue(TILE_FALLBACK_URL, DB_Layer.class_tile_fallback_url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error comparaison des layers à partir du N°{LayerId} : {ex.Message}");
            }

            if (conn is null)
            {
                Debug.WriteLine("La connection à la base de donnée est null");
                return;
            }

            if (LayerId == -1)
            {
                Debug.WriteLine("Adding to CUSTOMSLAYERS");
                int RowCount = Database.ExecuteScalarSQLCommand("SELECT COUNT(*) FROM 'main'.'CUSTOMSLAYERS'");
                int ID = 1000000 + RowCount;
                Debug.WriteLine(RowCount);
                Database.ExecuteNonQuerySQLCommand("INSERT INTO 'main'.'CUSTOMSLAYERS'('ID','NOM', 'DESCRIPTION', 'CATEGORIE', 'IDENTIFIANT', 'TILE_URL', 'TILE_FALLBACK_URL', 'MIN_ZOOM', 'MAX_ZOOM', 'FORMAT', 'SITE', 'SITE_URL', 'TILE_SIZE', 'FAVORITE', 'TILECOMPUTATIONSCRIPT', 'VISIBILITY', 'SPECIALSOPTIONS', 'VERSION') " +
                $"VALUES('{ID}', '{NOM}', '{DESCRIPTION}', '{CATEGORIE}', '{IDENTIFIANT}', '{TILE_URL}', '{TILE_FALLBACK_URL}', '{MIN_ZOOM}', '{MAX_ZOOM}', '{FORMAT}', '{SITE}', '{SITE_URL}', '{TILE_SIZE}', '{0}' , '{TILECOMPUTATIONSCRIPT}',  '{Visibility.Visible.ToString()}',  '{SPECIALSOPTIONS}',  '{1}')");
            }
            else if (Database.ExecuteScalarSQLCommand("SELECT COUNT(*) FROM 'main'.'EDITEDLAYERS' WHERE ID = " + LayerId) == 0)
            {
                Debug.WriteLine("Adding to EDITEDLAYERS");
                int FAVORITE = Layers.GetLayerById(LayerId).class_favorite ? 1 : 0;
                Database.ExecuteNonQuerySQLCommand("INSERT INTO 'main'.'EDITEDLAYERS'('ID', 'NOM', 'DESCRIPTION', 'CATEGORIE', 'IDENTIFIANT', 'TILE_URL', 'TILE_FALLBACK_URL', 'MIN_ZOOM', 'MAX_ZOOM', 'FORMAT', 'SITE', 'SITE_URL', 'TILE_SIZE', 'FAVORITE', 'TILECOMPUTATIONSCRIPT', 'VISIBILITY', 'SPECIALSOPTIONS', 'VERSION') " +
                $"VALUES('{LayerId}', '{NOM}', '{DESCRIPTION}', '{CATEGORIE}', '{IDENTIFIANT}', '{TILE_URL}', '{TILE_FALLBACK_URL}', '{MIN_ZOOM}', '{MAX_ZOOM}', '{FORMAT}', '{SITE}', '{SITE_URL}', '{TILE_SIZE}', '{FAVORITE}',  '{TILECOMPUTATIONSCRIPT}',  '{Visibility.Visible.ToString()}',  '{SPECIALSOPTIONS}',  '{Layers.GetLayerById(LayerId).class_version}')");
            }
            else
            {
                Debug.WriteLine("Update to EDITEDLAYERS");
                int LastVersion = Database.ExecuteScalarSQLCommand("SELECT VERSION FROM 'main'.'LAYERS' WHERE ID=" + LayerId);
                Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'NOM'='{NOM}','DESCRIPTION'='{DESCRIPTION}','CATEGORIE'='{CATEGORIE}','IDENTIFIANT'='{IDENTIFIANT}','TILE_URL'='{TILE_URL}','TILE_FALLBACK_URL'='{TILE_FALLBACK_URL}','MIN_ZOOM'='{MIN_ZOOM}','MAX_ZOOM'='{MAX_ZOOM}','FORMAT'='{FORMAT}','SITE'='{SITE}','SITE_URL'='{SITE_URL}','TILE_SIZE'='{TILE_SIZE}','TILECOMPUTATIONSCRIPT'='{TILECOMPUTATIONSCRIPT}','VISIBILITY'='{Visibility.Visible.ToString()}','SPECIALSOPTIONS'='{SPECIALSOPTIONS}','VERSION'='{LastVersion}' WHERE ID = {LayerId}");
            }
            MainPage.ClearCache(LayerId, false);

            Javascript.EngineStopAll();
            Javascript.EngineClearList();
            MainWindow._instance.FrameBack();
            MainPage._instance.ReloadPage();

            if (Layers.Curent.class_id == LayerId)
            {
                MainPage._instance.Set_current_layer(LayerId);
            }
        }

        public void UpdateMoinsUnLayer()
        {
            Debug.WriteLine("UpdateMoinsUnLayer");
            Javascript.JavascriptInstance.Logs = String.Empty;
            Javascript.EngineClearList();
            string NOM = TextboxLayerName.Text.Trim();
            string DESCRIPTION = TextboxLayerDescriptif.Text.Trim();
            string CATEGORIE = GetComboBoxValue(TextboxLayerCategories);
            string IDENTIFIANT = TextboxLayerIdentifiant.Text.Trim();
            string TILE_URL = TextboxLayerTileUrl.Text.Trim();
            int MIN_ZOOM = GetIntValueFromTextBox(TextBoxLayerMinZoom);
            int MAX_ZOOM = GetIntValueFromTextBox(TextBoxLayerMaxZoom);
            string FORMAT = TextboxLayerFormat.Text.Trim().ToLowerInvariant();
            string SITE = TextboxLayerSite.Text.Trim();
            string SITE_URL = TextboxLayerSiteUrl.Text.Trim();
            int TILE_SIZE = GetIntValueFromTextBox(TextboxLayerTileWidth);
            string TILECOMPUTATIONSCRIPT = TextboxLayerScript.Text.Trim();
            Layers layers = Layers.GetLayerById(-2);
            if (layers is null)
            {
                GenerateTempLayerInDicList();
                layers = Layers.GetLayerById(-2);
            }
            Layers.RemoveLayerById(-2);
            layers.class_name = NOM;
            layers.class_description = DESCRIPTION;
            layers.class_categorie = CATEGORIE;
            layers.class_identifiant = IDENTIFIANT;
            layers.class_tile_url = TILE_URL;
            layers.class_min_zoom = MIN_ZOOM;
            layers.class_max_zoom = MAX_ZOOM;
            layers.class_format = FORMAT;
            layers.class_site = SITE;
            layers.class_site_url = SITE_URL;
            if (!string.IsNullOrEmpty(TILE_SIZE.ToString()))
            {
                layers.class_tiles_size = TILE_SIZE;
            }
            else
            {
                layers.class_tiles_size = 256;
            }
            layers.class_tilecomputationscript = TILECOMPUTATIONSCRIPT;
            layers.class_specialsoptions = new Layers.SpecialsOptions()
            {
                BackgroundColor = TextboxSpecialOptionBackgroundColor.Text,
                PBFJsonStyle = TextboxSpecialOptionPBFJsonStyle.Text,
            };
            Dictionary<int, Layers> DicLayers = new Dictionary<int, Layers>
            {
                { -2, layers }
            };
            Layers.Layers_Dictionary_List.Add(DicLayers);
        }

        async void AutoDetectZoom()
        {
            string label_base_content = ClickableLabel.Content.ToString();
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                if (string.IsNullOrEmpty(TextboxLayerTileUrl.Text))
                {
                    Message.NoReturnBoxAsync("Le champ TileUrl doit être remplis avant de lancer cette fonction", "Erreur");
                    return;
                }
                Location location = mapviewerappercu.Center;
                ClickableLabel.IsEnabled = false;
                bool IsSuccessLastRequest = false;
                int ZoomMinimum = -1;
                int ZoomMaximum = -1;

                UpdateMoinsUnLayer();
                for (int i = 0; i < 30; i++)
                {
                    string infotext = String.Concat("Analyse.... (niveau de zoom ", i.ToString(), ")");
                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        ClickableLabel.Content = infotext;
                    }, null);
                    List<int> TileNumber = Collectif.CoordonneesToTile(location.Latitude, location.Longitude, i);
                    Javascript.Print(infotext, -2);
                    string url = Collectif.Replacements(TextboxLayerTileUrl.Text, TileNumber[0].ToString(), TileNumber[1].ToString(), i.ToString(), InternalEditorId, Collectif.GetUrl.InvokeFunction.getTile);

                    Task search_zoom_level = Task.Run(() =>
                    {
                        int result = Collectif.CheckIfDownloadSuccess(url);
                        if (result == 200)
                        {
                            if (!IsSuccessLastRequest)
                            {
                                if (ZoomMinimum == -1)
                                {
                                    ZoomMinimum = i;
                                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                                    {
                                        Javascript.Print("Zoom minimum détecté (" + ZoomMinimum + ") !", -2);
                                        TextBoxLayerMinZoom.Text = ZoomMinimum.ToString();
                                    }, null);
                                }
                            }
                            IsSuccessLastRequest = true;
                        }
                        else
                        {
                            if (IsSuccessLastRequest)
                            {
                                if (ZoomMaximum == -1)
                                {
                                    ZoomMaximum = i - 1;
                                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                                    {
                                        Javascript.Print("Zoom maximal détecté (" + ZoomMaximum + ") !", -2);
                                        TextBoxLayerMaxZoom.Text = ZoomMaximum.ToString();
                                        ClickableLabel.Content = label_base_content;
                                        ClickableLabel.IsEnabled = true;
                                    }, null);
                                    i = 30;
                                    return;
                                }
                            }
                            IsSuccessLastRequest = false;
                        }
                    });
                    await search_zoom_level;
                }
                if (ZoomMaximum == -1)
                {
                    await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
                    {
                        ClickableLabel.Content = "Le zoom maximal n'as pas pu être trouvé, veuillez réessayer...";
                        ClickableLabel.IsEnabled = true;
                    }, null);
                }
            }, null);
        }

        void DisposeElementOnLeave()
        {
            TileGeneratorSettings.AcceptJavascriptPrint = false;
            Javascript.EngineStopAll();
            if (UpdateTimer is not null)
            {
                UpdateTimer.Elapsed -= UpdateTimerElapsed_StartUpdateMoinsUnLayer;
            }
            Javascript.LogsChanged -= (o, e) => SetTextboxLayerScriptConsoleText(e.Logs);
        }

        System.Timers.Timer UpdateTimer;
        void DoWeNeedToUpdateMoinsUnLayer()
        {
            UpdateTimer?.Dispose();
            UpdateTimer = new System.Timers.Timer(500);
            UpdateTimer.Elapsed += UpdateTimerElapsed_StartUpdateMoinsUnLayer;
            UpdateTimer.AutoReset = false;
            UpdateTimer.Enabled = true;

            Debug.WriteLine(UpdateTimer.Interval);
        }

        void UpdateTimerElapsed_StartUpdateMoinsUnLayer(object source, EventArgs e)
        {
            //TextboxLayerScriptConsoleSender.KeyDown += TextboxLayerScriptConsoleSender_KeyDown;
            try
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Javascript.EngineStopById(-2);
                    UpdateMoinsUnLayer();
                    SetAppercuLayers(TextboxLayerTileUrl.Text);
                }), DispatcherPriority.ContextIdle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        #region MouseAndKeyboardEventListener

        private void TextBoxLayerMinZoom_LostFocus(object sender, RoutedEventArgs e)
        {
            DisableButtonOnError();
        }

        private void TextBoxLayerMaxZoom_LostFocus(object sender, RoutedEventArgs e)
        {
            DisableButtonOnError();
        }

        private void TextboxLayerScriptConsoleSender_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter))
            {
                if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                {
                    string commande = TextboxLayerScriptConsoleSender.Text;
                    TextboxLayerScriptConsoleSender.Text = "";
                    Javascript.ExecuteCommand(commande, -2);
                    e.Handled = true;
                }
            }
        }

        private async void ClosePage_button_Click(object sender, RoutedEventArgs e)
        {
            int ValuesHachCode = Collectif.CheckIfInputValueHaveChange(EditeurStackPanel);
            ContentDialogResult result = ContentDialogResult.Primary;
            if (DefaultValuesHachCode != ValuesHachCode)
            {
                var dialog = Message.SetContentDialog("Voullez-vous vraiment quitter cette page ? Les modifications effectuée seront perdues", "Confirmer", MessageDialogButton.YesCancel);
                result = await dialog.ShowAsync();
            }
            if (result == ContentDialogResult.Primary)
            {
                DisposeElementOnLeave(); Javascript.EngineStopAll();
                Javascript.EngineClearList();
                MainWindow._instance.FrameBack();
            }
        }

        private void ClickableLabel_MouseEnter(object sender, MouseEventArgs e)
        {
            Label label_element = sender as Label;
            if (label_element.IsEnabled)
            {
                label_element.Cursor = Cursors.Hand;
                label_element.Foreground = Collectif.HexValueToSolidColorBrush("#b4b4b4");
            }
            else
            {
                label_element.Cursor = Cursors.Arrow;
            }
        }

        private void ClickableLabel_MouseLeave(object sender, MouseEventArgs e)
        {
            Label label_element = sender as Label;
            label_element.Foreground = Collectif.HexValueToSolidColorBrush("#888989");
        }

        private void ClickableLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ClickableLabel.Foreground = Collectif.HexValueToSolidColorBrush("#888989");
            AutoDetectZoom();
        }

        private async void ResetInfoLayerClikableLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var result = await Message.SetContentDialog("Voullez-vous vraiment redefinir par default les informations et paramêtres de ce calque ? \nCette action est irréversible !", "Confirmer", MessageDialogButton.YesCancel).ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Javascript.EngineStopAll();
                    Javascript.EngineClearList();
                    Database.ExecuteNonQuerySQLCommand("DELETE FROM EDITEDLAYERS WHERE ID=" + LayerId);
                    MainPage._instance.ReloadPage();
                    MainWindow._instance.FrameBack(true);
                    MainWindow._instance.FrameLoad_CustomOrEditLayers(LayerId);
                }
                result = ContentDialogResult.None;
            }
            catch (Exception ex)
            {
                DebugMode.WriteLine(ex.Message);
            }
        }

        private async void DeleteLayerClikableLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dialog = Message.SetContentDialog("Voullez-vous vraiment supprimer ce calque ? \nCette action est irréversible !", "Confirmer", MessageDialogButton.YesCancel);
                var result = await dialog.ShowAsync();
                dialog.Visibility = Visibility.Visible;
                if (result == ContentDialogResult.Primary)
                {
                    Javascript.EngineStopAll();
                    Javascript.EngineClearList();
                    Database.ExecuteNonQuerySQLCommand(@$"
                    DELETE FROM EDITEDLAYERS WHERE ID={LayerId};
                    DELETE FROM CUSTOMSLAYERS WHERE ID={LayerId};
                    DELETE FROM LAYERS WHERE ID={LayerId};
                    ");

                    if (Database.ExecuteScalarSQLCommand($"UPDATE EDITEDLAYERS SET 'VISIBILITY'='DELETED' WHERE ID={LayerId};") == 0)
                    {
                        Database.ExecuteScalarSQLCommand($"INSERT INTO EDITEDLAYERS ('ID', 'VISIBILITY') VALUES ({LayerId},'DELETED')");
                    }

                    MainWindow._instance.FrameBack();
                }
                result = ContentDialogResult.None;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            MainPage._instance.ReloadPage();
        }

        private void TextboxLayerScriptConsoleSender_KeyUp(object sender, KeyEventArgs e)
        {
            TextboxLayerScriptConsoleSender.AcceptsReturn = false;
        }

        private void TextboxLayerScript_KeyUp(object sender, KeyEventArgs e)
        {
            DoWeNeedToUpdateMoinsUnLayer();
        }

        private void TextboxSpecialOptionPBFJsonStyle_KeyUp(object sender, KeyEventArgs e)
        {
            DoWeNeedToUpdateMoinsUnLayer();
        }

        private void TextboxSpecialOptionBackgroundColor_KeyUp(object sender, KeyEventArgs e)
        {

            DoWeNeedToUpdateMoinsUnLayer();
            Collectif.setBackgroundOnUIElement(mapviewerappercu, TextboxSpecialOptionBackgroundColor.Text);
        }

        #endregion

        private void TextboxLayerFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DoShowSpecialOptionPBFJsonStyle();
        }

        void DoShowSpecialOptionPBFJsonStyle()
        {
            if (SpecialOptionPBFJsonStyle is null || !IsInitialized)
            {
                return;
            }
            ComboBoxItem comboBoxItem = TextboxLayerFormat.SelectedItem as ComboBoxItem;
            if (comboBoxItem is null)
            {
                TextboxLayerFormat.SelectedItem = TextboxLayerFormat.Items[0];
                return;
            }
            if (comboBoxItem.Content.ToString() == "PBF")
            {
                SpecialOptionPBFJsonStyle.Visibility = Visibility.Visible;
            }
            else
            {
                SpecialOptionPBFJsonStyle.Visibility = Visibility.Collapsed;
            }
        }

        private void TextboxSpecialOptionBackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBox((TextBox)sender, new List<char>() { 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', '#' });
        }

        private void DisableTextBoxRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {   
            bool DoHandle = (e.OriginalSource is TextBox);
            e.Handled = DoHandle;
        } 
        
        private void DisableAllRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {   
            e.Handled = true;
        }
    }
}
