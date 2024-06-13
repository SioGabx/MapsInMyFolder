using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Generic.Network
{
    public static class Client
    {
        public static HttpClient CreateHttpClient(string Referrer = null, string UserAgent = null)
        {
            var HttpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            HttpClient.DefaultRequestHeaders.Define("User-Agent", UserAgent ?? Settings.Default.DefaultUserAgent);
            if (Referrer is not null)
            {
                HttpClient.DefaultRequestHeaders.Define("Referrer", Referrer);
            }
            return HttpClient;
        }
    }
}
