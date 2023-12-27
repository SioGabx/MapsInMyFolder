using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace MapsInMyFolder.UserControls
{
    /// <summary>
    /// Logique d'interaction pour SearchLocation.xaml
    /// </summary>
    public partial class SearchLocation : UserControl
    {
        public SearchLocation()
        {
            InitializeComponent();
        }

        // Dependency property to control the visibility of the button
        public static readonly DependencyProperty SearchResultPushpinVisibilityProperty =
            DependencyProperty.Register(
                "SearchResultPushpinVisibility",
                typeof(Visibility),
                typeof(SearchLocation),
                new PropertyMetadata(Visibility.Hidden));

        public Visibility SearchResultPushpinVisibility
        {
            get { return (Visibility)GetValue(SearchResultPushpinVisibilityProperty); }
            set { SetValue(SearchResultPushpinVisibilityProperty, value); }
        }

        public static readonly DependencyProperty CurrentMapViewerCenterProperty =
          DependencyProperty.Register(
              "CurrentMapViewerCenter",
              typeof(Location),
              typeof(SearchLocation),
              new PropertyMetadata(new Location(0,0)));

        public Location CurrentMapViewerCenter
        {
            get { return (Location)GetValue(CurrentMapViewerCenterProperty); }
            set { SetValue(CurrentMapViewerCenterProperty, value); }
        }

        public class SearchResultEventArgs : EventArgs
        {
            public Location SearchResultLocation { get; }
            public BoundingBox MapViewerBoundingBox { get; }

            public SearchResultEventArgs(Location searchResultLocation, BoundingBox mapViewerBoundingBox)
            {
                SearchResultLocation = searchResultLocation;
                MapViewerBoundingBox = mapViewerBoundingBox;
            }
        }
        public delegate void SearchResultEventHandler(object sender, SearchResultEventArgs e);

        public event SearchResultEventHandler SearchResultEvent;
        public event EventHandler SearchLostFocusRequest;
        protected virtual void OnSearchResultEvent(Location searchResultLocation, BoundingBox mapViewerBoundingBox)
        {
            SearchResultEvent?.Invoke(this, new SearchResultEventArgs(searchResultLocation, mapViewerBoundingBox));
        }

        private void MapSearchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (mapSearchbar.Text == Languages.Current["searchMapPlaceholder"])
            {
                mapSearchbar.Text = "";
            }

            SearchStart();
            mapSearchbarSuggestion.Visibility = Visibility.Visible;
            mapSearchbarOverflow.Visibility = Visibility.Hidden;
        }

        private void MapSearchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            mapSearchbarSuggestion.Visibility = Visibility.Hidden;
            mapSearchbarOverflow.Visibility = Visibility.Visible;

            if (string.IsNullOrWhiteSpace(mapSearchbar.Text))
            {
                SearchResultPushpinVisibility = Visibility.Hidden;
                mapSearchbar.Text = Languages.Current["searchMapPlaceholder"];
                mapSearchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
        }

        private readonly System.Timers.Timer mapSearchbarTimer = new System.Timers.Timer(500);

        private void MapSearchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mapSearchbar.Text != Languages.Current["searchMapPlaceholder"])
            {
                SearchResultPushpinVisibility = Visibility.Hidden;
                mapSearchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }

            if (string.IsNullOrWhiteSpace(mapSearchbar.Text))
            {
                mapSearchbarSuggestion.Height = 0;
                mapSearchbarSuggestion.ItemsSource = new List<string>();
            }

            mapSearchbarTimer.Stop();
            mapSearchbarTimer.Elapsed += MapSearchbarTimer_Elapsed_StartSearch;
            mapSearchbarTimer.AutoReset = false;
            mapSearchbarTimer.Enabled = true;
        }

        private void MapSearchbarTimer_Elapsed_StartSearch(object source, EventArgs e)
        {
            SearchStart();
        }

        private string lastSearch = Languages.Current["searchMapPlaceholder"];

        private async void SearchStart(bool selectFirst = false)
        {
            string text = "";
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                text = mapSearchbar.Text.Trim();

                if (text != "" && text != lastSearch)
                {
                    lastSearch = text;
                    (List<string> ListOfAddresses, Dictionary<int, SearchEngineResult> SearchEngineResult) searchResults = (null, null);
                    var userMapLocation = CurrentMapViewerCenter;
                    await Task.Run(() => searchResults = SearchEngine.Search(text, userMapLocation.Latitude, userMapLocation.Longitude));
                    if (lastSearch != text)
                    {
                        return;
                    }

                    List<string> searchListResult = searchResults.ListOfAddresses;

                    if (searchListResult != null && searchListResult.Count > 0)
                    {

                        SearchEngineResult.SetSearchResults(searchResults.SearchEngineResult);
                        mapSearchbarSuggestion.Foreground = System.Windows.Media.Brushes.White;
                        mapSearchbarSuggestion.ItemsSource = searchListResult;
                        mapSearchbarSuggestion.Height = searchListResult.Count * 35;

                        if (selectFirst)
                        {
                            SetSelection(0);
                        }
                    }
                    else
                    {
                        //Aucun résultat
                        SearchEngineResult.ClearSearchResults();
                        mapSearchbarSuggestion.Height = 35;
                        mapSearchbarSuggestion.ItemsSource = new List<string> { Languages.Current["searchMapResultNone"] };
                        mapSearchbarSuggestion.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
                    }
                }
            }, null);
        }

        private void MapSearchbarSuggestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = mapSearchbarSuggestion.SelectedIndex;
            SetSelection(index);
        }

        private void SetSelection(int index)
        {
            if (index >= 0)
            {
                SearchEngineResult selectedSearchResult = SearchEngineResult.GetResultById(index);

                if (selectedSearchResult != null)
                {
                    mapSearchbar.Text = selectedSearchResult.DisplayName;
                    SearchResultPushpinVisibility = Visibility.Visible;
                    
                    Location searchResultLocation = new Location(Convert.ToDouble(selectedSearchResult.Latitude), Convert.ToDouble(selectedSearchResult.Longitude));
                    BoundingBox mapviewerBoundingBox = null;
                    if (!string.IsNullOrEmpty(selectedSearchResult.BoundingBox))
                    {
                        string[] boundingBox = selectedSearchResult.BoundingBox.Split(',');
                        //mapviewer.ZoomToBounds();
                        mapviewerBoundingBox = new BoundingBox(Convert.ToDouble(boundingBox[0]),
                                                               Convert.ToDouble(boundingBox[2]),
                                                               Convert.ToDouble(boundingBox[1]),
                                                               Convert.ToDouble(boundingBox[3]));

                        //LayerTilePreview_RequestUpdate();
                        //await Task.Delay((int)Math.Round(Settings.animations_duration_millisecond * 0.7));
                        //LayerTilePreview_RequestUpdate();
                        //await Task.Delay((int)Math.Round(Settings.animations_duration_millisecond * 0.3));
                        //LayerTilePreview_RequestUpdate();
                    }

                    OnSearchResultEvent(searchResultLocation, mapviewerBoundingBox);
                }
            }
        }

        private void MapSearchbar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    lastSearch = "";
                    SearchStart(true);
                    SearchLostFocusRequest.Invoke(this, EventArgs.Empty);
                }
                catch { }
            }

            if (e.Key == Key.Escape)
            {
                SearchLostFocusRequest.Invoke(this, EventArgs.Empty);
            }
        }

       

    }
}
