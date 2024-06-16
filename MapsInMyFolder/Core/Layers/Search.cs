using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Generic.Network;
using MapsInMyFolder.Core.Geodetic.Search;
using MapsInMyFolder.Core.Geodetic.Search.Vendor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapsInMyFolder.Core.Layers
{
    public class Search
    {
        public static async void Query(string Input, List<Layer> layers)
        {
            Debug.WriteLine("--");
            ExtractQuotedText(Input).ForEach(el => Debug.WriteLine(Regex.Unescape(el)));


            const double Latitude = 48.2271673;
            const double Longitude = 6.050189;
            ISearch SearchVendor = new MapsInMyFolder.Core.Geodetic.Search.Vendor.Google();
           var SearchResult = await SearchVendor.GetSuggestion(Input, Latitude, Longitude);
            SearchResult.ForEach(result => Debug.WriteLine(result.ToString()));
        }

        public static List<string> ExtractQuotedText(string value)
        {
           
            List<string> result = new List<string>();

            int StartQuoteIndex = -1;
            int NumberContinousOfSlashBefore = 0;
            for (int TextIndex = 0; TextIndex < value.Length; TextIndex++)
            {
                char Current = value[TextIndex];

                //if number of continous before the quote (") is odd that mean the quote is escape (\" => ") 
                if (Current == '"' && NumberContinousOfSlashBefore % 2 == 0)
                {
                    if (StartQuoteIndex >= 0)
                    {
                        result.Add(value.Substring(StartQuoteIndex, (TextIndex - StartQuoteIndex) + 1));
                        StartQuoteIndex = -1;
                    }
                    else
                    {
                        StartQuoteIndex = TextIndex;
                    }
                }

                if (Current == '\\')
                {
                    NumberContinousOfSlashBefore++;
                }
                else
                {
                    NumberContinousOfSlashBefore = 0;
                }
            }
            return result;
        }


    }
}
