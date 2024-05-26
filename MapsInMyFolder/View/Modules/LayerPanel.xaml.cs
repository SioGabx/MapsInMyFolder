using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
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

namespace MapsInMyFolder.View.Modules
{
    /// <summary>
    /// Logique d'interaction pour LayerPanel.xaml
    /// </summary>
    public partial class LayerPanel : UserControl
    {
        public static readonly DependencyProperty LayersSourceProperty = DependencyProperty.Register(
        "LayersSource",
        typeof(ObservableCollection<Layer>),
        typeof(LayerPanel),
        new PropertyMetadata(null));

        public ObservableCollection<Layer> LayersSource
        {
            get { return (ObservableCollection<Layer>)GetValue(LayersSourceProperty); }
            set { SetValue(LayersSourceProperty, value); }
        }

        public LayerPanel()
        {
            InitializeComponent();
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(LayersSource.Count().ToString());
        }
    }
}
