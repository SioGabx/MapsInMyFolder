// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Commun;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.MapControl
{
    public static partial class ImageLoader
    {
        /// <summary>
        /// The System.Net.Http.HttpClient instance used to download images via a http or https Uri.
        /// </summary>
        public static HttpClient HttpClient { get; set; } = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };


        public static async Task<ImageSource> LoadImageAsync(Uri uri, int x = 0, int y = 0, int z = -1, TileSource tileSource = null)
        {
            ImageSource image = null;
            try
            {
                if (!uri.IsAbsoluteUri || uri.IsFile)
                {
                    image = await LoadImageAsync(uri.IsAbsoluteUri ? uri.LocalPath : uri.OriginalString);
                }
                else if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    Commun.HttpResponse response;
                    if (z != -1)
                    {
                        string SaveTempDir = "";
                        string fileformat = string.Empty;
                        if (!(tileSource is null) && tileSource.LayerID != 0)
                        {
                            Layers layers = Layers.GetLayerById(tileSource.LayerID);
                            if (layers is null)
                            {
                                return null;
                            }
                            SaveTempDir = Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifiant, z);
                            fileformat = layers.class_format;
                        }
                        response = await TileGeneratorSettings.TileLoaderGenerator.GetImageAsync(uri.ToString(), x, y, z, tileSource.LayerID, fileformat, SaveTempDir);
                    }
                    else
                    {
                        var resp = await GetHttpResponseAsync(uri);
                        response = new Commun.HttpResponse(resp.Buffer, resp.Reponse);
                        resp = null;
                    }

                    if (response?.Buffer != null && response.ResponseMessage.IsSuccessStatusCode)
                    {
                        image = await LoadImageAsync(response.Buffer).ConfigureAwait(false);
                    }
                    else if (Settings.map_view_error_tile)
                    {
                        image = await LoadImageAsync(Collectif.GetEmptyImageBufferFromText(response)).ConfigureAwait(false);
                    }
                }
                else
                {
                    image = new BitmapImage(uri);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ImageLoader: {uri}: {ex.Message}");
            }
            return image;
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