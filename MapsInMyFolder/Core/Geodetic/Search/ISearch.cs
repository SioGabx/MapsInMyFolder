using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Geodetic.Search
{
    public interface ISearch
    {
        public Task<List<SearchResult>> GetSuggestion(string SearchValue, double UserLocationLatitude, double UserLocationLongitude);
    }
}
