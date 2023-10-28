using Jint.Runtime;
using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using static MapsInMyFolder.Commun.Collectif.GetUrl;
using TextBox = System.Windows.Controls.TextBox;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : System.Windows.Controls.Page
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Les champs non constants ne doivent pas être visibles", Justification = "for access everywhere")]
        public static MainPage _instance;
        bool isInitialised = false;
        public static MapSelectable MapSelectable { get; set; }
        private static MapFigures MapFigures;
        public MainPage()
        {
            _instance = this;
            InitializeComponent();
            var requestHandler = new CustomRequestHandler();
            layer_browser.RequestHandler = requestHandler;
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
                Preload();
                Init();
            }
        }

        public void Preload()
        {
            ReloadPage();
            MapLoad();
        }

        void Init()
        {
            InitDownloadPanel();
            InitLayerPanel();
            isInitialised = true;
            Notification.UpdateNotification += UpdateNotification;
            layer_browser.ToolTipOpening += (o, e) => e.Handled = true;
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
            NOLatitudeTextBox.Text = Commun.Map.CurentSelection.NO_Latitude.ToString();
            NOLongitudeTextBox.Text = Commun.Map.CurentSelection.NO_Longitude.ToString();


            Grid sudEstGrid = getGrid();
            var sudEstTextBox = setDoubleColumnTextBox(sudEstGrid, Languages.Current["editorSelectionsPropertyNameSoutheastLatitude"], Languages.Current["editorSelectionsPropertyNameSoutheastLongitude"]);
            stackPanel.Children.Add(sudEstGrid);
            var SELatitudeTextBox = sudEstTextBox.LeftTextBox;
            var SELongitudeTextBox = sudEstTextBox.RightTextBox;
            SELatitudeTextBox.Text = Commun.Map.CurentSelection.SE_Latitude.ToString();
            SELongitudeTextBox.Text = Commun.Map.CurentSelection.SE_Longitude.ToString();

            Label CopyLabel = new Label()
            {
                Content = "→ " + Languages.Current["mapSpecifySelectionCoordinatesCopy"],
                Foreground = Collectif.HexValueToSolidColorBrush("888989"),
                Margin = new Thickness(0,20,0,0)
            };
            CopyLabel.MouseLeftButtonUp += CopyLabel_MouseLeftButtonUp;
            CopyLabel.Unloaded += CopyLabel_Unloaded;
            CopyLabel.MouseEnter += Collectif.ClickableLabel_MouseEnter;
            CopyLabel.MouseLeave += Collectif.ClickableLabel_MouseLeave;
            stackPanel.Children.Add(CopyLabel);
            void CopyLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                try
                {
                    CopyLabel.Cursor = Cursors.Arrow;
                    Clipboard.SetText($"{NOLatitudeTextBox.Text},{NOLongitudeTextBox.Text},{SELatitudeTextBox.Text},{SELongitudeTextBox.Text}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            void CopyLabel_Unloaded(object sender, RoutedEventArgs e)
            {
                CopyLabel.MouseRightButtonUp -= CopyLabel_MouseLeftButtonUp;
                CopyLabel.Unloaded -= CopyLabel_Unloaded;
                CopyLabel.MouseEnter -= Collectif.ClickableLabel_MouseEnter;
                CopyLabel.MouseLeave -= Collectif.ClickableLabel_MouseLeave;
            }

            Label PasteLabel = new Label()
            {
                Content = "→ " + Languages.Current["mapSpecifySelectionCoordinatesPaste"],
                Foreground = Collectif.HexValueToSolidColorBrush("888989")
            };
            PasteLabel.MouseLeftButtonUp += PasteLabel_MouseLeftButtonUp;
            PasteLabel.Unloaded += PasteLabel_Unloaded;
            PasteLabel.MouseEnter += Collectif.ClickableLabel_MouseEnter;
            PasteLabel.MouseLeave += Collectif.ClickableLabel_MouseLeave;
            stackPanel.Children.Add(PasteLabel);
            void PasteLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                try
                {
                    PasteLabel.Cursor = Cursors.Arrow;
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
                PasteLabel.MouseRightButtonUp -= PasteLabel_MouseLeftButtonUp;
                PasteLabel.Unloaded -= PasteLabel_Unloaded;
                PasteLabel.MouseEnter -= Collectif.ClickableLabel_MouseEnter;
                PasteLabel.MouseLeave -= Collectif.ClickableLabel_MouseLeave;
            }




            var result = await Message.SetContentDialog(stackPanel, "MapsInMyFolder", MessageDialogButton.OKCancel).ShowAsync();
            if (result == ModernWpf.Controls.ContentDialogResult.Primary)
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
            Debug.WriteLine(Languages.Current["searchLayerPlaceholder"]);
            //Download_panel_close();
        }

        private void Layer_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (layer_searchbar.Text == Languages.Current["searchLayerPlaceholder"])
            {
                layer_searchbar.Text = "";
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private void Layer_searchbar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    lastSearch = "";
                    SearchLayerStart();
                    layer_browser.Focus();
                }
                catch { }
            }
            if (e.Key == Key.Escape)
            {
                layer_browser.Focus();
            }
        }

        private void Layer_searchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(layer_searchbar.Text))
            {
                layer_searchbar.Text = Languages.Current["searchLayerPlaceholder"];
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
            else
            {
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private void Layer_searchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchLayerStart();
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
                MapSelectable.RequestedPreviewUpdate += (o, e) => LayerTilePreview_RequestUpdate();

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
            Commun.Map.CurentSelection.NO_Latitude = NO.Latitude;
            Commun.Map.CurentSelection.NO_Longitude = NO.Longitude;
            Commun.Map.CurentSelection.SE_Latitude = SE.Latitude;
            Commun.Map.CurentSelection.SE_Longitude = SE.Longitude;

            if (Javascript.CheckIfFunctionExist(Layers.Current.Id, Javascript.InvokeFunction.selectionChanged.ToString(), null))
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
            AnimateLabel(ZoomLevelIndicator);
        }

        private bool isAnimating = false; // Indique si l'animation est en cours d'exécution

        private void AnimateLabel(Label label)
        {
            label.Visibility = Visibility.Visible;
            // Si l'animation est déjà en cours d'exécution, annule l'animation de disparition en cours et recommence l'animation d'apparition à la valeur d'opacité actuelle
            DoubleAnimation fadeInAnimation = new DoubleAnimation();

            if (isAnimating)
            {
                label.BeginAnimation(OpacityProperty, null);
                fadeInAnimation.From = label.Opacity;
                fadeInAnimation.To = 1.0;
            }
            else
            {
                fadeInAnimation.From = 0.0;
                fadeInAnimation.To = 1.0;
            }

            TimeSpan fadeInDuration = TimeSpan.FromSeconds(0.2);
            TimeSpan fadeOutDuration = TimeSpan.FromSeconds(0.5);

            fadeInAnimation.Duration = fadeInDuration;
            Storyboard storyboard = new Storyboard();
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = fadeOutDuration,

                // Démarre après l'animation d'apparition + 1 seconde
                BeginTime = fadeInDuration + TimeSpan.FromSeconds(0.5)
            };

            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(fadeOutAnimation);
            Storyboard.SetTarget(fadeInAnimation, label);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(fadeOutAnimation, label);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(OpacityProperty));

            void AnimationCompleted(object sender, EventArgs e)
            {
                isAnimating = false;
                fadeOutAnimation.Completed -= AnimationCompleted;
            }

            fadeOutAnimation.Completed += AnimationCompleted;

            storyboard.Begin();
            isAnimating = true;
        }

        private void Start_Download_Click(object sender, RoutedEventArgs e)
        {
            MapSelectable.CleanRectangleLocations();
            MainWindow.Instance.FrameLoad_PrepareDownload();
        }
    }
}
