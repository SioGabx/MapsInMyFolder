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
            string dataURL = @"http://wxs.ign.fr/an7nvfzojv5wa96dsga5nk8w/geoportail/wmts?SERVICE=WMTS&REQUEST=GetCapabilities";
            var data = await WMTSParser.ParseAsync(dataURL);
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
                int StartId = _items.FirstOrDefault(null as Layers)?.Id ?? 1;
                Layers.SetCurrentLayer(StartId);
                //(int)Layers.ReservedId.TempLayerGeneric
                LayerPanel.Init();
                MapFigures = new MapFigures();
                MapLoad(); 
                SetMapLayer(StartId);
            }
        }

        public void MapLoad()
        {
            mapviewer.MapLayer = new MapTileLayer();
            NO_PIN.Visibility = Settings.visibility_pins;
            SE_PIN.Visibility = Settings.visibility_pins;

            if (mapviewer.Center.Latitude == 0 && mapviewer.Center.Longitude == 0)
            {
                mapviewer.Center = new Location((Settings.NO_PIN_starting_location_latitude + Settings.SE_PIN_starting_location_latitude) / 2, (Settings.NO_PIN_starting_location_longitude + Settings.SE_PIN_starting_location_longitude) / 2);
                mapviewer.ZoomLevel = Settings.map_defaut_zoom_level;
            }
            mapviewer.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(255,
                (byte)Settings.background_layer_color_R,
                (byte)Settings.background_layer_color_G,
                (byte)Settings.background_layer_color_B)
            );
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

        private void LayerPanel_SetCurrentLayerEvent(object sender, UserControls.LayersPanel.LayerIdEventArgs e)
        {
            SetMapLayer(e.LayerId);
        }

        public void SetMapLayer(int id)
        {
            Layers.SetMapLayer(_items.GetLayerById(id), mapviewer, MapTileLayer_Transparent, MapFigures, mapviewerRectangles);
        }

        private void Mapviewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MapFigures?.UpdateFiguresFromZoomLevel(mapviewer?.TargetZoomLevel ?? 0);
            Collectif.AnimateLabel(ZoomLevelIndicator);
            LayerTilePreview_RequestUpdate();
        }

        public void LayerTilePreview_RequestUpdate()
        {
            var bbox = mapviewer.ViewRectToBoundingBox(new Rect(0, 0, mapviewer.ActualWidth, mapviewer.ActualHeight));
            Map.CurentView.NO_Latitude = bbox.North;
            Map.CurentView.NO_Longitude = bbox.West;
            Map.CurentView.SE_Latitude = bbox.South;
            Map.CurentView.SE_Longitude = bbox.East;
            LayerPanel.PreviewRequestUpdate();
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
            e.Layer = _items.GetLayerById(EditPage.LayerId);
        }

        private void EditPage_SaveLayerEvent(object sender, Layers.LayersEventArgs e)
        {
            var OriginalLayer = _items.GetLayerById(e.Layer.Id);
            var Index = _items.IndexOf(OriginalLayer);
            if (Index == -1)
            {
                //Item is found inside the list (editing)
                _items.Remove(OriginalLayer);
                _items.Insert(Index, e.Layer);
            }
            else
            {
                //Item is NOT found inside the list (adding)
                _items.Add(e.Layer);
            }
            e.Cancel = true;
        }
    }
}
