using MapsInMyFolder.Core.Geodetic.Search;
using MapsInMyFolder.Core.Layers;
using MapsInMyFolder.View.Controls.Map;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MapsInMyFolder.View.Modules
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
                typeof(Map),
                typeof(SearchLocation),
                new PropertyMetadata(null));

        public Map SearchResultMap
        {
            get { return (Map)GetValue(SearchResultMapProperty); }
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

        private async void MapSearchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (mapSearchbar.Text == GetPlaceHolderString())
            {
                mapSearchbar.Text = "";
            }

            await SearchStart();
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
                mapSearchbar.Text = GetPlaceHolderString();
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
            if (mapSearchbar.Text != GetPlaceHolderString())
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

        private async void MapSearchbarTimer_Elapsed_StartSearch(object source, EventArgs e)
        {
            await SearchStart();
        }

        private string lastSearch = "";

        private string GetPlaceHolderString()
        {
            return "searchMapPlaceholder";
        }

        public Location GetMapLocation()
        {
            return SearchResultMap?.Center ?? new Location(0, 0);
        }

        private async System.Threading.Tasks.Task SearchStart(bool selectFirstResult = false)
        {
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                var Query = mapSearchbar.Text;
                if (string.IsNullOrWhiteSpace(Query) || Query == lastSearch || Query == GetPlaceHolderString())
                {
                    return;
                }

                lastSearch = Query;
                Debug.WriteLine("Search start");
                Location centerLocation = GetMapLocation();

                var SearchResults = await MapsInMyFolder.Core.Geodetic.Search.Search.Query(Query, centerLocation.Latitude, centerLocation.Longitude);
                if (SearchResults != null && SearchResults.Count > 0)
                {
                    mapSearchbarSuggestion.ItemsSource = SearchResults;
                    mapSearchbarSuggestion.Height = SearchResults.Count * 35;
                    if (selectFirstResult)
                    {
                        SelectSearchResult(0);
                    }
                }
                else
                {
                    mapSearchbarSuggestion.ItemsSource = new List<SearchResult> { new SearchResult(SearchResultType.Suggestion, "Aucun résultats", "", "", 0, 0) };
                    mapSearchbarSuggestion.Height = 35;
                }
            }, null);
            //string text = "";
            //await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            //{
            //    text = mapSearchbar.Text.Trim();

            //    if (text != "" && text != lastSearch)
            //    {
            //        lastSearch = text;
            //        (List<string> ListOfAddresses, Dictionary<int, MapLocationSearchEngineResult> SearchEngineResult) searchResults = (null, null);
            //        var userMapLocation = GetMapLocation();
            //        await Task.Run(() => searchResults = MapLocationSearchEngine.Search(text, userMapLocation.Latitude, userMapLocation.Longitude));
            //        if (lastSearch != text)
            //        {
            //            return;
            //        }

            //        List<string> searchListResult = searchResults.ListOfAddresses;

            //        if (searchListResult != null && searchListResult.Count > 0)
            //        {

            //            MapLocationSearchEngineResult.SetSearchResults(searchResults.SearchEngineResult);
            //            mapSearchbarSuggestion.Foreground = System.Windows.Media.Brushes.White;
            //            mapSearchbarSuggestion.ItemsSource = searchListResult;
            //            mapSearchbarSuggestion.Height = searchListResult.Count * 35;

            //            if (selectFirst)
            //            {
            //                SetSelection(0);
            //            }
            //        }
            //        else
            //        {
            //            //Aucun résultat
            //            MapLocationSearchEngineResult.ClearSearchResults();
            //            mapSearchbarSuggestion.Height = 35;
            //            mapSearchbarSuggestion.ItemsSource = new List<string> { Languages.Current["searchMapResultNone"] };
            //            mapSearchbarSuggestion.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            //        }
            //    }
            //}, null);
        }

        private void MapSearchbarSuggestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = mapSearchbarSuggestion.SelectedIndex;
            SelectSearchResult(index);
        }

        private void SelectSearchResult(int index)
        {
            if (index < 0) { return; }
            if (mapSearchbarSuggestion.Items[index] is SearchResult searchResult)
            {
                Debug.WriteLine(searchResult.Name);
                if (searchResult.Type == SearchResultType.Place)
                {
                    mapSearchbar.Text = searchResult.ToString();
                    SetPushpinVisibility(Visibility.Visible);
                    SetMapView(new Location(searchResult.Latitude, searchResult.Longitude));
                }
            }
            //if (index >= 0)
            //{
            //    MapLocationSearchEngineResult selectedSearchResult = MapLocationSearchEngineResult.GetResultById(index);

            //    if (selectedSearchResult != null)
            //    {
            //        mapSearchbar.Text = selectedSearchResult.DisplayName;
            //        SetPushpinVisibility(Visibility.Visible);

            //        Location searchResultLocation = new Location(Convert.ToDouble(selectedSearchResult.Latitude), Convert.ToDouble(selectedSearchResult.Longitude));
            //        BoundingBox mapviewerBoundingBox = null;
            //        if (!string.IsNullOrEmpty(selectedSearchResult.BoundingBox))
            //        {
            //            string[] boundingBox = selectedSearchResult.BoundingBox.Split(',');
            //            //mapviewer.ZoomToBounds();
            //            mapviewerBoundingBox = new BoundingBox(Convert.ToDouble(boundingBox[0]),
            //                                                   Convert.ToDouble(boundingBox[2]),
            //                                                   Convert.ToDouble(boundingBox[1]),
            //                                                   Convert.ToDouble(boundingBox[3]));
            //        }
            //        SetMapView(searchResultLocation, mapviewerBoundingBox);
            //        OnSearchResultEvent(searchResultLocation, mapviewerBoundingBox);
            //        IsFloatingSearchBarVisible = false;
            //    }
            //}
        }

        public Task SetMapView(Location searchResultLocation)
        {
            if (SearchResultPushpin != null)
            {
                MapPanel.SetLocation(SearchResultPushpin, searchResultLocation);
            }

            if (SearchResultMap != null)
            {
                SearchResultMap.ZoomToBounds(new BoundingBox(searchResultLocation.Latitude, searchResultLocation.Longitude, searchResultLocation.Latitude, searchResultLocation.Longitude));
            }
            return Task.Delay((int)500);
        }


        private async void MapSearchbar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    lastSearch = "";
                    await SearchStart(true);
                    SearchLostFocusRequest?.Invoke(this, EventArgs.Empty);
                   SearchResultMap.Focus();
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
