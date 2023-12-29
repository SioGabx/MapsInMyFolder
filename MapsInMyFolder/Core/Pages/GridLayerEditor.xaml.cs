using CefSharp;
using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        public static Func<IEnumerable<Layers>> GetLayersListMethod
        {
            get
            {
                Func<IEnumerable<Layers>> func = () =>
                {
                    return _items;
                };
                return func;
            }
        }

        private void GetData()
        {
            _items = new ObservableCollection<Layers>();

            foreach (Layers layers in Layers.GetLayersList())
            {
                if (layers.SiteName.StartsWith("G"))
                _items.Add(layers);
            }
            LayerGrid.ItemsSource = _items;
        }

        private void LayerGrid_Loaded(object sender, RoutedEventArgs e)
        {
            GetData();
            LayerPanel.Init();
            MapFigures = new MapFigures();
        }

        private void Layer_browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                //layer_browser.GetMainFrame().EvaluateScriptAsync(LayerGetDefaultSelectByIdScript());
            }
        }
        private static MapFigures MapFigures;
        private void Mapviewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MapFigures.UpdateFiguresFromZoomLevel(mapviewer.TargetZoomLevel);
            Collectif.AnimateLabel(ZoomLevelIndicator);
        }

        public void LayerTilePreview_RequestUpdate()
        {
            var bbox = mapviewer.ViewRectToBoundingBox(new Rect(0, 0, mapviewer.ActualWidth, mapviewer.ActualHeight));
            Map.CurentView.NO_Latitude = bbox.North;
            Map.CurentView.NO_Longitude = bbox.West;
            Map.CurentView.SE_Latitude = bbox.South;
            Map.CurentView.SE_Longitude = bbox.East;

            LayerPanel.LayerBrowser.ExecuteScriptAsyncWhenPageLoaded("UpdatePreview();");
            return;
        }

        private void mapLocationSearchBar_SearchLostFocusRequest(object sender, EventArgs e)
        {
            LayerPanel.LayerBrowser.Focus();
        }

        private async void mapLocationSearchBar_SearchResultEvent(object sender, UserControls.SearchLocation.SearchResultEventArgs e)
        {
            MapPanel.SetLocation(searchResult, e.SearchResultLocation);
            if (e.MapViewerBoundingBox != null)
            {
                mapviewer.ZoomToBounds(e.MapViewerBoundingBox);
            }
            await Task.Delay((int)Settings.animations_duration_millisecond);
            LayerTilePreview_RequestUpdate();
        }
    }
}
