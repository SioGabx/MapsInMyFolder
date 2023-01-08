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
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Media;
using System.Text.RegularExpressions;

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
            public static int numberofurlgenere = 0;
            public static string FromTileXYZ(string urlbase, int x, int y, int z, int LayerID)
            {
                if (LayerID == -1)
                {
                    return urlbase;
                }
                string finalurl = urlbase;
                List<double> location_topleft = Commun.Collectif.TileToCoordonnees(x, y, z);
                List<double> location_bottomright = Commun.Collectif.TileToCoordonnees(x + 1, y + 1, z);
                List<double> location = Commun.Collectif.GetCenterBetweenTwoPoints(location_topleft, location_bottomright);
                Dictionary<string, object> argument = new Dictionary<string, object>()
                {
                      { "x",  x.ToString() },
                     { "y",  y.ToString() },
                      { "z",  z.ToString() },
                    { "zoom",  z.ToString() },
                      { "lat",  location[0].ToString() },
                      { "lng",  location[1].ToString()},
                      { "t_lat",  location_topleft[0].ToString() },
                      { "t_lng",  location_topleft[1].ToString()},
                      { "b_lat",  location_bottomright[0].ToString() },
                      { "b_lng",  location_bottomright[1].ToString()},
                      { "layerid",  LayerID.ToString() },
                };
                //Jint.Native.JsValue JavascriptMainResult = Commun.Javascript.ExecuteScript("function main() { var js = new TheType(); log(js.TestDoubleReturn(0,0)); return args; }", argument);
                Layers calque = Layers.GetLayerById(LayerID);
                string TileComputationScript;
                if (calque is null)
                {
                    return "";
                }
                else
                {
                    TileComputationScript = calque.class_tilecomputationscript;
                }
                if (!string.IsNullOrEmpty(TileComputationScript))
                {
                    Jint.Native.JsValue JavascriptMainResult = null;
                    try
                    {
                        //JavascriptMainResult = Commun.Javascript.ExecuteScript(TileComputationScript, argument, LayerID);
                        DebugMode.WriteLine("DEBUG JS : LayerId" + LayerID);
                        JavascriptMainResult = Commun.Javascript.ExecuteScript(TileComputationScript, argument, LayerID);
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
                numberofurlgenere++;
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
                List<int> NO_tile = Collectif.CoordonneesToTile(location["NO_Latitude"], location["NO_Longitude"], z);
                List<int> SE_tile = Collectif.CoordonneesToTile(location["SE_Latitude"], location["SE_Longitude"], z);
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
                        string url_to_add_inside_list = FromTileXYZ(urlbase, tuileX, tuileY, z, LayerID);
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
            //"#BCBCBC"
            if (!hexvalue.StartsWith("#"))
            {
                hexvalue = "#" + hexvalue;
            }
            return (SolidColorBrush)new BrushConverter().ConvertFrom(hexvalue);
        }

        public static string GetSaveTempDirectory(string nom, string identifiant, int zoom = -1, string temp_folder = "")
        {
            string settings_temp_folder;
            if (string.IsNullOrEmpty(temp_folder))
            {
                settings_temp_folder = Commun.Settings.temp_folder;
            }
            else
            {
                settings_temp_folder = temp_folder;
            }
            //Debug.WriteLine(settings_temp_folder);
            string nom_charclean = string.Concat(nom.Split(Path.GetInvalidFileNameChars()));
            string chemin = System.IO.Path.Combine(settings_temp_folder, nom_charclean + "_" + identifiant + "\\");
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
            //encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            // byte[] bit = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
                byte[] bit = stream.ToArray();
                stream.Close();
                return bit;
            }
        }

        public static byte[] GetBytesFromBitmapSource2(BitmapSource bmp)
        {
            return GetBytesFromBitmapSource(bmp);
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

            int max_retry = Commun.Settings.max_redirection_download_tile;
            int retry = 0;

            bool do_need_retry;

            //List<System.Net.HttpStatusCode> ListResponseMessagesToRetry = new List<System.Net.HttpStatusCode>()
            //{
            //    System.Net.HttpStatusCode.Found,
            //};
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
                            Debug.WriteLine("Error null response");
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
                            Debug.WriteLine($"DownloadByteUrl Retry : {parsing_url} to {new_location}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
                            do_need_retry = true;
                            parsing_url = new_location;
                        }
                        else
                        {
                            Debug.WriteLine($"DownloadByteUrl: {parsing_url}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
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
                                    return new HttpResponse(null, new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
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
                }
            } while (do_need_retry && (retry < max_retry));

            return response;
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
                    System.IO.File.Delete(save_temp_directory + filename);
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


        public static int CheckIfInputValueHaveChange(UIElement SourcePanel)
        {

            List<System.Type> TypeOfSearchElement = new List<System.Type>
            {
                typeof(TextBox),
                typeof(ComboBox),
                typeof(CheckBox),
                typeof(RadioButton)
            };

            var ListOfisualChildren = Commun.Collectif.FindVisualChildren(SourcePanel, TypeOfSearchElement);

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
            range.Load(SM, System.Windows.DataFormats.Text);
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
            bool TextHasBeenFilteredAndChanged = false;
            textbElement.TextChanged -= action;
            if (Collectif.FilterDigitOnlyWhileWritingInTextBox(textbElement))
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
        public static string FilterDigitOnly(string origin, List<char> char_supplementaire)
        {
            //string str = new string((from c in origin where char.IsDigit(c) select c).ToArray());
            if (string.IsNullOrEmpty(origin.Trim())) { return origin; }
            string str = "";
            char[] split_origin = origin.ToCharArray();
            foreach (char caractere in split_origin)
            {
                if (str.Length > 10)
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
                    if (!(car == '.' && str.Contains(car)))
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

        public static string Replacements(string origin, string x, string y, string z, int LayerID)
        {
            //string origin_result = origin;
            //origin_result = origin_result.Replace("{x}", x);
            //origin_result = origin_result.Replace("{y}", y);
            //origin_result = origin_result.Replace("{z}", z);
            return Collectif.GetUrl.FromTileXYZ(origin, Convert.ToInt32(x), Convert.ToInt32(y), Convert.ToInt32(z), LayerID).Replace(" ", "%20");
        }

        public static int CheckIfDownloadSuccess(string url)
        {
            async Task<HttpStatusCode> InternalCheckIfDownloadSuccess(string internalurl)
            {
                //var httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Add("User-Agent", Settings.user_agent);
                try
                {
                    HttpResponse reponseHttpResponse = await Collectif.ByteDownloadUri(new Uri(internalurl), 0, true);
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

            //MessageBox.Show(url);
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
    }
}
