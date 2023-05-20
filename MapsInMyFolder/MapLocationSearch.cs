using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        private readonly SearchEngine searchEngine = new SearchEngine();

        public List<string> Search(string search)
        {
            try
            {
                List<string> returnListOfAddresses = new List<string>();
                string encodedSearch = System.Web.HttpUtility.UrlEncode(search.Trim());
                string url = "https://nominatim.openstreetmap.org/search.php?q=" + encodedSearch + "&polygon_geojson=1&limit=10&format=xml&email=siogabx@siogabx.fr";

                using (HttpClient client = new HttpClient())
                {
                    using HttpResponseMessage response = client.GetAsync(url).Result;
                    using Stream responseStream = response.Content.ReadAsStream();

                    response.EnsureSuccessStatusCode();

                    using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(responseStream))
                    {
                        searchEngine.ClearSearchResults();
                        reader.MoveToContent();
                        int idSearch = 0;

                        while (reader.Read())
                        {
                            string address = reader.GetAttribute("display_name");

                            if (!string.IsNullOrEmpty(address))
                            {
                                string latitude = reader.GetAttribute("lat");
                                string longitude = reader.GetAttribute("lon");
                                string boundingBox = reader.GetAttribute("boundingbox");
                                returnListOfAddresses.Add(address);
                                searchEngine.Add(new SearchEngineResult(idSearch, address, latitude, longitude, boundingBox), idSearch);
                                idSearch++;
                            }
                        }
                    }
                }

                return returnListOfAddresses;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        private void MapSearchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (mapSearchbar.Text == "Rechercher un lieu...")
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
                mapSearchbar.Text = "Rechercher un lieu...";
                mapSearchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
        }

        private readonly System.Timers.Timer mapSearchbarTimer = new System.Timers.Timer(500);

        private void MapSearchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (mapSearchbar.Text != "Rechercher un lieu...")
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

        private string lastSearch = "";

        private async void SearchStart(bool selectFirst = false)
        {
            string text = "";
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                text = mapSearchbar.Text.Trim();

                if (text != "" && text != lastSearch)
                {
                    lastSearch = text;
                    List<string> listOfSearchResults = new List<string>();
                    Task searchTask = Task.Run(() => listOfSearchResults = Search(text));
                    await searchTask;

                    if (listOfSearchResults.Count > 0)
                    {
                        mapSearchbarSuggestion.Foreground = System.Windows.Media.Brushes.White;
                        mapSearchbarSuggestion.ItemsSource = listOfSearchResults;
                        mapSearchbarSuggestion.Height = listOfSearchResults.Count * 35;

                        if (selectFirst)
                        {
                            SetSelection(0);
                        }
                    }
                    else
                    {
                        mapSearchbarSuggestion.Height = 35;
                        mapSearchbarSuggestion.ItemsSource = new List<string> { "Aucun résultat" };
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
                SearchEngineResult selectedSearchResult = searchEngine.GetResultById(index);

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

    public class SearchEngineResult
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string BoundingBox { get; set; }

        public SearchEngineResult(int id = -1, string displayName = "", string latitude = "", string longitude = "", string boundingBox = "")
        {
            if (id != -1)
            {
                Id = id;
                DisplayName = displayName;
                Latitude = latitude;
                Longitude = longitude;
                BoundingBox = boundingBox;
            }
        }
    }

    public class SearchEngine
    {
        public List<Dictionary<int, SearchEngineResult>> SearchResultList { get; } = new List<Dictionary<int, SearchEngineResult>>();

        public int Add(SearchEngineResult engineResult, int id)
        {
            int numberOfEnginesInList = SearchResultList.Count + 1;
            Dictionary<int, SearchEngineResult> tempDictionary = new Dictionary<int, SearchEngineResult>
            {
                { id, engineResult }
            };

            SearchResultList.Add(tempDictionary);
            return numberOfEnginesInList;
        }

        public void ClearSearchResults()
        {
            SearchResultList.Clear();
        }

        public List<SearchEngineResult> GetResultList()
        {
            List<SearchEngineResult> resultList = new List<SearchEngineResult>();

            foreach (Dictionary<int, SearchEngineResult> searchDictionary in SearchResultList)
            {
                SearchEngineResult value = searchDictionary.Values.First();
                resultList.Add(value);
            }

            return resultList;
        }

        public SearchEngineResult GetResultById(int id)
        {
            foreach (Dictionary<int, SearchEngineResult> searchDictionary in SearchResultList)
            {
                try
                {
                    if (searchDictionary.Keys.First() == id)
                    {
                        SearchEngineResult returnSearchResult = searchDictionary[id];
                        return returnSearchResult;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Erreur : l'id n'existe pas.");
                }
            }

            return null;
        }
    }
}
