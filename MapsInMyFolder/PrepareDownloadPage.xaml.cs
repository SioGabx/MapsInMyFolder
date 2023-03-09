using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MapsInMyFolder.Commun;
using Microsoft.Win32;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour PrepareDownloadPage.xaml
    /// </summary>
    public partial class PrepareDownloadPage : Page
    {
        public PrepareDownloadPage()
        {
            InitializeComponent();
        }

        string center_view_city;
        public string default_filename;
        int Last_Redim_HUnit_Selected = 0;
        int Last_Redim_WUnit_Selected = 0;
        double LastResquestZoom = -1;

        System.Timers.Timer UpdateTimer;

        void UpdateTimerElapsed_UpdateMigniatureParralele(object source, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                UpdateMigniatureParralele();
            }, null);
        }
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTimer = new System.Timers.Timer(500);
            UpdateTimer.Elapsed += UpdateTimerElapsed_UpdateMigniatureParralele;
            UpdateTimer.AutoReset = false;
            UpdateTimer.Enabled = true;

            int minimum_zoom = Layers.Curent.class_min_zoom;
            int maximum_zoom = Layers.Curent.class_max_zoom;
            if (ZoomSlider.Value < minimum_zoom)
            {
                ZoomSlider.Value = minimum_zoom;
            }
            if (ZoomSlider.Value > maximum_zoom)
            {
                ZoomSlider.Value = maximum_zoom;
            }
            Update_Labels();

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Init();
            System.Windows.Media.SolidColorBrush brush = Collectif.HexValueToSolidColorBrush(Layers.Curent.class_specialsoptions.BackgroundColor);
            StackPanel_ImageTilePreview_0.Background = brush;
            StackPanel_ImageTilePreview_1.Background = brush;
            StackPanel_ImageTilePreview_2.Background = brush;
            SolidColorBrush GrayColor = new SolidColorBrush(Color.FromRgb(136, 137, 137));
            SolidColorBrush WhiteColor = Collectif.HexValueToSolidColorBrush("#BCBCBC");
            TextBox_Redim_Width.Foreground = GrayColor;
            TextBox_Redim_Height.Foreground = GrayColor;
            TextBox_Redim_WUnit.Foreground = GrayColor;
            TextBox_Redim_HUnit.Foreground = GrayColor;
            TextBox_quality_number.Foreground = WhiteColor;
            RedimSwitch.OnContent = "Activé";
            RedimSwitch.OffContent = "Désactivé";
        }

        public void Init()
        {
            Update_Labels();
            LastResquestZoom = -1;
            UpdateMigniatureParralele();
            GetCenterViewCityName();
            TextBox_Redim_HUnit.Opacity = 0.56;
            TextBox_Redim_WUnit.Opacity = 0.56;
            WrapPanel_Largeur.IsEnabled = false;
            WrapPanel_Hauteur.IsEnabled = false;
            Label_SliderMinMax.Content = "Niveau de zoom (min=" + Layers.Curent.class_min_zoom + ", max=" + Layers.Curent.class_max_zoom + ")";
            RedimSwitch.IsOn = false;
            ZoomSlider.Value = MainWindow._instance.MainPage.mapviewer.ZoomLevel;
            TextBox_quality_number.Text = "100";

            const int largeur = 128;
            const int stride = largeur / 8;
            BitmapSource EmptyImage = BitmapSource.Create(largeur, largeur, 96, 96, PixelFormats.Indexed1, BitmapPalettes.BlackAndWhiteTransparent, new byte[largeur * stride], stride);
            ImageTilePreview_0_0.Source = EmptyImage;
            ImageTilePreview_1_0.Source = EmptyImage;
            ImageTilePreview_0_1.Source = EmptyImage;
            ImageTilePreview_1_1.Source = EmptyImage;
            ImageTilePreview_0_2.Source = EmptyImage;
            ImageTilePreview_1_2.Source = EmptyImage;
            StartDownloadButton.Focus();
        }

        CancellationTokenSource UpdateMigniatureParraleleTokenSource = new CancellationTokenSource();
        CancellationToken UpdateMigniatureParraleleToken = new CancellationToken();
        async void UpdateMigniatureParralele(bool force = false)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (!force && LastResquestZoom == ZoomSlider.Value)
            {
                return;
            }

            ImageIsLoading.BeginAnimation(OpacityProperty, Collectif.GetOpacityAnimation(1, 0.2));
            //ImageIsLoading.Visibility = Visibility.Visible;
            LastResquestZoom = ZoomSlider.Value;
            int LayerID = Layers.Curent.class_id;
            if (MainPage.mapSelectable is null) { return; }
            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            var NO_PIN_Location = (SelectionLocation.NO.Latitude, SelectionLocation.NO.Longitude);
            var SE_PIN_Location = (SelectionLocation.SE.Latitude, SelectionLocation.SE.Longitude);
            var LocaMillieux = Collectif.GetCenterBetweenTwoPoints(NO_PIN_Location, SE_PIN_Location);

            int zoom = Convert.ToInt32(ZoomSlider.Value);
            int maximum_zoom = Layers.Curent.class_max_zoom;
            if (zoom > maximum_zoom)
            {
                zoom = maximum_zoom;
            }
            UpdateMigniatureParraleleTokenSource.Cancel();
            UpdateMigniatureParraleleTokenSource = new CancellationTokenSource();
            UpdateMigniatureParraleleToken = UpdateMigniatureParraleleTokenSource.Token;
            var CoordonneesTile = Collectif.CoordonneesToTile(LocaMillieux.Latitude, LocaMillieux.Longitude, zoom);
            int TileX = CoordonneesTile.X - 1;
            int TileY = CoordonneesTile.Y;

            byte[,][] BitmapImageArray = new byte[3, 2][];
            string[,] BitmapErrorsArray = new string[3, 2];

            try
            {
                await Task.Run(() =>
                {
                    var ListOfUrls = new[] { new { url = "", index_x = 0, index_y = 0 } }.ToList();
                    ListOfUrls.Clear();
                    for (int index_x = 0; index_x < 3; index_x++)
                    {
                        for (int index_y = 0; index_y < 2; index_y++)
                        {
                            string urlbase = Collectif.GetUrl.FromTileXYZ(Layers.Curent.class_tile_url, TileX + index_x, TileY + index_y, zoom, LayerID, Collectif.GetUrl.InvokeFunction.getTile);
                            ListOfUrls.Add(new { url = urlbase, index_x, index_y });
                        }
                    }
                    //Téléchargement en parralele des fichiers
                    Parallel.ForEach(ListOfUrls, new ParallelOptions { MaxDegreeOfParallelism = Settings.max_download_tiles_in_parralele }, url =>
                    {
                        if (UpdateMigniatureParraleleToken.IsCancellationRequested && UpdateMigniatureParraleleToken.CanBeCanceled)
                        {
                            //Cancel Parallel ListOfUrls UpdateMigniatureParralele
                            return;
                        }
                        HttpResponse httpResponse = TileGeneratorSettings.TileLoaderGenerator.GetImageAsync(url.url, TileX + url.index_x, TileY + url.index_y, zoom, LayerID, pbfdisableadjacent: true).Result;
                        if (UpdateMigniatureParraleleToken.IsCancellationRequested && UpdateMigniatureParraleleToken.CanBeCanceled)
                        {
                            //Cancel Parallel ListOfUrls UpdateMigniatureParralele
                            return;
                        }

                        if (httpResponse?.ResponseMessage.IsSuccessStatusCode == true)
                        {
                            BitmapImageArray[url.index_x, url.index_y] = httpResponse.Buffer;
                        }
                        else
                        {
                            BitmapImageArray[url.index_x, url.index_y] = Collectif.GetEmptyImageBufferFromText(httpResponse);
                        }
                    });
                }, UpdateMigniatureParraleleToken);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exeception loading preview : " + ex.Message);
            }

            if (zoom == Convert.ToInt32(ZoomSlider.Value))
            {

                ComboBoxItem SelectedComboBoxItem = Combobox_color_conversion.SelectedItem as ComboBoxItem;
                string ComboboxColorConversionSelectedItemTag = SelectedComboBoxItem.Tag as string;
                NetVips.Enums.Interpretation interpretation = (NetVips.Enums.Interpretation)Enum.Parse(typeof(NetVips.Enums.Interpretation), ComboboxColorConversionSelectedItemTag);

                bool parseResult = int.TryParse(TextBox_quality_number.Text, out int qualityInt);
                if (!parseResult)
                {
                    //quality 1 is the minimum value
                    qualityInt = 1;
                }
                string format = Layers.Curent.class_format;
                if (format != "png")
                {
                    format = "jpeg";
                }

                NetVips.VOption saveVOption = Collectif.getSaveVOption(format, qualityInt, Layers.Curent.class_tiles_size);
                BitmapSource ApplyEffectOnImageFromBuffer(byte[] ImgArray)
                {
                    if (ImgArray is null) { return null; }
                    NetVips.Image NImage = NetVips.Image.NewFromBuffer(ImgArray);
                    if (interpretation != NetVips.Enums.Interpretation.Srgb)
                    {
                        try
                        {
                            NImage = NImage.Colourspace(interpretation);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Colourspace change error : " + ex.Message);
                        }
                    }
                    byte[] NImageByteWithSaveOptions = NImage.WriteToBuffer("." + format, saveVOption);
                    NImage.Dispose();
                    NImage.Close();
                    return ByteArrayToBitmapSource(NImageByteWithSaveOptions);
                }
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(BitmapImageArray[0, 0]), zoom, ImageTilePreview_0_0);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(BitmapImageArray[1, 0]), zoom, ImageTilePreview_0_1);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(BitmapImageArray[2, 0]), zoom, ImageTilePreview_0_2);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(BitmapImageArray[0, 1]), zoom, ImageTilePreview_1_0);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(BitmapImageArray[1, 1]), zoom, ImageTilePreview_1_1);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(BitmapImageArray[2, 1]), zoom, ImageTilePreview_1_2);

                DoubleAnimation show_anim = Collectif.GetOpacityAnimation(1, 2);
                ImageTilePreview_0_0.BeginAnimation(OpacityProperty, show_anim);
                ImageTilePreview_1_0.BeginAnimation(OpacityProperty, show_anim);
                ImageTilePreview_0_1.BeginAnimation(OpacityProperty, show_anim);
                ImageTilePreview_1_1.BeginAnimation(OpacityProperty, show_anim);
                ImageTilePreview_0_2.BeginAnimation(OpacityProperty, show_anim);
                ImageTilePreview_1_2.BeginAnimation(OpacityProperty, show_anim);

                ImageIsLoading.BeginAnimation(OpacityProperty, Collectif.GetOpacityAnimation(0, 1));
            }
        }

        public static BitmapSource ByteArrayToBitmapSource(byte[] array)
        {
            try
            {
                if (array == null)
                {
                    const int width = 256;
                    const int height = width;
                    const int stride = width / 8;
                    byte[] pixels = new byte[height * stride];

                    BitmapSource image = BitmapSource.Create(
                        width,
                        height,
                        96,
                        96,
                        PixelFormats.Indexed1,
                        BitmapPalettes.BlackAndWhiteTransparent,
                        pixels,
                        stride);

                    return image;
                }
                using (var ms = Collectif.ByteArrayToStream(array))
                {
                    var image = new BitmapImage();
                    if (ms != null)
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad; // here
                        image.StreamSource = ms;
                        image.EndInit();
                    }
                    return image;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur F ToImage : " + ex.Message);
            }
            return new BitmapImage();
        }

        void SetBitmapIntoImageTilePreview(BitmapSource bitmap, int zoom, Image ImageTilePreview)
        {
            int ZoomSlider = Convert.ToInt32(this.ZoomSlider.Value);
            if (ZoomSlider != zoom)
            {
                return;
            }
            if (bitmap is not null && ImageTilePreview is not null)
            {
                bitmap.Freeze();
                ImageTilePreview.Source = bitmap;
            }
            else if (bitmap is null)
            {
                Debug.WriteLine("Null element");
                bitmap = new BitmapImage();
                bitmap.Freeze();
                ImageTilePreview.Source = bitmap;
            }
            Update_Labels();
        }

        async void GetCenterViewCityName()
        {
            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            var NO_PIN_Location = (SelectionLocation.NO.Latitude, SelectionLocation.NO.Longitude);
            var SE_PIN_Location = (SelectionLocation.SE.Latitude, SelectionLocation.SE.Longitude);
            var LocaMillieux = Collectif.GetCenterBetweenTwoPoints(NO_PIN_Location, SE_PIN_Location);
            MapControl.Location Mloc = new MapControl.Location(LocaMillieux.Latitude, LocaMillieux.Longitude);
            await Task.Run(() =>
            {
                try
                {
                    static bool ValidateResult(string center_view_city_arg)
                    {
                        return center_view_city_arg != null && !string.IsNullOrEmpty(center_view_city_arg.Trim());
                    }
                    string url = "https://nominatim.openstreetmap.org/reverse?lat=" + Mloc.Latitude + "&lon=" + Mloc.Longitude + "&zoom=18&format=xml&email=siogabx@siogabx.fr";
                    DebugMode.WriteLine("Recherche ville centre : lat=" + Mloc.Latitude + " lon=" + Mloc.Longitude + "\nurl=" + url);
                    using (HttpClient client = new HttpClient())
                    {
                        using (HttpResponseMessage response = client.GetAsync(url).Result)
                        using (Stream responseStream = response.Content.ReadAsStream())
                        {
                            response.EnsureSuccessStatusCode();
                            Dictionary<string, string> city_name = new Dictionary<string, string>
                            {
                                { "village", string.Empty },
                                { "town", string.Empty },
                                { "city", string.Empty },
                                { "city_district", string.Empty },
                                { "municipality", string.Empty },
                                { "county", string.Empty },
                                { "state", string.Empty }
                            };
                            using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(responseStream))
                            {
                                reader.MoveToContent();
                                while (reader.Read())
                                {
                                    if (reader.IsStartElement())
                                    {
                                        string name = reader.Name;
                                        string value = reader.ReadString();
                                        foreach (var city in city_name)
                                        {
                                            if (city.Key == name)
                                            {
                                                if (ValidateResult(value))
                                                {
                                                    city_name[city.Key] = value.Trim();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            foreach (var city in city_name)
                            {
                                if (ValidateResult(city.Value))
                                {
                                    center_view_city = "_" + city.Value;
                                    return;
                                }
                            }
                        }
                    }
                    center_view_city = "";
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Erreur recherche nom de ville : " + ex.Message);
                }
            });
        }

        void Update_Labels()
        {
            if (!IsInitialized) { return; }
            Dictionary<string, int> rognage_info = GetOriginalImageSize();
            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            double NO_PIN_Latitude = SelectionLocation.NO.Latitude;
            double NO_PIN_Longitude = SelectionLocation.NO.Longitude;
            double SE_PIN_Latitude = SelectionLocation.SE.Latitude;
            double SE_PIN_Longitude = SelectionLocation.SE.Longitude;
            int Zoom = Convert.ToInt16(Math.Floor(ZoomSlider.Value));
            var NO_tile = Collectif.CoordonneesToTile(NO_PIN_Latitude, NO_PIN_Longitude, Zoom);
            var SE_tile = Collectif.CoordonneesToTile(SE_PIN_Latitude, SE_PIN_Longitude, Zoom);
            int lat_tile_number = Math.Abs(SE_tile.X - NO_tile.X) + 1;
            int long_tile_number = Math.Abs(SE_tile.Y - NO_tile.Y) + 1;

            void Update_Label_TileSize()
            {
                Label_LargeurTuile.Content = Layers.Curent.class_tiles_size;
            }
            void Update_Label_LayerName()
            {
                Label_NomCalque.Content = Layers.Curent.class_name;
            }

            void Update_Label_TileSource()
            {
                Label_Source.Content = Layers.Curent.class_site_url;
            }

            void Update_Label_NbrTiles()
            {
                int nbr_of_tiles = lat_tile_number * long_tile_number;
                Label_NbrTiles.Content = lat_tile_number.ToString() + " x " + long_tile_number.ToString() + " = " + nbr_of_tiles.ToString();
                return;
            }

            List<int> FinalSize()
            {
                int final_size_W = 0;
                if (TextBox_Redim_WUnit.SelectedIndex == 0)
                {
                    final_size_W += Convert.ToInt32(TextBox_Redim_Width.Text);
                }
                else if (TextBox_Redim_WUnit.SelectedIndex == 1)
                {
                    final_size_W += Convert.ToInt32(Math.Round(Convert.ToDouble(TextBox_Redim_Width.Text) / 100 * rognage_info["width"]));
                }
                int final_size_H = 0;
                if (TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    final_size_H += Convert.ToInt32(TextBox_Redim_Height.Text);
                }
                else if (TextBox_Redim_HUnit.SelectedIndex == 1)
                {
                    final_size_H += Convert.ToInt32(Math.Round(Convert.ToDouble(TextBox_Redim_Height.Text) / 100 * rognage_info["height"]));
                }
                return new List<int>() { final_size_W, final_size_H };
            }

            void Update_Label_Label_ImgSizeF()
            {
                if (RedimSwitch.IsOn)
                {
                    string final_size = "";
                    List<int> final_size_int = FinalSize();
                    final_size += final_size_int[0];
                    final_size += " x ";
                    final_size += final_size_int[1];
                    final_size += " pixels";
                    Label_ImgSizeF.Content = final_size;
                }
                else
                {
                    Label_ImgSizeF.Content = Label_ImgSize.Content;
                }
                return;
            }

            void Update_Label_ImageInitialSize()
            {
                int image_size_lat = rognage_info["width"];
                int image_size_long = rognage_info["height"];
                Label_ImgSize.Content = image_size_lat.ToString() + " x " + image_size_long.ToString() + " pixels";
                if (TextBox_Redim_HUnit.SelectedIndex == 0 && TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    if (!RedimSwitch.IsOn)
                    {
                        TextBox_Redim_Width.Text = image_size_lat.ToString();
                        TextBox_Redim_Height.Text = image_size_long.ToString();
                    }
                }
                return;
            }

            void Update_CheckIfSizeInf65000()
            {
                Dictionary<string, int> ResizedImageSize = GetResizedImageSize();
                Dictionary<string, int> OriginalImageSize = GetOriginalImageSize();
                bool ResizedImageSizeHIsOK = ResizedImageSize["height"] >= 65000;
                bool ResizedImageSizeWIsOK = ResizedImageSize["width"] >= 65000;
                bool OriginalImageSizeHIsOK = OriginalImageSize["height"] >= 65000;
                bool OriginalImageSizeWIsOK = OriginalImageSize["width"] >= 65000;
                SolidColorBrush OKColor = Collectif.HexValueToSolidColorBrush("#888989");
                SolidColorBrush WrongColor = Collectif.HexValueToSolidColorBrush("#FF953C32");
                if (OriginalImageSizeHIsOK || OriginalImageSizeWIsOK)
                {
                    Label_ImgSize.Foreground = WrongColor;
                }
                else
                {
                    Label_ImgSize.Foreground = OKColor;
                }
                if (ResizedImageSizeHIsOK || ResizedImageSizeWIsOK)
                {
                    Label_ImgSizeF.Foreground = WrongColor;
                }
                else
                {
                    Label_ImgSizeF.Foreground = OKColor;
                }
                if (OriginalImageSizeHIsOK || OriginalImageSizeWIsOK || ResizedImageSizeHIsOK || ResizedImageSizeWIsOK)
                {
                    StartDownloadButton.Opacity = 0.5;
                    StartDownloadButton.IsEnabled = false;
                }
                else
                {
                    StartDownloadButton.Opacity = 1;
                    StartDownloadButton.IsEnabled = true;
                }
            }
            Update_Label_NbrTiles();
            Update_Label_ImageInitialSize();
            Update_Label_Label_ImgSizeF();
            Update_Label_TileSize();
            Update_Label_TileSource();
            Update_Label_LayerName();
            Update_CheckIfSizeInf65000();
            return;
        }

        private void RedimSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Update_Labels();
            if (RedimSwitch.IsOn)
            {
                TextBox_Redim_Width_TextChanged(null, null);
                WrapPanel_Largeur.Opacity = 1;
                WrapPanel_Hauteur.Opacity = 1;
                TextBox_Redim_HUnit.Opacity = 1;
                TextBox_Redim_WUnit.Opacity = 1;
                WrapPanel_Largeur.IsEnabled = true;
                WrapPanel_Hauteur.IsEnabled = true;
                SolidColorBrush WhiteColor = Collectif.HexValueToSolidColorBrush("#BCBCBC"); //new SolidColorBrush(BrushConverter(BCBCBC);
                TextBox_Redim_Width.Foreground = WhiteColor;
                TextBox_Redim_Height.Foreground = WhiteColor;
                TextBox_Redim_WUnit.Foreground = WhiteColor;
                TextBox_Redim_HUnit.Foreground = WhiteColor;
            }
            else
            {
                Update_Labels();
                WrapPanel_Largeur.Opacity = 0.8;
                WrapPanel_Hauteur.Opacity = 0.8;
                TextBox_Redim_HUnit.Opacity = 0.56;
                TextBox_Redim_WUnit.Opacity = 0.56;
                WrapPanel_Largeur.IsEnabled = false;
                WrapPanel_Hauteur.IsEnabled = false;
                SolidColorBrush GrayColor = new SolidColorBrush(Color.FromRgb(136, 137, 137));
                TextBox_Redim_Width.Foreground = GrayColor;
                TextBox_Redim_Height.Foreground = GrayColor;
                TextBox_Redim_WUnit.Foreground = GrayColor;
                TextBox_Redim_HUnit.Foreground = GrayColor;
            }
        }

        Dictionary<string, int> GetOriginalImageSize()
        {
            if (!IsInitialized) { return null; }

            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            double NO_PIN_Latitude = SelectionLocation.NO.Latitude;
            double NO_PIN_Longitude = SelectionLocation.NO.Longitude;
            double SE_PIN_Latitude = SelectionLocation.SE.Latitude;
            double SE_PIN_Longitude = SelectionLocation.SE.Longitude;
            int Zoom = Convert.ToInt16(Math.Floor(ZoomSlider.Value));
            Dictionary<string, int> rognage_info = MainWindow.GetRognageValue(NO_PIN_Latitude, NO_PIN_Longitude, SE_PIN_Latitude, SE_PIN_Longitude, Zoom, Layers.Curent.class_tiles_size);
            return rognage_info;
        }
        Dictionary<string, int> GetResizedImageSize()
        {
            if (!IsInitialized) { return null; }
            int RedimWidth = -1;
            int RedimHeight = -1;
            Dictionary<string, int> rognage_info = GetOriginalImageSize();
            if (RedimSwitch.IsOn)
            {
                if (TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    RedimHeight = Convert.ToInt32(TextBox_Redim_Height.Text);
                }
                else if (TextBox_Redim_HUnit.SelectedIndex == 1)
                {
                    RedimHeight = (int)Math.Round((double)rognage_info["height"] * (Convert.ToDouble(TextBox_Redim_Height.Text) / 100));
                }

                if (TextBox_Redim_WUnit.SelectedIndex == 0)
                {
                    RedimWidth = Convert.ToInt32(TextBox_Redim_Width.Text);
                }
                else if (TextBox_Redim_WUnit.SelectedIndex == 1)
                {
                    RedimWidth = (int)Math.Round((double)rognage_info["width"] * (Convert.ToDouble(TextBox_Redim_Width.Text) / 100));
                }
                Dictionary<string, int> ResizedImageSize = new Dictionary<string, int>
                {
                    { "height", RedimHeight },
                    { "width", RedimWidth }
                };
                return ResizedImageSize;
            }
            else
            {
                Dictionary<string, int> OriginalImageSize = new Dictionary<string, int>
                {
                    { "height", rognage_info["height"] },
                    { "width", rognage_info["width"] }
                };
                return OriginalImageSize;
            }
        }

        private void TextBox_quality_number_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            string filtered_string = Collectif.FilterDigitOnly(TextBox_quality_number.Text, null);
            var cursor_position = TextBox_quality_number.SelectionStart;
            TextBox_quality_number.Text = filtered_string;
            TextBox_quality_number.SelectionStart = cursor_position;
            void ChangeSelectedIndex(int index)
            {
                TextBox_quality_name.SelectionChanged += TextBox_quality_name_SelectionChanged;
                TextBox_quality_name.SelectedIndex = index;
                TextBox_quality_name.SelectionChanged -= TextBox_quality_name_SelectionChanged;
                UpdateMigniatureParralele(true);
            }

            if (string.IsNullOrEmpty(filtered_string.Trim())) { return; }
            int quality = Convert.ToInt32(filtered_string);
            if (quality < 30)
            {
                //faible
                ChangeSelectedIndex(0);
                return;
            }
            else if (quality < 60)
            {
                //moyenne
                ChangeSelectedIndex(1);
                return;
            }
            else if (quality < 80)
            {
                //elevée
                ChangeSelectedIndex(2);
                return;
            }
            else if (quality < 100)
            {
                //supperieur
                ChangeSelectedIndex(3);
                return;
            }
            else if (quality > 99)
            {
                //supperieur+
                ChangeSelectedIndex(4);
                TextBox_quality_number.Text = "100";
                TextBox_quality_number.SelectionStart = 3;
                return;
            }
        }

        private void TextBox_quality_name_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var str = TextBox_quality_number.Text;
            int quality = -1;
            if (!string.IsNullOrEmpty(str.Trim()))
            {
                quality = Convert.ToInt32(str);
            }

            TextBox_quality_number.TextChanged -= TextBox_quality_number_TextChanged;
            if (TextBox_quality_name.SelectedIndex == 0)
            {
                //faible
                if (quality < 0 || quality >= 30)
                {
                    TextBox_quality_number.Text = "10";
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 1)
            {
                //moyenne
                if (quality < 30 || quality >= 60)
                {
                    TextBox_quality_number.Text = "30";
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 2)
            {
                //elevée
                if (quality < 60 || quality >= 80)
                {
                    TextBox_quality_number.Text = "60";
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 3)
            {
                //supperieur
                if (quality < 80 || quality >= 100)
                {
                    TextBox_quality_number.Text = "80";
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 4)
            {
                //supperieur +
                if (quality < 100)
                {
                    TextBox_quality_number.Text = "100";
                }
            }
            TextBox_quality_number.TextChanged += TextBox_quality_number_TextChanged;
            UpdateMigniatureParralele(true);
            return;
        }

        private void TextBox_Redim_Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            if (string.IsNullOrEmpty(TextBox_Redim_Height.Text.Trim()))
            {
                TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                TextBox_Redim_Width.Text = "";
                TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
                return;
            }

            Dictionary<string, int> rognage_info = GetOriginalImageSize();
            if (!RedimSwitch.IsOn) { return; }
            if (TextBox_Redim_HUnit.SelectedIndex == 0)
            {
                TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBox_Redim_Height);
                TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
                if (Convert.ToInt32(TextBox_Redim_Height.Text) > 65000)
                {
                    TextBox_Redim_Height.Text = "65000";
                    TextBox_Redim_Height.SelectionStart = TextBox_Redim_Height.Text.Length;
                }
                double hrink = (double)Convert.ToDouble(TextBox_Redim_Height.Text) / (double)rognage_info["height"];
                int value = (int)Math.Round((double)rognage_info["width"] * hrink);

                if (value > 65000)
                {
                    TextBox_Redim_Width.Text = value.ToString();
                }
                else
                {
                    TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                    TextBox_Redim_Width.Text = value.ToString();
                    TextBox_Redim_Width.SelectionStart = TextBox_Redim_Width.Text.Length;
                    TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
                }
            }
            else if (TextBox_Redim_HUnit.SelectedIndex == 1)
            {
                TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBox_Redim_Height, new List<char>() { '.' });
                TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
                if (string.IsNullOrEmpty(TextBox_Redim_Height.Text.Trim()))
                { return; }
                if (Convert.ToDouble(TextBox_Redim_Height.Text) > (65000 / (double)rognage_info["height"] * 100))
                {
                    TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                    TextBox_Redim_Height.Text = Math.Round(65000 / (double)rognage_info["height"] * 100, 2).ToString();
                    TextBox_Redim_Height.SelectionStart = TextBox_Redim_Height.Text.Length;
                    TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
                }
                TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                TextBox_Redim_Width.Text = TextBox_Redim_Height.Text;
                TextBox_Redim_Width.SelectionStart = TextBox_Redim_Width.Text.Length;
                TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
            }

            Update_Labels();
        }

        private void TextBox_Redim_Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            Dictionary<string, int> rognage_info = GetOriginalImageSize();
            if (string.IsNullOrEmpty(TextBox_Redim_Width.Text.Trim()))
            {
                TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                TextBox_Redim_Height.Text = "";
                TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
                return;
            }
            if (!RedimSwitch.IsOn) { return; }
            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBox_Redim_Width);

                TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
                if (Convert.ToInt32(TextBox_Redim_Width.Text) > 65000)
                {
                    TextBox_Redim_Width.Text = "65000";
                    TextBox_Redim_Width.SelectionStart = TextBox_Redim_Width.Text.Length;
                }
                double Vrink = (double)Convert.ToDouble(TextBox_Redim_Width.Text) / (double)rognage_info["width"];
                int value = (int)Math.Round((double)rognage_info["height"] * Vrink);

                if (value > 65000)
                {
                    TextBox_Redim_Height.Text = value.ToString();
                }
                else
                {
                    TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                    TextBox_Redim_Height.Text = value.ToString();
                    TextBox_Redim_Height.SelectionStart = TextBox_Redim_Height.Text.Length;
                    TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
                }
            }
            else if (TextBox_Redim_WUnit.SelectedIndex == 1)
            {
                TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBox_Redim_Width, new List<char>() { '.' });
                TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
                if (string.IsNullOrEmpty(TextBox_Redim_Width.Text.Trim()))
                {
                    return;
                }
                if (Convert.ToDouble(TextBox_Redim_Width.Text) > 65000 / (double)rognage_info["width"] * 100)
                {
                    TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                    TextBox_Redim_Width.Text = Math.Round(65000 / (double)rognage_info["width"] * 100, 2).ToString();
                    TextBox_Redim_Width.SelectionStart = TextBox_Redim_Width.Text.Length;
                    TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
                }
                TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                TextBox_Redim_Height.Text = TextBox_Redim_Width.Text;
                TextBox_Redim_Height.SelectionStart = TextBox_Redim_Height.Text.Length;
                TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
            }
            Update_Labels();
        }

        private void TextBox_Redim_Width_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                if (string.IsNullOrEmpty(TextBox_Redim_Width.Text.Trim()) || Convert.ToInt32(TextBox_Redim_Width.Text) < 10)
                {
                    TextBox_Redim_Width.Text = "10";
                }
            }
        }

        private void TextBox_Redim_Height_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                if (string.IsNullOrEmpty(TextBox_Redim_Height.Text.Trim()) || Convert.ToInt32(TextBox_Redim_Height.Text) < 10)
                {
                    TextBox_Redim_Height.Text = "10";
                }
            }
        }

        private void TextBox_Redim_HUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Dictionary<string, int> rognage_info = GetOriginalImageSize();
            if (string.IsNullOrEmpty(TextBox_Redim_Height.Text.Trim())) { return; }
            double txtBValue = Convert.ToDouble(TextBox_Redim_Height.Text);
            if (TextBox_Redim_HUnit.SelectedIndex == 1 && Last_Redim_HUnit_Selected == 0)
            {
                Last_Redim_HUnit_Selected = 1;
                TextBox_Redim_WUnit.SelectedIndex = 1;
                TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                TextBox_Redim_Height.Text = Math.Round((double)txtBValue / (double)rognage_info["height"] * 100, 1).ToString();
                TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
            }
            else if (TextBox_Redim_HUnit.SelectedIndex == 0 && Last_Redim_HUnit_Selected == 1)
            {
                Last_Redim_HUnit_Selected = 0;
                TextBox_Redim_WUnit.SelectedIndex = 0;
                TextBox_Redim_Height.TextChanged -= TextBox_Redim_Height_TextChanged;
                TextBox_Redim_Height.Text = Math.Round(rognage_info["height"] * ((double)txtBValue / 100)).ToString();
                TextBox_Redim_Height.TextChanged += TextBox_Redim_Height_TextChanged;
            }
        }

        private void TextBox_Redim_WUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Dictionary<string, int> rognage_info = GetOriginalImageSize();
            if (string.IsNullOrEmpty(TextBox_Redim_Width.Text.Trim())) { return; }
            double txtBValue = Convert.ToDouble(TextBox_Redim_Width.Text);
            if (TextBox_Redim_WUnit.SelectedIndex == 1 && Last_Redim_WUnit_Selected == 0)
            {
                Last_Redim_WUnit_Selected = 1;
                TextBox_Redim_HUnit.SelectedIndex = 1;
                TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                TextBox_Redim_Width.Text = Math.Round((double)txtBValue / (double)rognage_info["width"] * 100, 1).ToString();
                TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
            }
            else if (TextBox_Redim_WUnit.SelectedIndex == 0 && Last_Redim_WUnit_Selected == 1)
            {
                Last_Redim_WUnit_Selected = 0;
                TextBox_Redim_HUnit.SelectedIndex = 0;
                TextBox_Redim_Width.TextChanged -= TextBox_Redim_Width_TextChanged;
                TextBox_Redim_Width.Text = Math.Round(rognage_info["width"] * ((double)txtBValue / 100)).ToString();
                TextBox_Redim_Width.TextChanged += TextBox_Redim_Width_TextChanged;
            }
        }

        private void StartDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            int zoom = Convert.ToInt32(ZoomSlider.Value);
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            string filter = "";
            const string pngfilter = "PNG|*.png";
            const string jpgfilter = "JPEG|*.jpg";
            const string tifffilter = "TIFF|*.tif";
            string filterconcat(string filter1, string filter2)
            {
                return filter1 + "|" + filter2;
            }
            if (string.Equals(Layers.Curent.class_format, "png", StringComparison.OrdinalIgnoreCase))
            {
                filter = pngfilter;
            }
            else if (string.Equals(Layers.Curent.class_format, "jpeg", StringComparison.OrdinalIgnoreCase))
            {
                filter = jpgfilter;
            }
            else if (string.Equals(Layers.Curent.class_format, "pbf", StringComparison.OrdinalIgnoreCase))
            {
                filter = filterconcat(jpgfilter, pngfilter);
            }
            saveFileDialog1.Filter = filterconcat(filter, tifffilter);
            saveFileDialog1.Title = "Selectionnez un emplacement de sauvegarde :";
            saveFileDialog1.RestoreDirectory = true;
            saveFileDialog1.OverwritePrompt = true;
            string str_name = default_filename + center_view_city + "_" + zoom;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                string c2 = c.ToString();
                str_name = str_name.Replace(c2, string.Empty);
            }
            saveFileDialog1.FileName = str_name;

            saveFileDialog1.ValidateNames = true;
            bool? IsValidate = saveFileDialog1.ShowDialog();
            if (string.IsNullOrEmpty(saveFileDialog1.FileName.Trim()) || IsValidate != true)
            {
                return;
            }
            string save_directory = Path.GetDirectoryName(saveFileDialog1.FileName) + @"\";
            string filename = Path.GetFileName(saveFileDialog1.FileName);

            //MapControl.Location NO_PIN_Location = MainWindow._instance.MainPage.NO_PIN.Location;
           // MapControl.Location SE_PIN_Location = MainWindow._instance.MainPage.SE_PIN.Location;
            string format = Path.GetExtension(saveFileDialog1.FileName);
            format = format switch
            {
                ".jpeg" => "jpeg",
                ".jpg" => "jpeg",
                ".png" => "png",
                ".tif" => "tif",
                ".tiff" => "tiff",
                _ => "jpeg",
            };
            int quality = Convert.ToInt32(TextBox_quality_number.Text);
            int RedimWidth = -1;
            int RedimHeight = -1;
            if (RedimSwitch.IsOn)
            {
                Dictionary<string, int> rognage_info = GetOriginalImageSize();

                if (TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    RedimHeight = Convert.ToInt32(TextBox_Redim_Height.Text);
                }
                else if (TextBox_Redim_HUnit.SelectedIndex == 1)
                {
                    RedimHeight = (int)Math.Round((double)rognage_info["height"] * (Convert.ToDouble(TextBox_Redim_Height.Text) / 100));
                }

                if (TextBox_Redim_WUnit.SelectedIndex == 0)
                {
                    RedimWidth = Convert.ToInt32(TextBox_Redim_Width.Text);
                }
                else if (TextBox_Redim_WUnit.SelectedIndex == 1)
                {
                    RedimWidth = (int)Math.Round((double)rognage_info["width"] * (Convert.ToDouble(TextBox_Redim_Width.Text) / 100));
                }
            }
            else
            {
                RedimWidth = -1;
                RedimHeight = -1;
            }

            ComboBoxItem SelectedComboBoxItem = Combobox_color_conversion.SelectedItem as ComboBoxItem;
            string ComboboxColorConversionSelectedItemTag = SelectedComboBoxItem.Tag as string;
            NetVips.Enums.Interpretation interpretation = (NetVips.Enums.Interpretation)Enum.Parse(typeof(NetVips.Enums.Interpretation), ComboboxColorConversionSelectedItemTag);

            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            Download_Options download_Options = new Download_Options(0, save_directory, format, filename, "", "", 0, zoom, quality, "", SelectionLocation.NO, SelectionLocation.SE, RedimWidth, RedimHeight, interpretation);
            MainWindow._instance.PrepareDownloadBeforeStart(download_Options);
            ClosePage();
        }

        private void ClosePage_button_Click(object sender, RoutedEventArgs e)
        {
            ClosePage();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClosePage();
            }
        }

        public void ClosePage()
        {
            DoubleAnimation hide_anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond * 0))
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut },
                BeginTime = new TimeSpan(0, 0, 0, 0, 500),
            };
            ImageTilePreview_0_0.BeginAnimation(OpacityProperty, hide_anim);
            ImageTilePreview_1_0.BeginAnimation(OpacityProperty, hide_anim);
            ImageTilePreview_0_1.BeginAnimation(OpacityProperty, hide_anim);
            ImageTilePreview_1_1.BeginAnimation(OpacityProperty, hide_anim);
            ImageTilePreview_0_2.BeginAnimation(OpacityProperty, hide_anim);
            ImageTilePreview_1_2.BeginAnimation(OpacityProperty, hide_anim);
            MainWindow._instance.FrameBack();
        }

        private void GridImagePreviewInfoScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GridImagePreviewInfoScrollViewer.ScrollableHeight > 0)
            {
                GridImagePreviewInfo.Margin = new Thickness(0, 0, Math.Min(15, GridImagePreviewInfoScrollViewer.ScrollableHeight), 70);
                if (GridImagePreviewInfoScrollViewer.ScrollableHeight > 10)
                {
                    GridImagePreviewInfoScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                }
            }
            else
            {
                GridImagePreviewInfoScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                GridImagePreviewInfo.Margin = new Thickness(0, 0, 0, 70);
            }
        }

        private void Combobox_color_conversion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMigniatureParralele(true);
        }
    }
}
