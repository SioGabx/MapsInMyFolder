using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Geodetic.Search
{
    public enum SearchResultType { Suggestion, Place, AutoComplete}
    public class SearchResult
    {
        public override string ToString()
        {
            return $"{Type} - {Name}, {Address}, {Country} - [{Latitude},{Longitude}]";
        }

        public SearchResult(SearchResultType type, string name, string country, string address, double latitude, double longitude)
        {
            Type = type;
            Name = name;
            Country = country;
            Address = address;
            Latitude = latitude;
            Longitude = longitude;
        }

        public SearchResultType Type { get; set; }
        public string Name { get; set; }
        public string Country { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
