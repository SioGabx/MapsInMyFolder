using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Generic.Network;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Geodetic.Search.Vendor
{
    public class Google : ISearch
    {
        private static HttpClient _RequestClient = null;
        private static HttpClient RequestClient
        {
            get
            {
                if (_RequestClient is null)
                {
                    const string Referer = "https://www.google.fr/";
                    _RequestClient = Client.CreateHttpClient(Referer);
                }
                return _RequestClient;
            }
        }

        public async Task<List<SearchResult>> GetSuggestion(string SearchValue, double UserLocationLatitude, double UserLocationLongitude)
        {
            var BackEndApiUrl = $"https://www.google.com/s?tbm=map&gs_ri=maps&suggest=p&authuser=0&pf=t&tch=1&ech={12}&q={System.Web.HttpUtility.UrlEncode(SearchValue)}&pb=!2d{UserLocationLatitude}!3d{UserLocationLongitude}";
             Debug.WriteLine(BackEndApiUrl);
            var BackEndResponse = await RequestClient.SendRequestAutoRedirect(BackEndApiUrl);

            List<SearchResult> Results = new List<SearchResult>();

            if (BackEndResponse.IsSuccessStatusCode)
            {
                using (StreamReader reader = new StreamReader(await BackEndResponse.Content.ReadAsStreamAsync(), Encoding.UTF8))
                {
                    var Json = await reader.ReadToEndAsync();
                    var CleanedJson = CleanJson(Json);
                    Debug.WriteLine(CleanedJson);
                    if (CleanedJson != null)
                    {
                        var SearchResults = (JArray)JsonConvert.DeserializeObject(CleanedJson);
                        foreach (JArray Result in (SearchResults?.TryGet(0)?.TryGet(1)).Cast<JArray>())
                        {
                            JArray MainValues = (JArray)Result.TryGet(22);
                            if (MainValues is null) { continue; }
                            var PlaceFullName = MainValues.TryGet(0).TryGet(0).ToString();
                            var PlaceName = MainValues.TryGet(1).TryGet(0).ToString();
                            var PlaceLocation = MainValues.TryGet(2)?.TryGet(0)?.ToString();
                            var PlaceLatitude = MainValues.TryGet(11)?.TryGet(2)?.ToString();
                            var PlaceLongitude = MainValues.TryGet(11)?.TryGet(3)?.ToString();
                            //If not Latitude, longitude => suggestion of typing
                            
                            SearchResult SearchResult = new SearchResult(
                                string.IsNullOrEmpty(PlaceLatitude) ? SearchResultType.Suggestion : SearchResultType.Place,
                                PlaceName,
                                string.Empty,
                                PlaceLocation,
                                PlaceLatitude.ToDouble(0),
                                PlaceLongitude.ToDouble(0)
                            );
                            Results.Add(SearchResult);
                        }
                    }



                }
            }

            return Results;
        }

      


        private static string CleanJson(string Json)
        {
            try
            {
                var FilteredJson = Json;
                FilteredJson = Regex.Replace(FilteredJson, @"/\*[^*]+\*/", ""); //Remove /*""*/ at the end

                var DeserializedJson = (JObject)JsonConvert.DeserializeObject(FilteredJson);

                var SearchResultsJson = DeserializedJson?["d"]?.ToString();
                SearchResultsJson = Regex.Replace(SearchResultsJson, @"/^[^,]+,/", "");
                SearchResultsJson = Regex.Replace(SearchResultsJson, @"/\n\][^\]]+\][^\]]+$/", "");
                SearchResultsJson = Regex.Replace(SearchResultsJson, @"/,+/g", "");
                SearchResultsJson = Regex.Replace(SearchResultsJson, @"/\n/g", "");
                SearchResultsJson = Regex.Replace(SearchResultsJson, @"/\[,/g", "");
                SearchResultsJson = Regex.Replace(SearchResultsJson, @"\)]}'\n", "");

                return SearchResultsJson;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return null;
        }

    }
}
