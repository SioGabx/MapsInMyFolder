using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Layers;
using MapsInMyFolder.Properties;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Downloader
{
    public class Client : IDisposable
    {
        private HttpClient _httpClient;
        private Layer _layer;

        public Client(Layer layer)
        {
            _layer = layer;
        }

        private HttpClient HttpClient
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
        private HttpClient CreateHttpClient()
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

            var UserAgent = _layer.UserAgent ?? Settings.Default.DefaultUserAgent;
            HttpClient.DefaultRequestHeaders.Define("User-Agent", UserAgent);
            HttpClient.DefaultRequestHeaders.Define("Referrer", _layer.SiteUrl);

            return HttpClient;
        }

        public async Task<HttpResponseMessage> SendRequest(string url)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            return await HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
        }

        public async Task<HttpResponseMessage> SendRequestAutoRedirect(string url)
        {
            HttpResponseMessage httpResponseMessage = null;
            for (int RetryNumber = 0; RetryNumber < Math.Max(Settings.Default.MaxRequestRedirection, (short)1); RetryNumber++)
            {
                httpResponseMessage = await SendRequest(url);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    return httpResponseMessage;
                }

                var NewRedirectLocation = httpResponseMessage?.Headers?.Location?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(NewRedirectLocation))
                {
                    Debug.WriteLine("Redirect request to" + NewRedirectLocation);
                    url = NewRedirectLocation;
                }
                else
                {
                    return httpResponseMessage;
                }
            }
            Debug.WriteLine("SendRequestAutoRedirect : Too many redirect");
            return httpResponseMessage;
        }

        public async Task<HttpResponseMessage> SendRequestAutoRetry(string url, short MaxNumberOfRetry, TimeSpan DelayBetweenRetry)
        {
            HttpResponseMessage httpResponseMessage = null;
            for (int RetryNumber = 0; RetryNumber < Math.Max(MaxNumberOfRetry, (short)1); RetryNumber++)
            {
                httpResponseMessage = await SendRequestAutoRedirect(url);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    return httpResponseMessage;
                }
                await Task.Delay(DelayBetweenRetry);
            }
            Debug.WriteLine("SendRequestAutoRetry : Too many retry");
            return httpResponseMessage;
        }


        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
        }
    }
}
