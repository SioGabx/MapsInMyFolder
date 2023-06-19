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

namespace MapsInMyFolder
{
    public partial class MainPage : Page
    {
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
                searchResult.Visibility = Visibility.Hidden;
                mapSearchbar.Text = Languages.Current["searchMapPlaceholder"];
                mapSearchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
        }

        private readonly System.Timers.Timer mapSearchbarTimer = new System.Timers.Timer(500);

        private void MapSearchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mapSearchbar.Text != Languages.Current["searchMapPlaceholder"])
            {
                searchResult.Visibility = Visibility.Hidden;
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
                    var userMapLocation = mapviewer.Center;
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
                    searchResult.Visibility = Visibility.Visible;
                    MapPanel.SetLocation(searchResult, new Location(Convert.ToDouble(selectedSearchResult.Latitude), Convert.ToDouble(selectedSearchResult.Longitude)));

                    if (!string.IsNullOrEmpty(selectedSearchResult.BoundingBox))
                    {
                        string[] boundingBox = selectedSearchResult.BoundingBox.Split(',');
                        mapviewer.ZoomToBounds(new BoundingBox(Convert.ToDouble(boundingBox[0]),
                                                               Convert.ToDouble(boundingBox[2]),
                                                               Convert.ToDouble(boundingBox[1]),
                                                               Convert.ToDouble(boundingBox[3])));

                        LayerTilePreview_RequestUpdate();
                        mapviewer.Cursor = Cursors.Arrow;
                    }
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
                    layer_browser.Focus();
                }
                catch { }
            }

            if (e.Key == Key.Escape)
            {
                layer_browser.Focus();
            }
        }

        private void SearchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lastSearch = "";
            SearchStart(true);
        }
    }
}
