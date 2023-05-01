using MapsInMyFolder.Commun;
using Microsoft.Win32;
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

            int minimum_zoom = Layers.Curent.class_min_zoom ?? 0;
            int maximum_zoom = Layers.Curent.class_max_zoom ?? 0;
            if (ZoomSlider.Value < minimum_zoom)
            {
                ZoomSlider.Value = minimum_zoom;
            }
            if (ZoomSlider.Value > maximum_zoom)
            {
                ZoomSlider.Value = maximum_zoom;
            }
            Update_Labels();
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


        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Init();
            SolidColorBrush brush = Collectif.HexValueToSolidColorBrush(Layers.Curent.class_specialsoptions.BackgroundColor);
            StackPanel_ImageTilePreview_0.Background = brush;
            StackPanel_ImageTilePreview_1.Background = brush;
            StackPanel_ImageTilePreview_2.Background = brush;
            //SolidColorBrush WhiteColor = Collectif.HexValueToSolidColorBrush("#BCBCBC");
            //TextBox_quality_number.Foreground = WhiteColor;
            //RedimSwitch.OnContent = "Activé";
            //RedimSwitch.OffContent = "Désactivé";
            SetTextBoxRedimDimension();
        }


        private void SetTextBoxRedimDimension()
        {
            RognageInfo rognage_info = GetOriginalImageSize();
            if (rognage_info != null)
            {
                Label_ImgSize.Content = rognage_info.width.ToString() + " x " + rognage_info.height.ToString() + " pixels";
                if (TextBox_Redim_HUnit.SelectedIndex == 0 && TextBox_Redim_HUnit.SelectedIndex == 0)
                {
                    TextBox_Redim_Width.SetText(rognage_info.width.ToString());
                    TextBox_Redim_Height.SetText(rognage_info.height.ToString());
                }
            }
        }


        public void Init()
        {
            Update_Labels();
            LastResquestZoom = -1;
            UpdateMigniatureParralele();
            GetCenterViewCityName();
            Label_SliderMinMax.Content = "Niveau de zoom (min=" + Layers.Curent.class_min_zoom + ", max=" + Layers.Curent.class_max_zoom + ")";
            //RedimSwitch.IsOn = false;
            ZoomSlider.Value = Math.Round(MainWindow._instance.MainPage.mapviewer.ZoomLevel);
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
            ZoomSlider.Maximum = Math.Max(21, Layers.Curent.class_max_zoom ?? 21);


            if (Layers.Curent.class_hasscale)
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
                Label_ScalePrefix.Content = "Sans echelle";
                Label_ScalePrefix.Foreground = Collectif.HexValueToSolidColorBrush("#5A5A5A");
                Label_Scale.Foreground = Collectif.HexValueToSolidColorBrush("#5A5A5A");
            }


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
            int maximum_zoom = Layers.Curent.class_max_zoom ?? 0;
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

                if (!int.TryParse(TextBox_quality_number.Text, out int qualityInt)) { qualityInt = 1; }

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
        }

        MapControl.Location GetSelectionCenterLocation()
        {
            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            var NO_PIN_Location = (SelectionLocation.NO.Latitude, SelectionLocation.NO.Longitude);
            var SE_PIN_Location = (SelectionLocation.SE.Latitude, SelectionLocation.SE.Longitude);
            var LocaMillieux = Collectif.GetCenterBetweenTwoPoints(NO_PIN_Location, SE_PIN_Location);
            return new MapControl.Location(LocaMillieux.Latitude, LocaMillieux.Longitude);
        }

        async void GetCenterViewCityName()
        {
            var LocaMillieux = GetSelectionCenterLocation();
            await Task.Run(() =>
            {
                try
                {
                    static bool ValidateResult(string center_view_city_arg)
                    {
                        return center_view_city_arg != null && !string.IsNullOrEmpty(center_view_city_arg.Trim());
                    }
                    string url = "https://nominatim.openstreetmap.org/reverse?lat=" + LocaMillieux.Latitude + "&lon=" + LocaMillieux.Longitude + "&zoom=18&format=xml&email=siogabx@siogabx.fr";
                    DebugMode.WriteLine("Recherche ville centre : lat=" + LocaMillieux.Latitude + " lon=" + LocaMillieux.Longitude + "\nurl=" + url);
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
            RognageInfo rognage_info = GetOriginalImageSize();

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
                string final_size = "";
                List<string> final_size_int = FinalSize();
                final_size += final_size_int[0];
                final_size += " x ";
                final_size += final_size_int[1];
                final_size += " pixels";

                if (!double.TryParse(TextBox_Redim_Width.Text, out double ScaleConvertedAttachedTextBoxText)) return;
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
                    final_size += $" | {Math.Round(1 - ScaleShrink, 2)}x plus petite";
                }
                else if (ScaleShrink > 1)
                {
                    final_size += $" | {Math.Abs(Math.Round(ScaleShrink, 2))}x plus grande";
                }

                Label_ImgSizeF.Content = final_size;
            }

            void Update_CheckIfSizeInf65000()
            {
                if (!IsInitialized) { return; }
                var ResizedImageSize = GetResizedImageSize();
                var OriginalImageSize = GetOriginalImageSize();
                bool ResizedImageSizeHIsOK = ResizedImageSize.height >= 65000;
                bool ResizedImageSizeWIsOK = ResizedImageSize.width >= 65000;
                bool OriginalImageSizeHIsOK = OriginalImageSize.height >= 65000;
                bool OriginalImageSizeWIsOK = OriginalImageSize.height >= 65000;
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

            void UpdateScale()
            {
                if (!TextBox_Scale.IsFocused && !TextBoxScaleIsLock())
                {
                    TextBox_Scale.SetText(Math.Round(GetScale().Scale, 0).ToString());
                    UpdateScaleInformation();
                }
            }

            Update_Label_NbrTiles();
            Update_Label_Label_ImgSizeF();
            Update_Label_TileSize();
            Update_Label_TileSource();
            Update_Label_LayerName();
            UpdateScale();
            LockRedimTextBox(false);
            UpdateScaleInformation();
            Update_CheckIfSizeInf65000();
        }

        (double Resolution, double Scale) GetScale()
        {
            const double DPI = 96;
            const double InchPerMeters = 0.0254;
            const double EarthEquatorialRadiusInMeters = 6378137;
            double MetterPerPx = EarthEquatorialRadiusInMeters * 2 * Math.PI / (double)Layers.Curent.class_tiles_size;
            double Latitude = GetSelectionCenterLocation().Latitude;
            double Zoom = ZoomSlider.Value;
            double LatitudeCos = Math.Cos(Latitude * Math.PI / 180);
            double Resolution = MetterPerPx * LatitudeCos / Math.Pow(2, Zoom);
            double Scale = DPI * 1 / InchPerMeters * Resolution;

            return (Resolution, Scale);
        }

        void UpdateScaleInformation()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (!double.TryParse(TextBox_Scale.Text, out double TargetScale)) return;
            double DistanceInMeterPerPixels = ScaleInfo.GetDistanceInMeterPerPixels(TargetScale);
            TextBox_Scale.ToolTip = $"Mêtres par pixels : {Math.Round(DistanceInMeterPerPixels, 3).ToString()}";

            var RedimSize = GetSizeAfterRedim();
            var OptimalScale = ScaleInfo.SearchOptimalScale(DistanceInMeterPerPixels, RedimSize.with);

            bool CheckBoxAddScaleToImageEnable = IsEnoughPlaceForScale(RedimSize.with, RedimSize.height, OptimalScale.Scale) && Layers.Curent.class_hasscale;
            CheckBoxAddScaleToImage.IsEnabled = CheckBoxAddScaleToImageEnable;
            if (CheckBoxAddScaleToImageEnable)
            {
                CheckBoxAddScaleToImage.Content = "Ajouter une echelle graphique à l'image";
            }
            else
            {
                if (Layers.Curent.class_hasscale)
                {
                    CheckBoxAddScaleToImage.Content = "Ajouter une echelle graphique à l'image (image trop petite)";
                }
                else
                {
                    CheckBoxAddScaleToImage.Content = "Ajouter une echelle graphique à l'image (les tuiles de ce calque ne sont pas à l'echelle)";
                }
            }
        }

        bool IsEnoughPlaceForScale(double width, double height, double scale)
        {
            if (height >= 30 && scale > 0)
            {
                return true;
            }
            return false;
        }

        private void LockRedimTextBox(bool Lock)
        {
            if (Lock)
            {
                WrapPanel_Largeur.Opacity = 0.8;
                WrapPanel_Hauteur.Opacity = 0.8;
                TextBox_Redim_HUnit.Opacity = 0.56;
                TextBox_Redim_WUnit.Opacity = 0.56;
                WrapPanel_Largeur.IsEnabled = false;
                WrapPanel_Hauteur.IsEnabled = false;
                SolidColorBrush GrayColor = Collectif.RgbValueToSolidColorBrush(136, 137, 137);
                TextBox_Redim_Width.Foreground = GrayColor;
                TextBox_Redim_Height.Foreground = GrayColor;
                TextBox_Redim_WUnit.Foreground = GrayColor;
                TextBox_Redim_HUnit.Foreground = GrayColor;
            }
            else
            {
                WrapPanel_Largeur.Opacity = 1;
                WrapPanel_Hauteur.Opacity = 1;
                TextBox_Redim_HUnit.Opacity = 1;
                TextBox_Redim_WUnit.Opacity = 1;
                WrapPanel_Largeur.IsEnabled = true;
                WrapPanel_Hauteur.IsEnabled = true;
                SolidColorBrush WhiteColor = Collectif.HexValueToSolidColorBrush("#BCBCBC");
                TextBox_Redim_Width.Foreground = WhiteColor;
                TextBox_Redim_Height.Foreground = WhiteColor;
                TextBox_Redim_WUnit.Foreground = WhiteColor;
                TextBox_Redim_HUnit.Foreground = WhiteColor;
            }
        }

        RognageInfo GetOriginalImageSize()
        {
            if (!IsInitialized) { return null; }

            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            double NO_PIN_Latitude = SelectionLocation.NO.Latitude;
            double NO_PIN_Longitude = SelectionLocation.NO.Longitude;
            double SE_PIN_Latitude = SelectionLocation.SE.Latitude;
            double SE_PIN_Longitude = SelectionLocation.SE.Longitude;
            int Zoom = Convert.ToInt16(Math.Floor(ZoomSlider.Value));
            RognageInfo rognage_info = RognageInfo.GetRognageValue(NO_PIN_Latitude, NO_PIN_Longitude, SE_PIN_Latitude, SE_PIN_Longitude, Zoom, Layers.Curent.class_tiles_size);
            return rognage_info;
        }
        (int width, int height) GetResizedImageSize()
        {
            int RedimWidth = -1;
            int RedimHeight = -1;
            RognageInfo rognage_info = GetOriginalImageSize();

            if (TextBox_Redim_HUnit.SelectedIndex == 0)
            {
                RedimHeight = Convert.ToInt32(TextBox_Redim_Height.Text);
            }
            else if (TextBox_Redim_HUnit.SelectedIndex == 1)
            {
                RedimHeight = (int)Math.Round(rognage_info.height * (Convert.ToDouble(TextBox_Redim_Height.Text) / 100));
            }

            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                RedimWidth = Convert.ToInt32(TextBox_Redim_Width.Text);
            }
            else if (TextBox_Redim_WUnit.SelectedIndex == 1)
            {
                RedimWidth = (int)Math.Round(rognage_info.width * (Convert.ToDouble(TextBox_Redim_Width.Text) / 100));
            }

            return (RedimWidth, RedimHeight);

        }

        private void TextBox_quality_number_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            string filtered_string = Collectif.FilterDigitOnly(TextBox_quality_number.Text, null);
            var cursor_position = TextBox_quality_number.SelectionStart;
            TextBox_quality_number.SetText(filtered_string);
            TextBox_quality_number.SelectionStart = cursor_position;
            void ChangeSelectedIndex(int index)
            {
                TextBox_quality_name.SelectedIndex = index;
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
            string str = TextBox_quality_number.Text;
            int quality = -1;
            if (!string.IsNullOrEmpty(str.Trim()))
            {
                quality = Convert.ToInt32(str);
            }

            if (TextBox_quality_name.SelectedIndex == 0)
            {
                //faible
                if (quality < 0 || quality >= 30)
                {
                    TextBox_quality_number.SetText("10");
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 1)
            {
                //moyenne
                if (quality < 30 || quality >= 60)
                {
                    TextBox_quality_number.SetText("30");
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 2)
            {
                //elevée
                if (quality < 60 || quality >= 80)
                {
                    TextBox_quality_number.SetText("60");
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 3)
            {
                //supperieur
                if (quality < 80 || quality >= 100)
                {
                    TextBox_quality_number.SetText("80");
                }
            }
            else if (TextBox_quality_name.SelectedIndex == 4)
            {
                //supperieur +
                if (quality < 100)
                {
                    TextBox_quality_number.SetText("100");
                }
            }
            UpdateMigniatureParralele(true);
        }

        void TextBoxRedimDimensionChanged(TextBox AttachedTextbox, ComboBox AttachedComboBoxUnit, TextBox OpositeTextbox, string SourcePropertyName)
        {
            if (!IsInitialized) { return; }
            if (string.IsNullOrEmpty(AttachedTextbox.Text.Trim()))
            {
                OpositeTextbox.SetText("");
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
                throw new System.ArgumentException("Unknown SourcePropertyName", SourcePropertyName);
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
                    OpositeTextbox.SetText(value.ToString());
                    OpositeTextbox.SelectionStart = OpositeTextbox.Text.Length;
                }
            }
            else if (AttachedComboBoxUnit.SelectedIndex == 1)
            {
                Collectif.FilterDigitOnlyWhileWritingInTextBoxWithMaxValue(AttachedTextbox, 65000, new List<char>() { '.' });
                OpositeTextbox.SetText(AttachedTextbox.Text);
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
                TextBox_Scale.SetText(Math.Round(GetScale().Scale * ScaleShrink).ToString());
                UpdateScaleInformation();
            }
        }

        private void TextBox_Redim_Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxRedimDimensionChanged(TextBox_Redim_Height, TextBox_Redim_HUnit, TextBox_Redim_Width, "height");
        }

        private void TextBox_Redim_Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBoxRedimDimensionChanged(TextBox_Redim_Width, TextBox_Redim_WUnit, TextBox_Redim_Height, "width");
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
                    AttachedTextbox.SetText(SourcePropertyRognageDimension.ToString());
                }
                else if (AttachedComboBoxUnit.SelectedIndex == 1)
                {
                    AttachedTextbox.SetText("100");
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
                        Last_Redim_WUnit_Selected = value;
                    }
                    return Last_Redim_WUnit_Selected;
                }
                else
                {
                    if (value != -1)
                    {
                        Last_Redim_HUnit_Selected = value;
                    }
                    return Last_Redim_HUnit_Selected;
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
                AttachedTextbox.SetText(Math.Round((double)txtBValue / (double)SourcePropertyRognageDimension * 100, 1).ToString());

            }
            else if (AttachedComboBoxUnit.SelectedIndex == 0 && GetLastRedimTypeUnit() == 1)
            {
                GetLastRedimTypeUnit(0);
                OpositeComboBoxUnit.SelectedIndex = 0;
                AttachedTextbox.SetText(Math.Round(SourcePropertyRognageDimension * ((double)txtBValue / 100)).ToString());
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

        public (int with, int height) GetSizeAfterRedim()
        {
            var rognage_info = GetOriginalImageSize();
            int RedimWidth = -1;
            int RedimHeight = -1;
            if (TextBox_Redim_WUnit.SelectedIndex == 0)
            {
                RedimWidth = Convert.ToInt32(TextBox_Redim_Width.Text);
            }
            else if (TextBox_Redim_WUnit.SelectedIndex == 1)
            {
                RedimWidth = Math.Max(10, (int)Math.Round(rognage_info.width * (Convert.ToDouble(TextBox_Redim_Width.Text) / 100)));
            }

            if (TextBox_Redim_HUnit.SelectedIndex == 0)
            {
                RedimHeight = Convert.ToInt32(TextBox_Redim_Height.Text);
            }
            else if (TextBox_Redim_HUnit.SelectedIndex == 1)
            {
                RedimHeight = Math.Max(10, (int)Math.Round(rognage_info.height * (Convert.ToDouble(TextBox_Redim_Height.Text) / 100)));
            }

            return (RedimWidth, RedimHeight);
        }

        private void StartDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            int zoom = Convert.ToInt32(ZoomSlider.Value);

            string filter = "";
            const string pngfilter = "PNG|*.png";
            const string jpgfilter = "JPEG|*.jpg";
            const string tifffilter = "TIFF|*.tif";
            string filterconcat(string filter1, string filter2)
            {
                return filter1 + "|" + filter2;
            }
            if (string.Equals(Layers.Curent.class_format, "png", StringComparison.OrdinalIgnoreCase)) filter = pngfilter;
            if (string.Equals(Layers.Curent.class_format, "jpeg", StringComparison.OrdinalIgnoreCase)) filter = jpgfilter;
            if (string.Equals(Layers.Curent.class_format, "pbf", StringComparison.OrdinalIgnoreCase)) filter = filterconcat(jpgfilter, pngfilter);

            string str_name = default_filename + center_view_city + "_" + zoom;
            str_name = str_name.Replace(Path.GetInvalidFileNameChars(), string.Empty);
            str_name = str_name.ReplaceLoop(" ", "_");
            str_name = str_name.ReplaceLoop("__", "_");
            SaveFileDialog saveFileDialog1 = new SaveFileDialog()
            {
                Filter = filterconcat(filter, tifffilter),
                Title = "Selectionnez un emplacement de sauvegarde :",
                RestoreDirectory = true,
                OverwritePrompt = true,
                FileName = str_name,
                ValidateNames = true
            };

            bool? IsValidate = saveFileDialog1.ShowDialog();
            if (string.IsNullOrEmpty(saveFileDialog1.FileName.Trim()) || IsValidate != true)
            {
                return;
            }
            string save_directory = Path.GetDirectoryName(saveFileDialog1.FileName) + @"\";
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
            int RedimWidth;
            int RedimHeight;
            if (!(TextBox_Redim_WUnit.SelectedIndex == 1 && TextBox_Redim_Width.Text == "100") && !(TextBox_Redim_HUnit.SelectedIndex == 1 && TextBox_Redim_Height.Text == "100"))//***************************************************
            {
                var Redim = GetSizeAfterRedim();
                RedimWidth = Redim.with;
                RedimHeight = Redim.height;
            }
            else
            {
                RedimWidth = -1;
                RedimHeight = -1;
            }

            NetVips.Enums.Interpretation interpretation = (NetVips.Enums.Interpretation)Enum.Parse(typeof(NetVips.Enums.Interpretation), (Combobox_color_conversion.SelectedItem as ComboBoxItem).Tag as string);

            double initialScale = GetScale().Scale;
            if (!double.TryParse(TextBox_Scale.Text, out double targetScale))
            {
                throw new FormatException("Impossible de convertir la valeur d'echelle en numéro (double)");
            };
            double DistanceInMeterPerPixels = ScaleInfo.GetDistanceInMeterPerPixels(targetScale);
            var DrawScaleInfo = ScaleInfo.SearchOptimalScale(DistanceInMeterPerPixels, RedimWidth);
            bool DrawScale = (CheckBoxAddScaleToImage.IsChecked ?? false) && IsEnoughPlaceForScale(RedimWidth, RedimHeight, DrawScaleInfo.Scale) && Layers.Curent.class_hasscale;

            ScaleInfo scaleInfo = new ScaleInfo(initialScale, targetScale, DistanceInMeterPerPixels, DrawScale, DrawScaleInfo.Scale, DrawScaleInfo.PixelLenght);

            var SelectionLocation = MainPage.mapSelectable.GetRectangleLocation();
            Download_Options download_Options = new Download_Options(0, save_directory, format, filename, "", "", 0, zoom, quality, "", SelectionLocation.NO, SelectionLocation.SE, RedimWidth, RedimHeight, interpretation, scaleInfo);
            MainWindow._instance.PrepareDownloadBeforeStart(download_Options);
            TextBoxScaleIsLock(false);
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

        private void Label_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void TextBox_Scale_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) { return; }
            if (!TextBox_Scale.IsFocused) { return; }
            Collectif.FilterDigitOnlyWhileWritingInTextBox(TextBox_Scale, null);
            if (!double.TryParse(TextBox_Scale.Text, out double CurrentTextBoxTargetScale)) { return; }
            SetTextBoxRedimDimensionTextBasedOnPourcentage(TextBox_Redim_Width, TextBox_Redim_WUnit, "width", GetScale().Scale / CurrentTextBoxTargetScale * 100);
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
