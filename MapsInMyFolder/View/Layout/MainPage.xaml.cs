using MapsInMyFolder.Core.Layers;
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

namespace MapsInMyFolder.View.Layout
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : System.Windows.Controls.Page
    {
        public MainPage() { 
            InitializeComponent();

            Layer.CurrentLayerChanged += Layer_CurrentLayerChanged;
        }

        private void Layer_CurrentLayerChanged(object sender, Layer.LayerChangedEventArgs e)
        {
            MapViewer.SetMapLayer(e.NewLayer, Layer.Default);
        }

        private void Start_Download_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Layer.Current = new Layer()
            {
                Name = "Géoportail - Parcelles cadastrales",
                Description = "Tracé noir sur fond transparent",
                Tags = "Urbanisme; Parcelles",
                Identifier = "CADASTRALPARCELS.PARCELS",
                TileUrl = "http://wxs.ign.fr/an7nvfzojv5wa96dsga5nk8w/geoportail/wmts?layer=CADASTRALPARCELS.PARCELS&style=bdparcellaire&tilematrixset=PM&Service=WMTS&Request=GetTile&Version=1.0.0&Format=image/png&TileMatrix={z}&TileCol={x}&TileRow={y}",
                MinZoom = 0, MaxZoom = 20,
                TilesFormat = Format.png,
                SiteName = "Geoportail",
                SiteUrl = "geoportail.gouv.fr",
                TileSize = 256,
            };
        }

        private void Map_panel_open_location_panel_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void Download_panel_close_button_Click(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void MapLocationSearchBar_SearchLostFocusRequest(object sender, EventArgs e)
        {

        }

        private void MapLocationSearchBar_SearchResultEvent(object sender, Modules.SearchLocation.SearchResultEventArgs e)
        {

        }

        private void MapViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }
    }
}
