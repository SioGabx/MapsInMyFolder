using System.Net.Http.Headers;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class HttpRequestHeadersExtensions
    {
        public static void Define(this HttpRequestHeaders requestHeaders, string name, string value)
        {
            if (requestHeaders == null)
            {
                return;
            }
            requestHeaders.Remove(name);
            requestHeaders.Add(name, value);
        }
    }
}
