using Jint;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using MapsInMyFolder.Commun;
using NetVips;

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
                string finalurl;
                if (string.IsNullOrEmpty(urlbase))
                {
                    finalurl = calque.class_tile_url;
                }
                else
                {
                    finalurl = urlbase;
                }
                List<double> location_topleft = TileToCoordonnees(Tilex, Tiley, z);
                List<double> location_bottomright = TileToCoordonnees(Tilex + 1, Tiley + 1, z);
                List<double> location = GetCenterBetweenTwoPoints(location_topleft, location_bottomright);

                Dictionary<string, object> argument = new Dictionary<string, object>()
                {
                      { "x",  Tilex.ToString() },
                      { "y",  Tiley.ToString() },
                      { "z",  z.ToString() },
                      { "zoom",  z.ToString() },
                      { "lat",  location[0].ToString() },
                      { "lng",  location[1].ToString()},

                      { "t_lat",  location_topleft[0].ToString() },
                      { "t_lng",  location_topleft[1].ToString()},
                      { "b_lat",  location_bottomright[0].ToString() },
                      { "b_lng",  location_bottomright[1].ToString()},

                      { "layerid",  LayerID.ToString() },
                      { "url",  urlbase },
                };
                //Jint.Native.JsValue JavascriptMainResult = Commun.Javascript.ExecuteScript("function main() { var js = new TheType(); log(js.TestDoubleReturn(0,0)); return args; }", argument);

                string TileComputationScript = calque.class_tilecomputationscript;

                if (!string.IsNullOrEmpty(TileComputationScript))
                {
                    Jint.Native.JsValue JavascriptMainResult = null;
                    try
                    {
                        JavascriptMainResult = Javascript.ExecuteScript(TileComputationScript, argument, LayerID, InvokeFunction);
                    }
                    catch (Exception ex)
                    {
                        Javascript.PrintError(ex.Message);
                    }
                    if (!(JavascriptMainResult is null) && JavascriptMainResult.IsObject())
                    {
                        object JavascriptMainResultObject = JavascriptMainResult.ToObject();
                        var JavascriptMainResultJson = JsonConvert.SerializeObject(JavascriptMainResultObject);
                        var JavascriptMainResultDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(JavascriptMainResultJson);

                        if (JavascriptMainResultDictionary.TryGetValue("url", out string urlResult))
                        {
                            if (!string.IsNullOrEmpty(urlResult))
                            {
                                finalurl = urlResult;
                            }
                        }

                        foreach (var JavascriptReplacementVar in JavascriptMainResultDictionary)
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
                List<int> NO_tile = CoordonneesToTile(location["NO_Latitude"], location["NO_Longitude"], z);
                List<int> SE_tile = CoordonneesToTile(location["SE_Latitude"], location["SE_Longitude"], z);
                int NO_x = NO_tile[0];
                int NO_y = NO_tile[1];
                int SE_x = SE_tile[0];
                int SE_y = SE_tile[1];

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
                        DebugMode.WriteLine("Need to download tuileX:" + tuileX + " tuileY:" + tuileY);
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
            return new DoubleAnimation(toValue, TimeSpan.FromTicks((long)(Settings.animations_duration.Ticks * durationMultiplicator)))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
            };
        }

        //private static void GetAllManifestResourceNames()
        //{
        //    Assembly assembly = Assembly.GetEntryAssembly();
        //    //get assembly ManifestResourceNames
        //    Debug.WriteLine("get assembly ManifestResourceNames");
        //    for (int i = 0; i < assembly.GetManifestResourceNames().Length; i++)
        //    {
        //        string name = assembly.GetManifestResourceNames()[i];
        //        Debug.WriteLine("ManifestResourceNames : " + name);
        //    }
        //}

        public static string ReadResourceString(string pathWithSlash)
        {
            Stream stream = ReadResourceStream(pathWithSlash);
            string return_rsx = String.Empty;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return_rsx = reader.ReadToEnd();
            }
            return return_rsx;
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

        public static SolidColorBrush HexValueToSolidColorBrush(string hexvalue)
        {
            if (!string.IsNullOrEmpty(hexvalue))
            {
                hexvalue = hexvalue.Trim('#');
            }
            if (!string.IsNullOrEmpty(hexvalue) && (Int32.TryParse(hexvalue, System.Globalization.NumberStyles.HexNumber, null, out _)))
            {
                if (hexvalue.Length == 3)
                {
                    return (SolidColorBrush)new BrushConverter().ConvertFrom('#' + string.Concat(hexvalue[0], hexvalue[0], hexvalue[1], hexvalue[1], hexvalue[2], hexvalue[2]));
                }
                else if (hexvalue.Length == 6)
                {
                    return (SolidColorBrush)new BrushConverter().ConvertFrom('#' + hexvalue);
                }
            }
            return RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B);
        }

        public static SolidColorBrush RgbValueToSolidColorBrush(int R, int G, int B)
        {
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)R, (byte)G, (byte)B));
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
                Background = Collectif.HexValueToSolidColorBrush("#303031"),
            };
            ScrollViewerElementContent.Children.Add(FormatDiffTextblock);
            ScrollViewerElement.Content = ScrollViewerElementContent;
            return ScrollViewerElement;
        }


        public static TextBlock FormatDiff(string texteBase, string texteModif)
        {
            DiffMatchPatch dmp = new DiffMatchPatch();
            dmp.Diff_Timeout = 0;
            TextBlock ContentTextBlock = new TextBlock()
            {
                TextWrapping = TextWrapping.WrapWithOverflow,
                Foreground = Collectif.HexValueToSolidColorBrush("#FFE2E2E1"),
                TextAlignment = TextAlignment.Justify
            };
            List<Diff> diffs = dmp.diff_main(texteBase, texteModif);
            SolidColorBrush BackgroundGreen = Collectif.HexValueToSolidColorBrush("#6b803f");
            SolidColorBrush BackgroundRed = Collectif.HexValueToSolidColorBrush("#582e2e");
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
                brush = Collectif.RgbValueToSolidColorBrush(Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B); ;
            }
            else
            {
                brush = Collectif.HexValueToSolidColorBrush(hexcolor);
            }

            //element.GetType().GetProperty("Background").GetValue(element);
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
            //Debug.WriteLine(settings_temp_folder);
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

        static readonly object Locker = new object();
        public static byte[] GetEmptyImageBufferFromText(HttpResponse httpResponse)
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
                BitmapErrorsMessage = (((int)httpResponse?.ResponseMessage?.StatusCode).ToString() + " - " + httpResponse?.ResponseMessage?.ReasonPhrase);
            }

            return GetEmptyImageBufferFromText(BitmapErrorsMessage);
        }

        public static byte[] GetEmptyImageBufferFromText(string BitmapErrorsMessage)
        {
            const int tile_size = 300;
            const int border_size = 1;
            if (string.IsNullOrEmpty(BitmapErrorsMessage))
            {
                return null;
            }
            lock (Locker)
            {

                using (NetVips.Image text = NetVips.Image.Text(WordWrap(BitmapErrorsMessage, 20), null, null, null, NetVips.Enums.Align.Centre, null, 100, true, 5, null))
                {
                    const int border_tile_size = tile_size - (border_size * 2);
                    int offsetX = (int)Math.Floor((double)(border_tile_size - text.Width) / 2);
                    int offsetY = (int)Math.Floor((double)(border_tile_size - text.Height) / 2);
                    int[] graycolor = new int[] { 100, 100, 100 };
                    const string format = "jpeg";
                    NetVips.VOption saveVOption = getSaveVOption(format, 100, tile_size);
                    var color = new double[] { Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B };
                    using (NetVips.Image image = NetVips.Image.Black(border_tile_size, border_tile_size).Linear(color, color).Composite2(text, NetVips.Enums.BlendMode.Atop, offsetX, offsetY))
                    using (NetVips.Image border = NetVips.Image.Black(tile_size, tile_size))
                    {
                        try
                        {
                            return border.Insert(image, border_size, border_size).WriteToBuffer("." + format, saveVOption);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            return null;
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

        public static VOption getSaveVOption(string final_saveformat, int quality, int tile_size)
        {
            if (quality <= 0)
            {
                quality = 1;
            }

            VOption saving_options;
            if (final_saveformat == "png")
            {
                saving_options = new VOption
                    {
                        { "Q", quality },
                        { "compression", 100 },
                        { "interlace", true },
                        { "strip", true },
                    };
            }
            else if (final_saveformat == "jpeg")
            {
                saving_options = new VOption
                    {
                        { "Q", quality },
                        { "interlace", true },
                        { "optimize_coding", true },
                        { "strip", true },

                    };
            }
            else if (final_saveformat == "tiff")
            {
                saving_options = new VOption
                    {
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

        public static async Task<Stream> StreamDownloadUri(Uri url)
        {
            try
            {
                using (var responseMessage = await TileGeneratorSettings.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (responseMessage.IsSuccessStatusCode)
                    {
                        return await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Debug.WriteLine($"DownloadStreamUrl: {url}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DownloadStreamUrl: {url}: {ex.Message}");
            }

            return null;
        }

        public static async Task<HttpResponse> ByteDownloadUri(Uri url, int LayerId, bool getRealRequestMessage = false)
        {
            HttpResponse response = HttpResponse.HttpResponseError;

            int max_retry = Settings.max_redirection_download_tile;
            int retry = 0;

            bool do_need_retry;

            Uri parsing_url = url;

            do
            {
                do_need_retry = false;
                retry++;

                try
                {
                    using (var responseMessage = await TileGeneratorSettings.HttpClient.GetAsync(parsing_url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
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
                        else if (!(responseMessage is null) && !(responseMessage.Headers is null) && !(responseMessage.Headers.Location is null) && !string.IsNullOrEmpty(responseMessage.Headers.Location.ToString().Trim()))
                        {
                            // Redirect found (autodetect)  System.Net.HttpStatusCode.Found
                            Uri new_location = responseMessage.Headers.Location;
                            DebugMode.WriteLine($"DownloadByteUrl Retry : {parsing_url} to {new_location}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                            do_need_retry = true;
                            parsing_url = new_location;
                        }
                        else
                        {
                            DebugMode.WriteLine($"DownloadByteUrl: {parsing_url}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                            if (LayerId == -2)
                            {
                                Javascript.PrintError($"DownloadUrl - Error {(int)responseMessage.StatusCode} : {responseMessage.ReasonPhrase}. Url : {parsing_url}");
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
                    Debug.WriteLine($"DownloadByteUrl catch: {url}: {ex.Message}");
                    if (LayerId == -2)
                    {
                        Javascript.PrintError($"DownloadUrl - Error {ex.Message}. Url : {url}");
                    }
                    response = new HttpResponse(null, null, ex.Message);
                }
            } while (do_need_retry && (retry < max_retry));

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
                using (var response = await TileGeneratorSettings.HttpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
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

        public static Boolean CheckIfDownloadIsNeededOrCached(string save_temp_directory, string filename, int settings_max_tiles_cache_days)
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
        public static void InsertTextAtCaretPosition(TextBox TextBox, string text)
        {
            if (TextBox.SelectionLength == 0)
            {
                int CaretIndex = TextBox.CaretIndex;
                TextBox.Text = TextBox.Text.Insert(CaretIndex, text);
                TextBox.CaretIndex = CaretIndex + text.Length;
            }
            else
            {
                TextBox.SelectedText = text;
                TextBox.CaretIndex += TextBox.SelectedText.Length;
                TextBox.SelectionLength = 0;
            }
            int lineIndex = TextBox.GetLineIndexFromCharacterIndex(TextBox.CaretIndex);
            TextBox.ScrollToLine(lineIndex);
        }


        public static List<UIElement> FindVisualChildren(UIElement obj, List<System.Type> BlackListNoSearchChildren = null)
        {
            List<UIElement> children = new List<UIElement>();

            if (obj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
                {
                    UIElement objChild = (UIElement)VisualTreeHelper.GetChild(obj, i);
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
        public static T FindChild<T>(DependencyObject parent, string childName)
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
                    foundChild = FindChild<T>(child, childName);

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


        public static int CheckIfInputValueHaveChange(UIElement SourcePanel)
        {

            List<System.Type> TypeOfSearchElement = new List<System.Type>
            {
                typeof(TextBox),
                typeof(ComboBox),
                typeof(CheckBox),
                typeof(RadioButton)
            };

            var ListOfisualChildren = FindVisualChildren(SourcePanel, TypeOfSearchElement);

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
                        else
                        {
                            throw new System.NotSupportedException("The type " + type.Name + " is not supported by the function");
                        }
                        strHachCode += hachCode.ToString();
                    }
                }
            });
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
            //MessageBox.Show(return_texte);
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
            string return_texte = textRange.Text;
            return return_texte;
        }

        public static bool FilterDigitOnlyWhileWritingInTextBox(TextBox textbElement, List<char> char_supplementaire = null)
        {
            string textboxtext = textbElement.Text;
            var cursor_position = textbElement.SelectionStart;
            string filtered_string = FilterDigitOnly(textboxtext, char_supplementaire);
            textbElement.Text = filtered_string;
            if (textboxtext != filtered_string)
            {
                textbElement.SelectionStart = cursor_position - 1;
                return false;
            }
            else
            {
                textbElement.SelectionStart = cursor_position;
                return true;
            }
        }

        public static bool FilterDigitOnlyWhileWritingInTextBox(TextBox textbElement, System.Windows.Controls.TextChangedEventHandler action, int MaxInt = -1)
        {
            if (textbElement is null)
            {
                return false;
            }
            bool TextHasBeenFilteredAndChanged = false;
            textbElement.TextChanged -= action;
            if (FilterDigitOnlyWhileWritingInTextBox(textbElement))
            {
                if (MaxInt != -1 && Convert.ToUInt32(textbElement.Text) > MaxInt)
                {
                    string MaxIntString = MaxInt.ToString();
                    textbElement.Text = MaxIntString;
                    textbElement.SelectionStart = MaxIntString.Length;
                }
                else
                {
                    TextHasBeenFilteredAndChanged = true;
                }
            }
            textbElement.TextChanged += action;

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
                if (!(char_supplementaire is null))
                {
                    if (!(car == '.' && str.Contains(car) && onlyOnePoint))
                    {
                        if (char_supplementaire.Contains(car))
                        {
                            str += car.ToString();
                        }
                    }
                }
            }
            return str;
        }

        public static string Replacements(string tileBaseUrl, string x, string y, string z, int LayerID, Collectif.GetUrl.InvokeFunction invokeFunction)
        {
            if (string.IsNullOrEmpty(tileBaseUrl)) { return String.Empty; }
            return GetUrl.FromTileXYZ(tileBaseUrl, Convert.ToInt32(x), Convert.ToInt32(y), Convert.ToInt32(z), LayerID, invokeFunction).Replace(" ", "%20");
        }

        public static int CheckIfDownloadSuccess(string url)
        {
            async Task<HttpStatusCode> InternalCheckIfDownloadSuccess(string internalurl)
            {
                try
                {
                    HttpResponse reponseHttpResponse = await ByteDownloadUri(new Uri(internalurl), 0, true);
                    if (reponseHttpResponse is null || reponseHttpResponse.ResponseMessage is null)
                    {
                        return HttpStatusCode.SeeOther;
                    }
                    HttpResponseMessage reponse = reponseHttpResponse.ResponseMessage;
                    if (reponse.IsSuccessStatusCode)
                    {
                        return HttpStatusCode.OK;
                    }
                    else
                    {
                        return reponse.StatusCode;
                    }
                }
                catch (Exception a)
                {
                    Debug.WriteLine("Exception CIDS : " + a.Message);
                }
                return HttpStatusCode.SeeOther;
            }

            HttpStatusCode code = InternalCheckIfDownloadSuccess(url).Result;
            switch (code)
            {
                case HttpStatusCode.NotFound:
                    return 404;
                case HttpStatusCode.OK:
                    return 200;
                default: return -1;
            }
        }

        public static List<Double> GetCenterBetweenTwoPoints(List<Double> PointA, List<Double> PointB)
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

            List<Double> Calculate(List<Double> C_PointA, List<Double> C_PointB)
            {
                double x = 0;
                double y = 0;
                double x1, y1;
                var latslong = new (double Latitude, double Longitude)[] { (0, 0), (0, 0) };
                var sincos_lats = new (double Sin_Latitude, double Cos_Latitude)[] { (0, 0), (0, 0) };
                var totdays = 0;
                latslong[0].Latitude = RAD(C_PointA[0]);
                latslong[0].Longitude = RAD(C_PointA[1]);
                latslong[1].Latitude = RAD(C_PointB[0]);
                latslong[1].Longitude = RAD(C_PointB[1]);
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
                return new List<Double>() { midlat, midlng };
            }
            return Calculate(PointA, PointB);
        }

        public static List<int> CoordonneesToTile(double Latitude, double Longitude, int zoom)
        {
            double ValuetoRadians(float value)
            {
                return value * Math.PI / 180;
            }
            int x = Convert.ToInt32(Math.Floor(((float)Longitude + 180) / 360 * Math.Pow(2, zoom)));
            int y = Convert.ToInt32(Math.Floor((1 - (Math.Log(Math.Tan(ValuetoRadians((float)Latitude)) + (1 / Math.Cos(ValuetoRadians((float)Latitude)))) / Math.PI)) / 2 * Math.Pow(2, zoom)));
            return new List<int>() { x, y };
        }

        public static List<double> TileToCoordonnees(int TileX, int TileY, int zoom)
        {
            double x = (TileX / Math.Pow(2, zoom) * 360) - 180;
            double y = Math.Atan(Math.Sinh(Math.PI * (1 - (2 * TileY / Math.Pow(2, zoom))))) * 180 / Math.PI;
            return new List<double>() { x, y };
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
}
