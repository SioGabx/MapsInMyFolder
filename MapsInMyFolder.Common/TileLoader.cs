using System;
using System.IO;
using System.Net;
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

    public class TilesUrl
    {
        public string url;
        public int x;
        public int y;
        public int z;
        public Status status;
        public int downloadid;

        public TilesUrl(string url, int x, int y, int z, Status status, int downloadid)
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

    public static class Tiles
    {
        static Tiles()
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        public static bool AcceptJavascriptPrint { get; set; }

        public static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        public static HttpClient HttpClient { get; set; } = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(Settings.http_client_timeout_in_seconds) };
        public static TileLoader Loader { get; set; } = new TileLoader();

    }

    public partial class TileLoader
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
                    return await GetTilePBF(layerID, urlBase, TileX, TileY, TileZoom, save_temp_directory, (int)Math.Floor((double)(Layer.class_tiles_size ?? 0) * TileSize), TileSize, 0.5, pbfdisableadjacent).ConfigureAwait(false);

                default:
                    return await GetTile(layerID, urlBase, TileX, TileY, TileZoom).ConfigureAwait(false);
            }
        }


        public string GetStyle(int layerID)
        {
            string styleValueOrUrlOrPath;
            Layers layers = Layers.GetLayerById(layerID);

            try
            {
                styleValueOrUrlOrPath = layers?.class_style;
            }
            catch (Exception ex)
            {
                Javascript.Functions.PrintError("Layer style error : " + ex.Message, layerID);
                return null;
            }

            if (string.IsNullOrWhiteSpace(styleValueOrUrlOrPath))
            {
                Javascript.Functions.PrintError("The layer style is not defined.", layerID);
                return null;
            }

            if (IsUrlStyle(styleValueOrUrlOrPath))
            {
                string path = GetStyleFilePath(layers.class_name, layers.class_identifier, styleValueOrUrlOrPath);
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
                else
                {
                    styleValueOrUrlOrPath = DownloadStyleFromUrl(styleValueOrUrlOrPath, path, layerID);
                }
            }

            return styleValueOrUrlOrPath;
        }

        private static bool IsUrlStyle(string styleValueOrUrlOrPath)
        {
            return Uri.IsWellFormedUriString(styleValueOrUrlOrPath, UriKind.Absolute) && Collectif.IsUrlValid(styleValueOrUrlOrPath);
        }

        private static string GetStyleFilePath(string className, string classIdentifier, string styleValueOrUrlOrPath)
        {
            string fileName = styleValueOrUrlOrPath.GetHashCode().ToString() + ".json";
            string directoryPath = Path.Combine(Collectif.GetSaveTempDirectory(className, classIdentifier), "layerstyle");
            return Path.Combine(directoryPath, fileName);
        }

        private static readonly object _lock = new object();
        private static string DownloadStyleFromUrl(string url, string path, int layerID)
        {
            lock (_lock)
            {
                if (File.Exists(path))
                {
                    return path;
                }

                try
                {
                    HttpResponse httpResponse = Collectif.ByteDownloadUri(new Uri(url), layerID, true).Result;
                    if (httpResponse?.ResponseMessage.IsSuccessStatusCode == true && httpResponse.Buffer != null)
                    {
                        string styleValueOrUrlOrPath = Collectif.ByteArrayToString(httpResponse.Buffer);
                        if (!string.IsNullOrEmpty(styleValueOrUrlOrPath))
                        {
                            string directoryPath = Path.GetDirectoryName(path);
                            if (!Directory.Exists(directoryPath))
                            {
                                Directory.CreateDirectory(directoryPath);
                            }

                            File.WriteAllText(path, styleValueOrUrlOrPath);
                            return path;
                        }
                        else
                        {
                            Javascript.Functions.PrintError("Une erreur s'est produite lors du téléchargement (style est null)");
                        }
                    }
                    else
                    {
                        Javascript.Functions.PrintError("Une erreur s'est produite lors du téléchargement. " + httpResponse?.ResponseMessage);
                    }
                }
                catch (Exception ex)
                {
                    Javascript.Functions.PrintError(ex.Message);
                }
            }

            return null;
        }












    }
}
