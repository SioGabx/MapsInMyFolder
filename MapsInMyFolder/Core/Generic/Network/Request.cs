using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Generic.Network
{
    public static class Request
    {
        public static async Task<HttpResponseMessage> SendRequest(this HttpClient HttpClient, string url)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            return await HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
        }

        public static async Task<HttpResponseMessage> SendRequestAutoRedirect(this HttpClient HttpClient, string url)
        {
            HttpResponseMessage httpResponseMessage = null;
            for (int RetryNumber = 0; RetryNumber < Math.Max(Settings.Default.MaxRequestRedirection, (short)1); RetryNumber++)
            {
                httpResponseMessage = await HttpClient.SendRequest(url);
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

        public static async Task<HttpResponseMessage> SendRequestAutoRetry(this HttpClient HttpClient, string url, short MaxNumberOfRetry, TimeSpan DelayBetweenRetry)
        {
            HttpResponseMessage httpResponseMessage = null;
            for (int RetryNumber = 0; RetryNumber < Math.Max(MaxNumberOfRetry, (short)1); RetryNumber++)
            {
                httpResponseMessage = await HttpClient.SendRequestAutoRedirect(url);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    return httpResponseMessage;
                }
                await Task.Delay(DelayBetweenRetry);
            }
            Debug.WriteLine("SendRequestAutoRetry : Too many retry");
            return httpResponseMessage;
        }



    }
}
