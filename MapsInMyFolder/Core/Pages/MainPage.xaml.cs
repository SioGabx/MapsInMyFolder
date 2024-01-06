using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TextBox = System.Windows.Controls.TextBox;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : System.Windows.Controls.Page
    {
        public static MainPage Instance { get; set; }
        bool isInitialised = false;
        public static MapSelectable MapSelectable { get; set; }
        private static MapFigures MapFigures;

        public static Func<IEnumerable<Layers>> GetLayersListMethod
        {
            get
            {
                return Layers.GetLayersList;
            }
        }

        public MainPage()
        {
            Instance = this;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInitialised)
            {
                Location NO_PIN_starting_location = new Location(Settings.NO_PIN_starting_location_latitude, Settings.NO_PIN_starting_location_longitude);
                Location SE_PIN_starting_location = new Location(Settings.SE_PIN_starting_location_latitude, Settings.SE_PIN_starting_location_longitude);

                MapSelectable = new MapSelectable(mapviewer, NO_PIN_starting_location, SE_PIN_starting_location, this)
                {
                    //Disable event dispose on navigate beceause this is the main page
                    DisposeElementsOnUnload = false
                };
                MapFigures = new MapFigures();
                Init();
            }
        }


        void Init()
        {
            MapLoad();
            InitDownloadPanel();
            LayerPanel.Init();
            isInitialised = true;
            Notification.UpdateNotification += UpdateNotification;
        }

        private async void Map_panel_open_location_panel_Click(object sender, RoutedEventArgs e)
        {
            (TextBox LeftTextBox, TextBox RightTextBox) setDoubleColumnTextBox(Grid grid, string leftLabelText, string rightLabelText)
            {
                grid.Margin = new Thickness(0, 20, 0, 0);
                TextBox GetTextBox(object content, int column)
                {
                    Label label = new Label()
                    {
                        Content = content,
                    };
                    Grid.SetRow(label, 0);
                    Grid.SetColumn(label, column);
                    TextBox textbox = new TextBox
                    {
                        Width = 225,
                        Height = 25,
                        Foreground = Collectif.HexValueToSolidColorBrush("#BCBCBC"),
                        Style = TryFindResource("TextBoxCleanStyleDefault") as Style
                    };
                    Grid.SetRow(textbox, 2);
                    Grid.SetColumn(textbox, column);
                    grid.Children.Add(label);
                    grid.Children.Add(textbox);
                    return textbox;
                }

                TextBox leftTextbox = GetTextBox(leftLabelText, 0);
                TextBox rightTextbox = GetTextBox(rightLabelText, 2);
                return (leftTextbox, rightTextbox);
            }

            Grid getGrid()
            {
                Grid contentGrid = new Grid();
                contentGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(17)
                });
                contentGrid.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(3)
                });
                contentGrid.RowDefinitions.Add(new RowDefinition());
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = new GridLength(10)
                });
                contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
                return contentGrid;
            }

            StackPanel stackPanel = new StackPanel()
            {
                Margin = new Thickness(10, 10, 5, 0),
            };

            stackPanel.Children.Add(new Label()
            {
                Content = Languages.Current["mapSpecifySelectionCoordinates"],
            });
            Grid nordOuestGrid = getGrid();
            var (LeftTextBox, RightTextBox) = setDoubleColumnTextBox(nordOuestGrid, Languages.Current["editorSelectionsPropertyNameNorthwestLatitude"], Languages.Current["editorSelectionsPropertyNameNorthwestLongitude"]);
            stackPanel.Children.Add(nordOuestGrid);
            var NOLatitudeTextBox = LeftTextBox;
            var NOLongitudeTextBox = RightTextBox;
            NOLatitudeTextBox.Text = Map.CurentSelection.NO_Latitude.ToString();
            NOLongitudeTextBox.Text = Map.CurentSelection.NO_Longitude.ToString();


            Grid sudEstGrid = getGrid();
            var sudEstTextBox = setDoubleColumnTextBox(sudEstGrid, Languages.Current["editorSelectionsPropertyNameSoutheastLatitude"], Languages.Current["editorSelectionsPropertyNameSoutheastLongitude"]);
            stackPanel.Children.Add(sudEstGrid);
            var SELatitudeTextBox = sudEstTextBox.LeftTextBox;
            var SELongitudeTextBox = sudEstTextBox.RightTextBox;
            SELatitudeTextBox.Text = Map.CurentSelection.SE_Latitude.ToString();
            SELongitudeTextBox.Text = Map.CurentSelection.SE_Longitude.ToString();

            UserControls.ClickableLabel CopyLabel = new UserControls.ClickableLabel()
            {
                ContentValue = "→ " + Languages.Current["mapSpecifySelectionCoordinatesCopy"],
                Foreground = Collectif.HexValueToSolidColorBrush("888989"),
                Margin = new Thickness(0, 20, 0, 0)
            };
            CopyLabel.Click += CopyLabel_Click;
            CopyLabel.Unloaded += CopyLabel_Unloaded;
            stackPanel.Children.Add(CopyLabel);
            void CopyLabel_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    Clipboard.SetText($"{NOLatitudeTextBox.Text},{NOLongitudeTextBox.Text},{SELatitudeTextBox.Text},{SELongitudeTextBox.Text}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            void CopyLabel_Unloaded(object sender, RoutedEventArgs e)
            {
                CopyLabel.Click -= CopyLabel_Click;
                CopyLabel.Unloaded -= CopyLabel_Unloaded;
            }

            UserControls.ClickableLabel PasteLabel = new UserControls.ClickableLabel()
            {
                ContentValue = "→ " + Languages.Current["mapSpecifySelectionCoordinatesPaste"],
                Foreground = Collectif.HexValueToSolidColorBrush("888989")
            };
            PasteLabel.Click += PasteLabel_Click;
            PasteLabel.Unloaded += PasteLabel_Unloaded;
            stackPanel.Children.Add(PasteLabel);
            void PasteLabel_Click(object sender, RoutedEventArgs e)
            {
                try
                {
                    string locationData = Clipboard.GetText(TextDataFormat.Text);
                    string[] locationDataValues = locationData.Split(',');

                    // Assigner chaque valeur à une variable
                    if (locationDataValues.Length == 4)
                    {
                        double NOLatitude = double.Parse(locationDataValues[0]);
                        double NOLongitude = double.Parse(locationDataValues[1]);
                        double SELatitude = double.Parse(locationDataValues[2]);
                        double SELongitude = double.Parse(locationDataValues[3]);


                        NOLatitudeTextBox.Text = NOLatitude.ToString();
                        NOLongitudeTextBox.Text = NOLongitude.ToString();

                        SELatitudeTextBox.Text = SELatitude.ToString();
                        SELongitudeTextBox.Text = SELongitude.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            void PasteLabel_Unloaded(object sender, RoutedEventArgs e)
            {
                PasteLabel.Click -= PasteLabel_Click;
                PasteLabel.Unloaded -= PasteLabel_Unloaded;
            }




            var result = await Message.ShowContentDialog(stackPanel, "MapsInMyFolder", MessageDialogButton.OKCancel);
            if (result == ContentDialogResult.Primary)
            {
                if (double.TryParse(NOLatitudeTextBox.Text, out double NO_Lat) &&
                double.TryParse(NOLongitudeTextBox.Text, out double NO_Long) &&
                double.TryParse(SELatitudeTextBox.Text, out double SE_Lat) &&
                double.TryParse(SELongitudeTextBox.Text, out double SE_Long))
                {
                    Javascript.Functions.SetSelection(NO_Lat, NO_Long, SE_Lat, SE_Long, true, Layers.Current.Id);
                }
            }
        }

        private void Download_panel_close_button_Click(object sender, RoutedEventArgs e)
        {
            //Debug.WriteLine(Languages.Current["searchLayerPlaceholder"]);
            DownloadPanelClose();
        }

        public void UpdateNotification(object sender, (string NotificationId, string Destinateur) NotificationInternalArgs)
        {
            if (sender is Notification Notif)
            {
                if (NotificationInternalArgs.Destinateur != "MainPage")
                {
                    return;
                }
                Grid ContentGrid = Notif.Get();
                if (Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
                {
                    if (Notif.replaceOld == false)
                    {
                        return;
                    }
                    Grid NotificationZoneContentGrid = Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                    NotificationZone.Children.Remove(NotificationZoneContentGrid);
                    NotificationZone.Children.Insert(Math.Min(Notif.InsertPosition, NotificationZone.Children.Count), ContentGrid);
                    NotificationZoneContentGrid.Children.Clear();
                    NotificationZoneContentGrid = null;
                }
                else
                {
                    ContentGrid.Opacity = 0;
                    var doubleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond));
                    ContentGrid.BeginAnimation(OpacityProperty, doubleAnimation);
                    Notif.InsertPosition = NotificationZone.Children.Add(ContentGrid);
                }
            }
            else if (Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
            {
                Grid ContentGrid = Collectif.FindChildByName<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                var doubleAnimation = new DoubleAnimation(ContentGrid.ActualHeight, 0, new Duration(TimeSpan.FromSeconds(0.1)));
                void DeleteAfterAnimation(object sender, EventArgs e)
                {
                    ContentGrid?.Children?.Clear();
                    NotificationZone.Children.Remove(ContentGrid);
                    ContentGrid = null;
                    doubleAnimation.Completed -= DeleteAfterAnimation;
                }
                doubleAnimation.Completed += DeleteAfterAnimation;
                ContentGrid.BeginAnimation(MaxHeightProperty, doubleAnimation);
            }
        }


        public void MapLoad()
        {
            mapviewer.MapLayer = new MapTileLayer();
            NO_PIN.Visibility = Settings.visibility_pins;
            SE_PIN.Visibility = Settings.visibility_pins;

            if (mapviewer.Center.Latitude == 0 && mapviewer.Center.Longitude == 0)
            {
                mapviewer.Center = new Location((Settings.NO_PIN_starting_location_latitude + Settings.SE_PIN_starting_location_latitude) / 2, (Settings.NO_PIN_starting_location_longitude + Settings.SE_PIN_starting_location_longitude) / 2);
                mapviewer.ZoomLevel = Settings.map_defaut_zoom_level;
                MapSelectable.OnLocationUpdated += OnLocationUpdated;
                MapSelectable.RequestedPreviewUpdate += (o, e) => SetBBOXPreviewRequestUpdate();

            }
            mapviewer.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(255,
                (byte)Settings.background_layer_color_R,
                (byte)Settings.background_layer_color_G,
                (byte)Settings.background_layer_color_B)
            );

            OnLocationUpdated();
        }

        private void OnLocationUpdated(object sender = null, MapPolygon e = null)
        {
            var (NO, SE) = MapSelectable.GetRectangleLocation();
            NO_PIN.Location = NO;
            SE_PIN.Location = SE;
            Map.CurentSelection.NO_Latitude = NO.Latitude;
            Map.CurentSelection.NO_Longitude = NO.Longitude;
            Map.CurentSelection.SE_Latitude = SE.Latitude;
            Map.CurentSelection.SE_Longitude = SE.Longitude;

            if (Javascript.CheckIfFunctionExist(Layers.Current, Javascript.InvokeFunction.selectionChanged.ToString(), null))
            {
                string script = Layers.Current.Script;

                Dictionary<string, Dictionary<string, double>> selectionArguments = Javascript.Functions.GetSelection();
                // Populate the original dictionary with some data

                Dictionary<string, object> arguments = new Dictionary<string, object>();
                foreach (var outerKey in selectionArguments.Keys)
                {
                    var innerDictionary = selectionArguments[outerKey];
                    Dictionary<string, object> innerConvertedDictionary = new Dictionary<string, object>();
                    foreach (var innerKey in innerDictionary.Keys)
                    {
                        innerConvertedDictionary[innerKey] = innerDictionary[innerKey];
                    }
                    arguments[outerKey] = innerConvertedDictionary;
                }
                try
                {
                    Javascript.ExecuteScript(script, new Dictionary<string, object>(arguments), Layers.Current.Id, Javascript.InvokeFunction.selectionChanged);
                }
                catch (Exception ex)
                {
                    Javascript.Functions.PrintError(ex.Message);
                }
            }


            if (Settings.visibility_pins == Visibility.Visible)
            {
                NO_PIN.Content = $"{Languages.Current["mapLatitude"]} = {Math.Round(NO_PIN.Location.Latitude, 6)}\n{Languages.Current["mapLongitude"]} = {Math.Round(NO_PIN.Location.Longitude, 6)}";
                SE_PIN.Content = $"{Languages.Current["mapLatitude"]} = {Math.Round(SE_PIN.Location.Latitude, 6)}\n{Languages.Current["mapLongitude"]} = {Math.Round(SE_PIN.Location.Longitude, 6)}";
            }
        }

        public void MapViewerSetSelection(Dictionary<string, double> locations, bool ZoomToNewLocation = true)
        {
            var ActiveRectangleSelection = MapSelectable.GetRectangleLocation();
            if (ActiveRectangleSelection.NO.Latitude != locations["NO_Latitude"] ||
                ActiveRectangleSelection.NO.Longitude != locations["NO_Longitude"] ||
                ActiveRectangleSelection.SE.Latitude != locations["SE_Latitude"] ||
                ActiveRectangleSelection.SE.Longitude != locations["SE_Longitude"])
            {
                ActiveRectangleSelection.NO = new Location(locations["NO_Latitude"], locations["NO_Longitude"]);
                ActiveRectangleSelection.SE = new Location(locations["SE_Latitude"], locations["SE_Longitude"]);
                MapSelectable.SetRectangleLocation(ActiveRectangleSelection.NO, ActiveRectangleSelection.SE);
            }


            if (ZoomToNewLocation)
            {
                ActiveRectangleSelection = MapSelectable.GetRectangleLocation();
                double pourcentage_Lat = Math.Abs((ActiveRectangleSelection.NO.Latitude - ActiveRectangleSelection.SE.Latitude) / 2);
                double pourcentage_Lng = Math.Abs((ActiveRectangleSelection.NO.Latitude - ActiveRectangleSelection.SE.Latitude) / 2);
                Location NO_Location_Bounds = new Location(ActiveRectangleSelection.NO.Latitude - pourcentage_Lat, ActiveRectangleSelection.NO.Longitude - pourcentage_Lng);
                Location SE_Location_Bounds = new Location(pourcentage_Lat + ActiveRectangleSelection.SE.Latitude, pourcentage_Lng + ActiveRectangleSelection.SE.Longitude);
                mapviewer.ZoomToBounds(new BoundingBox(NO_Location_Bounds.Latitude, NO_Location_Bounds.Longitude, SE_Location_Bounds.Latitude, SE_Location_Bounds.Longitude));
            }
        }

        private void Mapviewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MapFigures.UpdateFiguresFromZoomLevel(mapviewer.TargetZoomLevel);
            Collectif.AnimateLabel(ZoomLevelIndicator);
        }

        private void Start_Download_Click(object sender, RoutedEventArgs e)
        {
            MapSelectable.CleanRectangleLocations();
            MainWindow.Instance.FrameLoad_PrepareDownload();
        }

        private void MapLocationSearchBar_SearchResultEvent(object sender, UserControls.SearchLocation.SearchResultEventArgs e)
        {
            SetBBOXPreviewRequestUpdate();
        }

        private void MapLocationSearchBar_SearchLostFocusRequest(object sender, EventArgs e)
        {
            LayerPanel.LayerBrowser.Focus();
        }

        private void LayerPanel_SetCurrentLayerEvent(object sender, UserControls.LayersPanel.LayerIdEventArgs e)
        {
            SetCurrentLayer(e.LayerId);
        }

        public void SetCurrentLayer(int id)
        {
            Layers.SetCurrentLayer(id);
            Layers.SetMapLayer(Layers.Current, mapviewer, MapTileLayer_Transparent, MapFigures, mapviewerRectangles);
        }

        public void RefreshMap()
        {
            SetCurrentLayer(Layers.Current.Id);
        }
        public void RequestReloadPage()
        {
            LayerPanel.Reload();
        }

        public async void ShowLayerWarning(int id)
        {
            int EditedDB_VERSION;
            string EditedDB_SCRIPT;
            string EditedDB_TILE_URL;

            int LastDB_VERSION;
            string LastDB_SCRIPT;
            string LastDB_TILE_URL;


            var DatabaseEditedLayerExecutable = Database.ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'EDITEDLAYERS' WHERE ID = {id}");
            using (DatabaseEditedLayerExecutable.conn)
            {
                using (SQLiteDataReader editedlayers_sqlite_datareader = DatabaseEditedLayerExecutable.Reader)
                {
                    if (!editedlayers_sqlite_datareader.Read())
                    {
                        return;
                    }

                    EditedDB_VERSION = editedlayers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    EditedDB_SCRIPT = editedlayers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    EditedDB_TILE_URL = editedlayers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                }
            }

            var DatabaseLayerExecutable = Database.ExecuteExecuteReaderSQLCommand($"SELECT * FROM 'LAYERS' WHERE ID = {id}");
            using (DatabaseLayerExecutable.conn)
            {
                using (SQLiteDataReader layers_sqlite_datareader = DatabaseLayerExecutable.Reader)
                {
                    layers_sqlite_datareader.Read();
                    LastDB_VERSION = layers_sqlite_datareader.GetIntFromOrdinal("VERSION") ?? 0;
                    LastDB_SCRIPT = layers_sqlite_datareader.GetStringFromOrdinal("SCRIPT");
                    if (string.IsNullOrEmpty(LastDB_SCRIPT))
                    {
                        LastDB_SCRIPT = "";
                    }
                    LastDB_TILE_URL = layers_sqlite_datareader.GetStringFromOrdinal("TILE_URL");
                }
            }

            if (EditedDB_VERSION != LastDB_VERSION)
            {
                bool HasActionToBeTaken = false;
                StackPanel AskMsg = new StackPanel();
                string RemoveSQL = "";

                if (EditedDB_SCRIPT != LastDB_SCRIPT && !string.IsNullOrWhiteSpace(EditedDB_SCRIPT))
                {
                    HasActionToBeTaken = true;
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Languages.Current["layerMessageErrorUpdateScriptChanged"],
                        TextWrapping = TextWrapping.Wrap
                    };
                    AskMsg.Children.Add(textBlock);
                    AskMsg.Children.Add(Collectif.FormatDiffGetScrollViewer(EditedDB_SCRIPT, LastDB_SCRIPT));
                    RemoveSQL += $"'SCRIPT'=NULL";
                }

                if (EditedDB_TILE_URL != LastDB_TILE_URL && !string.IsNullOrWhiteSpace(EditedDB_TILE_URL))
                {
                    HasActionToBeTaken = true;
                    TextBlock textBlock = new TextBlock
                    {
                        Text = Languages.Current["layerMessageErrorUpdateTileURLChanged"],
                        TextWrapping = TextWrapping.Wrap
                    };
                    AskMsg.Children.Add(textBlock);
                    AskMsg.Children.Add(Collectif.FormatDiffGetScrollViewer(EditedDB_TILE_URL, LastDB_TILE_URL));
                    RemoveSQL += $"'TILE_URL'=NULL";
                }

                TextBlock textBlockAsk = new TextBlock
                {
                    Text = Languages.Current["layerMessageErrorUpdateAskFix"],
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeight.FromOpenTypeWeight(600)
                };
                AskMsg.Children.Add(textBlockAsk);
                ContentDialogResult? dialogErrorUpdateAskFixResult = ContentDialogResult.Secondary;
                if (HasActionToBeTaken)
                {
                    dialogErrorUpdateAskFixResult = await Message.ShowContentDialog(AskMsg, "MapsInMyFolder", MessageDialogButton.YesNoCancel);
                }
                if (dialogErrorUpdateAskFixResult == ContentDialogResult.Primary)
                {
                    Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}',{RemoveSQL} WHERE ID = {id};");
                }
                else if (dialogErrorUpdateAskFixResult == ContentDialogResult.Secondary)
                {
                    Database.ExecuteNonQuerySQLCommand($"UPDATE 'main'.'EDITEDLAYERS' SET 'VERSION'='{LastDB_VERSION}' WHERE ID = {id};");
                }
                else
                {
                    return;
                }

                RequestReloadPage();
                SetCurrentLayer(Layers.Current.Id);
            }
        }

        public void SetBBOXPreviewRequestUpdate()
        {
            var bbox = mapviewer.ViewRectToBoundingBox(new Rect(0, 0, mapviewer.ActualWidth, mapviewer.ActualHeight));
            Map.CurentView.NO_Latitude = bbox.North;
            Map.CurentView.NO_Longitude = bbox.West;
            Map.CurentView.SE_Latitude = bbox.South;
            Map.CurentView.SE_Longitude = bbox.East;

            LayerPanel.PreviewRequestUpdate();
            return;
        }

        private void LayerPanel_OpenEditLayerPageEvent(object sender, UserControls.LayersPanel.LayerIdEventArgs e)
        {
            if (e.Args is not CustomOrEditLayersPage.EditingMode EditMode)
            {
                EditMode = CustomOrEditLayersPage.EditingMode.New;
            }
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                CustomOrEditLayersPage EditPage = MainWindow.Instance.FrameLoad_CustomOrEditLayers(e.LayerId, EditMode);
                EditPage.Init();
            }, null);
        }

    }
}
