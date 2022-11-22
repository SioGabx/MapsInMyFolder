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
        public static HttpResponse HttpResponseError { get; } = new HttpResponse(null, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));

        public HttpResponse(byte[] buffer, HttpResponseMessage responseMessage)
        {
            Buffer = buffer;
            ResponseMessage = responseMessage;
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
        waitfordownloading, error, cancel, success, pause, progress, no_data, assemblage, rognage, enregistrement, deleted
    }

    public static class TileGeneratorSettings
    {
        //public static string Temp_folder { get; set; } = Commun.Settings.temp_folder;
        //public static int Tiles_cache_expire_after_x_days { get; set; } = Commun.Settings.tiles_cache_expire_after_x_days;

        //public static int Max_redirection_download_tile { get; set; } = Commun.Settings.max_redirection_download_tile;
        public static int Number_tile_converted { get; set; } = 0;
        public static bool AcceptJavascriptPrint { get; set; } = false;

        public static Dictionary<int, int> Numbers_tiles_converted { get; set; } = new Dictionary<int, int>();
        public static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        public static HttpClient HttpClient { get; set; } = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
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
        public Layers Layer { get; set; } = Layers.Empty();
         public async Task<HttpResponse> GetImageAsync(string urlBase, int TileX, int TileY, int TileZoom, int layerID, string fileformat = null, string save_temp_directory = "", int settings_max_tiles_cache_days = 0, bool pbfdisableadjacent = false)
        {
            string SwitchFormat;
            if (string.IsNullOrEmpty(fileformat) && !(Layer is null))
            {
                SwitchFormat = Layer.class_format.ToString();
            }
            else
            {
                SwitchFormat = fileformat;
            }
            

            switch (SwitchFormat)
            {
                case "pbf":
                    int TileSize = 1;
                    return await GetTilePBF(layerID, urlBase, TileX, TileY, TileZoom, save_temp_directory, settings_max_tiles_cache_days, Layer.class_tiles_size * TileSize, TileSize, 0.5, pbfdisableadjacent).ConfigureAwait(false);

                default:
                    //Debug.WriteLine("Classic format start Converting");
                    return await GetTile(layerID, urlBase, TileX, TileY, TileZoom).ConfigureAwait(false);
            }
        }

    }
}
