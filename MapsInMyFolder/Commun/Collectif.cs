using NetVips;
using NReco.Csv;
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
    public class NameHiddenIdValue
    {
        public object Id { get; }
        public object Name { get; }

        public NameHiddenIdValue(object id, object name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public static class Collectif
    {

        public static DoubleAnimation GetOpacityAnimation(int toValue, double durationMultiplicator = 1)
        {
            return new DoubleAnimation(toValue, TimeSpan.FromMilliseconds((long)(Settings.animations_duration_millisecond * durationMultiplicator)))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut }
            };
        }


        private static bool isAnimating = false; // Indique si l'animation est en cours d'exécution
        public static void AnimateLabel(Label label)
        {
            label.Visibility = Visibility.Visible;
            // Si l'animation est déjà en cours d'exécution, annule l'animation de disparition en cours et recommence l'animation d'apparition à la valeur d'opacité actuelle
            DoubleAnimation fadeInAnimation = new DoubleAnimation();

            if (isAnimating)
            {
                label.BeginAnimation(UIElement.OpacityProperty, null);
                fadeInAnimation.From = label.Opacity;
                fadeInAnimation.To = 1.0;
            }
            else
            {
                fadeInAnimation.From = 0.0;
                fadeInAnimation.To = 1.0;
            }

            TimeSpan fadeInDuration = TimeSpan.FromSeconds(0.2);
            TimeSpan fadeOutDuration = TimeSpan.FromSeconds(0.5);

            fadeInAnimation.Duration = fadeInDuration;
            Storyboard storyboard = new Storyboard();
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = fadeOutDuration,

                // Démarre après l'animation d'apparition + 1 seconde
                BeginTime = fadeInDuration + TimeSpan.FromSeconds(0.5)
            };

            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(fadeOutAnimation);
            Storyboard.SetTarget(fadeInAnimation, label);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(fadeOutAnimation, label);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));

            void AnimationCompleted(object sender, EventArgs e)
            {
                isAnimating = false;
                fadeOutAnimation.Completed -= AnimationCompleted;
            }

            fadeOutAnimation.Completed += AnimationCompleted;

            storyboard.Begin();
            isAnimating = true;
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
                if (stream == null)
                {
                    return string.Empty;
                }
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
            string applicationPath = Environment.ProcessPath;
            Debug.WriteLine("Path : " + applicationPath);
            Debug.WriteLine("Working directory : " + Path.GetDirectoryName(applicationPath));
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

        public static void StartApplication(string path, TimeSpan delay)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c timeout {delay.Seconds} & start /min \"\" \"{path}\"",
                CreateNoWindow = true,       // Ne pas créer de fenêtre CMD
                UseShellExecute = false,     // N'utilise pas le shell
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();
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
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
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
                TextWrapping = TextWrapping.Wrap,
                Foreground = HexValueToSolidColorBrush("#FFE2E2E1"),
                TextAlignment = TextAlignment.Justify,
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

                ContentTextBlock.Inlines.Add(new Run()
                {
                    Text = diff.text,
                    Background = curentBrush,
                });
            }
            return ContentTextBlock;
        }

        public static void SetBackgroundOnUIElement(UIElement element, string hexcolor)
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

        public static string GetSaveTempDirectory(string nom, string identifier, int zoom = -1, string temp_folder = "")
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
            string chemin = Path.Combine(settings_temp_folder, "layers", nom_charclean + "_" + identifier + "\\");
            if (zoom != -1)
            {
                chemin += zoom + "\\";
            }
            return chemin;
        }

        public static MemoryStream ByteArrayToStream(byte[] input)
        {
            if (input == null) { return null; }
            return new MemoryStream(input);
        }

        public static byte[] GetBytesFromBitmapSource(this BitmapSource bmp)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapFrame bitmap = BitmapFrame.Create(bmp);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(bitmap);
                encoder.Save(stream);
                byte[] bit = stream.ToArray();
                stream.Close();
                return bit;
            }
        }

        public static byte[] GetEmptyImageBufferFromText(TileLoader.HttpResponse httpResponse, int LayerID, string FileFormat)
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

            return GetEmptyImageBufferFromText(BitmapErrorsMessage, LayerID, FileFormat);
        }

        public static byte[] GetEmptyImageBufferFromText(string BitmapErrorsMessage, int LayerID, string FileFormat)
        {
            const int tile_size = 300;
            const int border_size = 1;
            const int border_tile_size = tile_size - (border_size * 2);
            if (FileFormat == "png" || LayerID == -2)
            {
                const string format = "png";
                double[] color = new double[] { Settings.background_layer_color_R, Settings.background_layer_color_G, Settings.background_layer_color_B, 60 };
                VOption saveVOption = Downloader.GetSaveVOption(format, 100, tile_size);

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
                VOption saveVOption = Downloader.GetSaveVOption(format, 100, tile_size);

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

            return string.Format("{0:0} {1}", dblSByte, Suffix[i]);
        }



        //public static async Task<HttpResponse> ByteDownloadUri(Uri url, int LayerId, bool getRealRequestMessage = false)
        //{

        //    if (!Network.FastIsNetworkAvailable())
        //    {
        //        return new HttpResponse(null, new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        //        {
        //            ReasonPhrase = "Aucune connexion : Vérifiez votre connexion Internet"
        //        });
        //    }

        //    HttpResponse response = HttpResponse.HttpResponseError;
        //    int maxRetry = Settings.max_redirection_download_tile;
        //    int retry = 0;
        //    bool doNeedToRetry;
        //    Uri parsing_url = url;
        //    do
        //    {
        //        doNeedToRetry = false;
        //        retry++;

        //        try
        //        { 
        //            using (var responseMessage = await Tiles.HttpClient.GetAsync(parsing_url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
        //            {
        //                if (responseMessage is null)
        //                {
        //                    return response;
        //                }

        //                if (responseMessage.IsSuccessStatusCode)
        //                {
        //                    byte[] buffer = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        //                    return new HttpResponse(buffer, responseMessage);
        //                }
        //                else if (!string.IsNullOrWhiteSpace(responseMessage?.Headers?.Location?.ToString()?.Trim()))
        //                {
        //                    // Redirect found (autodetect)  System.Net.HttpStatusCode.Found
        //                    Uri new_location = responseMessage.Headers.Location;
        //                    doNeedToRetry = true;
        //                    parsing_url = new_location;
        //                }
        //                else
        //                {
        //                    if (LayerId == -2)
        //                    {
        //                        Javascript.Functions.PrintError($"DownloadUrl - Error {(int)responseMessage.StatusCode} : {responseMessage.ReasonPhrase}. Url : {parsing_url}");
        //                    }

        //                    if (Settings.generate_transparent_tiles_on_error)
        //                    {
        //                        if (getRealRequestMessage)
        //                        {
        //                            return new HttpResponse(null, responseMessage);
        //                        }
        //                        else
        //                        {
        //                            return new HttpResponse(null, new System.Net.Http.HttpResponseMessage(HttpStatusCode.NotFound));
        //                        }
        //                    }
        //                }
        //                if (getRealRequestMessage)
        //                {
        //                    response = new HttpResponse(response.Buffer, responseMessage);
        //                }

        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            if (ex.InnerException is System.Net.Sockets.SocketException socketException && socketException.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
        //            {
        //                // Check for internet connectivity when the host is unknown.
        //                Network.IsNetworkAvailable();
        //            }
        //            Debug.WriteLine($"DownloadByteUrl catch: {url}: {ex}");

        //            if (LayerId == -2)
        //            {
        //                Javascript.Functions.PrintError($"DownloadUrl - Error {ex.Message}. Url : {url}");
        //            }
        //            response = new HttpResponse(null, null, ex.Message);
        //        }
        //        finally
        //        {

        //        }
        //    } while (doNeedToRetry && (retry < maxRetry));

        //    return response;
        //}
        public static string AddHttpToUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "http://" + url;
                }
            }
            return url;
        }

        public static async Task<TileLoader.HttpResponse> ByteDownloadUri(Uri url, int LayerId, bool getRealRequestMessage = false)
        {
            if (!Network.FastIsNetworkAvailable())
            {
                return new TileLoader.HttpResponse(null, new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    ReasonPhrase = "Aucune connexion : Vérifiez votre connexion Internet"
                });
            }

            TileLoader.HttpResponse response = TileLoader.HttpResponse.HttpResponseError;
            string originalReferer = Layers.GetLayerById(LayerId)?.SiteUrl;

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
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, parsing_url);
                    if (!string.IsNullOrWhiteSpace(originalReferer))
                    {
                        requestMessage.Headers.Referrer = new Uri(AddHttpToUrl(originalReferer)); // Ajoute le referer original à la requête
                    }

                    using var responseMessage = await Tiles.HttpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    if (responseMessage is null)
                    {
                        return response;
                    }

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        byte[] buffer = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        return new TileLoader.HttpResponse(buffer, responseMessage);
                    }
                    else if (!string.IsNullOrWhiteSpace(responseMessage?.Headers?.Location?.ToString()?.Trim()))
                    {
                        // Redirect found (autodetect)  System.Net.HttpStatusCode.Found
                        Uri new_location = responseMessage.Headers.Location;
                        doNeedToRetry = true;
                        parsing_url = new_location;
                    }
                    else
                    {
                        if (LayerId == -2)
                        {
                            var ErrorPageContent = "";
                            if (responseMessage?.Content != null)
                            {
                                byte[] buffer = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                                var TempErrorPageContent = ByteArrayToString(buffer);
                                if (!string.IsNullOrWhiteSpace(TempErrorPageContent))
                                {
                                    ErrorPageContent = "\n" + TempErrorPageContent + "\n";
                                }
                            }
                            Javascript.Functions.PrintError($"DownloadUrl - Error {(int)responseMessage.StatusCode} : {responseMessage.ReasonPhrase}. Url : {parsing_url}" + ErrorPageContent);
                        }

                        if (Settings.generate_transparent_tiles_on_error)
                        {
                            if (getRealRequestMessage)
                            {
                                return new TileLoader.HttpResponse(null, responseMessage);
                            }
                            else
                            {
                                return new TileLoader.HttpResponse(null, new System.Net.Http.HttpResponseMessage(HttpStatusCode.NotFound));
                            }
                        }
                    }
                    if (getRealRequestMessage)
                    {
                        response = new TileLoader.HttpResponse(response.Buffer, responseMessage);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is System.Net.Sockets.SocketException socketException && socketException.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
                    {
                        // Check for internet connectivity when the host is unknown.
                        Network.IsNetworkAvailable();
                    }
                    Debug.WriteLine($"DownloadByteUrl catch: {url}: {ex}");

                    if (LayerId == -2)
                    {
                        Javascript.Functions.PrintError($"DownloadUrl - Error {ex.Message}. Url : {url}");
                    }
                    response = new TileLoader.HttpResponse(null, null, ex.Message);
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
                        var bytesRead = await contentStream.ReadAsync(buffer);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            TriggerProgressChanged(totalDownloadSize, totalBytesRead);
                            continue;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));

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

            if (File.Exists(Path.Combine(save_temp_directory, filename)))
            {
                FileInfo filinfo = new FileInfo(Path.Combine(save_temp_directory, filename));
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

        public static void LockPreviousUndo(TextBox uIElement)
        {
            int undo_limit = uIElement.UndoLimit;
            uIElement.UndoLimit = 0;
            uIElement.UndoLimit = undo_limit;
        }





        public static void TextEditorCursorPositionChanged(ICSharpCode.AvalonEdit.TextEditor textEditor, Grid grid, ScrollViewer scrollViewer, int MarginTop = 25)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed) { return; }
            const double Margin = 40;
            double TextboxLayerScriptTopPosition = textEditor.TranslatePoint(new Point(0, 0), grid).Y;
            double TextboxLayerScriptCaretTopPosition = textEditor.TextArea.Caret.CalculateCaretRectangle().Top + TextboxLayerScriptTopPosition;
            if (TextboxLayerScriptCaretTopPosition > (grid.ActualHeight - Margin))
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + (TextboxLayerScriptCaretTopPosition - (grid.ActualHeight - Margin)));
            }
            else if (TextboxLayerScriptCaretTopPosition < MarginTop)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - Math.Abs((MarginTop) - TextboxLayerScriptCaretTopPosition));
            }
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
                    if (objChild is not null)
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

        public static int CheckIfInputValueHaveChange(UIElement SourcePanel)
        {
            List<System.Type> TypeOfSearchElement = new List<System.Type>
            {
                typeof(TextBox),
                typeof(ComboBox),
                typeof(CheckBox),
                typeof(RadioButton),
                typeof(UserControls.TitleScript),
                typeof(UserControls.ScriptEditor),
                typeof(ICSharpCode.AvalonEdit.TextEditor),
                typeof(BlackPearl.Controls.CoreLibrary.MultiSelectCombobox)
            };

            List<UIElement> ListOfisualChildren = Collectif.FindVisualChildren(SourcePanel, TypeOfSearchElement);

            string strHachCode = string.Empty;
            ListOfisualChildren.ForEach(element =>
            {
                if (TypeOfSearchElement.Contains(element.GetType()))
                {
                    string elementXName = element.GetName();
                    if (elementXName != null)
                    {
                        int hachCode = 0;
                        Type type = element.GetType();
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
                        else if (type == typeof(UserControls.TitleScript))
                        {
                            UserControls.TitleScript TitleScript = (UserControls.TitleScript)element;
                            hachCode = TitleScript.ScriptName.GetHashCode();
                            hachCode += TitleScript.Script.GetHashCode();
                        }
                        else if (type == typeof(UserControls.ScriptEditor))
                        {
                            UserControls.ScriptEditor TitleScript = (UserControls.ScriptEditor)element;
                            hachCode = TitleScript?.Script?.GetHashCode() ?? 0;
                        }
                        else if (type == typeof(BlackPearl.Controls.CoreLibrary.MultiSelectCombobox))
                        {
                            BlackPearl.Controls.CoreLibrary.MultiSelectCombobox MultiSelectCombobox = (BlackPearl.Controls.CoreLibrary.MultiSelectCombobox)element;
                            if (MultiSelectCombobox.SelectedItems != null && MultiSelectCombobox.SelectedItems.Count > 0)
                            {

                                hachCode = string.Join(";", MultiSelectCombobox.SelectedValuesAsString("EnglishName")).GetHashCode();
                            }
                            else
                            {
                                hachCode = 0;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException("The type " + type.Name + " is not supported by the function");
                        }
                        strHachCode += hachCode.ToString();
                    }
                }
            });
            ListOfisualChildren.Clear();
            return strHachCode.GetHashCode();
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
                if (child is not T)
                {
                    // recursively drill down the tree
                    foundChild = FindChildByName<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    // If the child's name is set for search
                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
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

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
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

        public static bool FilterDigitOnlyWhileWritingInTextBox(TextBox textbElement, List<char> char_supplementaire = null, bool onlyOnePoint = true, bool limitLenght = true)
        {
            string textboxtext = textbElement.Text;
            var cursor_position = textbElement.SelectionStart;
            string filtered_string = FilterDigitOnly(textboxtext, char_supplementaire, onlyOnePoint, limitLenght);
            textbElement.Text = filtered_string;
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
                    textbElement.Text = MaxIntString;
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
            if (string.IsNullOrEmpty(origin)) { return string.Empty; }
            string str = "";
            foreach (char caractere in origin)
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
                if (char_supplementaire is not null && char_supplementaire.Contains(car))
                {
                    if (!(car == '.' && str.Contains(car) && onlyOnePoint))
                    {
                        str += car.ToString();
                    }
                }
            }
            return str;
        }

        public static string Replacements(string tileBaseUrl, string x, string y, string z, int LayerID, Javascript.InvokeFunction invokeFunction)
        {
            if (string.IsNullOrEmpty(tileBaseUrl)) { return string.Empty; }
            return GetUrl.FromTileXYZ(tileBaseUrl, Convert.ToInt32(x), Convert.ToInt32(y), Convert.ToInt32(z), LayerID, invokeFunction).Replace(" ", "%20");
        }

        public static async Task<int> CheckIfDownloadSuccess(string url)
        {
            static async Task<HttpStatusCode> InternalCheckIfDownloadSuccess(string internalurl)
            {
                try
                {
                    TileLoader.HttpResponse response = await ByteDownloadUri(new Uri(internalurl), 0, true);
                    if (response is null || response.ResponseMessage is null)
                    {
                        return HttpStatusCode.SeeOther;
                    }

                    HttpResponseMessage httpResponse = response.ResponseMessage;
                    Debug.WriteLine("CheckIfDownloadSuccess :" + httpResponse.StatusCode.ToString());
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

            return await InternalCheckIfDownloadSuccess(url) switch
            {
                HttpStatusCode.NotFound => 404,
                HttpStatusCode.OK => 200,
                _ => -1,
            };
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
            if (Latitude is double.NaN || Longitude is double.NaN)
            {
                return (0, 0);
            }
            try
            {
                static double ValuetoRadians(float value)
                {
                    return value * Math.PI / 180;
                }
                int x = Convert.ToInt32(Math.Floor(((float)Longitude + 180) / 360 * Math.Pow(2, zoom)));
                int y = Convert.ToInt32(Math.Floor((1 - (Math.Log(Math.Tan(ValuetoRadians((float)Latitude)) + (1 / Math.Cos(ValuetoRadians((float)Latitude)))) / Math.PI)) / 2 * Math.Pow(2, zoom)));
                return (x, y);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Message.NoReturnBoxAsync($"Unable to convert latitude and longitude into tile coordinates.\nLatitude : {Latitude}, Longitude : {Longitude}, Zoom : {zoom}\n" + ex.Message, Languages.Current["dialogTitleOperationFailed"]);
                return (0, 0);
            }
        }

        public static (double Latitude, double Longitude) TileToCoordonnees(int TileX, int TileY, int zoom)
        {
            double Longitude = (TileX / Math.Pow(2, zoom) * 360) - 180;
            double Latitude = Math.Atan(Math.Sinh(Math.PI * (1 - (2 * TileY / Math.Pow(2, zoom))))) * 180 / Math.PI;
            return (Latitude, Longitude);
        }

        public static string WordWrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width == 0 || width >= text.Length)
            {
                return text;
            }
            var sb = new StringBuilder();
            var sr = new StringReader(text);
            string line;
            var first = true;
            while ((line = sr.ReadLine()) != null)
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
                    if (i != 0)
                    {
                        sb.Append(' ');
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

        public static string[][] ParseCSV(string input, string delimiter = "\t")
        {
            var csvStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input));
            var csvReader = new CsvReader(new StreamReader(csvStream), delimiter);
            var Lines = new List<string[]>();
            while (csvReader.Read())
            {
                var Field = new List<string>();
                for (int i = 0; i < csvReader.FieldsCount; i++)
                {
                    Field.Add(csvReader[i]);
                }
                Lines.Add(Field.ToArray());
            }
            return Lines.ToArray();
        }













    }

    public static class ScrollViewerHelper
    {
        //from https://stackoverflow.com/questions/8932720/listview-inside-of-scrollviewer-prevents-scrollviewer-scroll/61092700#61092700

        public static void SetScrollViewerMouseWheelFix(ScrollViewer scrollViewer)
        {
            if (scrollViewer == null) return;

            scrollViewer.PreviewMouseWheel += FixMouseWheel_PreviewMouseWheel;
            scrollViewer.Unloaded += FixMouseWheel_Unloaded;

            void FixMouseWheel_PreviewMouseWheel(object sender, MouseWheelEventArgs e2)
            {
                if (scrollViewer.Parent is not UIElement parent)
                {
                    return;
                };

                var argsCopy = Copy(e2);
                parent.RaiseEvent(argsCopy);
            }

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
