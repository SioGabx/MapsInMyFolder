using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        private readonly SearchEngine searchEngine = new SearchEngine();

        public List<string> Search(string search)
        {
            try
            {
                searchEngine.ClearSearchResults();
                string encodedSearch = System.Web.HttpUtility.UrlEncode(search.Trim());

                switch (Settings.search_engine)
                {
                    case SearchEngines.BingMaps:
                        //Bing Search
                        return BingMapSearch(encodedSearch);
                    case SearchEngines.OpenStreetMap:
                        //OSM Search
                        return OpenStreetMapSearch(encodedSearch);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return new List<string>();
        }

        public List<string> OpenStreetMapSearch(string encodedSearch)
        {
            //OSM Search
            List<string> returnListOfAddresses = new List<string>();

            string url = $"https://nominatim.openstreetmap.org/search.php?q={encodedSearch}&polygon_geojson=0&limit=5&format=xml&email=siogabx@siogabx.fr";
            Debug.WriteLine(url);
            using HttpResponseMessage response = TileGeneratorSettings.HttpClient.GetAsync(url).Result;
            using Stream responseStream = response.Content.ReadAsStream();

            response.EnsureSuccessStatusCode();

            using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(responseStream))
            {
                reader.MoveToContent();
                int idSearch = 0;

                while (reader.Read())
                {
                    string displayAdress = reader.GetAttribute("display_name");

                    if (!string.IsNullOrEmpty(displayAdress))
                    {
                        string latitude = reader.GetAttribute("lat");
                        string longitude = reader.GetAttribute("lon");
                        string boundingBox = reader.GetAttribute("boundingbox");
                        returnListOfAddresses.Add(displayAdress);
                        searchEngine.Add(new SearchEngineResult(idSearch, displayAdress, latitude, longitude, boundingBox), idSearch);
                        idSearch++;
                    }
                }
            }
            return returnListOfAddresses;
        }

        public List<string> BingMapSearch(string encodedSearch)
        {
            //OSM Search
            List<string> returnListOfAddresses = new List<string>();
            const int MaxResultNumber = 5;
            string BingMapApiKey = ApiKeys.BingMaps;

            if (BingMapApiKey == "YOUR_API_KEY")
            {
#if DEBUG
                MessageBox.Show("Your Bing API Key is not set, please check MapsInMyFolder.Commun/ApiKey.cs. Fallback OpenStreetMapSearch is used");
#endif
                return OpenStreetMapSearch(encodedSearch);
            }

            var userMapLocation = Application.Current.Dispatcher.Invoke(new Func<Location>(() => mapviewer.Center));
            string url = $"https://dev.virtualearth.net/REST/v1/Locations?o=xml&query={encodedSearch}&userLocation={userMapLocation.Latitude},{userMapLocation.Longitude}&maxResults={MaxResultNumber}&key={BingMapApiKey}&culture={CultureInfo.CurrentUICulture.Name}";
            Debug.WriteLine(url);
            using HttpResponseMessage response = new HttpClient().GetAsync(url).Result;
            using Stream responseStream = response.Content.ReadAsStream();
            response.EnsureSuccessStatusCode();

            using (XmlReader reader = System.Xml.XmlReader.Create(responseStream))
            {
                reader.MoveToContent();
                int idSearch = 0;

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "Location")
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        string InnerXML = reader.ReadOuterXml();
                        xmlDoc.LoadXml(InnerXML);

                        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                        nsManager.AddNamespace("ns", "http://schemas.microsoft.com/search/local/ws/rest/v1");

                        string name = getNodeValue("/Location/Name");

                        string latitude = getNodeValue("/Location/Point/Latitude");
                        string longitude = getNodeValue("/Location/Point/Longitude");

                        string southLatitude = getNodeValue("/Location/BoundingBox/SouthLatitude");
                        string westLongitude = getNodeValue("/Location/BoundingBox/WestLongitude");
                        string northLatitude = getNodeValue("/Location/BoundingBox/NorthLatitude");
                        string eastLongitude = getNodeValue("/Location/BoundingBox/EastLongitude");

                        string addressLine = getNodeValue("/Location/Address/AddressLine");
                        string adminDistrict = getNodeValue("/Location/Address/AdminDistrict");
                        string adminDistrict2 = getNodeValue("/Location/Address/AdminDistrict2");
                        string countryRegion = getNodeValue("/Location/Address/CountryRegion");
                        string formattedAddress = getNodeValue("/Location/Address/FormattedAddress");
                        string locality = getNodeValue("/Location/Address/Locality");
                        string postalCode = getNodeValue("/Location/Address/PostalCode");

                        string getNodeValue(string xpath)
                        {
                            XmlNode node = xmlDoc.SelectSingleNode('/' + xpath.Replace("/", "/ns:"), nsManager);
                            return node?.InnerText ?? string.Empty;
                        }

                        string concatIfNotNullOrEmpty(params string[] append)
                        {
                            string baseString = string.Empty;
                            List<string> ConcatetenedElements = new List<string>();
                            foreach (string appendItem in append)
                            {
                                if (!string.IsNullOrEmpty(appendItem) && !ConcatetenedElements.Contains(appendItem) && (string.IsNullOrEmpty(baseString) || !append[0].Contains(appendItem)))
                                {
                                    baseString += ", " + appendItem;
                                    ConcatetenedElements.Add(appendItem);
                                }
                            }
                            return baseString.Trim(',', ' ');

                        }
                        string displayAdress = concatIfNotNullOrEmpty(name, locality, adminDistrict2, adminDistrict, countryRegion);
                        string boundingBox = concatIfNotNullOrEmpty(northLatitude, southLatitude, westLongitude, eastLongitude);
                        returnListOfAddresses.Add(displayAdress);
                        searchEngine.Add(new SearchEngineResult(idSearch, displayAdress, latitude, longitude, boundingBox), idSearch);
                        idSearch++;
                    }
                }
            }
            lastSearch = null;
            return returnListOfAddresses;
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

        private string lastSearch = "Rechercher un lieu...";

        private async void SearchStart(bool selectFirst = false)
        {
            string text = "";
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                text = mapSearchbar.Text.Trim();

                if (text != "" && text != lastSearch)
                {
                    lastSearch = text;
                    List<string> listOfSearchResults = null;
                    await Task.Run(() => listOfSearchResults = Search(text));

                    if (listOfSearchResults != null && listOfSearchResults.Count > 0)
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
