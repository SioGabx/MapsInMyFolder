using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MapsInMyFolder
{
    public class StatusCode
    {
        public HttpStatusCode Status { get; set; }
        public string DisplayName { get; set; }

        public StatusCode(HttpStatusCode status, string displayName)
        {
            Status = status;
            DisplayName = displayName;
        }

        public static IEnumerable<HttpStatusCode> GetListFromString(string list)
        {
            var splittedlist = list?.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (HttpStatusCode status in getList())
            {
                if (splittedlist.Contains(((int)status).ToString()))
                {
                    yield return status;
                }

            }
        }

        public static IEnumerable<HttpStatusCode> getList()
        {
            return (HttpStatusCode[])Enum.GetValues(typeof(HttpStatusCode));
        }

    }
}
