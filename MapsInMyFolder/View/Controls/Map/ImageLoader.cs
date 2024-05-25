// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapsInMyFolder.View.Controls.Map
{
    public static partial class ImageLoader
    {
        /// <summary>
        /// The System.Net.Http.HttpClient instance used to download images via a http or https Uri.
        /// </summary>
        public static HttpClient HttpClient { get; set; } = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };


        public static async Task<ImageSource> LoadImageAsync(Uri uri, int x = 0, int y = 0, int z = -1, TileSource tileSource = null)
        {
            try
            {
                return tileSource.Layer.GetTile(x, y, z);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ImageLoader: {uri}: {ex.Message}");
            }
            return null;
        }

        internal class HttpResponse
        {
            public byte[] Buffer { get; }
            public HttpResponseMessage Reponse { get; }

            //.Headers.CacheControl?.MaxAge
            public HttpResponse(byte[] buffer, HttpResponseMessage reponse)
            {
                this.Buffer = buffer;
                this.Reponse = reponse;
            }
        }

        internal static async Task<HttpResponse> GetHttpResponseAsync(Uri uri)
        {
            HttpResponse response = null;

            try
            {
                using var responseMessage = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (responseMessage.IsSuccessStatusCode)
                {
                    byte[] buffer = null;

                    if (!responseMessage.Headers.TryGetValues("X-VE-Tile-Info", out IEnumerable<string> tileInfo) ||
                        !tileInfo.Contains("no-tile"))
                    {
                        buffer = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    }

                    response = new HttpResponse(buffer, responseMessage);
                }
                else
                {
                    Debug.WriteLine($"ImageLoader: {uri}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ImageLoader: {uri}: {ex.Message}");
            }

            return response;
        }
    }
}