using MapsInMyFolder.Core.Generic.Network;
using MapsInMyFolder.Core.Layers;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Downloader
{
    public class TilesClient : IDisposable
    {
        private HttpClient _httpClient;
        private Layer _layer;

        public TilesClient(Layer layer)
        {
            _layer = layer;
        }

        private HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    _httpClient = Generic.Network.Client.CreateHttpClient(_layer.SiteUrl, _layer.UserAgent);

                }
                return _httpClient;
            }
        }


        public async Task<HttpResponseMessage> SendRequest(string url)
        {
            return await HttpClient.SendRequest(url);
        }

        public async Task<HttpResponseMessage> SendRequestAutoRedirect(string url)
        {
            return await HttpClient.SendRequestAutoRedirect(url);
        }

        public async Task<HttpResponseMessage> SendRequestAutoRetry(string url, short MaxNumberOfRetry, TimeSpan DelayBetweenRetry)
        {
            return await HttpClient.SendRequestAutoRetry(url, MaxNumberOfRetry, DelayBetweenRetry);
        }


        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
