using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Generic.Network;
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
    public class SearchAlgorithm
    {
        public static async void Search(string Input, List<Layer> layers)
        {
            Debug.WriteLine("--");
            ExtractQuotedText(Input).ForEach(el => Debug.WriteLine(Regex.Unescape(el)));



            double Latitude = 48.2271673;
            double Longitude = 6.050189;
            var MapsSearchAPIUrl = $"https://www.google.com/s?tbm=map&gs_ri=maps&suggest=p&authuser=0&pf=t&tch=1&ech={12}&q={System.Web.HttpUtility.UrlEncode(Input)}&pb=!2d{Longitude}!3d{Latitude}";

            HttpClient Client = Generic.Network.Client.CreateHttpClient();
            var MapsSearchResult = await Client.SendRequestAutoRedirect(MapsSearchAPIUrl);
            if (MapsSearchResult.IsSuccessStatusCode)
            {
                using (StreamReader reader = new StreamReader(MapsSearchResult.Content.ReadAsStream(), Encoding.UTF8))
                {
                    var Content = reader.ReadToEnd();
                    //Debug.WriteLine(Content);
                    //string value = Regex.Match(Content, @"null,\[null,null,([\s\S]*?)\]").Groups[1].Value;
                    //Debug.WriteLine(value);
                    try {
                        var Vlaue = Content;
                        Vlaue = Regex.Replace(Vlaue, @"/\*[^*]+\*/", ""); //Remove /*""*/ at the end
                        //Vlaue = JsonConvert.SerializeObject(Vlaue);
                        var Vlaue2 = (JObject)JsonConvert.DeserializeObject(Vlaue);

                        var r1Vlaue = Vlaue2?["d"]?.ToString();

                        r1Vlaue = Regex.Replace(r1Vlaue, @"/^[^,]+,/", "");
                        r1Vlaue = Regex.Replace(r1Vlaue, @"/\n\][^\]]+\][^\]]+$/", "");
                        r1Vlaue = Regex.Replace(r1Vlaue, @"/,+/g", "");
                        r1Vlaue = Regex.Replace(r1Vlaue, @"/\n/g", "");
                        r1Vlaue = Regex.Replace(r1Vlaue, @"/\[,/g", "");
                        r1Vlaue = Regex.Replace(r1Vlaue, @"\)]}'\n", "");


                        Debug.WriteLine(r1Vlaue);
                        var Vlaue5 = (JArray)JsonConvert.DeserializeObject(r1Vlaue);


                        var ResultTree = Vlaue5?.TryGet(0)?.TryGet(1);
                        foreach (JArray item in ResultTree)
                        {
                            JArray Ele = (JArray)item[22];
                            
                            var PlaceFullName = Ele.TryGet(0).TryGet(0).ToString();
                            var PlaceName = Ele.TryGet(1).TryGet(0).ToString();
                            var PlaceLocation = Ele.TryGet(2)?.TryGet(0)?.ToString();
                            var PlaceLatitude = Ele.TryGet(11)?.TryGet(2)?.ToString();
                            var PlaceLongitude = Ele.TryGet(11)?.TryGet(3)?.ToString();
                            //If not Latitude, longitude => suggestion of typing
                            Debug.WriteLine($"Name : {PlaceFullName}\nLatitude : {PlaceLatitude}\nLongitude : {PlaceLongitude}\n\n");
                        }
                    }catch(Exception ex) { }
                }



            }


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
