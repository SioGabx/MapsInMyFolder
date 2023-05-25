using Jint;
using MapsInMyFolder.Commun;
using NetVips;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.Commun
{
    public static class DebugMode
    {
        public static void WriteLine(object text)
        {
            if (Settings.is_in_debug_mode)
            {
                Debug.WriteLine(text.ToString());
            }
        }
    }


    public class NameHiddenIdValue
    {
        public int Id { get; }
        public string Name { get; }

        public NameHiddenIdValue(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class Collectif
    {
        public static class GetUrl
        {
            public enum InvokeFunction { getTile, getPreview, getPreviewFallback }

            public static (Dictionary<string, object> DefaultCallValue, Dictionary<string, string> ResultCallValue) CallFunctionAndGetResult(string urlbase, string TileComputationScript, int Tilex, int Tiley, int z, int LayerID, InvokeFunction InvokeFunction)
            {
                if (LayerID == -1)
                {
                    return (null, null);
                }

                var location_topleft = TileToCoordonnees(Tilex, Tiley, z);
                var location_bottomright = TileToCoordonnees(Tilex + 1, Tiley + 1, z);
                var location = GetCenterBetweenTwoPoints(location_topleft, location_bottomright);

                Dictionary<string, object> argument = new Dictionary<string, object>()
                {
                      { "x",  Tilex.ToString() },
                      { "y",  Tiley.ToString() },
                      { "z",  z.ToString() },
                      { "zoom",  z.ToString() },
                      { "lat",  location.Latitude.ToString() },
                      { "lng",  location.Longitude.ToString()},

                      { "t_lat",  location_topleft.Latitude.ToString() },
                      { "t_lng",  location_topleft.Longitude.ToString()},
                      { "b_lat",  location_bottomright.Latitude.ToString() },
                      { "b_lng",  location_bottomright.Longitude.ToString()},

                      { "layerid",  LayerID.ToString() },
                      { "url",  urlbase },
                };


                if (!string.IsNullOrEmpty(TileComputationScript))
                {
                    Jint.Native.JsValue JavascriptMainResult = null;
                    try
                    {
                        JavascriptMainResult = Javascript.ExecuteScript(TileComputationScript, new Dictionary<string, object>(argument), LayerID, InvokeFunction);
                    }
                    catch (Exception ex)
                    {
                        Javascript.Functions.PrintError(ex.Message);
                    }
                    if (!(JavascriptMainResult is null) && JavascriptMainResult.IsObject())
                    {
                        object JavascriptMainResultObject = JavascriptMainResult.ToObject();
                        var JavascriptMainResultJson = JsonConvert.SerializeObject(JavascriptMainResultObject);
                        var JavascriptMainResultDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(JavascriptMainResultJson);
                        return (argument, JavascriptMainResultDictionary);
                    }
                }
                return (null, null);
            }

            public static string FromTileXYZ(string urlbase, int Tilex, int Tiley, int z, int LayerID, InvokeFunction InvokeFunction)
            {
                if (LayerID == -1)
                {
                    return urlbase;
                }

                Layers calque = Layers.GetLayerById(LayerID);
                if (calque is null)
                {
                    return string.Empty;
                }
                string TileComputationScript = calque.class_tilecomputationscript;
                var ValuesDictionnary = CallFunctionAndGetResult(urlbase, TileComputationScript, Tilex, Tiley, z, LayerID, InvokeFunction);
                if (ValuesDictionnary.ResultCallValue is null)
                {
                    return string.Empty;
                }

                string finalurl;
                if (string.IsNullOrEmpty(urlbase))
                {
                    finalurl = calque.class_tile_url;
                }
                else
                {
                    finalurl = urlbase;
                }
                if (ValuesDictionnary.ResultCallValue.TryGetValue("url", out string urlResult))
                {
                    if (!string.IsNullOrEmpty(urlResult))
                    {
                        finalurl = urlResult;
                    }
                }

                foreach (var JavascriptReplacementVar in ValuesDictionnary.ResultCallValue)
                {
                    string replacementValue = string.Empty;
                    try
                    {
                        if (JavascriptReplacementVar.Value is null)
                        {
                            replacementValue = "null";
                        }
                        else
                        {
                            replacementValue = JavascriptReplacementVar.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        replacementValue = "null";
                    }
                    finally
                    {
                        finalurl = finalurl.Replace("{" + JavascriptReplacementVar.Key + "}", replacementValue);
                    }
                }
                return finalurl;
            }

            public static List<int> NextNumberFromPara(int x, int y, int x_Max, int y_Max)
            {
                int return_x;
                int return_y;
                if (x < x_Max - 1)
                {
                    return_x = x + 1;
                    return_y = y;
                }
                else if (y < y_Max - 1)
                {
                    return_x = 0;
                    return_y = y + 1;
                }
                else
                {
                    return_x = 0;
                    return_y = 0;
                }
                return new List<int>() { return_x, return_y };
            }

            public static List<Url_class> GetListOfUrlFromLocation(Dictionary<string, double> location, int z, string urlbase, int LayerID, int downloadid = 0)
            {
                var NO_tile = CoordonneesToTile(location["NO_Latitude"], location["NO_Longitude"], z);
                var SE_tile = CoordonneesToTile(location["SE_Latitude"], location["SE_Longitude"], z);
                int NO_x = NO_tile.X;
                int NO_y = NO_tile.Y;
                int SE_x = SE_tile.X;
                int SE_y = SE_tile.Y;

                List<Url_class> list_of_url_to_download = new List<Url_class>();
                int Download_X_tile = 0;
                int Download_Y_tile = 0;
                int max_x = Math.Abs(SE_x - NO_x) + 1;
                int max_y = Math.Abs(SE_y - NO_y) + 1;
                for (int i = 0; i < max_y; i++)
                {
                    for (int a = 0; a < max_x; a++)
                    {
                        int tuileX = NO_x + Download_X_tile;
                        int tuileY = NO_y + Download_Y_tile;
                        string url_to_add_inside_list = FromTileXYZ(urlbase, tuileX, tuileY, z, LayerID, InvokeFunction.getTile);
                        list_of_url_to_download.Add(new Url_class(url_to_add_inside_list, tuileX, tuileY, z, Status.waitfordownloading, downloadid));
                        List<int> next_num_list = NextNumberFromPara(Download_X_tile, Download_Y_tile, max_x, max_y);
                        Download_X_tile = next_num_list[0];
                        Download_Y_tile = next_num_list[1];
                    }
                }
                return list_of_url_to_download;
            }
        }


        public static DoubleAnimation GetOpacityAnimation(int toValue, double durationMultiplicator = 1)
        {
            return new DoubleAnimation(toValue, TimeSpan.FromMilliseconds((long)(Settings.animations_duration_millisecond * durationMultiplicator)))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
            };
        }

        public static void GetAllManifestResourceNames()
        {
            //get assembly ManifestResourceNames
            Assembly assembly = Assembly.GetEntryAssembly();
            for (int i = 0; i < assembly.GetManifestResourceNames().Length; i++)
            {
                string name = assembly.GetManifestResourceNames()[i];
                Debug.WriteLine("ManifestResourceNames : " + name);
            }
        }

        public static string ReadResourceString(string pathWithSlash)
        {
            using (Stream stream = ReadResourceStream(pathWithSlash))
            {
                string return_rsx = String.Empty;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    return_rsx = reader.ReadToEnd();
                }
                return return_rsx;
            }
        }

        public static Stream ReadResourceStream(string pathWithSlash)
        {
            //GetEntryAssembly return the main instance (MapsInMyFolder)
            Assembly assembly = Assembly.GetEntryAssembly();
            //Change path to assembly ressource path
            string pathWithPoint = pathWithSlash;
            pathWithPoint = pathWithPoint.Replace("/", ".");
            pathWithPoint = pathWithPoint.Replace("\\", ".");
            string resourceName = "MapsInMyFolder." + pathWithPoint;
            return assembly.GetManifestResourceStream(resourceName);
        }

        public static void RestartApplication()
        {
            string applicationPath = Process.GetCurrentProcess().MainModule.FileName;
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo(applicationPath)
                {
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = Path.GetDirectoryName(applicationPath)
                }
            };

            process.Start();

            Application.Current.Shutdown();
        }

        public static SolidColorBrush HexValueToSolidColorBrush(string hexvalue, string defaulthexvalue = null)
        {
            if (!string.IsNullOrEmpty(hexvalue))
            {
                hexvalue = hexvalue.Trim('#');
            }
            if (!string.IsNullOrWhiteSpace(hexvalue) && (Int32.TryParse(hexvalue, System.Globalization.NumberStyles.HexNumber, null, out _)))
            {
                if (hexvalue.Length == 3)
                {
                    return (SolidColorBrush)new BrushConverter().ConvertFrom('#' + string.Concat(hexvalue[0], hexvalue[0], hexvalue[1], hexvalue[1], hexvalue[2], hexvalue[2]));
                }
                else if (hexvalue.Length == 6 || hexvalue.Length == 8)
                {
                    return (SolidColorBrush)new BrushConverter().ConvertFrom('#' + hexvalue);
                }
            }

            if (string.IsNullOrWhiteSpace(defaulthexvalue))
            {
                return RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B);
            }
            else
            {
                return HexValueToSolidColorBrush(defaulthexvalue);
            }

        }

        public static SolidColorBrush RgbValueToSolidColorBrush(int R, int G, int B)
        {
            return new SolidColorBrush(Color.FromArgb(255, (byte)R, (byte)G, (byte)B));
        }

        public static ScrollViewer FormatDiffGetScrollViewer(string texteBase, string texteModif)
        {
            TextBlock FormatDiffTextblock = FormatDiff(texteBase, texteModif);
            FormatDiffTextblock.Padding = new Thickness(5, 5, 5, 5);
            ScrollViewer ScrollViewerElement = new ScrollViewer()
            {
                MaxHeight = 200,
                Margin = new Thickness(0, 5, 0, 20),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            StackPanel ScrollViewerElementContent = new StackPanel()
            {
                Margin = new Thickness(0, 0, 25, 0),
                Background = HexValueToSolidColorBrush("#303031"),
            };
            ScrollViewerElementContent.Children.Add(FormatDiffTextblock);
            ScrollViewerElement.Content = ScrollViewerElementContent;
            return ScrollViewerElement;
        }


        public static TextBlock FormatDiff(string texteBase, string texteModif)
        {
            DiffMatchPatch dmp = new DiffMatchPatch()
            {
                Diff_Timeout = 0
            };

            TextBlock ContentTextBlock = new TextBlock()
            {
                TextWrapping = TextWrapping.WrapWithOverflow,
                Foreground = HexValueToSolidColorBrush("#FFE2E2E1"),
                TextAlignment = TextAlignment.Justify
            };
            List<Diff> diffs = dmp.diff_main(texteBase, texteModif);
            SolidColorBrush BackgroundGreen = HexValueToSolidColorBrush("#6b803f");
            SolidColorBrush BackgroundRed = HexValueToSolidColorBrush("#582e2e");
            foreach (var diff in diffs)
            {
                SolidColorBrush curentBrush = Brushes.Transparent;
                if (diff.operation == Operation.INSERT)
                {
                    curentBrush = BackgroundGreen;
                }
                else if (diff.operation == Operation.DELETE)
                {
                    curentBrush = BackgroundRed;
                }

                ContentTextBlock.Inlines.Add(new System.Windows.Documents.Run()
                {
                    Text = diff.text,
                    Background = curentBrush,
                });
            }
            return ContentTextBlock;
        }

        public static void setBackgroundOnUIElement(UIElement element, string hexcolor)
        {
            SolidColorBrush brush;
            if (string.IsNullOrEmpty(hexcolor?.Trim()))
            {
                brush = RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B); ;
            }
            else
            {
                brush = HexValueToSolidColorBrush(hexcolor);
            }

            element.GetType().GetProperty("Background").SetValue(element, brush);

        }

        public static string GetSaveTempDirectory(string nom, string identifiant, int zoom = -1, string temp_folder = "")
        {
            string settings_temp_folder;
            if (string.IsNullOrEmpty(temp_folder))
            {
                settings_temp_folder = Settings.temp_folder;
            }
            else
            {
                settings_temp_folder = temp_folder;
            }

            string nom_charclean = string.Concat(nom.Split(Path.GetInvalidFileNameChars()));
            string chemin = Path.Combine(settings_temp_folder, "layers", nom_charclean + "_" + identifiant + "\\");
            if (zoom != -1)
            {
                chemin += zoom + "\\";
            }
            return chemin;
        }

        public static MemoryStream ByteArrayToStream(byte[] input)
        {
            if (input == null) { return null; }
            MemoryStream ms = new MemoryStream(input);
            return ms;
        }

        public static byte[] GetBytesFromBitmapSource(BitmapSource bmp)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
                byte[] bit = stream.ToArray();
                stream.Close();
                return bit;
            }
        }

        public static byte[] GetEmptyImageBufferFromText(HttpResponse httpResponse, string FileFormat)
        {
            string BitmapErrorsMessage;
            if (httpResponse is null || httpResponse.ResponseMessage is null)
            {
                if (!string.IsNullOrEmpty(httpResponse.CustomMessage))
                {
                    BitmapErrorsMessage = httpResponse.CustomMessage;
                }
                else
                {
                    BitmapErrorsMessage = "Null response";
                }
            }
            else
            {
                BitmapErrorsMessage = $"{(int)httpResponse?.ResponseMessage?.StatusCode} - {httpResponse?.ResponseMessage?.ReasonPhrase}";
            }

            return GetEmptyImageBufferFromText(BitmapErrorsMessage, FileFormat);
        }

        public static byte[] GetEmptyImageBufferFromText(string BitmapErrorsMessage, string FileFormat)
        {
            const int tile_size = 300;
            const int border_size = 1;
            const int border_tile_size = tile_size - (border_size * 2);
            if (FileFormat == "png")
            {
                const string format = "png";
                double[] color = new double[] { Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B, 60 };
                VOption saveVOption = getSaveVOption(format, 100, tile_size);

                if (string.IsNullOrEmpty(BitmapErrorsMessage))
                {
                    return null;
                }

                using (NetVips.Image text = NetVips.Image.Text(WordWrap(BitmapErrorsMessage, 20), null, null, null, Enums.Align.Centre, null, 100, 5, null, true))
                using (NetVips.Image background = NetVips.Image.Black(border_tile_size, border_tile_size))
                {
                    int offsetX = (int)Math.Floor((double)(border_tile_size - text.Width) / 2);
                    int offsetY = (int)Math.Floor((double)(border_tile_size - text.Height) / 2);

                    using (NetVips.Image image = background.Linear(color, color))
                    using (NetVips.Image SrgbImage = image.Copy(interpretation: Enums.Interpretation.Srgb))
                    using (NetVips.Image finalImage = SrgbImage.Composite2(text, Enums.BlendMode.Xor, offsetX, offsetY))
                    using (NetVips.Image GravityFinalImage = finalImage.Gravity(Enums.CompassDirection.Centre, tile_size, tile_size, Enums.Extend.Background, new double[] { 0, 0, 0, 255 }))
                    {
                        return GravityFinalImage.WriteToBuffer("." + format, saveVOption);
                    }
                }
            }
            else
            {
                const string format = "jpeg";
                var color = new double[] { Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B };
                VOption saveVOption = getSaveVOption(format, 100, tile_size);

                if (string.IsNullOrEmpty(BitmapErrorsMessage))
                {
                    return null;
                }

                using (NetVips.Image text = NetVips.Image.Text(WordWrap(BitmapErrorsMessage, 20), null, null, null, Enums.Align.Centre, null, 100, 5, null, true))
                using (NetVips.Image background = NetVips.Image.Black(border_tile_size, border_tile_size))
                {
                    int offsetX = (int)Math.Floor((double)(border_tile_size - text.Width) / 2);
                    int offsetY = (int)Math.Floor((double)(border_tile_size - text.Height) / 2);

                    using (NetVips.Image image = background.Linear(color, color))
                    using (NetVips.Image finalImage = image.Composite2(text, Enums.BlendMode.Atop, offsetX, offsetY))
                    using (NetVips.Image GravityFinalImage = finalImage.Gravity(Enums.CompassDirection.Centre, tile_size, tile_size, Enums.Extend.Black))
                    {
                        return GravityFinalImage.WriteToBuffer("." + format, saveVOption);
                    }
                }
            }
        }

        public static long GetDirectorySize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }
        public static string FormatBytes(long bytes)
        {
            string[] Suffix = { "octets", "Ko", "Mo", "Go", "To" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0} {1}", dblSByte, Suffix[i]);
        }

        public static VOption getSaveVOption(string final_saveformat, int quality, int? tile_size)
        {
            if (quality <= 0)
            {
                quality = 1;
            }

            VOption saving_options;
            if (final_saveformat == "png")
            {
                saving_options = new VOption {
                    { "Q", quality },
                    { "compression", 100 },
                    { "interlace", true },
                    { "strip", true },
                };
            }
            else if (final_saveformat == "jpeg")
            {
                saving_options = new VOption {
                    { "Q", quality },
                    { "interlace", true },
                    { "optimize_coding", true },
                    { "strip", true },
                };
            }
            else if (final_saveformat == "tiff")
            {
                saving_options = new VOption {
                    { "Q", quality },
                    { "tileWidth", tile_size },
                    { "tileHeight", tile_size },
                    { "compression", "jpeg" },
                    { "interlace", true },
                    { "tile", true },
                    { "pyramid", true },
                    { "bigtif", true }
                };
            }
            else
            {
                saving_options = new VOption();
            }
            return saving_options;
        }

        public static async Task<HttpResponse> ByteDownloadUri(Uri url, int LayerId, bool getRealRequestMessage = false)
        {

            if (!Network.FastIsNetworkAvailable())
            {
                return new HttpResponse(null, new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = "Aucune connexion : Vérifiez votre connexion Internet"
                });
            }

            HttpResponse response = HttpResponse.HttpResponseError;
            int maxRetry = Settings.max_redirection_download_tile;
            int retry = 0;
            bool doNeedToRetry;
            Uri parsing_url = url;
            do
            {
                doNeedToRetry = false;
                retry++;

                try
                {
                    using (var responseMessage = await Tiles.HttpClient.GetAsync(parsing_url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                    {
                        if (responseMessage is null)
                        {
                            DebugMode.WriteLine("Error null response");
                            return response;
                        }

                        if (responseMessage.IsSuccessStatusCode)
                        {
                            byte[] buffer = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                            return new HttpResponse(buffer, responseMessage);
                        }
                        else if (!string.IsNullOrWhiteSpace(responseMessage?.Headers?.Location?.ToString()?.Trim()))
                        {
                            // Redirect found (autodetect)  System.Net.HttpStatusCode.Found
                            Uri new_location = responseMessage.Headers.Location;
                            DebugMode.WriteLine($"DownloadByteUrl Retry : {parsing_url} to {new_location}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                            doNeedToRetry = true;
                            parsing_url = new_location;
                        }
                        else
                        {
                            DebugMode.WriteLine($"DownloadByteUrl: {parsing_url}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                            if (LayerId == -2)
                            {
                                Javascript.Functions.PrintError($"DownloadUrl - Error {(int)responseMessage.StatusCode} : {responseMessage.ReasonPhrase}. Url : {parsing_url}");
                            }

                            if (Settings.generate_transparent_tiles_on_error)
                            {
                                if (getRealRequestMessage)
                                {
                                    return new HttpResponse(null, responseMessage);
                                }
                                else
                                {
                                    return new HttpResponse(null, new System.Net.Http.HttpResponseMessage(HttpStatusCode.NotFound));
                                }
                            }
                        }
                        if (getRealRequestMessage)
                        {
                            response = new HttpResponse(response.Buffer, responseMessage);
                        }

                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is System.Net.Sockets.SocketException socketException && socketException.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
                    {
                        // Check si internet lorsque l'hôte est inconnu
                        Network.IsNetworkAvailable();
                    }
                    Debug.WriteLine($"DownloadByteUrl catch: {url}: {ex.Message}");

                    if (LayerId == -2)
                    {
                        Javascript.Functions.PrintError($"DownloadUrl - Error {ex.Message}. Url : {url}");
                    }
                    response = new HttpResponse(null, null, ex.Message);
                }
            } while (doNeedToRetry && (retry < maxRetry));

            return response;
        }

        public class HttpClientDownloadWithProgress
        {
            private readonly string _downloadUrl;
            private readonly string _destinationFilePath;

            public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

            public event ProgressChangedHandler ProgressChanged;

            public HttpClientDownloadWithProgress(string downloadUrl, string destinationFilePath)
            {
                _downloadUrl = downloadUrl;
                _destinationFilePath = destinationFilePath;
            }

            public async Task StartDownload()
            {
                using (var response = await Tiles.HttpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    await DownloadFileFromHttpResponseMessage(response);
                }
            }

            private async Task DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    await ProcessContentStream(totalBytes, contentStream);
                }
            }

            private async Task ProcessContentStream(long? totalDownloadSize, Stream contentStream)
            {
                var totalBytesRead = 0L;
                var readCount = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;

                using (var fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    do
                    {
                        var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                            continue;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead);

                        totalBytesRead += bytesRead;
                        readCount += 1;

                        if (readCount % 100 == 0)
                        {
                            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                        }
                    }
                    while (isMoreToRead);
                }
            }

            private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
            {
                if (ProgressChanged == null)
                {
                    return;
                }

                double? progressPercentage = null;
                if (totalDownloadSize.HasValue)
                {
                    progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
                }

                ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
            }
        }

        public static string ByteArrayToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static bool CheckIfDownloadIsNeededOrCached(string save_temp_directory, string filename, int settings_max_tiles_cache_days)
        {
            if (!Directory.Exists(save_temp_directory))
            {
                Directory.CreateDirectory(save_temp_directory);
                return true;
            }
            if (File.Exists(save_temp_directory + filename))
            {
                FileInfo filinfo = new FileInfo(save_temp_directory + filename);
                long size = filinfo.Length;
                DateTime date_du_telechargement = DateTime.Now;
                date_du_telechargement = date_du_telechargement.AddDays(-Math.Abs(settings_max_tiles_cache_days));
                if ((size == 0) || (DateTime.Compare(date_du_telechargement, filinfo.LastWriteTime) >= 0))
                {
                    File.Delete(save_temp_directory + filename);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static void LockPreviousUndo(RichTextBox uIElement)
        {
            int undo_limit = uIElement.UndoLimit;
            uIElement.UndoLimit = 0;
            uIElement.UndoLimit = undo_limit;
        }

        public static void LockPreviousUndo(TextBox uIElement)
        {
            int undo_limit = uIElement.UndoLimit;
            uIElement.UndoLimit = 0;
            uIElement.UndoLimit = undo_limit;
        }

        public static void InsertTextAtCaretPosition(ICSharpCode.AvalonEdit.TextEditor TextBox, string text)
        {
            int CaretIndex = TextBox.CaretOffset;
            if (TextBox.SelectionLength == 0)
            {
                TextBox.TextArea.Document.Insert(CaretIndex, text);
            }
            else
            {
                TextBox.SelectedText = text;
                TextBox.SelectionLength = 0;
            }

            TextBox.CaretOffset = Math.Min(TextBox.Text.Length, CaretIndex + text.Length);
        }

        public static void TextEditorCursorPositionChanged(ICSharpCode.AvalonEdit.TextEditor textEditor, Grid grid, ScrollViewer scrollViewer, int MarginTop = 25)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) { return; }
            double Margin = 40;

            double TextboxLayerScriptTopPosition = textEditor.TranslatePoint(new Point(0, 0), grid).Y;
            double TextboxLayerScriptCaretTopPosition = textEditor.TextArea.Caret.CalculateCaretRectangle().Top + TextboxLayerScriptTopPosition;
            if (TextboxLayerScriptCaretTopPosition > (grid.ActualHeight - Margin))
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + (TextboxLayerScriptCaretTopPosition - (grid.ActualHeight - Margin)));
                return;
            }
            else if (TextboxLayerScriptCaretTopPosition < MarginTop)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - Math.Abs((MarginTop) - TextboxLayerScriptCaretTopPosition));
                return;
            }
        }

        public static void IndenterCode(object sender, EventArgs e, ICSharpCode.AvalonEdit.TextEditor textBox)
        {
            Commun.JSBeautify jSBeautify = new JSBeautify(textBox.Text, new JSBeautifyOptions() { preserve_newlines = false, indent_char = ' ', indent_size = 4 });
            textBox.Text = jSBeautify.GetResult();
        }

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            if (element.GetType() == type)
            {
                return element;
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
            {
                (element as FrameworkElement).ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }

        public static List<UIElement> FindVisualChildren(UIElement obj, List<System.Type> BlackListNoSearchChildren = null)
        {
            List<UIElement> children = new List<UIElement>();

            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    UIElement objChild;
                    try
                    {
                        objChild = (UIElement)VisualTreeHelper.GetChild(obj, i);
                    }
                    catch (InvalidCastException)
                    {
                        //Unable to cast object to type 'System.Windows.UIElement'
                        continue;
                    }
                    if (!(objChild is null))
                    {
                        children.Add(objChild);
                        if (BlackListNoSearchChildren?.Contains(objChild.GetType()) != true)
                        {
                            children = children.Concat(FindVisualChildren(objChild, BlackListNoSearchChildren)).ToList();
                        }
                    }
                }
            }
            return children;
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// https://stackoverflow.com/questions/636383/how-can-i-find-wpf-controls-by-name-or-type
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChildByName<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChildByName<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = (child as T) ?? FindChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        public static int CheckIfInputValueHaveChange(UIElement SourcePanel)
        {
            List<System.Type> TypeOfSearchElement = new List<System.Type>
            {
                typeof(TextBox),
                typeof(ComboBox),
                typeof(CheckBox),
                typeof(RadioButton),
                typeof(ICSharpCode.AvalonEdit.TextEditor),
                typeof(BlackPearl.Controls.CoreLibrary.MultiSelectCombobox)
            };

            List<UIElement> ListOfisualChildren = FindVisualChildren(SourcePanel, TypeOfSearchElement);

            string strHachCode = String.Empty;
            ListOfisualChildren.ForEach(element =>
            {
                if (TypeOfSearchElement.Contains(element.GetType()))
                {
                    string elementXName = element.GetName();
                    if (!string.IsNullOrEmpty(elementXName))
                    {
                        int hachCode = 0;
                        System.Type type = element.GetType();
                        if (type == typeof(TextBox))
                        {
                            TextBox TextBox = (TextBox)element;
                            string value = TextBox.Text;
                            if (!string.IsNullOrEmpty(value))
                            {
                                hachCode = value.GetHashCode();
                            }
                        }
                        else if (type == typeof(ICSharpCode.AvalonEdit.TextEditor))
                        {
                            ICSharpCode.AvalonEdit.TextEditor TextEditor = (ICSharpCode.AvalonEdit.TextEditor)element;
                            string value = TextEditor.Text;
                            if (!string.IsNullOrEmpty(value))
                            {
                                hachCode = value.GetHashCode();
                            }
                        }
                        else if (type == typeof(ComboBox))
                        {
                            ComboBox ComboBox = (ComboBox)element;
                            string value = ComboBox.Text;
                            if (!string.IsNullOrEmpty(value))
                            {
                                hachCode = value.GetHashCode();
                            }
                        }
                        else if (type == typeof(CheckBox))
                        {
                            CheckBox CheckBox = (CheckBox)element;
                            hachCode = CheckBox.IsChecked.GetHashCode();

                        }
                        else if (type == typeof(RadioButton))
                        {
                            RadioButton RadioButton = (RadioButton)element;
                            hachCode = RadioButton.IsChecked.GetHashCode();
                        }
                        else if (type == typeof(BlackPearl.Controls.CoreLibrary.MultiSelectCombobox))
                        {
                            BlackPearl.Controls.CoreLibrary.MultiSelectCombobox MultiSelectCombobox = (BlackPearl.Controls.CoreLibrary.MultiSelectCombobox)element;
                            if (MultiSelectCombobox.SelectedItems != null && MultiSelectCombobox.SelectedItems.Count > 0)
                            {

                                hachCode = string.Join(";", MultiSelectCombobox.SelectedValues("EnglishName")).GetHashCode();
                            }
                            else
                            {
                                hachCode = 0;
                            }
                        }
                        else
                        {
                            throw new System.NotSupportedException("The type " + type.Name + " is not supported by the function");
                        }
                        strHachCode += hachCode.ToString();
                    }
                }
            });
            ListOfisualChildren.Clear();
            return strHachCode.GetHashCode();
        }

        public static string HTMLEntities(string texte, bool decode = false)
        {
            if (string.IsNullOrEmpty(texte))
            {
                return "";
            }
            Dictionary<string, string> HTMLEntitiesEquivalent = new Dictionary<string, string>
            {
                    {"<", "&lt;"},
                    {">", "&gt;"},
                    {"&", "&amp;"},
                    {"\"", "&quot;"},
                    {"'", "&apos;"},
                    {"¢", "&cent;"},
                    {"£", "&pound;"},
                    {"¥", "&yen;"},
                    {"€", "&euro;"},
                    {"©", "&copy;"},
                    {"®", "&reg;"},
                    {"%", "&percnt;"},
                    {"»", "&raquo;"},
                    {"À", "&Agrave;"},
                    {"Ç", "&Ccedil;"},
                    {"È", "&Egrave;"},
                    {"É", "&Eacute;"},
                    {"Ê", "&Ecirc;"},
                    {"Ô", "&Ocirc;"},
                    {"Ù", "&Ugrave;"},
                    {"à", "&agrave;"},
                    {"ß", "&szlig;"},
                    {"á", "&aacute;"},
                    {"â", "&acirc;"},
                    {"æ", "&aelig;"},
                    {"ç", "&ccedil;"},
                    {"è", "&egrave;"},
                    {"é", "&eacute;"},
                    {"ê", "&ecirc;"},
                    {"ë", "&euml;"},
                    {"ô", "&ocirc;"},
                    {"ù", "&ugrave;"},
                    {"ü", "&uuml;"},
                    {"∣", "&mid;"},
            };

            string return_texte = texte;
            foreach (KeyValuePair<string, string> Entities in HTMLEntitiesEquivalent)
            {
                if (decode)
                {
                    return_texte = return_texte.Replace(Entities.Value, Entities.Key);
                }
                else
                {
                    return_texte = return_texte.Replace(Entities.Key, Entities.Value);
                }
            }

            return return_texte;
        }

        public static bool IsUrlValid(string url)
        {
            const string pattern = @"(http|https|ftp|)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_]*)?([a-zA-Z0-9\-\?\,\'\/\+&%\$#_]+)";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

        public static void SetRichTextBoxText(RichTextBox textBox, string text)
        {
            Stream SM = new MemoryStream(Encoding.UTF8.GetBytes(text));
            TextRange range = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
            range.Load(SM, DataFormats.Text);
            SM.Close();
        }

        public static string GetRichTextBoxText(RichTextBox textBox)
        {
            TextRange textRange = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd);
            return textRange.Text;
        }

        public static bool FilterDigitOnlyWhileWritingInTextBox(TextBox textbElement, List<char> char_supplementaire = null, bool onlyOnePoint = true, bool limitLenght = true)
        {
            string textboxtext = textbElement.Text;
            var cursor_position = textbElement.SelectionStart;
            string filtered_string = FilterDigitOnly(textboxtext, char_supplementaire, onlyOnePoint, limitLenght);
            textbElement.SetText(filtered_string);
            if (textboxtext != filtered_string)
            {
                if (cursor_position > 0) textbElement.SelectionStart = cursor_position - 1;
                return false;
            }
            else
            {
                if (cursor_position > 0) textbElement.SelectionStart = cursor_position;
                return true;
            }
        }

        public static bool FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(TextBox textbElement, int MaxInt = -1, List<char> char_supplementaire = null)
        {
            if (textbElement is null)
            {
                return false;
            }
            bool TextHasBeenFilteredAndChanged = false;
            if (FilterDigitOnlyWhileWritingInTextBox(textbElement, char_supplementaire))
            {
                if (!double.TryParse(textbElement?.Text, out double textbElementTextValue)) { return false; }
                if (MaxInt != -1 && textbElementTextValue > MaxInt)
                {
                    string MaxIntString = MaxInt.ToString();
                    textbElement.SetText(MaxIntString);
                    textbElement.SelectionStart = MaxIntString.Length;
                }
                else
                {
                    TextHasBeenFilteredAndChanged = true;
                }
            }
            return TextHasBeenFilteredAndChanged;
        }

        public static string FilterDigitOnly(string origin, List<char> char_supplementaire, bool onlyOnePoint = true, bool limitLenght = true)
        {
            //string str = new string((from c in origin where char.IsDigit(c) select c).ToArray());
            if (string.IsNullOrEmpty(origin)) { return String.Empty; }
            string str = "";
            foreach (char caractere in origin.ToCharArray())
            {
                if (str.Length > 10 && limitLenght)
                {
                    return str;
                }
                char car = Convert.ToChar(caractere);
                if (char.IsDigit(car))
                {
                    str += car.ToString();
                }
                if (!(char_supplementaire is null) && char_supplementaire.Contains(car))
                {
                    if (!(car == '.' && str.Contains(car) && onlyOnePoint))
                    {
                        str += car.ToString();
                    }
                }
            }
            return str;
        }

        public static string Replacements(string tileBaseUrl, string x, string y, string z, int LayerID, Collectif.GetUrl.InvokeFunction invokeFunction)
        {
            if (string.IsNullOrEmpty(tileBaseUrl)) { return string.Empty; }
            return GetUrl.FromTileXYZ(tileBaseUrl, Convert.ToInt32(x), Convert.ToInt32(y), Convert.ToInt32(z), LayerID, invokeFunction).Replace(" ", "%20");
        }

        public static async Task<int> CheckIfDownloadSuccess(string url)
        {
            async Task<HttpStatusCode> InternalCheckIfDownloadSuccess(string internalurl)
            {
                try
                {
                    HttpResponse response = await ByteDownloadUri(new Uri(internalurl), 0, true);
                    if (response is null || response.ResponseMessage is null)
                    {
                        return HttpStatusCode.SeeOther;
                    }

                    HttpResponseMessage httpResponse = response.ResponseMessage;

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        return HttpStatusCode.OK;
                    }
                    else
                    {
                        return httpResponse.StatusCode;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception CIDS : " + ex.Message);
                }

                return HttpStatusCode.SeeOther;
            }

            switch (await InternalCheckIfDownloadSuccess(url))
            {
                case HttpStatusCode.NotFound:
                    return 404;
                case HttpStatusCode.OK:
                    return 200;
                default:
                    return -1;
            }
        }


        public static (double Latitude, double Longitude) GetCenterBetweenTwoPoints((double Latitude, double Longitude) PointA, (double Latitude, double Longitude) PointB)
        {
            //method midpoint from http://www.geomidpoint.com/ -> Thanks
            double RAD(double dg)
            {
                return dg * Math.PI / 180;
            }

            double DEG(double rd)
            {
                return rd * 180 / Math.PI;
            }

            double normalizeLongitude(double lon)
            {
                const double Pi = Math.PI;
                if (lon > Pi)
                {
                    lon -= 2 * Pi; //lon - (2 * n);
                }
                else if (lon < -Pi)
                {
                    lon += 2 * Pi;//lon + (2 * n);
                }
                return lon;
            }

            (double Latitude, double Longitude) Calculate()
            {
                double x = 0;
                double y = 0;
                double x1, y1;
                var latslong = new (double Latitude, double Longitude)[] { (0, 0), (0, 0) };
                var sincos_lats = new (double Sin_Latitude, double Cos_Latitude)[] { (0, 0), (0, 0) };
                var totdays = 0;
                latslong[0].Latitude = RAD(PointA.Latitude);
                latslong[0].Longitude = RAD(PointA.Longitude);
                latslong[1].Latitude = RAD(PointB.Latitude);
                latslong[1].Longitude = RAD(PointB.Longitude);
                sincos_lats[0].Sin_Latitude = Math.Sin(latslong[0].Latitude);
                sincos_lats[0].Cos_Latitude = Math.Cos(latslong[0].Latitude);
                sincos_lats[1].Sin_Latitude = Math.Sin(latslong[1].Latitude);
                sincos_lats[1].Cos_Latitude = Math.Cos(latslong[1].Latitude);
                totdays++;//days1[0];
                x1 = sincos_lats[0].Cos_Latitude * Math.Cos(latslong[0].Longitude);
                y1 = sincos_lats[0].Cos_Latitude * Math.Sin(latslong[0].Longitude);
                x += x1 * 1;
                y += y1 * 1;
                totdays++;//days1[0];
                x1 = sincos_lats[1].Cos_Latitude * Math.Cos(latslong[1].Longitude);
                y1 = sincos_lats[1].Cos_Latitude * Math.Sin(latslong[1].Longitude);
                x += x1 * 1;
                y += y1 * 1;
                x /= totdays;//x = x / totdays;
                y /= totdays;//y = y / totdays;
                double midlng = Math.Atan2(y, x);
                double midlat;
                y = 0;
                x = 0;
                y += latslong[0].Latitude;
                y += latslong[1].Latitude;
                x += normalizeLongitude(latslong[0].Longitude - midlng);
                x += normalizeLongitude(latslong[1].Longitude - midlng);
                midlat = y / totdays;
                midlng = normalizeLongitude((x / totdays) + midlng);
                midlat = DEG(midlat);
                midlng = DEG(midlng);
                return (midlat, midlng);
            }
            return Calculate();
        }

        public static (int X, int Y) CoordonneesToTile(double Latitude, double Longitude, int zoom)
        {
            double ValuetoRadians(float value)
            {
                return value * Math.PI / 180;
            }
            int x = Convert.ToInt32(Math.Floor(((float)Longitude + 180) / 360 * Math.Pow(2, zoom)));
            int y = Convert.ToInt32(Math.Floor((1 - (Math.Log(Math.Tan(ValuetoRadians((float)Latitude)) + (1 / Math.Cos(ValuetoRadians((float)Latitude)))) / Math.PI)) / 2 * Math.Pow(2, zoom)));
            return (x, y);
        }

        public static (double Latitude, double Longitude) TileToCoordonnees(int TileX, int TileY, int zoom)
        {
            double Longitude = (TileX / Math.Pow(2, zoom) * 360) - 180;
            double Latitude = Math.Atan(Math.Sinh(Math.PI * (1 - (2 * TileY / Math.Pow(2, zoom))))) * 180 / Math.PI;
            return (Latitude, Longitude);
        }

        public static string WordWrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || 0 == width || width >= text.Length)
            {
                return text;
            }
            var sb = new StringBuilder();
            var sr = new StringReader(text);
            string line;
            var first = true;
            while (null != (line = sr.ReadLine()))
            {
                var col = 0;
                if (!first)
                {
                    sb.AppendLine();
                    col = 0;
                }
                else
                {
                    first = false;
                }
                char[] wordBreakChars = new char[] { ' ', '_', '\n', '\r', '\v', '\f', '\0' };
                var words = line.Split(wordBreakChars, StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i < words.Length; i++)
                {
                    var word = words[i];
                    if (0 != i)
                    {
                        sb.Append(" ");
                        ++col;
                    }
                    if (col + word.Length > width)
                    {
                        sb.AppendLine();
                        col = 0;
                    }
                    sb.Append(word);
                    col += word.Length;
                }
            }
            return sb.ToString();
        }

    }

    public static class ScrollViewerHelper
    {
        //from https://stackoverflow.com/questions/8932720/listview-inside-of-scrollviewer-prevents-scrollviewer-scroll/61092700#61092700
        // Attached property boilerplate
        public static bool GetFixMouseWheel(ScrollViewer scrollViewer) => (bool)scrollViewer?.GetValue(FixMouseWheelProperty);
        public static void SetFixMouseWheel(ScrollViewer scrollViewer, bool value) => scrollViewer?.SetValue(FixMouseWheelProperty, value);
        public static readonly DependencyProperty FixMouseWheelProperty =
            DependencyProperty.RegisterAttached("FixMouseWheel", typeof(bool), typeof(ScrollViewerHelper),
                new PropertyMetadata(OnFixMouseWheelChanged));
        // End attached property boilerplate

        static void OnFixMouseWheelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ScrollViewer;
            if (scrollViewer == null) return;

            void FixMouseWheel_PreviewMouseWheel(object sender, MouseWheelEventArgs e2)
            {
                var parent = scrollViewer.Parent as UIElement;
                if (parent is null) return;

                var argsCopy = Copy(e2);
                parent.RaiseEvent(argsCopy);
            }

            scrollViewer.PreviewMouseWheel += FixMouseWheel_PreviewMouseWheel;
            scrollViewer.Unloaded += FixMouseWheel_Unloaded;

            void FixMouseWheel_Unloaded(object sender, EventArgs e2)
            {
                scrollViewer.PreviewMouseWheel -= FixMouseWheel_PreviewMouseWheel;
                scrollViewer.Unloaded -= FixMouseWheel_Unloaded;
            }
        }

        static MouseWheelEventArgs Copy(MouseWheelEventArgs e) => new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = e.Source,
        };
    }
}
