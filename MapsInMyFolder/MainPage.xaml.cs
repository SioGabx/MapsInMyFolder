using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Les champs non constants ne doivent pas être visibles", Justification = "for access everywhere")]
        public static MainPage _instance;
        bool isInitialised = false;
        public static MapSelectable mapSelectable;
        public MainPage()
        {
            _instance = this;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInitialised)
            {
                Location NO_PIN_starting_location = new Location(Settings.NO_PIN_starting_location_latitude, Settings.NO_PIN_starting_location_longitude);
                Location SE_PIN_starting_location = new Location(Settings.SE_PIN_starting_location_latitude, Settings.SE_PIN_starting_location_longitude);
                
                mapSelectable = new MapSelectable(mapviewer, NO_PIN_starting_location, SE_PIN_starting_location, null, this);
                mapviewer.MouseWheel += (o, e) => MapFigures.UpdateFiguresFromZoomLevel(mapviewer.TargetZoomLevel);
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
            Init_download_panel();
            Init_layer_panel();
            isInitialised = true;
            Notification.UpdateNotification += (Notification, NotificationArgs) => UpdateNotification((Notification)Notification, NotificationArgs);
        }
        private void Map_panel_open_location_panel_Click(object sender, RoutedEventArgs e)
        {
             Message.NoReturnBoxAsync("Cette fonctionnalité fait l'objet d'une prochaine mise à jour, elle n'as pas encore été ajoutée à cette version !", "Erreur");
        }

        private void Download_panel_close_button_Click(object sender, RoutedEventArgs e)
        {
            Download_panel_close();
        }

        private void Layer_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (layer_searchbar.Text == "Rechercher un calque, un site...")
            {
                layer_searchbar.Text = "";
            }
        }

        private void Layer_searchbar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    last_search = "";
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
                layer_searchbar.Text = "Rechercher un calque, un site...";
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
            else
            {
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private void Layer_searchbar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchLayerStart();
        }

        public void UpdateNotification(Notification sender,(string NotificationId, string Destinateur) NotificationInternalArgs)
        {
            if (sender is not null)
            {
                if (NotificationInternalArgs.Destinateur != "MainPage")
                {
                    return;
                }
                Grid ContentGrid = sender.Get();
                if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
                {
                    Grid NotificationZoneContentGrid = Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                    NotificationZone.Children.Remove(NotificationZoneContentGrid);
                    NotificationZone.Children.Insert(Math.Min(sender.InsertPosition, NotificationZone.Children.Count), ContentGrid);
                    NotificationZoneContentGrid.Children.Clear();
                    NotificationZoneContentGrid = null;
                }
                else
                {
                    ContentGrid.Opacity = 0;
                    var doubleAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond));
                    ContentGrid.BeginAnimation(UIElement.OpacityProperty, doubleAnimation);
                    sender.InsertPosition = NotificationZone.Children.Add(ContentGrid);
                }
            }
            else if (Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId) != null)
            {
                Grid ContentGrid = Collectif.FindChild<Grid>(NotificationZone, NotificationInternalArgs.NotificationId);
                var doubleAnimation = new DoubleAnimation(ContentGrid.ActualHeight, 0, new Duration(TimeSpan.FromSeconds(0.1)));
                doubleAnimation.Completed += (sender, e) =>
                {
                    ContentGrid?.Children?.Clear();
                    ContentGrid = null;
                    NotificationZone.Children.Remove(ContentGrid);
                };
                ContentGrid.BeginAnimation(Grid.MaxHeightProperty, doubleAnimation);
            }
        }

        public void MapLoad()
        {
            mapviewer.MapLayer = new MapTileLayer();
            NO_PIN.Visibility = Settings.visibility_pins;
            SE_PIN.Visibility = Settings.visibility_pins;

            mapviewer.Center = new Location((Settings.NO_PIN_starting_location_latitude + Settings.SE_PIN_starting_location_latitude) / 2, (Settings.NO_PIN_starting_location_longitude + Settings.SE_PIN_starting_location_longitude) / 2);
            mapviewer.ZoomLevel = Settings.map_defaut_zoom_level;
            mapviewer.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(255,
                (byte)Settings.background_layer_color_R,
                (byte)Settings.background_layer_color_G,
                (byte)Settings.background_layer_color_B)
            );
            Action OnLocationUpdated = () =>
            {
                var ActiveRectangleSelection = mapSelectable.GetRectangleLocation();
                NO_PIN.Location = ActiveRectangleSelection.NO;
                SE_PIN.Location = ActiveRectangleSelection.SE;
                Commun.Map.CurentSelection.NO_Latitude = ActiveRectangleSelection.NO.Latitude;
                Commun.Map.CurentSelection.NO_Longitude = ActiveRectangleSelection.NO.Longitude;
                Commun.Map.CurentSelection.SE_Latitude = ActiveRectangleSelection.SE.Latitude;
                Commun.Map.CurentSelection.SE_Longitude = ActiveRectangleSelection.SE.Longitude;
                if (Settings.visibility_pins == Visibility.Visible)
                {
                    NO_PIN.Content = "Latitude = " + Math.Round(NO_PIN.Location.Latitude, 6).ToString() + "\nLongitude = " + Math.Round(NO_PIN.Location.Longitude, 6).ToString();
                    SE_PIN.Content = "Latitude = " + Math.Round(SE_PIN.Location.Latitude, 6).ToString() + "\nLongitude = " + Math.Round(SE_PIN.Location.Longitude, 6).ToString();
                }
            };
            mapSelectable.OnLocationUpdated += (o,e) => OnLocationUpdated();
            OnLocationUpdated();
        }

        public void MapViewerSetSelection(Dictionary<string, double> locations, bool ZoomToNewLocation = true)
        {
            var ActiveRectangleSelection = mapSelectable.GetRectangleLocation();
            if (ActiveRectangleSelection.NO.Latitude != locations["NO_Latitude"] &&
                ActiveRectangleSelection.NO.Longitude != locations["NO_Longitude"] &&
                ActiveRectangleSelection.SE.Latitude != locations["SE_Latitude"] &&
                ActiveRectangleSelection.SE.Longitude != locations["SE_Longitude"])
            {
                ActiveRectangleSelection.NO = new Location(locations["NO_Latitude"], locations["NO_Longitude"]);
                ActiveRectangleSelection.SE = new Location(locations["SE_Latitude"], locations["SE_Longitude"]);
                mapSelectable.SetRectangleLocation(ActiveRectangleSelection.NO, ActiveRectangleSelection.SE);
            }


            if (ZoomToNewLocation)
            {
                ActiveRectangleSelection = mapSelectable.GetRectangleLocation();
                Point NO_Point_Bounds = mapviewer.LocationToView(ActiveRectangleSelection.NO);
                Point SE_Point_Bounds = mapviewer.LocationToView(ActiveRectangleSelection.SE);
                double pourcentage_Lat = Math.Abs((ActiveRectangleSelection.NO.Latitude - ActiveRectangleSelection.SE.Latitude) / 2);
                double pourcentage_Lng = Math.Abs((ActiveRectangleSelection.NO.Latitude - ActiveRectangleSelection.SE.Latitude) / 2);
                Location NO_Location_Bounds = new Location(ActiveRectangleSelection.NO.Latitude - pourcentage_Lat, ActiveRectangleSelection.NO.Longitude - pourcentage_Lng);
                Location SE_Location_Bounds = new Location(pourcentage_Lat + ActiveRectangleSelection.SE.Latitude, pourcentage_Lng + ActiveRectangleSelection.SE.Longitude);
                mapviewer.ZoomToBounds(new BoundingBox(NO_Location_Bounds.Latitude, NO_Location_Bounds.Longitude, SE_Location_Bounds.Latitude, SE_Location_Bounds.Longitude));
            }
        }
    }


















}
