using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Layers
{
    public class SearchAlgorithm
    {
        public static List<Layer> Search(string Input, List<Layer> layers)
        {
            Debug.WriteLine("--");
            ExtractQuotedText(Input).ForEach(el => Debug.WriteLine(Regex.Unescape(el)));
            /*
             function extractQuotedStrings(texte) {
                let start = -1;
                let end = -1;
                let openedQuotes = 0;
                const matches = [];

                for (let i = 0; i < texte.length; i++) {
                    const currentChar = texte[i];
                    const previousChar = i > 0 ? texte[i - 1] : null;
                    const nextChar = i < texte.length - 1 ? texte[i + 1] : null;

                    if (currentChar === '"' && (previousChar === ' ' || previousChar === ':' || previousChar === null)) {
                        if (openedQuotes === 0) {
                            start = i;
                        }
                        openedQuotes++;
                    } else if (currentChar === '"' && (nextChar === ' ' || nextChar === null)) {
                        openedQuotes--;
                        if (openedQuotes === 0) {
                            end = i;
                            matches.push(texte.substring(start, end + 1));
                            start = -1;
                            end = -1;
                        }
                    }
                }
            */

            return new List<Layer>() { };
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
