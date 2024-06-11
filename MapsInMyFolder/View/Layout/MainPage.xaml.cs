using MapsInMyFolder.Core.Database;
using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MapsInMyFolder.View.Layout
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : System.Windows.Controls.Page, INotifyPropertyChanged
    {
        private ObservableCollection<Layer> _layerCollection;

        public ObservableCollection<Layer> LayerCollection
        {
            get { return _layerCollection; }
            set
            {
                _layerCollection = value;
                OnPropertyChanged(nameof(LayerCollection));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public MainPage()
        {
            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Layer.CurrentLayerChanged += Layer_CurrentLayerChanged;
            LoadLayers();
            Layer.Current = Layer.Default;
        }


        public void LoadLayers()
        {
            Database database = new Database()
            {
                Path = @".\SampleDb.db",
                AvailableTables = Tables.LAYERS,
            };

            LayerCollection = Core.Layers.Loader.LoadFromDatabase(database, Tables.LAYERS).ToObservableCollection();

        }

        private void LayerPanel_SelectedLayerChanged(object sender, Layer.LayerChangedEventArgs e)
        {
            Layer.Current = e.NewLayer;
        }

        private void Layer_CurrentLayerChanged(object sender, Layer.LayerChangedEventArgs e)
        {
            Debug.WriteLine("Layer_CurrentLayerChanged");
            MapViewer.SetMapLayer(e.NewLayer, Layer.Default);
            LayerPanel.CurrentSelectedLayer = e.NewLayer;
        }

        private void Start_Download_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Layer.Current = LayerCollection.First();
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
