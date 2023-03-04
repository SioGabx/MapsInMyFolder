using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour FullscreenMap.xaml
    /// </summary>
    public partial class FullscreenMap : Page
    {
        public FullscreenMap()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Layers Layer = Layers.GetLayerById(Settings.layer_startup_id) ?? Layers.Empty();
            MapViewer.MapLayer = new MapTileLayer
            {
                TileSource = new TileSource { UriFormat = "https://tile.openstreetmap.org/{z}/{x}/{y}.png", LayerID = Layer.class_id },
                SourceName = Layer.class_identifiant,
                MaxZoomLevel = Layer.class_max_zoom,
                MinZoomLevel = Layer.class_min_zoom,
                Description = ""
            };

            MapSelectable mapSelectable = new MapSelectable(MapViewer, new Location(Settings.NO_PIN_starting_location_latitude, Settings.NO_PIN_starting_location_longitude), new Location(Settings.SE_PIN_starting_location_latitude, Settings.SE_PIN_starting_location_longitude), null, this);


        }
    }
}
