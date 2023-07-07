using MapsInMyFolder.Commun;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        string centerViewCity;
        public string defaultFilename;
        int lastRedimHUnitSelected;
        int lastRedimWUnitSelected;
        double lastRequestZoom = -1;
        System.Timers.Timer updateTimer;
        void UpdateTimerElapsed_UpdateMigniatureParralele(object source, EventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)delegate
            {
                UpdateMigniatureParralele();
            }, null);
        }
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            updateTimer?.Stop();
            updateTimer = new System.Timers.Timer(500);
            updateTimer.Elapsed += UpdateTimerElapsed_UpdateMigniatureParralele;
            updateTimer.AutoReset = false;
            updateTimer.Start();
            int minimumZoom = Layers.Current.class_min_zoom ?? 0;
            int maximumZoom = Layers.Current.class_max_zoom ?? 0;
            ZoomSlider.Value = Math.Min(Math.Max(ZoomSlider.Value, minimumZoom), maximumZoom);


            if (IsInitialized && TextBoxScaleIsLock())
            {
                if (!double.TryParse(TextBox_Scale.Text, out double CurrentTextBoxTargetScale))
                {
                    TextBoxScaleIsLock(false);
                    return;
                }
                SetTextBoxRedimDimensionTextBasedOnPourcentage(TextBox_Redim_Width, TextBox_Redim_WUnit, "width", GetScale().Scale / CurrentTextBoxTargetScale * 100);
            }
            else
            {
                SetTextBoxRedimDimension();
            }
            Update_Labels();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SolidColorBrush brush = Collectif.HexValueToSolidColorBrush(Layers.Current.class_specialsoptions.BackgroundColor);
            StackPanel_ImageTilePreview_0.Background = brush;
            StackPanel_ImageTilePreview_1.Background = brush;
            StackPanel_ImageTilePreview_2.Background = brush;
            SetTextBoxRedimDimension();
        }
        private void SetTextBoxRedimDimension()
        {
            RognageInfo rognageInfo = GetOriginalImageSize();
            if (rognageInfo != null)
            {
                Label_ImgSize.Content = $"{rognageInfo.width} x {rognageInfo.height}";
                if (TextBox_Redim_HUnit.SelectedIndex == 0 && TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    TextBox_Redim_Width.SetText(rognageInfo.width.ToString(), TextBox_Redim_Width_TextChanged);
                    TextBox_Redim_Height.SetText(rognageInfo.height.ToString(), TextBox_Redim_Height_TextChanged);
                }
            }
        }
        public void Init()
        {
            Update_Labels();
            lastRequestZoom = -1;
            UpdateMigniatureParralele();
            GetCenterViewCityName();
            Label_SliderMinMax.Content = Languages.GetWithArguments("preparePropertyZoomLevelMinMax", Layers.Current.class_min_zoom, Layers.Current.class_max_zoom);
            ZoomSlider.Value = Math.Round(MainWindow._instance.MainPage.mapviewer.ZoomLevel);
            TextBox_quality_number.Text = "100";
            const int largeur = 128;
            const int stride = largeur / 8;
            BitmapSource emptyImage = BitmapSource.Create(largeur, largeur, 96, 96, PixelFormats.Indexed1, BitmapPalettes.BlackAndWhiteTransparent, new byte[largeur * stride], stride);
            ImageTilePreview_0_0.Source = emptyImage;
            ImageTilePreview_1_0.Source = emptyImage;
            ImageTilePreview_0_1.Source = emptyImage;
            ImageTilePreview_1_1.Source = emptyImage;
            ImageTilePreview_0_2.Source = emptyImage;
            ImageTilePreview_1_2.Source = emptyImage;
            StartDownloadButton.Focus();
            ZoomSlider.Maximum = Math.Max(21, Layers.Current.class_max_zoom ?? 21);
            if (Layers.Current.class_hasscale)
            {
                TextBox_Scale.Visibility = Visibility.Visible;
                TextBox_NoScale.Visibility = Visibility.Collapsed;
                Label_ScalePrefix.Content = "1/";
                Label_ScalePrefix.Foreground = Collectif.HexValueToSolidColorBrush("#BCBCBC");
                Label_Scale.Foreground = Collectif.HexValueToSolidColorBrush("#BCBCBC");
            }
            else
            {
                CheckBoxAddScaleToImage.IsEnabled = false;
                TextBox_Scale.Visibility = Visibility.Collapsed;
                TextBox_NoScale.Visibility = Visibility.Visible;
                Label_ScalePrefix.Content = Languages.Current["preparePropertyScaleNotDefined"];
                Label_ScalePrefix.Foreground = Collectif.HexValueToSolidColorBrush("#5A5A5A");
                Label_Scale.Foreground = Collectif.HexValueToSolidColorBrush("#5A5A5A");
            }
        }
        CancellationTokenSource updateMigniatureParraleleTokenSource = new CancellationTokenSource();
        CancellationToken updateMigniatureParraleleToken = new CancellationToken();

        async void UpdateMigniatureParralele(bool force = false)
        {
            if (!IsInitialized || (!force && lastRequestZoom == ZoomSlider.Value))
            {
                return;
            }

            ImageIsLoading.BeginAnimation(OpacityProperty, Collectif.GetOpacityAnimation(1, 0.2));
            lastRequestZoom = ZoomSlider.Value;
            int layerID = Layers.Current.class_id;

            if (MainPage.MapSelectable is null)
            {
                return;
            }

            var selectionLocation = MainPage.MapSelectable.GetRectangleLocation();
            var noPinLocation = (selectionLocation.NO.Latitude, selectionLocation.NO.Longitude);
            var sePinLocation = (selectionLocation.SE.Latitude, selectionLocation.SE.Longitude);
            var locaMillieux = Collectif.GetCenterBetweenTwoPoints(noPinLocation, sePinLocation);

            int zoom = Convert.ToInt32(ZoomSlider.Value);
            int maximumZoom = Layers.Current.class_max_zoom ?? 0;

            if (zoom > maximumZoom)
            {
                zoom = maximumZoom;
            }

            updateMigniatureParraleleTokenSource.Cancel();
            updateMigniatureParraleleTokenSource = new CancellationTokenSource();
            updateMigniatureParraleleToken = updateMigniatureParraleleTokenSource.Token;

            var coordonneesTile = Collectif.CoordonneesToTile(locaMillieux.Latitude, locaMillieux.Longitude, zoom);
            int tileX = coordonneesTile.X - 1;
            int tileY = coordonneesTile.Y;

            byte[,][] bitmapImageArray = new byte[3, 2][];
            string[,] bitmapErrorsArray = new string[3, 2];

            try
            {
                await Task.Run(() =>
                {
                    var listOfUrls = new List<(string url, int index_x, int index_y)>();

                    for (int index_x = 0; index_x < 3; index_x++)
                    {
                        for (int index_y = 0; index_y < 2; index_y++)
                        {
                            string urlbase = Collectif.GetUrl.FromTileXYZ(Layers.Current.class_tile_url, tileX + index_x, tileY + index_y, zoom, layerID, Javascript.InvokeFunction.getTile);
                            listOfUrls.Add((urlbase, index_x, index_y));
                        }
                    }

                    Parallel.ForEach(listOfUrls, new ParallelOptions { MaxDegreeOfParallelism = Settings.max_download_tiles_in_parralele }, url =>
                    {
                        if (updateMigniatureParraleleToken.IsCancellationRequested && updateMigniatureParraleleToken.CanBeCanceled)
                        {
                            return;
                        }

                        HttpResponse httpResponse = Tiles.Loader.GetImageAsync(url.url, tileX + url.index_x, tileY + url.index_y, zoom, layerID, pbfdisableadjacent: true).Result;

                        if (updateMigniatureParraleleToken.IsCancellationRequested && updateMigniatureParraleleToken.CanBeCanceled)
                        {
                            return;
                        }

                        if (httpResponse?.ResponseMessage.IsSuccessStatusCode == true)
                        {
                            bitmapImageArray[url.index_x, url.index_y] = httpResponse.Buffer;
                        }
                        else
                        {
                            bitmapImageArray[url.index_x, url.index_y] = Collectif.GetEmptyImageBufferFromText(httpResponse, layerID, "jpeg");
                        }
                    });
                }, updateMigniatureParraleleToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception loading preview: " + ex.Message);
            }

            if (zoom == Convert.ToInt32(ZoomSlider.Value))
            {
                ComboBoxItem selectedComboBoxItem = Combobox_color_conversion.SelectedItem as ComboBoxItem;
                string comboboxColorConversionSelectedItemTag = selectedComboBoxItem.Tag as string;
                NetVips.Enums.Interpretation interpretation = (NetVips.Enums.Interpretation)Enum.Parse(typeof(NetVips.Enums.Interpretation), comboboxColorConversionSelectedItemTag);

                if (!int.TryParse(TextBox_quality_number.Text, out int qualityInt))
                {
                    qualityInt = 1;
                }

                string format = Layers.Current.class_format;
                if (format != "png")
                {
                    format = "jpeg";
                }

                NetVips.VOption saveVOption = Collectif.GetSaveVOption(format, qualityInt, Layers.Current.class_tiles_size);

                BitmapSource ApplyEffectOnImageFromBuffer(byte[] imgArray)
                {
                    if (imgArray is null)
                    {
                        return null;
                    }

                    NetVips.Image NImage = NetVips.Image.NewFromBuffer(imgArray);

                    if (interpretation != NetVips.Enums.Interpretation.Srgb)
                    {
                        try
                        {
                            NImage = NImage.Colourspace(interpretation);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Colourspace change error: " + ex.Message);
                        }
                    }

                    byte[] NImageByteWithSaveOptions = NImage.WriteToBuffer("." + format, saveVOption);
                    NImage.Dispose();
                    NImage.Close();
                    return ByteArrayToBitmapSource(NImageByteWithSaveOptions);
                }

                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(bitmapImageArray[0, 0]), zoom, ImageTilePreview_0_0);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(bitmapImageArray[1, 0]), zoom, ImageTilePreview_0_1);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(bitmapImageArray[2, 0]), zoom, ImageTilePreview_0_2);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(bitmapImageArray[0, 1]), zoom, ImageTilePreview_1_0);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(bitmapImageArray[1, 1]), zoom, ImageTilePreview_1_1);
                SetBitmapIntoImageTilePreview(ApplyEffectOnImageFromBuffer(bitmapImageArray[2, 1]), zoom, ImageTilePreview_1_2);

                DoubleAnimation showAnim = Collectif.GetOpacityAnimation(1, 2);
                ImageTilePreview_0_0.BeginAnimation(OpacityProperty, showAnim);
                ImageTilePreview_1_0.BeginAnimation(OpacityProperty, showAnim);
                ImageTilePreview_0_1.BeginAnimation(OpacityProperty, showAnim);
                ImageTilePreview_1_1.BeginAnimation(OpacityProperty, showAnim);
                ImageTilePreview_0_2.BeginAnimation(OpacityProperty, showAnim);
                ImageTilePreview_1_2.BeginAnimation(OpacityProperty, showAnim);

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

                    return BitmapSource.Create(
                        width,
                        height,
                        96,
                        96,
                        PixelFormats.Indexed1,
                        BitmapPalettes.BlackAndWhiteTransparent,
                        pixels,
                        stride);
                }

                using (var ms = Collectif.ByteArrayToStream(array))
                {
                    var image = new BitmapImage();
                    if (ms != null)
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
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
            if (Convert.ToInt32(ZoomSlider.Value) != zoom)
            {
                return;
            }

            if (bitmap != null && ImageTilePreview != null)
            {
                bitmap.Freeze();
                ImageTilePreview.Source = bitmap;
            }
            else if (bitmap != null)
            {
                bitmap = new BitmapImage();
                bitmap.Freeze();
                ImageTilePreview.Source = bitmap;
            }
        }

        MapControl.Location GetSelectionCenterLocation()
        {
            var SelectionLocation = MainPage.MapSelectable.GetRectangleLocation();
            var NO_PIN_Location = (SelectionLocation.NO.Latitude, SelectionLocation.NO.Longitude);
            var SE_PIN_Location = (SelectionLocation.SE.Latitude, SelectionLocation.SE.Longitude);
            var LocaMillieux = Collectif.GetCenterBetweenTwoPoints(NO_PIN_Location, SE_PIN_Location);
            return new MapControl.Location(LocaMillieux.Latitude, LocaMillieux.Longitude);
        }

        async void GetCenterViewCityName()
        {
            var LocaMillieux = GetSelectionCenterLocation();

            try
            {
                bool ValidateResult(string center_view_city_arg)
                {
                    return center_view_city_arg != null && !string.IsNullOrEmpty(center_view_city_arg.Trim());
                }

                string url = "https://nominatim.openstreetmap.org/reverse?lat=" + LocaMillieux.Latitude + "&lon=" + LocaMillieux.Longitude + "&zoom=18&format=xml&email=siogabx@siogabx.fr";
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(url))
                using (Stream responseStream = await response.Content.ReadAsStreamAsync())
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
                                    if (city.Key == name && ValidateResult(value))
                                    {
                                        city_name[city.Key] = value.Trim();
                                    }
                                }
                            }
                        }
                    }

                    foreach (var city in city_name)
                    {
                        if (ValidateResult(city.Value))
                        {
                            centerViewCity = "_" + city.Value;
                            return;
                        }
                    }
                }

                centerViewCity = "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur recherche nom de ville : " + ex.Message);
            }
        }
        void Update_Labels()
        {
            if (!IsInitialized)
            {
                return;
            }

            RognageInfo rognage_info = GetOriginalImageSize();

            var SelectionLocation = MainPage.MapSelectable.GetRectangleLocation();
            double NO_PIN_Latitude = SelectionLocation.NO.Latitude;
            double NO_PIN_Longitude = SelectionLocation.NO.Longitude;
            double SE_PIN_Latitude = SelectionLocation.SE.Latitude;
            double SE_PIN_Longitude = SelectionLocation.SE.Longitude;
            int Zoom = (int)Math.Floor(ZoomSlider.Value);
            var NO_tile = Collectif.CoordonneesToTile(NO_PIN_Latitude, NO_PIN_Longitude, Zoom);
            var SE_tile = Collectif.CoordonneesToTile(SE_PIN_Latitude, SE_PIN_Longitude, Zoom);
            int lat_tile_number = Math.Abs(SE_tile.X - NO_tile.X) + 1;
            int long_tile_number = Math.Abs(SE_tile.Y - NO_tile.Y) + 1;

            void Update_Label_TileSize()
            {
                Label_LargeurTuile.Content = Layers.Current.class_tiles_size;
            }

            void Update_Label_LayerName()
            {
                Label_NomCalque.Content = Layers.Current.class_name;
            }

            void Update_Label_TileSource()
            {
                Label_Source.Content = Layers.Current.class_site_url;
            }

            void Update_Label_NbrTiles()
            {
                int nbr_of_tiles = lat_tile_number * long_tile_number;
                Label_NbrTiles.Content = $"{lat_tile_number} x {long_tile_number} = {nbr_of_tiles}";
            }

            List<string> FinalSize()
            {
                string final_size_W = "NaN";
                if (TextBox_Redim_WUnit.SelectedIndex == 0)
                {
                    final_size_W = TextBox_Redim_Width.Text;
                }
                else if (TextBox_Redim_WUnit.SelectedIndex == 1)
                {
                    if (double.TryParse(TextBox_Redim_Width.Text, out double Double_Redim_Width))
                    {
                        final_size_W = Math.Max(10, Math.Round(Double_Redim_Width / 100 * rognage_info.width, 0)).ToString();
                    }
                }

                string final_size_H = "NaN";
                if (TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    final_size_H = TextBox_Redim_Height.Text;
                }
                else if (TextBox_Redim_HUnit.SelectedIndex == 1)
                {
                    if (double.TryParse(TextBox_Redim_Height.Text, out double Double_Redim_Height))
                    {
                        final_size_H = Math.Max(10, Math.Round(Double_Redim_Height / 100 * rognage_info.height, 0)).ToString();
                    }
                }

                return new List<string>() { final_size_W, final_size_H };
            }

            void Update_Label_Label_ImgSizeF()
            {
                List<string> final_size_int = FinalSize();
                string final_size = $"{final_size_int[0]} x {final_size_int[1]} {Languages.Current["preparePropertyUnitsPixels"]}";

                if (!double.TryParse(TextBox_Redim_Width.Text, out double ScaleConvertedAttachedTextBoxText))
                {
                    return;
                }

                double SizeInPixel;
                if (TextBox_Redim_WUnit.SelectedIndex == 0)
                {
                    SizeInPixel = ScaleConvertedAttachedTextBoxText;
                }
                else
                {
                    SizeInPixel = Math.Max(10, Math.Round(rognage_info.width * ((double)ScaleConvertedAttachedTextBoxText / 100)));
                }

                double ScaleShrink = ((double)SizeInPixel / rognage_info.width);

                if (ScaleShrink < 1)
                {
                    final_size += $" | {Languages.GetWithArguments("preparePropertyResizedImageSizeIndicatorSmaller", Math.Round(1 - ScaleShrink, 2))}";
                }
                else if (ScaleShrink > 1)
                {
                    final_size += $" | {Languages.GetWithArguments("preparePropertyResizedImageSizeIndicatorBigger", Math.Abs(Math.Round(ScaleShrink, 2)))}";
                }

                Label_ImgSizeF.Content = final_size;
            }

            void Update_CheckIfSizeInf65000()
            {
                if (!IsInitialized)
                {
                    return;
                }

                var ResizedImageSize = GetResizedImageSize();
                var OriginalImageSize = GetOriginalImageSize();
                bool ResizedImageSizeHIsOK = ResizedImageSize.height >= 65000;
                bool ResizedImageSizeWIsOK = ResizedImageSize.width >= 65000;
                bool OriginalImageSizeHIsOK = OriginalImageSize.height >= 65000;
                bool OriginalImageSizeWIsOK = OriginalImageSize.height >= 65000;
                SolidColorBrush OKColor = Collectif.HexValueToSolidColorBrush("#888989");
                SolidColorBrush WrongColor = Collectif.HexValueToSolidColorBrush("#FF953C32");

                Label_ImgSize.Foreground = (OriginalImageSizeHIsOK || OriginalImageSizeWIsOK) ? WrongColor : OKColor;
                Label_ImgSizeF.Foreground = (ResizedImageSizeHIsOK || ResizedImageSizeWIsOK) ? WrongColor : OKColor;
                StartDownloadButton.Opacity = (OriginalImageSizeHIsOK || OriginalImageSizeWIsOK || ResizedImageSizeHIsOK || ResizedImageSizeWIsOK) ? 0.5 : 1;
                StartDownloadButton.IsEnabled = !(OriginalImageSizeHIsOK || OriginalImageSizeWIsOK || ResizedImageSizeHIsOK || ResizedImageSizeWIsOK);
            }

            void UpdateScale()
            {
                if (!TextBox_Scale.IsFocused && !TextBoxScaleIsLock())
                {
                    TextBox_Scale.Text = Math.Round(GetScale().Scale, 0).ToString();
                    UpdateScaleInformation();
                }
            }

            Update_Label_NbrTiles();
            Update_Label_TileSize();
            Update_Label_TileSource();
            Update_Label_LayerName();

            UpdateScale();
            LockRedimTextBox(false);
            UpdateScaleInformation();
            Update_CheckIfSizeInf65000();
            Update_Label_Label_ImgSizeF();
        }

        (double Resolution, double Scale) GetScale()
        {
            const double DPI = 96;
            const double InchPerMeters = 0.0254;
            const double EarthEquatorialRadiusInMeters = 6378137;

            double metterPerPx = EarthEquatorialRadiusInMeters * 2 * Math.PI / (double)Layers.Current.class_tiles_size;
            double latitude = GetSelectionCenterLocation().Latitude;
            double zoom = ZoomSlider.Value;
            double latitudeCos = Math.Cos(latitude * Math.PI / 180);

            double resolution = metterPerPx * latitudeCos / Math.Pow(2, zoom);
            double scale = DPI * 1 / InchPerMeters * resolution;

            return (resolution, scale);
        }

        void UpdateScaleInformation()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (!double.TryParse(TextBox_Scale.Text, out double targetScale))
            {
                return;
            }

            double distanceInMeterPerPixels = ScaleInfo.GetDistanceInMeterPerPixels(targetScale);
            TextBox_Scale.ToolTip = $"{Languages.Current["preparePropertyTooltipsScale"]} : {Math.Round(distanceInMeterPerPixels, 3)}";

            var redimSize = GetSizeAfterRedim();
            var optimalScale = ScaleInfo.SearchOptimalScale(distanceInMeterPerPixels, redimSize.Width);

            bool checkBoxAddScaleToImageEnable = IsEnoughPlaceForScale(redimSize.Width, redimSize.Height, optimalScale.Scale) && Layers.Current.class_hasscale;
            CheckBoxAddScaleToImage.IsEnabled = checkBoxAddScaleToImageEnable;

            if (checkBoxAddScaleToImageEnable)
            {
                CheckBoxAddScaleToImage.Content = Languages.Current["preparePropertyNameAddScaleBar"];
            }
            else
            {
                if (Layers.Current.class_hasscale)
                {
                    CheckBoxAddScaleToImage.Content = Languages.Current["preparePropertyNameAddScaleBar"] + " " + Languages.Current["preparePropertyNameAddScaleBarErrorsTooSmall"];
                }
                else
                {
                    CheckBoxAddScaleToImage.Content = Languages.Current["preparePropertyNameAddScaleBar"] + " " + Languages.Current["preparePropertyNameAddScaleBarErrorsLayerNotAtScale"];
                }
            }
        }

        bool IsEnoughPlaceForScale(double width, double height, double scale)
        {
            return height >= 30 && scale > 0;
        }

        private void LockRedimTextBox(bool lockValue)
        {
            if (lockValue)
            {
                WrapPanel_Largeur.Opacity = 0.8;
                WrapPanel_Hauteur.Opacity = 0.8;
                TextBox_Redim_HUnit.Opacity = 0.56;
                TextBox_Redim_WUnit.Opacity = 0.56;
                WrapPanel_Largeur.IsEnabled = false;
                WrapPanel_Hauteur.IsEnabled = false;
                SolidColorBrush grayColor = Collectif.RgbValueToSolidColorBrush(136, 137, 137);
                TextBox_Redim_Width.Foreground = grayColor;
                TextBox_Redim_Height.Foreground = grayColor;
                TextBox_Redim_WUnit.Foreground = grayColor;
                TextBox_Redim_HUnit.Foreground = grayColor;
            }
            else
            {
                WrapPanel_Largeur.Opacity = 1;
                WrapPanel_Hauteur.Opacity = 1;
                TextBox_Redim_HUnit.Opacity = 1;
                TextBox_Redim_WUnit.Opacity = 1;
                WrapPanel_Largeur.IsEnabled = true;
                WrapPanel_Hauteur.IsEnabled = true;
                SolidColorBrush whiteColor = Collectif.HexValueToSolidColorBrush("#BCBCBC");
                TextBox_Redim_Width.Foreground = whiteColor;
                TextBox_Redim_Height.Foreground = whiteColor;
                TextBox_Redim_WUnit.Foreground = whiteColor;
                TextBox_Redim_HUnit.Foreground = whiteColor;
            }
        }


        RognageInfo GetOriginalImageSize()
        {
            if (!IsInitialized) { return null; }

            var SelectionLocation = MainPage.MapSelectable.GetRectangleLocation();
            double NO_PIN_Latitude = SelectionLocation.NO.Latitude;
            double NO_PIN_Longitude = SelectionLocation.NO.Longitude;
            double SE_PIN_Latitude = SelectionLocation.SE.Latitude;
            double SE_PIN_Longitude = SelectionLocation.SE.Longitude;
            int Zoom = Convert.ToInt16(Math.Floor(ZoomSlider.Value));
            return RognageInfo.GetRognageValue(NO_PIN_Latitude, NO_PIN_Longitude, SE_PIN_Latitude, SE_PIN_Longitude, Zoom, Layers.Current.class_tiles_size);
        }
        (int width, int height) GetResizedImageSize()
        {
            int ResizeWidth = -1;
            int ResizeHeight = -1;
            RognageInfo rognage_info = GetOriginalImageSize();

            if (TextBox_Redim_HUnit.SelectedIndex == 0)
            {
                ResizeHeight = Convert.ToInt32(TextBox_Redim_Height.Text);
            }
            else if (TextBox_Redim_HUnit.SelectedIndex == 1)
            {
                ResizeHeight = (int)Math.Round(rognage_info.height * (Convert.ToDouble(TextBox_Redim_Height.Text) / 100));
            }

            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                ResizeWidth = Convert.ToInt32(TextBox_Redim_Width.Text);
            }
            else if (TextBox_Redim_WUnit.SelectedIndex == 1)
            {
                ResizeWidth = (int)Math.Round(rognage_info.width * (Convert.ToDouble(TextBox_Redim_Width.Text) / 100));
            }
            return (ResizeWidth, ResizeHeight);
        }
        private void TextBox_quality_number_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            string filteredString = Collectif.FilterDigitOnly(TextBox_quality_number.Text, null);
            int cursorPosition = TextBox_quality_number.SelectionStart;
            TextBox_quality_number.Text = filteredString;
            TextBox_quality_number.SelectionStart = cursorPosition;
            if (string.IsNullOrEmpty(filteredString.Trim())) { return; }
            int quality = Convert.ToInt32(filteredString);
            if (quality < 30)
            {
                //faible
                ChangeSelectedIndex(0);
            }
            else if (quality < 60)
            {
                //moyenne
                ChangeSelectedIndex(1);
            }
            else if (quality < 80)
            {
                //elevée
                ChangeSelectedIndex(2);
            }
            else if (quality < 100)
            {
                //supperieur
                ChangeSelectedIndex(3);
            }
            else if (quality > 99)
            {
                //supperieur+
                ChangeSelectedIndex(4);
                TextBox_quality_number.Text = "100";
                TextBox_quality_number.SelectionStart = 3;
            }
            void ChangeSelectedIndex(int index)
            {
                TextBox_quality_name.SelectedIndex = index;
                UpdateMigniatureParralele(true);
            }
        }
        private void TextBox_quality_name_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string str = TextBox_quality_number.Text;
            int quality = -1;
            if (!string.IsNullOrEmpty(str.Trim()))
            {
                quality = Convert.ToInt32(str);
            }
            if (TextBox_quality_name.SelectedIndex == 0 && (quality < 0 || quality >= 30))
            {
                TextBox_quality_number.Text = "10";
            }
            else if (TextBox_quality_name.SelectedIndex == 1 && (quality < 30 || quality >= 60))
            {
                TextBox_quality_number.Text = "30";
            }
            else if (TextBox_quality_name.SelectedIndex == 2 && (quality < 60 || quality >= 80))
            {
                TextBox_quality_number.Text = "60";
            }
            else if (TextBox_quality_name.SelectedIndex == 3 && (quality < 80 || quality >= 100))
            {
                TextBox_quality_number.Text = "80";
            }
            else if (TextBox_quality_name.SelectedIndex == 4 && quality < 100)
            {
                TextBox_quality_number.Text = "100";
            }
            UpdateMigniatureParralele(true);
        }

        void TextBoxRedimDimensionChanged(TextBox AttachedTextbox, ComboBox AttachedComboBoxUnit, TextBox OpositeTextbox, string SourcePropertyName, TextChangedEventHandler OpositeTextChangedEvent)
        {
            if (!IsInitialized)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(AttachedTextbox.Text))
            {
                OpositeTextbox.Text = "";
                return;
            }
            RognageInfo rognage_info = GetOriginalImageSize();
            double SourcePropertyAttachedRognageDimension;
            double SourcePropertyOpositeRognageDimension;
            if (SourcePropertyName == "width")
            {
                SourcePropertyAttachedRognageDimension = rognage_info?.width ?? 0;
                SourcePropertyOpositeRognageDimension = rognage_info?.height ?? 0;
            }
            else if (SourcePropertyName == "height")
            {
                SourcePropertyAttachedRognageDimension = rognage_info?.height ?? 0;
                SourcePropertyOpositeRognageDimension = rognage_info?.width ?? 0;
            }
            else
            {
                throw new ArgumentException("Unknown SourcePropertyName", SourcePropertyName);
            }

            if (AttachedComboBoxUnit.SelectedIndex == 0)
            {
                Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(AttachedTextbox, 65000);
                if (!double.TryParse(AttachedTextbox.Text, out double ConvertedAttachedTextBoxText)) return;

                double Shrink = ConvertedAttachedTextBoxText / (double)SourcePropertyAttachedRognageDimension;
                int value = (int)Math.Round((double)SourcePropertyOpositeRognageDimension * Shrink);

                if (value > 65000)
                {
                    OpositeTextbox.Text = value.ToString();
                }
                else
                {
                    OpositeTextbox.SetText(value.ToString(), OpositeTextChangedEvent);
                    OpositeTextbox.SelectionStart = OpositeTextbox.Text.Length;
                }
            }
            else if (AttachedComboBoxUnit.SelectedIndex == 1)
            {
                Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(AttachedTextbox, 65000, new List<char>() { '.' });
                OpositeTextbox.SetText(AttachedTextbox.Text, OpositeTextChangedEvent);
                OpositeTextbox.SelectionStart = OpositeTextbox.Text.Length;
                AttachedTextbox.SelectionStart = AttachedTextbox.Text.Length;
            }

            Update_Labels();

            if (AttachedTextbox.IsFocused)
            {
                TextBoxScaleIsLock(false);
                if (!double.TryParse(AttachedTextbox.Text, out double ScaleConvertedAttachedTextBoxText)) return;
                double SizeInPixel;
                if (AttachedComboBoxUnit.SelectedIndex == 0)
                {
                    SizeInPixel = ScaleConvertedAttachedTextBoxText;
                }
                else
                {
                    SizeInPixel = Math.Round(SourcePropertyAttachedRognageDimension * ((double)ScaleConvertedAttachedTextBoxText / 100));
                }
                double ScaleShrink = (double)SourcePropertyAttachedRognageDimension / SizeInPixel;
                Debug.WriteLine("ScaleShrink " + ScaleShrink);
                TextBox_Scale.Text = Math.Round(GetScale().Scale * ScaleShrink).ToString();
                UpdateScaleInformation();
            }
        }

        private void TextBox_Redim_Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxRedimDimensionChanged(TextBox_Redim_Height, TextBox_Redim_HUnit, TextBox_Redim_Width, "height", TextBox_Redim_Width_TextChanged);
        }

        private void TextBox_Redim_Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxRedimDimensionChanged(TextBox_Redim_Width, TextBox_Redim_WUnit, TextBox_Redim_Height, "width", TextBox_Redim_Height_TextChanged);
        }

        void SetDefaultRedimTextBoxValue(TextBox AttachedTextbox, ComboBox AttachedComboBoxUnit, string SourcePropertyName)
        {
            RognageInfo rognage_info = GetOriginalImageSize();
            double SourcePropertyRognageDimension;
            if (SourcePropertyName == "width")
            {
                SourcePropertyRognageDimension = rognage_info?.width ?? 0;
            }
            else if (SourcePropertyName == "height")
            {
                SourcePropertyRognageDimension = rognage_info?.height ?? 0;
            }
            else
            {
                throw new System.ArgumentException("Unknown SourcePropertyName", SourcePropertyName);
            }

            if (string.IsNullOrWhiteSpace(AttachedTextbox.Text.Trim()))
            {
                if (AttachedComboBoxUnit.SelectedIndex == 0)
                {
                    AttachedTextbox.Text = SourcePropertyRognageDimension.ToString();
                }
                else if (AttachedComboBoxUnit.SelectedIndex == 1)
                {
                    AttachedTextbox.Text = "100";
                }
            }
        }

        void TextBoxRedimUnitChanged(TextBox AttachedTextbox, ComboBox AttachedComboBoxUnit, ComboBox OpositeComboBoxUnit, string SourcePropertyName)
        {
            if (!IsInitialized) { return; }
            RognageInfo rognage_info = GetOriginalImageSize();

            double SourcePropertyRognageDimension;
            if (SourcePropertyName == "width")
            {
                SourcePropertyRognageDimension = rognage_info?.width ?? 0;
            }
            else if (SourcePropertyName == "height")
            {
                SourcePropertyRognageDimension = rognage_info?.height ?? 0;
            }
            else
            {
                throw new System.ArgumentException("Unknown SourcePropertyName", SourcePropertyName);
            }

            int GetLastRedimTypeUnit(int value = -1)
            {
                if (SourcePropertyName == "width")
                {
                    if (value != -1)
                    {
                        lastRedimWUnitSelected = value;
                    }
                    return lastRedimWUnitSelected;
                }
                else
                {
                    if (value != -1)
                    {
                        lastRedimHUnitSelected = value;
                    }
                    return lastRedimHUnitSelected;
                }
            }

            if (string.IsNullOrWhiteSpace(AttachedTextbox.Text.Trim()))
            {
                SetDefaultRedimTextBoxValue(AttachedTextbox, AttachedComboBoxUnit, SourcePropertyName);
            }

            if (!double.TryParse(AttachedTextbox.Text, out double txtBValue)) { return; };

            if (AttachedComboBoxUnit.SelectedIndex == 1 && GetLastRedimTypeUnit() == 0)
            {
                GetLastRedimTypeUnit(1);
                OpositeComboBoxUnit.SelectedIndex = 1;
                AttachedTextbox.Text = (Math.Round((double)txtBValue / (double)SourcePropertyRognageDimension * 100, 1).ToString());

            }
            else if (AttachedComboBoxUnit.SelectedIndex == 0 && GetLastRedimTypeUnit() == 1)
            {
                GetLastRedimTypeUnit(0);
                OpositeComboBoxUnit.SelectedIndex = 0;
                AttachedTextbox.Text = (Math.Round(SourcePropertyRognageDimension * ((double)txtBValue / 100)).ToString());
            }
        }

        private void TextBox_Redim_HUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBoxRedimUnitChanged(TextBox_Redim_Height, TextBox_Redim_HUnit, TextBox_Redim_WUnit, "height");
        }

        private void TextBox_Redim_WUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBoxRedimUnitChanged(TextBox_Redim_Width, TextBox_Redim_WUnit, TextBox_Redim_HUnit, "width");
        }

        void SetTextBoxRedimDimensionTextBasedOnPourcentage(TextBox AttachedTextbox, ComboBox AttachedComboBoxUnit, string SourcePropertyName, double PourcentageValue)
        {
            RognageInfo rognage_info = GetOriginalImageSize();
            double SourcePropertyRognageDimension;
            if (SourcePropertyName == "width")
            {
                SourcePropertyRognageDimension = rognage_info?.width ?? 0;
            }
            else if (SourcePropertyName == "height")
            {
                SourcePropertyRognageDimension = rognage_info?.height ?? 0;
            }
            else
            {
                throw new ArgumentException("Unknown SourcePropertyName", SourcePropertyName);
            }
            if (AttachedComboBoxUnit.SelectedIndex == 1)
            {
                AttachedTextbox.Text = PourcentageValue.ToString();

            }
            else if (AttachedComboBoxUnit.SelectedIndex == 0)
            {
                AttachedTextbox.Text = Math.Round(SourcePropertyRognageDimension * ((double)PourcentageValue / 100)).ToString();
            }

        }


        private void TextBox_Redim_Width_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox_Redim_Width.Text.Trim()))
            {
                SetDefaultRedimTextBoxValue(TextBox_Redim_Width, TextBox_Redim_WUnit, "width");
            }
            else
            {
                if (double.TryParse(TextBox_Redim_Width.Text, out double TextBox_Redim_Width_Value))
                {
                    if (TextBox_Redim_Width_Value < 10 && TextBox_Redim_WUnit.SelectedIndex == 0)
                    {
                        TextBox_Redim_Width.Text = "10";
                    }
                }
            }
        }
        private void TextBox_Redim_Height_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox_Redim_Height.Text.Trim()))
            {
                SetDefaultRedimTextBoxValue(TextBox_Redim_Height, TextBox_Redim_HUnit, "height");
            }
            else
            {
                if (double.TryParse(TextBox_Redim_Height.Text, out double TextBox_Redim_Height_Value))
                {
                    if (TextBox_Redim_Height_Value < 10 && TextBox_Redim_HUnit.SelectedIndex == 0)
                    {
                        TextBox_Redim_Height.Text = "10";
                    }
                }
            }
        }

        public (int Width, int Height) GetSizeAfterRedim()
        {
            var rognage_info = GetOriginalImageSize();
            int ResizeWidth = -1;
            int ResizeHeight = -1;
            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                ResizeWidth = Convert.ToInt32(TextBox_Redim_Width.Text);
            }
            else if (TextBox_Redim_WUnit.SelectedIndex == 1)
            {
                ResizeWidth = Math.Max(10, (int)Math.Round(rognage_info.width * (Convert.ToDouble(TextBox_Redim_Width.Text) / 100)));
            }

            if (TextBox_Redim_HUnit.SelectedIndex == 0)
            {
                ResizeHeight = Convert.ToInt32(TextBox_Redim_Height.Text);
            }
            else if (TextBox_Redim_HUnit.SelectedIndex == 1)
            {
                ResizeHeight = Math.Max(10, (int)Math.Round(rognage_info.height * (Convert.ToDouble(TextBox_Redim_Height.Text) / 100)));
            }

            return (ResizeWidth, ResizeHeight);
        }

        private void StartDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            int zoom = Convert.ToInt32(ZoomSlider.Value);

            string filter = "";
            const string pngFilter = "PNG|*.png";
            const string jpgFilter = "JPEG|*.jpg";
            const string tiffFilter = "TIFF|*.tif";

            string filterConcat(string filter1, string filter2)
            {
                return filter1 + "|" + filter2;
            }

            if (string.Equals(Layers.Current.class_format, "png", StringComparison.OrdinalIgnoreCase))
            {
                filter = pngFilter;
            }
            else if (string.Equals(Layers.Current.class_format, "jpeg", StringComparison.OrdinalIgnoreCase))
            {
                filter = jpgFilter;
            }
            else if (string.Equals(Layers.Current.class_format, "pbf", StringComparison.OrdinalIgnoreCase))
            {
                filter = filterConcat(jpgFilter, pngFilter);
            }

            string strName = defaultFilename + centerViewCity + "_" + zoom;
            strName = strName.Replace(Path.GetInvalidFileNameChars(), string.Empty);
            strName = strName.ReplaceLoop(" ", "_");
            strName = strName.ReplaceLoop("__", "_");
            strName = strName.ReplaceLoop("_-_", "_");

            SaveFileDialog saveFileDialog1 = new SaveFileDialog()
            {
                Filter = filterConcat(filter, tiffFilter),
                Title = Languages.Current["saveFileDialogSelectSaveLocation"],
                RestoreDirectory = true,
                OverwritePrompt = true,
                FileName = strName,
                ValidateNames = true
            };

            bool? isValidate = saveFileDialog1.ShowDialog();
            if (string.IsNullOrEmpty(saveFileDialog1.FileName.Trim()) || isValidate != true)
            {
                return;
            }

            string saveDirectory = Path.GetDirectoryName(saveFileDialog1.FileName) + @"\";
            string filename = Path.GetFileName(saveFileDialog1.FileName);
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

            int resizeWidth;
            int resizeHeight;
            if (!(TextBox_Redim_WUnit.SelectedIndex == 1 && TextBox_Redim_Width.Text == "100") &&
                !(TextBox_Redim_HUnit.SelectedIndex == 1 && TextBox_Redim_Height.Text == "100"))
            {
                var resizeDimension = GetSizeAfterRedim();
                resizeWidth = resizeDimension.Width;
                resizeHeight = resizeDimension.Height;
            }
            else
            {
                resizeWidth = -1;
                resizeHeight = -1;
            }

            NetVips.Enums.Interpretation interpretation = (NetVips.Enums.Interpretation)Enum.Parse(typeof(NetVips.Enums.Interpretation), (Combobox_color_conversion.SelectedItem as ComboBoxItem).Tag as string);

            double initialScale = GetScale().Scale;
            if (!double.TryParse(TextBox_Scale.Text, out double targetScale))
            {
                throw new FormatException("Unable to convert the scale value to a number (double).");
            }

            double distanceInMeterPerPixels = ScaleInfo.GetDistanceInMeterPerPixels(targetScale);
            var drawScaleInfo = ScaleInfo.SearchOptimalScale(distanceInMeterPerPixels, resizeWidth);
            bool drawScale = (CheckBoxAddScaleToImage.IsChecked ?? false) &&
                             IsEnoughPlaceForScale(resizeWidth, resizeHeight, drawScaleInfo.Scale) &&
                             Layers.Current.class_hasscale;

            ScaleInfo scaleInfo = new ScaleInfo(initialScale, targetScale, distanceInMeterPerPixels, drawScale, drawScaleInfo.Scale, drawScaleInfo.PixelLenght);

            var selectionLocation = MainPage.MapSelectable.GetRectangleLocation();
            DownloadOptions downloadOptions = new DownloadOptions(0, saveDirectory, format, filename, "", "", 0, zoom, quality, "", selectionLocation.NO, selectionLocation.SE, resizeWidth, resizeHeight, interpretation, scaleInfo);

            MainWindow._instance.PrepareDownloadBeforeStart(downloadOptions);
            ClosePage();
            TextBoxScaleIsLock(false);

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
        private void TextBox_Scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized || !TextBox_Scale.IsFocused)
            {
                return;
            }

            Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBox_Scale, null);

            if (!double.TryParse(TextBox_Scale.Text, out double currentTextBoxTargetScale))
            {
                return;
            }

            SetTextBoxRedimDimensionTextBasedOnPourcentage(TextBox_Redim_Width, TextBox_Redim_WUnit, "width", GetScale().Scale / currentTextBoxTargetScale * 100);
            TextBoxScaleIsLock(true);
            UpdateScaleInformation();
        }

        private void TextBox_Scale_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBox_Scale.Text))
            {
                TextBoxScaleIsLock(false);
                Update_Labels();
                SetTextBoxRedimDimensionTextBasedOnPourcentage(TextBox_Redim_Width, TextBox_Redim_WUnit, "width", 100);
                UpdateScaleInformation();
            }
        }

        bool TextBoxScaleIsLock(bool? ChangeIsLock = null)
        {
            if (!IsInitialized) { return false; }
            if (ChangeIsLock == true)
            {
                TextBox_Scale.Tag = "manual";
                return true;
            }
            else if (ChangeIsLock == false)
            {
                TextBox_Scale.Tag = "automatic";
                return false;
            }

            if (TextBox_Scale.Tag.ToString() == "manual")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
