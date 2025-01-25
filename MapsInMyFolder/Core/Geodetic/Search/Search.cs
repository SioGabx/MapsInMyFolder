using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Geodetic.Search
{
    public static class Search
    {
        public static async Task<List<SearchResult>> Query(string SearchValue, double UserLocationLatitude, double UserLocationLongitude)
        {
            Debug.WriteLine("--");
            ISearch SearchVendor = new MapsInMyFolder.Core.Geodetic.Search.Vendor.Google();
            var SearchResults = await SearchVendor.GetSuggestion(SearchValue, UserLocationLatitude, UserLocationLongitude);
            SearchResults.ForEach(result => Debug.WriteLine($"{result.Type} - {result.Name}, {result.Address}, {result.Country} - [{result.Latitude},{result.Longitude}]"));
            return SearchResults;
        }
    }
}
