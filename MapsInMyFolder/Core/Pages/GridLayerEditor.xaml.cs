using CefSharp;
using MapsInMyFolder.Commun;
using MapsInMyFolder.Commun.Capabilities;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

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

        public new bool IsInitialized { get; private set; }

        private static ObservableCollection<Layers> _items;

        public static Func<IEnumerable<Layers>> GetLayersListMethod
        {
            get
            {
                static IEnumerable<Layers> func()
                {
                    return _items;
                }
                return func;
            }
        }

        private async Task GetDataAsync()
        {
            _items = new ObservableCollection<Layers>();
            var data = await WMTSParser.ParseAsync();
            foreach (Layers layers in data)
            {
                _items.Add(layers);
            }


            LayerGrid.ItemsSource = _items;
        }
        public async void Init()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                await GetDataAsync();
                LayerPanel.Init();
                MapFigures = new MapFigures();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
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

        private void MapLocationSearchBar_SearchLostFocusRequest(object sender, EventArgs e)
        {
            LayerPanel.LayerBrowser.Focus();
        }

        private async void MapLocationSearchBar_SearchResultEvent(object sender, UserControls.SearchLocation.SearchResultEventArgs e)
        {
            MapPanel.SetLocation(searchResult, e.SearchResultLocation);
            if (e.MapViewerBoundingBox != null)
            {
                mapviewer.ZoomToBounds(e.MapViewerBoundingBox);
            }
            await Task.Delay((int)Settings.animations_duration_millisecond);
            LayerTilePreview_RequestUpdate();
        }

        private void LayerPanel_OpenEditLayerPageEvent(object sender, UserControls.LayersPanel.LayerIdEventArgs e)
        {
            if (e.Args is not CustomOrEditLayersPage.EditingMode EditMode)
            {
                EditMode = CustomOrEditLayersPage.EditingMode.New;
            }
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                CustomOrEditLayersPage EditPage = MainWindow.Instance.FrameLoad_CustomOrEditLayers(e.LayerId, EditMode);
                EditPage.OnSaveLayerEvent += EditPage_SaveLayerEvent;
                EditPage.OnInitEvent += EditPage_OnInitEvent;
                EditPage.OnLeaveEvent += EditPage_OnLeaveEvent;
                EditPage.Init();
                void EditPage_OnLeaveEvent(object sender, EventArgs e)
                {
                    EditPage.OnSaveLayerEvent -= EditPage_SaveLayerEvent;
                    EditPage.OnInitEvent -= EditPage_OnInitEvent;
                    EditPage.OnLeaveEvent -= EditPage_OnLeaveEvent;
                }

            }, null);
        }
        private void EditPage_OnInitEvent(object sender, Layers.LayersEventArgs e)
        {
            CustomOrEditLayersPage EditPage = sender as CustomOrEditLayersPage;
            e.Layer = _items.Where(l => l.Id == EditPage.LayerId).FirstOrDefault();
        }

        private void EditPage_SaveLayerEvent(object sender, Layers.LayersEventArgs e)
        {
            var OriginalLayer = _items.Where(l => l.Id == e.Layer.Id).FirstOrDefault();
            var Index = _items.IndexOf(OriginalLayer);
            _items.Remove(OriginalLayer);
            _items.Insert(Index, e.Layer);
            e.Cancel = true;
        }


    }
}
