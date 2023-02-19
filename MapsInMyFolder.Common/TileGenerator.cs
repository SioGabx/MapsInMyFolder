using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{
    public class HttpResponse
    {
        public byte[] Buffer { get; }
        public HttpResponseMessage ResponseMessage { get; }
        public string CustomMessage { get; }
        public static HttpResponse HttpResponseError { get; } = new HttpResponse(null, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));

        public HttpResponse(byte[] buffer, HttpResponseMessage responseMessage, string customMessage = "")
        {
            Buffer = buffer;
            ResponseMessage = responseMessage;
            CustomMessage = customMessage;
        }
    }

    public class Url_class
    {
        public string url;
        public int x;
        public int y;
        public int z;
        public Status status;
        public int downloadid;

        public Url_class(string url, int x, int y, int z, Status status, int downloadid)
        {
            this.url = url;
            this.x = x;
            this.y = y;
            this.z = z;
            this.status = status;
            this.downloadid = downloadid;
        }
    }

    public enum Status
    {
        waitfordownloading, error, cancel, success, pause, progress, no_data, assemblage, rognage, enregistrement, deleted, noconnection, cleanup
    }

    public static class TileGeneratorSettings
    {
        public static bool AcceptJavascriptPrint { get; set; } = false;

        public static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        public static HttpClient HttpClient { get; set; } = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(Settings.http_client_timeout_in_seconds) };
        public static TileGenerator TileLoaderGenerator = new TileGenerator();
        public static List<string> SupportedFileType = new List<string>()
        {
            "jpeg","jpg","png"
        };
        public static List<string> SupportedFileTypeProtobuf = new List<string>()
        {
            "pbf"
        };
    }

    public partial class TileGenerator
    {
        public async Task<HttpResponse> GetImageAsync(string urlBase, int TileX, int TileY, int TileZoom, int layerID, string fileformat = null, string save_temp_directory = "", bool pbfdisableadjacent = false)
        {
            Layers Layer = Layers.GetLayerById(layerID);
            string SwitchFormat;
            if (string.IsNullOrEmpty(fileformat) && !(Layer is null))
            {
                SwitchFormat = Layer.class_format;
            }
            else
            {
                SwitchFormat = fileformat;
            }

            switch (SwitchFormat)
            {
                case "pbf":
                    const int TileSize = 1;
                    return await GetTilePBF(layerID, urlBase, TileX, TileY, TileZoom, save_temp_directory, Layer.class_tiles_size * TileSize, TileSize, 0.5, pbfdisableadjacent).ConfigureAwait(false);

                default:
                    return await GetTile(layerID, urlBase, TileX, TileY, TileZoom).ConfigureAwait(false);
            }
        }
    }
}
