using MapsInMyFolder.Core.Generic.Extensions;

namespace MapsInMyFolder.Core.Geodetic.Search
{
    public enum SearchResultType { Suggestion, Place, AutoComplete }
    public class SearchResult
    {
        public override string ToString()
        {
            //return $"{Type} - {Name}, {Address}, {Country} - [{Latitude},{Longitude}]";
            var ConcatName = string.Empty;
            
            void AddInfo(string Info)
            {
                if (!string.IsNullOrWhiteSpace(Info))
                {
                    if (!string.IsNullOrEmpty(ConcatName) && ConcatName.GetLastChar() != ',')
                    {
                        ConcatName += ", ";
                    }
                    ConcatName += Info.Trim().TrimEnd(',');
                }
            }

            AddInfo(Name);
            AddInfo(Address);
            AddInfo(Country);

            return ConcatName;
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
