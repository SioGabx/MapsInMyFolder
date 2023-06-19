using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml;

namespace MapsInMyFolder.Commun
{

    public class SearchEngineResult
    {
        private static Dictionary<int, SearchEngineResult> SearchResultList = new Dictionary<int, SearchEngineResult>();
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




        public static void ClearSearchResults()
        {
            SearchResultList.Clear();
        }

        public static void SetSearchResults(Dictionary<int, SearchEngineResult> newResults)
        {
            SearchResultList = newResults;
        }
        public static Dictionary<int, SearchEngineResult> GetSearchResults()
        {
            return SearchResultList;
        }

        public static SearchEngineResult GetResultById(int id)
        {
            if (SearchResultList.TryGetValue(id, out SearchEngineResult result))
            {
                return result;
            }

            return null;
        }
    }

    public static class SearchEngine
    {
        public static (List<string> ListOfAddresses, Dictionary<int, SearchEngineResult> SearchEngineResult) Search(string search, double mapLatitude, double mapLongitude)
        {
            try
            {
                string encodedSearch = System.Web.HttpUtility.UrlEncode(search.Trim());

                switch (Settings.search_engine)
                {
                    case SearchEngines.ArcGIS:
                        return SearchEngine.ArcGISSearch(encodedSearch, mapLatitude, mapLongitude);
                    case SearchEngines.BingMaps:
                        //Bing Search
                        return SearchEngine.BingMapSearch(encodedSearch, mapLatitude, mapLongitude);
                    case SearchEngines.OpenStreetMap:
                        //OSM Search
                        return SearchEngine.OpenStreetMapSearch(encodedSearch);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return (new List<string>(), new Dictionary<int, SearchEngineResult>());
        }

        public static (List<string>, Dictionary<int, SearchEngineResult>) OpenStreetMapSearch(string encodedSearch)
        {
            //OSM Search
            List<string> returnListOfAddresses = new List<string>();
            Dictionary<int, SearchEngineResult> returnSearchEngineResult = new Dictionary<int, SearchEngineResult>();
            string url = $"https://nominatim.openstreetmap.org/search.php?q={encodedSearch}&polygon_geojson=0&limit=5&format=xml&email=siogabx@siogabx.fr";
            Debug.WriteLine(url);
            using HttpResponseMessage response = Tiles.HttpClient.GetAsync(url).Result;
            using Stream responseStream = response.Content.ReadAsStreamAsync().Result;

            response.EnsureSuccessStatusCode();

            using (System.Xml.XmlReader reader = XmlReader.Create(responseStream))
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
                        returnSearchEngineResult.Add(idSearch, new SearchEngineResult(idSearch, displayAdress, latitude, longitude, boundingBox));
                        idSearch++;
                    }
                }
            }
            return (returnListOfAddresses, returnSearchEngineResult);
        }

        public static (List<string>, Dictionary<int, SearchEngineResult>) BingMapSearch(string encodedSearch, double mapLatitude, double mapLongitude)
        {
            //OSM Search
            List<string> returnListOfAddresses = new List<string>();
            Dictionary<int, SearchEngineResult> returnSearchEngineResult = new Dictionary<int, SearchEngineResult>();
            const int MaxResultNumber = 5;
            string BingMapApiKey = ApiKeys.BingMaps;

            if (BingMapApiKey == "YOUR_API_KEY")
            {
#if DEBUG
                MessageBox.Show("Your Bing API Key is not set, please check MapsInMyFolder.Commun/ApiKey.cs. Fallback OpenStreetMapSearch is used");
#endif
                return OpenStreetMapSearch(encodedSearch);
            }

            string url = $"https://dev.virtualearth.net/REST/v1/Locations?o=xml&query={encodedSearch}&userLocation={mapLatitude},{mapLongitude}&maxResults={MaxResultNumber}&key={BingMapApiKey}&culture={CultureInfo.CurrentUICulture.Name}";
            Debug.WriteLine(url);
            using HttpResponseMessage response = new HttpClient().GetAsync(url).Result;
            using Stream responseStream = response.Content.ReadAsStreamAsync().Result;
            response.EnsureSuccessStatusCode();

            using (XmlReader reader = XmlReader.Create(responseStream))
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
                        returnSearchEngineResult.Add(idSearch, new SearchEngineResult(idSearch, displayAdress, latitude, longitude, boundingBox));
                        idSearch++;
                    }
                }
            }
            return (returnListOfAddresses, returnSearchEngineResult);
        }

        public static (List<string>, Dictionary<int, SearchEngineResult>) ArcGISSearch(string encodedSearch, double mapLatitude, double mapLongitude)
        {
            List<string> returnListOfAddresses = new List<string>();
            Dictionary<int, SearchEngineResult> returnSearchEngineResult = new Dictionary<int, SearchEngineResult>();
            static string MakeRequest(string url)
            {
                using HttpResponseMessage response = new HttpClient().GetAsync(url).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
                return responseString;
            }


            string userLocation = "location={\"spatialReference\":{\"latestWkid\":4326,\"wkid\":4326},\"x\":" + mapLongitude + ",\"y\":" + mapLatitude + "}";
            string url = $"https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?address={encodedSearch}&{userLocation}&outFields=*&outSR={{\"latestWkid\":4326,\"wkid\":4326}}&f=json";
            // Effectuer la recherche
            string locationJson = MakeRequest(url);
            Debug.WriteLine(url);

            // Extraire les informations de localisation du JSON
            JObject locationObject = JObject.Parse(locationJson);
            JArray candidatesArray = (JArray)locationObject["candidates"];
            int idSearch = 0;
            foreach (JObject candidate in candidatesArray.Cast<JObject>())
            {
                string longitude = candidate["location"]["x"].ToString();
                string latitude = candidate["location"]["y"].ToString();

                string displayAdress = candidate["attributes"]["LongLabel"].ToString();
                string xmax = candidate["extent"]["xmax"].ToString();
                string ymax = candidate["extent"]["ymax"].ToString();
                string xmin = candidate["extent"]["xmin"].ToString();
                string ymin = candidate["extent"]["ymin"].ToString();
                string boundingBox = $"{ymax}, {ymin},{xmax}, {xmin}";

                if (!returnListOfAddresses.Contains(displayAdress))
                {
                    returnListOfAddresses.Add(displayAdress);
                    returnSearchEngineResult.Add(idSearch, new SearchEngineResult(idSearch, displayAdress, latitude, longitude, boundingBox));
                    idSearch++;
                }
            }
            return (returnListOfAddresses, returnSearchEngineResult);
        }

    }
}
