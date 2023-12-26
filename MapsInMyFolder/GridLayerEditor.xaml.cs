using MapsInMyFolder.Commun;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour GridLayerEditor.xaml
    /// </summary>
    public partial class GridLayerEditor : Page
    {
        public GridLayerEditor()
        {
            InitializeComponent();
        }

        private static ObservableCollection<Layers> _items;

        private void GetData()
        {
            _items = new ObservableCollection<Layers>();

            foreach (Layers layers in Layers.GetLayersList())
            {
                _items.Add(layers);
            }
            LayerGrid.ItemsSource = _items;
        }

        private void LayerGrid_Loaded(object sender, RoutedEventArgs e)
        {
            GetData();
        }
    }
}
