using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using ModernWpf.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour CustomOrEditLayersPage.xaml
    /// </summary>
    public partial class CustomOrEditLayersPage : System.Windows.Controls.Page
    {
        private readonly bool ShowTileBorderArchive;
        private readonly bool IsInDebugModeArchive;
        public CustomOrEditLayersPage()
        {
            ShowTileBorderArchive = Settings.map_show_tile_border;
            IsInDebugModeArchive = Settings.is_in_debug_mode;
            Settings.map_show_tile_border = true;
            MapFigures = new MapFigures();
            InitializeComponent();
            IsInDebugModeSwitch.IsChecked = IsInDebugModeArchive;
            LayerId = Layers.StartupLayerId;
        }

        private static MapFigures MapFigures { get; set; }
        public int LayerId { get; set; }
        private static int InternalEditorId { get; set; } = -2;
        private int DefaultValuesHachCode = 0;
        public void Init_CustomOrEditLayersWindow(int prefilLayerId)
        {
            if (prefilLayerId == -1)
            {
                prefilLayerId = LayerId;
            }
            Javascript.instance.Logs = String.Empty;
            TextboxLayerScriptConsole.Text = String.Empty;
            Javascript.Functions.ClearVar(-1);
            Javascript.Functions.ClearVar(InternalEditorId);
            GenerateTempLayerInDicList();
            mapviewerappercu.Background = Collectif.RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B);
            mapviewerappercu.Center = MainPage._instance.mapviewer.Center;
            mapviewerappercu.ZoomLevel = MainPage._instance.mapviewer.ZoomLevel;
            TextBoxSetValueAndLock(TextboxLayerScript, Settings.tileloader_default_script);
            CountryComboBox.ItemSource = Country.GetList();

            SetContextMenu();


            Init_LayerEditableTextbox(prefilLayerId);
            SetAppercuLayers(forceUpdate: true);
            DoExpandStyleTextBox();

            if (Database.ExecuteScalarSQLCommand("SELECT COUNT(*) FROM 'main'.'EDITEDLAYERS' WHERE ID = " + LayerId) == 0)
            {
                ResetInfoLayerClikableLabel.IsEnabled = false;
                ResetInfoLayerClikableLabel.Opacity = 0.6;
            }
            //var keyeventHandler = new KeyEventHandler(TextboxLayerScriptConsoleSender_KeyDown);
            //TextboxLayerScriptConsoleSender.AddHandler(PreviewKeyDownEvent, keyeventHandler, handledEventsToo: true);
            TextboxLayerScriptConsoleSender.PreviewKeyDown += TextboxLayerScriptConsoleSender_KeyDown;
            TextboxRectangles.SetTextEditorPositionChangedHandler(EditeurGrid, EditeurScrollBar, 75);
            TextboxLayerScript.SetTextEditorPositionChangedHandler(EditeurGrid, EditeurScrollBar, 75);

            AutoUpdateLayer.IsChecked = Settings.editor_autoupdatelayer;
            DefaultValuesHachCode = Generic.CheckIfInputValueHaveChange(EditeurStackPanel);

            Javascript.LogsChanged += Javascript_LogsChanged;
            Javascript.JavascriptActionEvent += JavascriptActionEvent;
        }

        void SetContextMenu()
        {
            TextboxLayerScript.AddToContextMenu(Languages.Current["editorContextMenuIndent"], "\uE12F", (_, _) => TextboxLayerScript.Indent());

            MenuItem templateMenuItem = new MenuItem
            {
                Header = Languages.Current["editorContextMenuScriptTemplate"],
                Icon = new FontIcon() { Glyph = "\uE15C", Foreground = Collectif.HexValueToSolidColorBrush("#888989") }
            };
            Dictionary<string, string> myDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Settings.tileloader_template_script);
            foreach (var keyValue in myDictionary)
            {
                templateMenuItem.Items.Add(new MenuItem
                {
                    Header = keyValue.Key
                });
            }
            templateMenuItem.Click += templateMenuItem_Click;
            if (myDictionary.Count > 0)
            {
                TextboxLayerScript.ContextMenu.Items.Add(templateMenuItem);
            }
            void templateMenuItem_Click(object sender, RoutedEventArgs e)
            {
                var ClickedMenuItem = e.Source as MenuItem;
                PutScriptTemplate(TextboxLayerScript, ClickedMenuItem.Header?.ToString());
            }
            MenuItem clearConsoleMenuItem = new MenuItem
            {
                Header = Languages.Current["editorContextMenuConsoleErase"],
                Icon = new ModernWpf.Controls.FontIcon() { Glyph = "\uE127", Foreground = Collectif.HexValueToSolidColorBrush("#888989") }
            };
            clearConsoleMenuItem.Click += clearConsoleMenuItem_Click;
            TextboxLayerScriptConsole.ContextMenu.Items.Add(clearConsoleMenuItem);
            void clearConsoleMenuItem_Click(object sender, EventArgs e)
            {
                Javascript.Functions.PrintClear();
            }

            MenuItem helpConsoleMenuItem = new MenuItem
            {
                Header = Languages.Current["editorContextMenuConsoleHelp"],
                Icon = new FontIcon() { Glyph = "\uE11B", Foreground = Collectif.HexValueToSolidColorBrush("#888989") }
            };
            helpConsoleMenuItem.Click += helpConsoleMenuItem_Click;
            TextboxLayerScriptConsole.ContextMenu.Items.Add(helpConsoleMenuItem);
            void helpConsoleMenuItem_Click(object sender, EventArgs e)
            {
                Javascript.Functions.Help(-2);
                TextboxLayerScriptConsole.ScrollToEnd();
            }

            CustomOrEditLayers.Unloaded += CustomOrEditLayers_Unloaded;
            void CustomOrEditLayers_Unloaded(object sender, RoutedEventArgs e)
            {
                CustomOrEditLayers.Unloaded -= CustomOrEditLayers_Unloaded;
                helpConsoleMenuItem.Click -= helpConsoleMenuItem_Click;
                clearConsoleMenuItem.Click -= clearConsoleMenuItem_Click;
                templateMenuItem.Click -= templateMenuItem_Click;
                TextboxLayerScriptConsole.ContextMenu.Items.Remove(helpConsoleMenuItem);
                TextboxLayerScriptConsole.ContextMenu.Items.Remove(clearConsoleMenuItem);
                TextboxLayerScript.ContextMenu.Items.Remove(templateMenuItem);
            }
        }


        private void Javascript_LogsChanged(object sender, Javascript.LogsEventArgs e)
        {
            SetTextboxLayerScriptConsoleText(e.Logs);
        }

        public void JavascriptActionEvent(object sender, Javascript.JavascriptAction javascriptAction)
        {
            if (javascriptAction == Javascript.JavascriptAction.refreshMap)
            {
                SetAppercuLayers(forceUpdate: true);
            }
        }

        public void SetTextboxLayerScriptConsoleText(string text)
        {
            const int MaxLines = 750;
            string[] lines = text.Split('\n');
            if (lines.Length > MaxLines)
            {
                lines = lines.Skip(lines.Length - MaxLines).ToArray();
                text = string.Join("\n", lines);
            }
            Dispatcher.Invoke(new Action(() =>
            {
                TextboxLayerScriptConsole.Text = text;
                TextboxLayerScriptConsole.ScrollToEnd();
            }), DispatcherPriority.SystemIdle);
        }

        static void GenerateTempLayerInDicList()
        {
            Layers MoinsUnEditLayer = Layers.Empty();
            MoinsUnEditLayer.Id = InternalEditorId;
            Layers.Add(-2, MoinsUnEditLayer);
        }

        void Init_LayerEditableTextbox(int prefilLayerId)
        {
            InitComboboxAvailableValues();
            Layers LayerInEditMode = Layers.GetLayerById(prefilLayerId);
            if (LayerId > 0 && !string.IsNullOrEmpty(LayerInEditMode.Name.Trim()))
            {
                CalqueType.Content = string.Concat(Languages.Current["editorTitleLayer"], " - ", LayerInEditMode.Name);
            }
            else if (LayerId != prefilLayerId)
            {
                CalqueType.Content = Languages.GetWithArguments("editorTitleNewLayerBasedOn", LayerInEditMode.Id);
            }
            SetValuesFromLayers(LayerInEditMode);
        }

        public void SetValuesFromLayers(Layers LayerInEditMode)
        {
            TextboxLayerCategory.IsEditable = true;
            TextboxLayerSite.IsEditable = true;
            TextboxLayerSiteUrl.IsEditable = true;
            if (LayerInEditMode is null)
            {
                TextboxLayerCategory.Text = "";
                TextboxLayerSite.Text = "";
                TextboxLayerSiteUrl.Text = "";
                TextBoxSetValueAndLock(TextboxLayerTileWidth, "256");
                return;
            }
            TextboxLayerName.Text = LayerInEditMode.Name;

            TextboxLayerCategory.Text = LayerInEditMode.Tag;
            TextboxLayerSiteUrl.Text = LayerInEditMode.SiteUrl;
            TextboxLayerSite.Text = LayerInEditMode.SiteName;
            TextboxLayerFormat.Text = LayerInEditMode.TilesFormat.ToUpperInvariant();
            if (LayerInEditMode.TilesFormat.EndsWith("jpeg") || LayerInEditMode.TilesFormat.EndsWith("jpg"))
            {
                TextboxLayerFormat.SelectedIndex = 0;
            }
            else if (LayerInEditMode.TilesFormat.EndsWith("png"))
            {
                TextboxLayerFormat.SelectedIndex = 1;
            }
            else if (LayerInEditMode.TilesFormat.EndsWith("pbf"))
            {
                TextboxLayerFormat.SelectedIndex = 2;
            }
            else
            {
                TextboxLayerFormat.SelectedIndex = TextboxLayerFormat.Items.Add(LayerInEditMode.TilesFormat.ToUpperInvariant());
            }

            string tileSize = LayerInEditMode.TilesSize.ToString();
            if (string.IsNullOrEmpty(tileSize.Trim()))
            {
                tileSize = "256";
            }

            if (LayerId == LayerInEditMode.Id)
            {
                TextBoxSetValueAndLock(TextboxLayerIdentifier, LayerInEditMode.Identifier);
            }

            TextBoxSetValueAndLock(TextboxLayerDescription, LayerInEditMode.Description);
            TextBoxSetValueAndLock(TextboxLayerScript, LayerInEditMode.Script);
            TextBoxSetValueAndLock(TextboxRectangles, LayerInEditMode.BoundaryRectangles);
            TextBoxSetValueAndLock(TextboxLayerName, LayerInEditMode.Name);
            TextBoxSetValueAndLock(TextBoxLayerMinZoom, LayerInEditMode.MinZoom.ToString());
            TextBoxSetValueAndLock(TextBoxLayerMaxZoom, LayerInEditMode.MaxZoom.ToString());
            TextBoxSetValueAndLock(TextboxLayerTileUrl, LayerInEditMode.TileUrl);
            TextBoxSetValueAndLock(TextboxLayerTileWidth, tileSize);

            int MaxDownloadTilesInParralele = LayerInEditMode.SpecialsOptions.MaxDownloadTilesInParralele;
            int WaitingBeforeStartAnotherTile = LayerInEditMode.SpecialsOptions.WaitingBeforeStartAnotherTile;
            if (MaxDownloadTilesInParralele > 0)
            {
                TextBoxSetValueAndLock(TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele, MaxDownloadTilesInParralele.ToString());
            }
            if (WaitingBeforeStartAnotherTile > 0)
            {
                TextBoxSetValueAndLock(TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile, WaitingBeforeStartAnotherTile.ToString());
            }

            TextBoxSetValueAndLock(TextboxSpecialOptionBackgroundColor, LayerInEditMode.SpecialsOptions.BackgroundColor?.TrimEnd('#'));
            TextBoxSetValueAndLock(TextboxLayerStyle, LayerInEditMode.Style);

            string[] class_country = LayerInEditMode.Country.Split(';', StringSplitOptions.RemoveEmptyEntries);
            List<Country> SelectedCountries = Country.GetListFromEnglishName(class_country);
            if (SelectedCountries.Count != class_country.Length && LayerInEditMode.Country != null)
            {
                List<Country> CountryList = CountryComboBox.ItemSource.Cast<Country>().ToList();
                foreach (string country in class_country)
                {
                    if (!SelectedCountries.Contains(country))
                    {
                        Country newCountryToAdd = new Country
                        {
                            DisplayName = country,
                            EnglishName = country
                        };
                        CountryList.Add(newCountryToAdd);
                        SelectedCountries.Add(newCountryToAdd);
                    }
                }
                CountryComboBox.ItemSource = CountryList;
            }

            CountryComboBox.SelectedItems = SelectedCountries;

            string[] class_httpstatuscode = LayerInEditMode.SpecialsOptions?.ErrorsToIgnore?.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var SelectedHttpStatusCode = new List<StatusCode>();
            var listOfHttpStatusCode = new List<StatusCode>();
            foreach (HttpStatusCode status in StatusCode.getList())
            {
                var item = new StatusCode(status, $"{(int)status} - {status}");
                if (!listOfHttpStatusCode.Contains(item))
                {
                    listOfHttpStatusCode.Add(item);
                    if (class_httpstatuscode.Contains(((int)status).ToString()))
                    {
                        SelectedHttpStatusCode.Add(item);
                    }
                }
            }
            AlloweRequestErrorsComboBox.ItemSource = listOfHttpStatusCode;
            AlloweRequestErrorsComboBox.SelectedItems = SelectedHttpStatusCode;

            has_scale.IsChecked = LayerInEditMode.IsAtScale;
            Collectif.SetBackgroundOnUIElement(mapviewerappercu, LayerInEditMode?.SpecialsOptions?.BackgroundColor);
        }

        void InitComboboxAvailableValues()
        {
            List<string> Category = new List<string>();
            List<string> Site = new List<string>();
            List<string> SiteUrl = new List<string>();

            foreach (Layers layer in Layers.GetLayersList())
            {
                if (layer.Id < 0)
                {
                    continue;
                }
                string class_category = layer.Tag.Trim();
                if (!Category.Contains(class_category) && !string.IsNullOrWhiteSpace(class_category))
                {
                    Category.Add(class_category);
                    TextboxLayerCategory.Items.Add(class_category);
                }
                string class_site = layer.SiteName.Trim();
                if (!Site.Contains(class_site) && !string.IsNullOrWhiteSpace(class_site))
                {
                    Site.Add(class_site);
                    TextboxLayerSite.Items.Add(class_site);
                }
                string class_site_url = layer.SiteUrl.Trim();
                if (!SiteUrl.Contains(class_site_url) && !string.IsNullOrWhiteSpace(class_site_url))
                {
                    SiteUrl.Add(class_site_url);
                    TextboxLayerSiteUrl.Items.Add(class_site_url);
                }
            }
            Category.Clear();
            Site.Clear();
            SiteUrl.Clear();
            const System.ComponentModel.ListSortDirection listSortDirection = System.ComponentModel.ListSortDirection.Ascending;
            TextboxLayerCategory.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", listSortDirection));
            TextboxLayerSite.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", listSortDirection));
            TextboxLayerSiteUrl.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", listSortDirection));
        }


        void PutScriptTemplate(UserControls.ScriptEditor textBox, string scriptName)
        {
            Dictionary<string, string> myDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(Settings.tileloader_template_script);
            if (myDictionary.TryGetValue(scriptName, out string script))
            {
                textBox.InsertTextAtCaretPosition(script);
                DoWeNeedToUpdateMoinsUnLayer();
            }

        }

        void IndenterCode(UserControls.ScriptEditor textBox)
        {
            textBox.Indent();
            DoWeNeedToUpdateMoinsUnLayer();
        }

        static void TextBoxSetValueAndLock(TextBox textBox, string value)
        {
            if (textBox is null) { return; }
            textBox.Text = value;
            Collectif.LockPreviousUndo(textBox);
        }

        static void TextBoxSetValueAndLock(UserControls.ScriptEditor textBox, string value)
        {
            if (textBox is null) { return; }
            textBox.Script = value;
        }

        void SetAppercuLayers(string url = "", bool forceUpdate = false)
        {
            if (!(AutoUpdateLayer.IsChecked || forceUpdate))
            {
                return;
            }
            try
            {
                Javascript.instance.Logs = String.Empty;
                if (string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(TextboxLayerTileUrl.Text))
                {
                    url = TextboxLayerTileUrl.Text;
                }
                if (string.IsNullOrEmpty(TextBoxLayerMinZoom.Text))
                {
                    mapviewerappercu.MinZoomLevel = 0;
                    MapTileLayer_Transparent.MinZoomLevel = 0;
                }
                else
                {
                    int MinZoomLevel = System.Convert.ToInt32(TextBoxLayerMinZoom.Text);
                    if (MinZoomLevel < 0)
                    {
                        MinZoomLevel = 0;
                    }
                    else if (MinZoomLevel > 22)
                    {
                        MinZoomLevel = 22;
                    }
                    mapviewerappercu.MinZoomLevel = MinZoomLevel;
                    MapTileLayer_Transparent.MinZoomLevel = MinZoomLevel;
                }
                if (string.IsNullOrEmpty(TextBoxLayerMaxZoom.Text))
                {
                    mapviewerappercu.MaxZoomLevel = 20;
                    MapTileLayer_Transparent.MaxZoomLevel = 20;
                }
                else
                {
                    int MaxZoomLevel = System.Convert.ToInt32(TextBoxLayerMaxZoom.Text);
                    if (MaxZoomLevel < 1)
                    {
                        MaxZoomLevel = 1;
                    }
                    else if (MaxZoomLevel > 25)
                    {
                        MaxZoomLevel = 25;
                    }
                    mapviewerappercu.MaxZoomLevel = MaxZoomLevel;
                    MapTileLayer_Transparent.MaxZoomLevel = MaxZoomLevel;
                }
                if (!string.IsNullOrEmpty(url))
                {
                    UpdateMoinsUnLayer();
                    MapTileLayer_Transparent.TileSource = new TileSource { UriFormat = url, LayerID = InternalEditorId };

                }
                SetBackgroundMap(mapviewerappercu);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SetBackgroundMap(MapControl.Map map)
        {
            UIElement basemap;
            try
            {
                if (BackgroundSwitch?.IsChecked == true)
                {
                    Layers Layer = Layers.GetLayerById(Layers.StartupLayerId) ?? Layers.Empty();
                    basemap = new MapTileLayer
                    {
                        TileSource = new TileSource { UriFormat = Layer.TileUrl, LayerID = Layer.Id },
                        SourceName = Layer.Identifier,
                        MaxZoomLevel = Layer.MaxZoom ?? 0,
                        MinZoomLevel = Layer.MinZoom ?? 0,
                        Description = ""
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
                Debug.WriteLine(ex.Message);
            }
            basemap.Opacity = Settings.background_layer_opacity;
            map.MapLayer = basemap;
        }

        private void TextboxLayerTileUrl_TextChanged(object sender, TextChangedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                SetAppercuLayers(TextboxLayerTileUrl.Text);
            }, null);
        }

        private void TextboxLayerTileWidth_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(TextboxLayerTileWidth, 4096);
        }

        private void TextBoxLayerMinZoom_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(TextBoxLayerMinZoom, 4096))
            {
                SetAppercuLayers();
            }
        }

        private void TextBoxLayerMaxZoom_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(TextBoxLayerMaxZoom, 30))
            {
                SetAppercuLayers();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetAppercuLayers(forceUpdate: true);
        }

        bool HasErrorZoomLevelMinZoom = false;
        bool HasErrorZoomLevelMaxZoom = false;
        void DisableButtonOnError()
        {
            if (!string.IsNullOrEmpty(TextBoxLayerMaxZoom.Text) && !string.IsNullOrEmpty(TextBoxLayerMinZoom.Text))
            {
                if (!HasErrorZoomLevelMinZoom && Convert.ToInt32(TextBoxLayerMaxZoom.Text) <= Convert.ToInt32(TextBoxLayerMinZoom.Text))
                {
                    //Le zoom minimum ne peux pas être supérieur ou égal au zoom maximum !;
                    Message.NoReturnBoxAsync(Languages.Current["editorMessageErrorMinZoomGreaterThanMaxZoom"], Languages.Current["dialogTitleOperationFailed"]);
                    HasErrorZoomLevelMinZoom = true;
                }
                else
                {
                    HasErrorZoomLevelMinZoom = false;
                }
                if (!HasErrorZoomLevelMinZoom && Convert.ToInt32(TextBoxLayerMinZoom.Text) >= Convert.ToInt32(TextBoxLayerMaxZoom.Text))
                {
                    //Le zoom maximum ne peux pas être inférieur ou égal au zoom minimum !
                    Message.NoReturnBoxAsync(Languages.Current["editorMessageErrorMinZoomLessThanMaxZoom"], Languages.Current["dialogTitleOperationFailed"]);
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
                return "/";
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

        private async void ImportExportInfoLayerClikableLabel_Click(object sender, RoutedEventArgs e)
        {
            string UpdateSQLCommand = GetShareSQLPart();
            var InputBox = Message.SetInputBoxDialog("Pour importer ou exporter ce calque, copier ou collez la valeur dans le champs ci-dessous.", UpdateSQLCommand, "MapsInMyFolder", MessageDialogButton.OKCancel);
            InputBox.textBox.AcceptsReturn = true;
            InputBox.textBox.TextWrapping = TextWrapping.Wrap;
            InputBox.textBox.MinHeight = 80;
            var result = await InputBox.dialog.ShowAsync();
            if (InputBox.textBox.Text.Trim() != UpdateSQLCommand.Trim() && result == ContentDialogResult.Primary)
            {
                Debug.WriteLine("Executing");
                //LayerId = SaveLayer();
                try
                {
                    using (SQLiteConnection conn = Database.DB_MemoryConnection())
                    {
                        Database.ExecuteNonQuerySQLCommand(conn, "INSERT INTO LAYERS DEFAULT VALUES");
                        Database.ExecuteNonQuerySQLCommand(conn, $"UPDATE LAYERS {InputBox.textBox.Text} WHERE ID = 1");
                        SQLiteDataReader reader = Database.ExecuteExecuteReaderSQLCommand(conn, "SELECT * FROM LAYERS WHERE ID='1'");
                        reader.Read();
                        var calque = Layers.GetLayerFromSQLiteDataReader(reader);
                        SetValuesFromLayers(calque);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private string GetShareSQLPart()
        {
            try
            {
                UpdateMoinsUnLayer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return string.Empty;
            }
            Layers layers = Layers.GetLayerById(-2);
            if (layers is null)
            {
                return string.Empty;
            }
            var LayerSaveValues = Layers.GetValuesForSaving(layers);
            string ShareStringKeyValues = string.Empty;
            foreach (KeyValuePair<string, string> KeyPair in LayerSaveValues)
            {
                ShareStringKeyValues += $"'{KeyPair.Key}'='{KeyPair.Value}',";
            }

            return ($"SET {ShareStringKeyValues.Trim(',')}");
        }

        private int SaveLayer()
        {
            try
            {
                UpdateMoinsUnLayer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Message.NoReturnBoxAsync(ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                return -1;
            }
            Layers layers = Layers.GetLayerById(-2);
            if (layers is null)
            {
                Message.NoReturnBoxAsync(Languages.Current["editorMessageErrorDatabaseSave"], Languages.Current["dialogTitleOperationFailed"]);
                return -1;
            }

            var LayerSaveValues = Layers.GetValuesForSaving(layers);
            string NAME = LayerSaveValues["NAME"];
            string DESCRIPTION = LayerSaveValues["DESCRIPTION"];
            string CATEGORY = LayerSaveValues["CATEGORY"];
            string COUNTRY = LayerSaveValues["COUNTRY"];
            string IDENTIFIER = LayerSaveValues["IDENTIFIER"];
            string TILE_URL = LayerSaveValues["TILE_URL"];
            string MIN_ZOOM = LayerSaveValues["MIN_ZOOM"];
            string MAX_ZOOM = LayerSaveValues["MAX_ZOOM"];
            string FORMAT = LayerSaveValues["FORMAT"];
            string SITE = LayerSaveValues["SITE"];
            string SITE_URL = LayerSaveValues["SITE_URL"];
            string STYLE = LayerSaveValues["STYLE"];
            string TILE_SIZE = LayerSaveValues["TILE_SIZE"];
            string SCRIPT = LayerSaveValues["SCRIPT"];
            string RECTANGLES = LayerSaveValues["RECTANGLES"];
            string SPECIALSOPTIONS = LayerSaveValues["SPECIALSOPTIONS"];
            string HAS_SCALE = LayerSaveValues["HAS_SCALE"];

            string getSavingOptimalValueWithNULL(string formValue, object layerValue)
            {
                if (LayerId <= 0)
                {
                    return $"'{formValue}'";
                }
                return Database.GetSavingOptimalValueWithNULL(formValue, layerValue?.ToString());
            }


            try
            {
                Layers DB_Layer = Layers.Empty();

                List<Layers> LayersRead = Layers.LayerReadInDatabase($"SELECT * FROM LAYERS WHERE ID='{LayerId}'");
                if (LayersRead.Count > 0)
                {
                    DB_Layer = LayersRead[0];
                }
                else
                {
                    List<Layers> CustomLayersRead = Layers.LayerReadInDatabase($"SELECT * FROM CUSTOMSLAYERS WHERE ID='{LayerId}'");
                    if (CustomLayersRead.Count > 0)
                    {
                        DB_Layer = CustomLayersRead[0];
                    }
                }

                NAME = getSavingOptimalValueWithNULL(NAME, DB_Layer.Name);
                DESCRIPTION = getSavingOptimalValueWithNULL(DESCRIPTION, DB_Layer.Description);
                CATEGORY = getSavingOptimalValueWithNULL(CATEGORY, DB_Layer.Tag);
                COUNTRY = getSavingOptimalValueWithNULL(COUNTRY, DB_Layer.Country);
                IDENTIFIER = getSavingOptimalValueWithNULL(IDENTIFIER, DB_Layer.Identifier);
                MIN_ZOOM = getSavingOptimalValueWithNULL(MIN_ZOOM, DB_Layer.MinZoom);
                MAX_ZOOM = getSavingOptimalValueWithNULL(MAX_ZOOM, DB_Layer.MaxZoom);
                TILE_URL = getSavingOptimalValueWithNULL(TILE_URL, DB_Layer.TileUrl);
                FORMAT = getSavingOptimalValueWithNULL(FORMAT, DB_Layer.TilesFormat);
                SITE = getSavingOptimalValueWithNULL(SITE, DB_Layer.SiteName);
                SITE_URL = getSavingOptimalValueWithNULL(SITE_URL, DB_Layer.SiteUrl);
                SCRIPT = getSavingOptimalValueWithNULL(SCRIPT, DB_Layer.Script);
                STYLE = getSavingOptimalValueWithNULL(STYLE, DB_Layer.Style);
                TILE_SIZE = getSavingOptimalValueWithNULL(TILE_SIZE, DB_Layer.TilesSize);
                RECTANGLES = getSavingOptimalValueWithNULL(RECTANGLES, DB_Layer.BoundaryRectangles);
                SPECIALSOPTIONS = getSavingOptimalValueWithNULL(SPECIALSOPTIONS, DB_Layer.SpecialsOptions);
                HAS_SCALE = getSavingOptimalValueWithNULL(HAS_SCALE, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error comparaison des layers à partir du N°{LayerId} : {ex.Message}");

            }
            if (LayerId == -1)
            {
                int CustomLayersMaxID = Database.ExecuteScalarSQLCommand("SELECT MAX(ID) FROM 'main'.'CUSTOMSLAYERS'");
                int EditedLayersMaxID = Database.ExecuteScalarSQLCommand("SELECT MAX(ID) FROM 'main'.'EDITEDLAYERS'");
                int ID = Math.Max(1000000, Math.Max(CustomLayersMaxID, EditedLayersMaxID)) + 1;
                Database.ExecuteNonQuerySQLCommand("INSERT INTO 'main'.'CUSTOMSLAYERS'('ID','NAME', 'DESCRIPTION', 'CATEGORY', 'COUNTRY', 'IDENTIFIER', 'TILE_URL', 'MIN_ZOOM', 'MAX_ZOOM', 'FORMAT', 'SITE', 'SITE_URL', 'STYLE', 'TILE_SIZE', 'FAVORITE', 'SCRIPT', 'VISIBILITY', 'SPECIALSOPTIONS', 'RECTANGLES', 'VERSION', 'HAS_SCALE') " +
                $"VALUES({ID}, {NAME}, {DESCRIPTION}, {CATEGORY},{COUNTRY}, {IDENTIFIER}, {TILE_URL}, {MIN_ZOOM}, {MAX_ZOOM}, {FORMAT}, {SITE}, {SITE_URL}, {STYLE},{TILE_SIZE}, {0} , {SCRIPT},  '{Visibility.Visible}',  {SPECIALSOPTIONS}, {RECTANGLES}, {1}, {HAS_SCALE})");
                return ID;
            }
            else if (Database.ExecuteScalarSQLCommand("SELECT COUNT(*) FROM 'main'.'EDITEDLAYERS' WHERE ID = " + LayerId) == 0)
            {
                int FAVORITE = (Layers.GetLayerById(LayerId)?.IsFavorite == true) ? 1 : 0;
                int VERSION = Layers.GetLayerById(LayerId)?.Version ?? 1;
                Database.ExecuteNonQuerySQLCommand("INSERT INTO 'main'.'EDITEDLAYERS'('ID', 'NAME', 'DESCRIPTION', 'CATEGORY', 'COUNTRY', 'IDENTIFIER', 'TILE_URL', 'MIN_ZOOM', 'MAX_ZOOM', 'FORMAT', 'SITE', 'SITE_URL', 'STYLE', 'TILE_SIZE', 'FAVORITE', 'SCRIPT', 'VISIBILITY', 'SPECIALSOPTIONS', 'RECTANGLES', 'VERSION', 'HAS_SCALE') " +
                $"VALUES({LayerId}, {NAME}, {DESCRIPTION}, {CATEGORY},{COUNTRY}, {IDENTIFIER}, {TILE_URL}, {MIN_ZOOM}, {MAX_ZOOM}, {FORMAT}, {SITE}, {SITE_URL},{STYLE}, {TILE_SIZE}, {FAVORITE},  {SCRIPT},  '{Visibility.Visible}',  {SPECIALSOPTIONS}, {RECTANGLES}, {VERSION}, {HAS_SCALE})");
            }
            else
            {
                int LastVersion = Database.ExecuteScalarSQLCommand("SELECT VERSION FROM 'main'.'LAYERS' WHERE ID=" + LayerId);
                if (LastVersion < 1)
                {
                    LastVersion = 1;
                }
                Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'NAME'={NAME},'DESCRIPTION'={DESCRIPTION},'CATEGORY'={CATEGORY},'COUNTRY'={COUNTRY},'IDENTIFIER'={IDENTIFIER},'TILE_URL'={TILE_URL},'MIN_ZOOM'={MIN_ZOOM},'MAX_ZOOM'={MAX_ZOOM},'FORMAT'={FORMAT},'SITE'={SITE},'SITE_URL'={SITE_URL},'STYLE'={STYLE},'TILE_SIZE'={TILE_SIZE},'SCRIPT'={SCRIPT},'VISIBILITY'='{Visibility.Visible}','SPECIALSOPTIONS'={SPECIALSOPTIONS}, 'RECTANGLES'={RECTANGLES}, 'VERSION'={LastVersion}, 'HAS_SCALE'={HAS_SCALE} WHERE ID = {LayerId}");
            }
            return LayerId;
        }
        private async void ClosePage_button_Click(object sender, RoutedEventArgs e)
        {
            int ValuesHachCode = Generic.CheckIfInputValueHaveChange(EditeurStackPanel);
            ContentDialogResult result = ContentDialogResult.Primary;
            if (DefaultValuesHachCode != ValuesHachCode)
            {
                var dialog = Message.SetContentDialog(Languages.Current["editorMessageLeaveWithoutSaving"], Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesCancel);
                result = await dialog.ShowAsync();
            }
            if (result == ContentDialogResult.Primary)
            {
                DisposeElementBeforeLeave();
                Leave();
            }
        }
        private void SaveEditButton_Click(object sender, RoutedEventArgs e)
        {
            DisposeElementBeforeLeave();
            SaveLayer();
            Leave();
            if (Layers.Current.Id == LayerId)
            {
                MainPage._instance.SetCurrentLayer(Layers.Current.Id);
            }
        }

        public void Leave(bool NoTransition = false)
        {
            Layers.ClearCache(LayerId, false);
            Javascript.EngineStopAll();
            Settings.map_show_tile_border = ShowTileBorderArchive;
            Settings.is_in_debug_mode = IsInDebugModeArchive;
            MainWindow.Instance.FrameBack(NoTransition);
            Javascript.EngineClearList();
            MainPage._instance.RequestReloadPage();
        }

        public void UpdateMoinsUnLayer()
        {
            Javascript.EngineClearList();
            string NAME = TextboxLayerName.Text.Trim();
            string DESCRIPTION = TextboxLayerDescription.Text.Trim();
            string CATEGORY = GetComboBoxValue(TextboxLayerCategory);
            string COUNTRY = string.Join(';', CountryComboBox.SelectedValuesAsString("EnglishName"));
            string IDENTIFIER = TextboxLayerIdentifier.Text.Trim();
            string TILE_URL = TextboxLayerTileUrl.Text.Trim();
            int MIN_ZOOM = GetIntValueFromTextBox(TextBoxLayerMinZoom);
            int MAX_ZOOM = GetIntValueFromTextBox(TextBoxLayerMaxZoom);
            string FORMAT = TextboxLayerFormat.Text.Trim().ToLowerInvariant();
            string SITE = TextboxLayerSite.Text.Trim();
            string SITE_URL = TextboxLayerSiteUrl.Text.Trim();
            string STYLE = TextboxLayerStyle.Text.Trim();
            int TILE_SIZE = GetIntValueFromTextBox(TextboxLayerTileWidth);
            string SCRIPT = TextboxLayerScript.Script.Trim();
            string RECTANGLES = TextboxRectangles.Script.Trim();
            Layers layers = Layers.GetLayerById(-2);
            if (layers is null)
            {
                GenerateTempLayerInDicList();
                layers = Layers.GetLayerById(-2);
            }
            layers.Name = NAME;
            layers.Description = DESCRIPTION;
            layers.Tag = CATEGORY;
            layers.Country = COUNTRY;
            layers.Identifier = IDENTIFIER;
            layers.TileUrl = TILE_URL;
            layers.MinZoom = MIN_ZOOM;
            layers.MaxZoom = MAX_ZOOM;
            layers.TilesFormat = FORMAT;
            layers.SiteName = SITE;
            layers.SiteUrl = SITE_URL;
            layers.TilesSize = string.IsNullOrEmpty(TILE_SIZE.ToString()) ? 256 : TILE_SIZE;
            layers.Script = SCRIPT;
            layers.BoundaryRectangles = RECTANGLES;
            layers.SpecialsOptions = new Layers.LayersSpecialsOptions()
            {
                BackgroundColor = TextboxSpecialOptionBackgroundColor.Text,
                ErrorsToIgnore = string.Join(';', AlloweRequestErrorsComboBox.SelectedValuesAsInt("Status")),
                MaxDownloadTilesInParralele = 0,//Convert.ToInt32(TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele.Text),
                WaitingBeforeStartAnotherTile = 0//Convert.ToInt32(TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile.Text),
            };
            if (int.TryParse(TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele.Text, out int MaxDownloadTilesInParralele))
            {
                layers.SpecialsOptions.MaxDownloadTilesInParralele = MaxDownloadTilesInParralele;
            }
            if (int.TryParse(TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile.Text, out int WaitingBeforeStartAnotherTile))
            {
                layers.SpecialsOptions.WaitingBeforeStartAnotherTile = WaitingBeforeStartAnotherTile;
            }

            layers.Style = STYLE;
            layers.BoundaryRectangles = RECTANGLES;
            layers.IsAtScale = has_scale.IsChecked ?? false;
            Layers.Add(-2, layers);
        }

        async void AutoDetectZoom()
        {
            string label_base_content = LabelAutoDetectZoom.ContentValue.ToString();
            if (string.IsNullOrEmpty(TextboxLayerTileUrl.Text))
            {
                Message.NoReturnBoxAsync(Languages.Current["editorMessageErrorAutoDetectZoomURLNotDefined"], Languages.Current["dialogTitleOperationFailed"]);
                return;
            }

            Location location = mapviewerappercu.Center;
            LabelAutoDetectZoom.IsEnabled = false;
            bool IsSuccessLastRequest = false;
            int ZoomMinimum = -1;
            int ZoomMaximum = -1;

            UpdateMoinsUnLayer();

            for (int Z = 0; Z < 30; Z++)
            {
                string infotext = Languages.GetWithArguments("editorMessageAutoDetectReport", Z);
                LabelAutoDetectZoom.ContentValue = infotext;
                Javascript.Functions.Print(infotext, -2);

                var (X, Y) = Collectif.CoordonneesToTile(location.Latitude, location.Longitude, Z);
                string url = Collectif.Replacements(TextboxLayerTileUrl.Text, X.ToString(), Y.ToString(), Z.ToString(), InternalEditorId, Javascript.InvokeFunction.getTile);

                int result = await Collectif.CheckIfDownloadSuccess(url);
                Debug.WriteLine("-->" + result);
                if (result == 200)
                {
                    if (!IsSuccessLastRequest && ZoomMinimum == -1)
                    {
                        ZoomMinimum = Z;
                        Javascript.Functions.Print(Languages.GetWithArguments("editorMessageAutoDetectReportMinZoomDetected", ZoomMinimum), -2);
                        TextBoxLayerMinZoom.Text = ZoomMinimum.ToString();
                    }
                    IsSuccessLastRequest = true;
                }
                else
                {
                    if (IsSuccessLastRequest && ZoomMaximum == -1)
                    {
                        ZoomMaximum = Z - 1;
                        Javascript.Functions.Print(Languages.GetWithArguments("editorMessageAutoDetectReportMaxZoomDetected", ZoomMaximum), -2);
                        TextBoxLayerMaxZoom.Text = ZoomMaximum.ToString();
                        LabelAutoDetectZoom.ContentValue = label_base_content;
                        LabelAutoDetectZoom.IsEnabled = true;
                        break;
                    }
                    IsSuccessLastRequest = false;
                }
            }

            if (ZoomMaximum == -1)
            {
                LabelAutoDetectZoom.ContentValue = Languages.Current["editorMessageAutoDetectReportMaxZoomNotFound"];
                LabelAutoDetectZoom.IsEnabled = true;
            }
        }

        void DisposeElementBeforeLeave()
        {
            Javascript.Functions.PrintClear();
            Tiles.AcceptJavascriptPrint = false;
            Javascript.EngineStopAll();

            if (UpdateTimer is not null)
            {
                UpdateTimer.Elapsed -= UpdateTimerElapsed_StartUpdateMoinsUnLayer;
            }
            Javascript.LogsChanged -= Javascript_LogsChanged;
            Javascript.JavascriptActionEvent -= JavascriptActionEvent;
            TextboxLayerScriptConsoleSender.PreviewKeyDown -= TextboxLayerScriptConsoleSender_KeyDown;
            //make sure to reload base layer if curent layer is png
            Layers.Current.TilesFormat = "jpeg";
        }

        System.Timers.Timer UpdateTimer;
        void DoWeNeedToUpdateMoinsUnLayer()
        {
            if (UpdateTimer != null)
            {
                UpdateTimer.Elapsed -= UpdateTimerElapsed_StartUpdateMoinsUnLayer;
                UpdateTimer?.Dispose();
            }
            UpdateTimer = new System.Timers.Timer(500);
            UpdateTimer.Elapsed += UpdateTimerElapsed_StartUpdateMoinsUnLayer;
            UpdateTimer.AutoReset = false;
            UpdateTimer.Enabled = true;
        }

        void UpdateTimerElapsed_StartUpdateMoinsUnLayer(object source, EventArgs e)
        {
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
            if (Keyboard.IsKeyDown(Key.Enter) && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                string commande = TextboxLayerScriptConsoleSender.Text;
                TextboxLayerScriptConsoleSender.Text = "";
                Javascript.ExecuteCommand(commande, -2);
                e.Handled = true;
            }


            this.Dispatcher.InvokeAsync(() =>
            {
                var textBox = sender as TextBox;
                bool textBoxHasOverflowContent = textBox.ExtentWidth + 5 > textBox.ViewportWidth;
                if (textBoxHasOverflowContent)
                {
                    textBox.Padding = new Thickness(0, 0, 20, 15);
                }
                else
                {
                    textBox.Padding = new Thickness(0, 0, 20, 5);
                }
            });

        }



        private void LabelAutoDetectZoom_Click(object sender, RoutedEventArgs e)
        {
            LabelAutoDetectZoom.Foreground = Collectif.HexValueToSolidColorBrush("#888989");
            AutoDetectZoom();
        }

        private async void ResetInfoLayerClikableLabel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await Message.SetContentDialog(Languages.Current["editorMessageResetLayerProperty"], Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesCancel).ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Database.ExecuteNonQuerySQLCommand("DELETE FROM EDITEDLAYERS WHERE ID=" + LayerId);
                    Leave(true);
                    DisposeElementBeforeLeave();

                    MainWindow.Instance.FrameLoad_CustomOrEditLayers(LayerId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void DeleteLayerClikableLabel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = Message.SetContentDialog(Languages.Current["editorMessageDeleteLayer"], Languages.Current["dialogTitleOperationConfirm"], MessageDialogButton.YesCancel);
                var result = await dialog.ShowAsync();
                dialog.Visibility = Visibility.Visible;
                if (result == ContentDialogResult.Primary)
                {
                    DisposeElementBeforeLeave();
                    Database.ExecuteNonQuerySQLCommand(@$"
                    DELETE FROM EDITEDLAYERS WHERE ID={LayerId};
                    DELETE FROM CUSTOMSLAYERS WHERE ID={LayerId};
                    DELETE FROM LAYERS WHERE ID={LayerId};
                    ");

                    if (Database.ExecuteScalarSQLCommand($"UPDATE EDITEDLAYERS SET 'VISIBILITY'='DELETED' WHERE ID={LayerId};") == 0)
                    {
                        Database.ExecuteScalarSQLCommand($"INSERT INTO EDITEDLAYERS ('ID', 'VISIBILITY') VALUES ({LayerId},'DELETED')");
                    }

                    Leave();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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
            Collectif.SetBackgroundOnUIElement(mapviewerappercu, TextboxSpecialOptionBackgroundColor.Text);
        }

        #endregion

        private void TextboxLayerFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DoExpandStyleTextBox();
        }

        void DoExpandStyleTextBox()
        {
            if (TextboxLayerFormat is ComboBox combobox)
            {
                if (combobox.SelectedItem is ComboBoxItem comboBoxItem)
                {
                    if (TextboxLayerStyle is null)
                    {
                        return;
                    }

                    if (comboBoxItem.Content.ToString() == "PBF")
                    {
                        TextboxLayerStyle.MinHeight = 120;
                    }
                    else
                    {
                        TextboxLayerStyle.MinHeight = 0;
                    }
                }
                else
                {
                    TextboxLayerFormat.SelectedItem = TextboxLayerFormat?.Items[0];
                }
            }
        }

        private void TextboxSpecialOptionBackgroundColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBox((TextBox)sender, new List<char>() { 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', '#' });
        }

        private void TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBox((TextBox)sender, limitLenght: true);
        }
        private void TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele_TextChanged(object sender, TextChangedEventArgs e)
        {
            Collectif.FilterDigitOnlyWhileWritingInTextBox((TextBox)sender, limitLenght: true);
        }

        private void DisableTextBoxRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = (e.OriginalSource is TextBox);
        }

        private void DisableAllRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void BackgroundSwitch_Toggle(object sender, RoutedEventArgs e)
        {
            SetBackgroundMap(mapviewerappercu);
        }

        private void AutoUpdateLayer_Checked(object sender, RoutedEventArgs e)
        {
            SetBackgroundMap(mapviewerappercu);
            Settings.editor_autoupdatelayer = true;
            Settings.SaveIndividualSettings("editor_autoupdatelayer", true);
        }

        private void AutoUpdateLayer_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.editor_autoupdatelayer = false;
            Settings.SaveIndividualSettings("editor_autoupdatelayer", false);
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //CTRL + S
                try
                {
                    this.Cursor = Cursors.Wait;
                    Mouse.SetCursor(Cursors.Wait);
                    SaveLayer();
                    DefaultValuesHachCode = Generic.CheckIfInputValueHaveChange(EditeurStackPanel);
                }
                finally
                {
                    this.Cursor = Cursors.Arrow;
                    Mouse.UpdateCursor();
                }
            }
        }

        private void LabelOpenVisualRectanglesEditor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (MapTileLayer_Transparent.TileSource is null)
            {
                if (string.IsNullOrEmpty(TextboxLayerTileUrl.Text))
                {
                    Message.NoReturnBoxAsync(Languages.Current["editorMessageErrorAutoDetectZoomURLNotDefined"], Languages.Current["dialogTitleOperationFailed"]);
                    return;
                }
                else
                {
                    SetAppercuLayers(forceUpdate: true);
                }
            }
            SelectionRectangle.Rectangles.Clear();
            FullscreenRectanglesMap FullscreenRectanglesMap = new FullscreenRectanglesMap();
            Collectif.SetBackgroundOnUIElement(FullscreenRectanglesMap.MapViewer, TextboxSpecialOptionBackgroundColor.Text);
            SetBackgroundMap(FullscreenRectanglesMap.MapViewer);
            FullscreenRectanglesMap.MapViewer.Children.Add(new MapTileLayer()
            {
                TileSource = MapTileLayer_Transparent.TileSource
            });
            FullscreenRectanglesMap.MapViewer.Center = mapviewerappercu.Center;
            FullscreenRectanglesMap.MapViewer.ZoomLevel = mapviewerappercu.ZoomLevel;
            int ListOfRectanglesInTextbox = 1;
            try
            {
                ListOfRectanglesInTextbox = (JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(TextboxRectangles.Script) ?? new List<Dictionary<string, string>>()).Count;
            }
            catch (Exception ex)
            {
                Javascript.Functions.PrintError(ex.Message);
            }
            int NumberOfRectangleSuccessfullyAdded = 0;

            foreach (MapFigures.Figure Figure in MapFigures.GetFiguresFromJsonString(TextboxRectangles.Script))
            {
                FullscreenRectanglesMap.AddNewSelection(FullscreenRectanglesMap.mapSelectable.AddRectangle(Figure.NO, Figure.SE), Figure.Name, Figure.MinZoom.ToString(), Figure.MaxZoom.ToString(), Figure.Color, Figure.StrokeThickness.ToString());
                NumberOfRectangleSuccessfullyAdded++;
            }
            int NumberOfErrors = ListOfRectanglesInTextbox - NumberOfRectangleSuccessfullyAdded;
            if (NumberOfErrors > 0)
            {
                string infoText = (NumberOfErrors == 1) ?
                Languages.Current["editorMessageErrorRectangleConversion"] : Languages.GetWithArguments("editorMessageErrorRectanglesConversion", NumberOfErrors);
                Notification InfoUnusedRectangleDeleted = new NText(infoText, "MapsInMyFolder", "FullscreenMap", () => MainWindow.Instance.FrameBack())
                {
                    NotificationId = "InfoUnusedRectangleDeleted",
                    DisappearAfterAMoment = false,
                    IsPersistant = true
                };
                InfoUnusedRectangleDeleted.Register();
            }
            FullscreenRectanglesMap.SaveButton.Click += FullscreenMap_SaveButton_Click;
            FullscreenRectanglesMap.Unloaded += FullscreenRectanglesMap_Unloaded;
            MainWindow.Instance.MainContentFrame.Navigate(FullscreenRectanglesMap);

            void FullscreenRectanglesMap_Unloaded(object sender2, RoutedEventArgs e2)
            {
                FullscreenRectanglesMap.SaveButton.Click -= FullscreenMap_SaveButton_Click;
                FullscreenRectanglesMap.Unloaded -= FullscreenRectanglesMap_Unloaded;
                SelectionRectangle.Rectangles.Clear();
                SetContextMenu();
            }
        }

        private void FullscreenMap_SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var ListOfRectangleProperties = new List<Dictionary<string, object>>();
            foreach (SelectionRectangle selectionRectangle in SelectionRectangle.Rectangles)
            {
                if (!(double.TryParse(selectionRectangle.NOLatitudeTextBox.Text, out double NO_Lat) &&
                double.TryParse(selectionRectangle.NOLongitudeTextBox.Text, out double NO_Long) &&
                double.TryParse(selectionRectangle.SELatitudeTextBox.Text, out double SE_Lat) &&
                double.TryParse(selectionRectangle.SELongitudeTextBox.Text, out double SE_Long)))
                {
                    continue;
                }

                if (NO_Lat == NO_Long || SE_Lat == SE_Long || NO_Lat == SE_Lat || NO_Long == SE_Long)
                {
                    //Zero Width element
                    continue;
                }

                if (!double.TryParse(selectionRectangle.StrokeThicknessTextBox.Text, out double StrokeThickness))
                {
                    StrokeThickness = 1;
                }

                static object GetOptimalZoomValue(string ZoomValue)
                {
                    if (!string.IsNullOrEmpty(ZoomValue) && ZoomValue != "∞" && !string.Equals(ZoomValue, "infinity", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (double.TryParse(ZoomValue, out double doubleZoom) && doubleZoom >= 0)
                        {
                            return (int)Math.Floor(doubleZoom);
                        }
                    }
                    return "∞";
                }

                var RectangleDictionnary = new Dictionary<string, object>()
                    {
                        { "Name", selectionRectangle.NameTextBox.Text},
                        { "Color", selectionRectangle.ColorTextBox.Text},
                        { "StrokeThickness", StrokeThickness},
                        { "MinZoom", GetOptimalZoomValue(selectionRectangle.MinZoomTextBox.Text)},
                        { "MaxZoom", GetOptimalZoomValue(selectionRectangle.MaxZoomTextBox.Text)},
                        { "NO_Lat", NO_Lat},
                        { "NO_Long", NO_Long},
                        { "SE_Lat", SE_Lat},
                        { "SE_Long", SE_Long}
                    };
                if (RectangleDictionnary["Name"].ToString() == Languages.Current["editorSelectionsPropertyDefaultValueName"].ToString())
                {
                    RectangleDictionnary.Remove("Name");
                }
                ListOfRectangleProperties.Add(RectangleDictionnary);
            }

            string SerializedProperties = "";
            if (ListOfRectangleProperties.Count > 0)
            {
                SerializedProperties = JsonConvert.SerializeObject(ListOfRectangleProperties, Formatting.Indented);
            }
            TextboxRectangles.SetScriptNoEvent(SerializedProperties);
            DoWeNeedToUpdateMoinsUnLayer();
            MainWindow.Instance.FrameBack();
        }

        private void IsInDebugModeSwitch_Toggle(object sender, RoutedEventArgs e)
        {
            Settings.is_in_debug_mode = IsInDebugModeSwitch?.IsChecked == true;
            SetAppercuLayers(forceUpdate: true);
        }

        private void PrintUrl_Click(object sender, RoutedEventArgs e)
        {
            PrintPreviewUrls(Javascript.InvokeFunction.getTile);
        }

        private void PrintPreviewUrl_Click(object sender, RoutedEventArgs e)
        {
            PrintPreviewUrls(Javascript.InvokeFunction.getPreview);
        }
        private void PrintPreviewFallbackUrl_Click(object sender, RoutedEventArgs e)
        {
            PrintPreviewUrls(Javascript.InvokeFunction.getPreviewFallback);
        }

        private void PrintPreviewUrls(Javascript.InvokeFunction invokeFunction)
        {
            string Url = GetUrl(invokeFunction);
            Javascript.Functions.Print(invokeFunction.ToString() + " : " + Url, -2);
            Clipboard.SetText(Url);
        }

        private string GetUrl(Javascript.InvokeFunction invokeFunction)
        {
            int ZoomLevel = Convert.ToInt32(Math.Floor(mapviewerappercu.ZoomLevel));
            (int X, int Y) = Collectif.CoordonneesToTile(mapviewerappercu.Center.Latitude, mapviewerappercu.Center.Longitude, ZoomLevel);
            return Collectif.GetUrl.FromTileXYZ(TextboxLayerTileUrl.Text, X, Y, ZoomLevel, -2, invokeFunction);
        }

        private void SetPreviewUrl_Click(object sender, RoutedEventArgs e)
        {
            const Javascript.InvokeFunction invokeFunction = Javascript.InvokeFunction.getPreview;
            TextboxLayerScript.Script = Javascript.AddOrReplaceFunction(TextboxLayerScript.Script, invokeFunction.ToString(), GetPreviewFunction(invokeFunction));
            IndenterCode(TextboxLayerScript);
        }

        private void SetPreviewFallbackUrl_Click(object sender, RoutedEventArgs e)
        {
            const Javascript.InvokeFunction invokeFunction = Javascript.InvokeFunction.getPreviewFallback;
            TextboxLayerScript.Script = Javascript.AddOrReplaceFunction(TextboxLayerScript.Script, invokeFunction.ToString(), GetPreviewFunction(invokeFunction));
            IndenterCode(TextboxLayerScript);
        }

        private string GetPreviewFunction(Javascript.InvokeFunction invokeFunction)
        {
            string TileUrl = TextboxLayerTileUrl.Text;
            string Script = TextboxLayerScript.Script;
            int ZoomLevel = Convert.ToInt32(Math.Floor(mapviewerappercu.ZoomLevel));
            (int X, int Y) = Collectif.CoordonneesToTile(mapviewerappercu.Center.Latitude, mapviewerappercu.Center.Longitude, ZoomLevel);
            var (DefaultCallValue, ResultCallValue) = Collectif.GetUrl.CallFunctionAndGetResult(TileUrl, Script, X, Y, ZoomLevel, -2, Javascript.InvokeFunction.getTile);
            string functionContent = $"\nfunction {invokeFunction}(args){{";
            if (ResultCallValue?.Keys != null)
            {
                string[] basicRequired = new string[] { "x", "y", "z" };
                foreach (var Key in ResultCallValue?.Keys)
                {
                    var Value = ResultCallValue[Key];
                    bool stringUrlContainsKeyReplacement = TileUrl.Contains("{" + Key + "}") || basicRequired.Contains(Key);
                    bool IsInsideDefaultCallContainsKey = DefaultCallValue.ContainsKey(Key);
                    bool IsInsideDefaultCallButHaveChange = false;
                    if (IsInsideDefaultCallContainsKey)
                    {
                        var DefaultValue = DefaultCallValue[Key];
                        if (DefaultValue.ToString() != Value)
                        {
                            IsInsideDefaultCallButHaveChange = true;
                        }
                    }

                    if (stringUrlContainsKeyReplacement || IsInsideDefaultCallButHaveChange || !IsInsideDefaultCallContainsKey)
                    {
                        functionContent += $"\nargs.{Key} = \"{Value}\";";
                    }
                }
            }
            functionContent += "\nreturn args;\n}";
            return functionContent;
        }

        private void TextboxRectangles_TextChanged(object sender, EventArgs e)
        {
            string FiguresJsonString = TextboxRectangles.Script;
            MapFigures.DrawFigureOnMapItemsControlFromJsonString(mapviewerRectangles, FiguresJsonString, mapviewerappercu.ZoomLevel);
        }

        private void TextboxRectangles_KeyUp(object sender, KeyEventArgs e)
        {
            if (TextboxRectangles.Script.IsJson())
            {
                try
                {
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(TextboxRectangles.Script);
                }
                catch (Exception)
                {
                    return;
                }

                DoWeNeedToUpdateMoinsUnLayer();
            }
        }

        private void Mapviewerappercu_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MapFigures.UpdateFiguresFromZoomLevel(mapviewerappercu.TargetZoomLevel);
        }

        private void TextboxLayerName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextboxLayerIdentifier?.Text))
            {
                string saveIdentifier = TextboxLayerName.Text.Replace(System.IO.Path.GetInvalidFileNameChars(), "_");
                saveIdentifier = saveIdentifier.ReplaceLoop("__", "_");
                TextboxLayerIdentifier.Text = saveIdentifier;
            }
        }

        private async void ZoomToTile_Click(object sender, RoutedEventArgs e)
        {

            (StackPanel panel, TextBox textbox) getStackPanel(string labelContent)
            {
                TextBox getTextbox()
                {
                    return new TextBox
                    {
                        Height = 25,
                        Foreground = Collectif.HexValueToSolidColorBrush("#BCBCBC"),
                        Style = TryFindResource("TextBoxCleanStyleDefault") as System.Windows.Style,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                    };
                }

                Label getLabel()
                {
                    return new Label()
                    {
                        Content = labelContent
                    };
                }

                var StackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                StackPanel.Children.Add(getLabel());
                var TextBox = getTextbox();
                StackPanel.Children.Add(TextBox);
                return (StackPanel, TextBox);
            }

            var Grid = new Grid();
            Grid.ColumnDefinitions.Add(new ColumnDefinition());//x
            Grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(10)
            });
            Grid.ColumnDefinitions.Add(new ColumnDefinition());//y
            Grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(10)
            });
            Grid.ColumnDefinitions.Add(new ColumnDefinition());//z

            var ActionLabel = new Label()
            {
                Content = Languages.Current["editorMapOptionsZoomToTile"],
                Margin = new Thickness(0, 5, 0, 5)
            };
            var TileX = getStackPanel("X :");
            var TileY = getStackPanel("Y :");
            var TileZ = getStackPanel("Z :");
            Grid.SetColumn(TileX.panel, 0);
            Grid.SetColumn(TileY.panel, 2);
            Grid.SetColumn(TileZ.panel, 4);
            Grid.Children.Add(TileX.panel);
            Grid.Children.Add(TileY.panel);
            Grid.Children.Add(TileZ.panel);
            var StackPanel = new StackPanel
            {
                MinWidth = 400
            };
            StackPanel.Children.Add(ActionLabel);
            StackPanel.Children.Add(Grid);
            var modal = Message.SetContentDialog(StackPanel, "MapsInMyFolder", MessageDialogButton.OKCancel);
            MessageContentDialogHelpers.FocusSenderOnLoad(TileX.textbox);
            var result = await modal.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                if (int.TryParse(TileX.textbox.Text, out int X) &&
                int.TryParse(TileY.textbox.Text, out int Y) &&
                int.TryParse(TileZ.textbox.Text, out int Z))
                {
                    var location = Collectif.TileToCoordonnees(X, Y, Z);

                    double MaxZoomLevel = mapviewerappercu.MaxZoomLevel;
                    mapviewerappercu.MaxZoomLevel = Z;
                    mapviewerappercu.ZoomToBounds(MapSelectable.GetTileBounding(new Location(location.Latitude, location.Longitude), Z));
                    mapviewerappercu.MaxZoomLevel = MaxZoomLevel;
                }
            }
        }


        private async void ZoomToLocation_Click(object sender, RoutedEventArgs e)
        {
            (StackPanel panel, TextBox textbox) getStackPanel(string labelContent)
            {
                TextBox getTextbox()
                {
                    return new TextBox
                    {
                        Height = 25,
                        Foreground = Collectif.HexValueToSolidColorBrush("#BCBCBC"),
                        Style = TryFindResource("TextBoxCleanStyleDefault") as System.Windows.Style,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                    };
                }

                Label getLabel()
                {
                    return new Label()
                    {
                        Content = labelContent
                    };
                }

                var StackPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                StackPanel.Children.Add(getLabel());
                var TextBox = getTextbox();
                StackPanel.Children.Add(TextBox);
                return (StackPanel, TextBox);
            }

            var Grid = new Grid();
            Grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(10)
            });
            Grid.ColumnDefinitions.Add(new ColumnDefinition());

            var ActionLabel = new Label()
            {
                Content = Languages.Current["editorMapOptionsZoomToLocation"],
                Margin = new Thickness(0, 5, 0, 5)
            };
            var latitude = getStackPanel(Languages.Current["mapLatitude"]);
            var longitude = getStackPanel(Languages.Current["mapLongitude"]);
            Grid.SetColumn(latitude.panel, 0);
            Grid.SetColumn(longitude.panel, 2);
            Grid.Children.Add(latitude.panel);
            Grid.Children.Add(longitude.panel);
            var StackPanel = new StackPanel
            {
                MinWidth = 400
            };
            StackPanel.Children.Add(ActionLabel);
            StackPanel.Children.Add(Grid);
            var modal = Message.SetContentDialog(StackPanel, "MapsInMyFolder", MessageDialogButton.OKCancel);
            MessageContentDialogHelpers.FocusSenderOnLoad(latitude.textbox);
            var result = await modal.ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
            {
                if (double.TryParse(latitude.textbox.Text, out double lat) &&
                double.TryParse(longitude.textbox.Text, out double lng))
                {
                    double MaxZoomLevel = mapviewerappercu.MaxZoomLevel;
                    mapviewerappercu.MaxZoomLevel = mapviewerappercu.ZoomLevel;
                    mapviewerappercu.ZoomToBounds(MapSelectable.GetTileBounding(new Location(lat, lng), (int)Math.Floor(mapviewerappercu.ZoomLevel)));
                    mapviewerappercu.MaxZoomLevel = MaxZoomLevel;
                }
            }
        }

        private void TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele.Text == "0")
            {
                TextboxSpecialOptioneditorPropertyNameMaxDownloadTilesInParralele.Text = string.Empty;
            }
        }
        private void TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile.Text == "0")
            {
                TextboxSpecialOptioneditorPropertyNameWaitingBeforeStartAnotherTile.Text = string.Empty;
            }
        }

    }
}
