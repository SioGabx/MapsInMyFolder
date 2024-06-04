using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        public static readonly DependencyProperty CurrentLayerProperty =
           DependencyProperty.Register("CurrentLayer", typeof(Layer), typeof(LayerPanel), new PropertyMetadata(null));

        public Layer CurrentLayer
        {
            get { return (Layer)GetValue(CurrentLayerProperty); }
            set { SetValue(CurrentLayerProperty, value); }
        }


        public LayerPanel()
        {
            InitializeComponent();
        }


        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //CollectionViewSource.GetDefaultView(this.LayersSource).Filter = UserFilter;
            var Colec = (CollectionView)CollectionViewSource.GetDefaultView(this.LayersSource);
            using (Colec.DeferRefresh())
            {
                PropertyGroupDescription groupDescription2 = new PropertyGroupDescription("SiteName");
                if (Colec.GroupDescriptions.Count == 0) { 
                Colec.GroupDescriptions.Add(groupDescription2);
                }
                //SortDescription listSortDescription = new SortDescription("IsFavorite", ListSortDirection.Descending);
                //Colec.SortDescriptions.Add(listSortDescription);
            }
        }


        private void Item_Unloaded(object sender, RoutedEventArgs e)
        {
            ListViewItem ViewItem = sender as ListViewItem;
            var layer = ViewItem.Content as Layer;
            Debug.WriteLine("Unloaded of " + layer?.Name);
        }

        private void Item_Loaded(object sender, RoutedEventArgs e)
        {
            ListViewItem ViewItem = sender as ListViewItem;
            var layer = ViewItem.Content as Layer;
            Debug.WriteLine("Load of " + layer?.Name);
        }

        private void VisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            Button Button = sender as Button;
            var layer = (Button.TemplatedParent as ContentPresenter).Content as Layer;
            layer.Visibility = layer.Visibility == Display.visible ? Display.hidden : Display.visible;
            Debug.WriteLine(layer.Name);
            CollectionViewSource.GetDefaultView(this.LayersSource).Refresh();
        }
        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            Button Button = sender as Button;
            var ContentP = (Button.TemplatedParent as ContentPresenter);
            var layer = ContentP.Content as Layer;
            layer.IsFavorite = !layer.IsFavorite;
            Debug.WriteLine(layer.Name);
            //CollectionViewSource.GetDefaultView(this.LayersSource).Refresh();
        }

        private bool UserFilter(object item)
        {
            var Layer = item as Layer;
            return Layer.MinZoom == 0;
        }
    }
}

