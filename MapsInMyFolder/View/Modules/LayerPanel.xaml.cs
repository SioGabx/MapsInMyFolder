using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapsInMyFolder.View.Modules
{
    /// <summary>
    /// Logique d'interaction pour LayerPanel.xaml
    /// </summary>
    public partial class LayerPanel : UserControl
    {
        public static readonly DependencyProperty LayersSourceProperty =
            DependencyProperty.Register("LayersSource", typeof(ObservableCollection<Layer>), typeof(LayerPanel), new PropertyMetadata(null));
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


        private void TextBlock_Unloaded(object sender, RoutedEventArgs e)
        {

            Debug.WriteLine("Unloaded of " + (sender as TextBlock)?.Text);
        }

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {

            Debug.WriteLine("Load of " + (sender as TextBlock)?.Text);
        }
    }
}

