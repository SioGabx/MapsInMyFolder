using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Geodetic.Search
{
    public interface ISearch
    {
        public Task<List<SearchResult>> GetSuggestion(string SearchValue, double UserLocationLatitude, double UserLocationLongitude);
    }
}
