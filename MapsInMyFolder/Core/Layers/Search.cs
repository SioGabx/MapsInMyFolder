using MapsInMyFolder.Core.Geodetic.Search;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MapsInMyFolder.Core.Layers
{
    public static class Search
    {
        public static void Query(string Input, List<Layer> layers)
        {
            Debug.WriteLine("--");
            ExtractQuotedText(Input).ForEach(el => Debug.WriteLine(Regex.Unescape(el)));

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
