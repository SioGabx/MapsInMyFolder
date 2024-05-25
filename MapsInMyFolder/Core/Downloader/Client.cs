using System.Net.Http;

namespace MapsInMyFolder.Core.Downloader
{
    public static class Client
    {
        private static HttpClient _httpClient;

        public static HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpClient = CreateHttpClient();
                }
                return _httpClient;
            }
        }
        private static HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }
    }
}
