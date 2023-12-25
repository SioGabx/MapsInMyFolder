using MapsInMyFolder.Commun;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
