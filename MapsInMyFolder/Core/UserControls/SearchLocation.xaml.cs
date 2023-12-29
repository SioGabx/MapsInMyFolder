using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ToggleBarOnChange();
        }

        public static readonly DependencyProperty SearchResultPushpinProperty =
            DependencyProperty.Register(
                "SearchResultPushpin",
                typeof(Pushpin),
                typeof(SearchLocation),
                new PropertyMetadata(null));

        public Pushpin SearchResultPushpin
        {
            get { return (Pushpin)GetValue(SearchResultPushpinProperty); }
            set { SetValue(SearchResultPushpinProperty, value); }
        }

        public static readonly DependencyProperty SearchResultMapProperty =
            DependencyProperty.Register(
                "SearchResultMap",
                typeof(MapControl.Map),
                typeof(SearchLocation),
                new PropertyMetadata(null));

        public MapControl.Map SearchResultMap
        {
            get { return (MapControl.Map)GetValue(SearchResultMapProperty); }
            set { SetValue(SearchResultMapProperty, value); }
        }

       
        public static readonly DependencyProperty IsFloatingSearchBarProperty =
            DependencyProperty.Register(
                "IsFloatingSearchBar",
                typeof(bool),
                typeof(SearchLocation),
                new PropertyMetadata(false));

        public bool IsFloatingSearchBar
        {
            get { return (bool)GetValue(IsFloatingSearchBarProperty); }
            set { SetValue(IsFloatingSearchBarProperty, value); }
        }


        private static readonly DependencyProperty IsFloatingSearchBarVisibleProperty =
           DependencyProperty.Register(
               "IsFloatingSearchBarVisible",
               typeof(bool),
               typeof(SearchLocation),
               new PropertyMetadata(false));
        private bool IsFloatingSearchBarVisible
        {
            get { return (bool)GetValue(IsFloatingSearchBarVisibleProperty); }
            set { SetValue(IsFloatingSearchBarVisibleProperty, value); ToggleBarOnChange(); }
        }

        public void ToggleBarOnChange()
        {
            if (!IsFloatingSearchBar || IsFloatingSearchBarVisible)
            {
                mapSearchbarGrid.Visibility = Visibility.Visible;
                mapSearchbarToggle.Visibility = Visibility.Collapsed;
            }
            else
            {
                mapSearchbarGrid.Visibility = Visibility.Collapsed;
                mapSearchbarToggle.Visibility = Visibility.Visible;
            }
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
                SetPushpinVisibility(Visibility.Hidden);
                mapSearchbar.Text = Languages.Current["searchMapPlaceholder"];
                mapSearchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
                IsFloatingSearchBarVisible = false;
            }
        }

        public void SetPushpinVisibility(Visibility visibility)
        {
            if (SearchResultPushpin != null)
            {
                SearchResultPushpin.Visibility = visibility;
            }

        }


        private readonly System.Timers.Timer mapSearchbarTimer = new System.Timers.Timer(500);

        private void MapSearchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mapSearchbar.Text != Languages.Current["searchMapPlaceholder"])
            {
                SetPushpinVisibility(Visibility.Hidden);
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

        public Location GetMapLocation()
        {
            return SearchResultMap?.Center ?? new Location(0,0);
        }

        private async void SearchStart(bool selectFirst = false)
        {
            string text = "";
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                text = mapSearchbar.Text.Trim();

                if (text != "" && text != lastSearch)
                {
                    lastSearch = text;
                    (List<string> ListOfAddresses, Dictionary<int, MapLocationSearchEngineResult> SearchEngineResult) searchResults = (null, null);
                    var userMapLocation = GetMapLocation();
                    await Task.Run(() => searchResults = MapLocationSearchEngine.Search(text, userMapLocation.Latitude, userMapLocation.Longitude));
                    if (lastSearch != text)
                    {
                        return;
                    }

                    List<string> searchListResult = searchResults.ListOfAddresses;

                    if (searchListResult != null && searchListResult.Count > 0)
                    {

                        MapLocationSearchEngineResult.SetSearchResults(searchResults.SearchEngineResult);
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
                        MapLocationSearchEngineResult.ClearSearchResults();
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
                MapLocationSearchEngineResult selectedSearchResult = MapLocationSearchEngineResult.GetResultById(index);

                if (selectedSearchResult != null)
                {
                    mapSearchbar.Text = selectedSearchResult.DisplayName;
                    SetPushpinVisibility(Visibility.Visible);

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
                    }
                    SetMapView(searchResultLocation, mapviewerBoundingBox);
                    OnSearchResultEvent(searchResultLocation, mapviewerBoundingBox);
                    IsFloatingSearchBarVisible = false;
                }
            }
        }

        public async void SetMapView(Location searchResultLocation, BoundingBox mapViewerBoundingBox)
        {
            if (SearchResultPushpin != null)
            {
                MapPanel.SetLocation(SearchResultPushpin, searchResultLocation);
            }
           
            if (SearchResultMap != null && mapViewerBoundingBox != null)
            {
                SearchResultMap.ZoomToBounds(mapViewerBoundingBox);
            }
            await Task.Delay((int)Settings.animations_duration_millisecond);
        }


        private void MapSearchbar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    lastSearch = "";
                    SearchStart(true);
                    SearchLostFocusRequest?.Invoke(this, EventArgs.Empty);
                }
                catch { }
            }

            if (e.Key == Key.Escape)
            {
                SearchLostFocusRequest?.Invoke(this, EventArgs.Empty);
                IsFloatingSearchBarVisible = false;
            }
        }

        private void MapSearchbarToggle_Click(object sender, RoutedEventArgs e)
        {
            IsFloatingSearchBarVisible = true;
            mapSearchbar.Focus();
            mapSearchbar.CaretIndex = mapSearchbar.Text.Length;
        }

    }
}
