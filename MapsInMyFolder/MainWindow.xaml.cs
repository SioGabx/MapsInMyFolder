using System;
using System.IO;
using System.Windows;
using CefSharp;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using ModernWpf;
using System.Data.SQLite;
using CefSharp.Wpf;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Threading.Tasks;
using ModernWpf.Controls;
//using Windows.Foundation.Metadata;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Threading;
using System.Threading;
using System.Reflection;
using MapsInMyFolder.Commun;
using ModernWpf.Media.Animation;

namespace MapsInMyFolder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow _instance;
        public MainWindow()
        {
            _instance = this;
            InitializeComponent();
            MainContentFrame.Navigate(MainPage);
        }

        public MainPage MainPage = new MainPage();
        public PrepareDownloadPage PrepareDownloadPage = new PrepareDownloadPage();
        public CustomOrEditLayersPage CustomOrEditLayersPage;


        public void FrameBack(bool NoTransition = false)
        {

            
            AppTitleBar.Opacity = 1;
            if (CustomOrEditLayersPage is not null)
            {
                CustomOrEditLayersPage = null;
            }
            if (NoTransition)
            {
                MainContentFrame.GoBack(new SuppressNavigationTransitionInfo());
            }
            else
            {
                MainContentFrame.GoBack();
            }
        }

        public void FrameLoad_PrepareDownload()
        {
            if (Curent.Layer.class_tile_url is null)
            {
                Message.NoReturnBoxAsync("Une erreur s'est produite lors du chargement du calque.");
                return;
            }
            Popup_opening(false);
            PrepareDownloadPage.default_filename = Curent.Layer.class_display_name.Trim().Replace(" ", "_").ToLowerInvariant();
            PrepareDownloadPage.Init();
            MainContentFrame.Navigate(PrepareDownloadPage);
        }
        public void FrameLoad_CustomOrEditLayers(int Layerid)
        {
            Popup_opening(false);
            CustomOrEditLayersPage CustomOrEditLayersPage = new CustomOrEditLayersPage
            {
                LayerId = Layerid
            };
            CustomOrEditLayersPage.Init_CustomOrEditLayersWindow();
           
                MainContentFrame.Navigate(CustomOrEditLayersPage);
            Commun.TileGeneratorSettings.AcceptJavascriptPrint = true;
        }

        //SQLiteConnection global_conn;
        public void Init()
        {
            this.Title = "MapsInMyFolder";
            TitleTextBox.Text = "MapsInMyFolder";
            //Settings.SaveSettings();
            //Settings.LoadSettingsAsync();
            Commun.Settings.LoadSettingsAsync();
            Database.DB_Download();
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", Commun.Settings.user_agent);
            TileGeneratorSettings.HttpClient.DefaultRequestHeaders.Add("User-Agent", Commun.Settings.user_agent);
            MainPage.mapviewer.AnimationDuration = Settings.animations_duration;
            Debug.WriteLine("Version dotnet :" + Environment.Version.ToString());
            Javascript JavascriptLocationInstance = Javascript.JavascriptInstance;
            JavascriptLocationInstance.LocationChanged += (o, e) => MainPage.MapViewerSetSelection(Javascript.JavascriptInstance.Location, Javascript.JavascriptInstance.ZoomToNewLocation);
        }


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            MainPage.Set_current_layer(Commun.Settings.layer_startup_id);
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            MainPage.layer_browser.Dispose();
            MainPage.download_panel_browser.Dispose();
            Javascript.EngineStopAll();
            Cef.Shutdown();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }



        public void Popup_closing()
        {
            AppTitleBar.Opacity = 1;
            DoubleAnimation hide_anim = new DoubleAnimation(0d, Commun.Settings.animations_duration / 1.3)
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
            hide_anim.Completed += (s, e) =>
            {
                MainPage.popup_background.Visibility = Visibility.Hidden;
            };
            MainPage.popup_background.BeginAnimation(UIElement.OpacityProperty, hide_anim);

        }

        public void Popup_opening(bool ReduceOpacity = true)
        {
            AppTitleBar.Opacity = 0.5;
            MainPage.Download_panel_close();
            if (ReduceOpacity)
            {
                MainPage.popup_background.Visibility = Visibility.Visible;
                DoubleAnimation show_anim = new DoubleAnimation(1, Commun.Settings.animations_duration * 1)
                {
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
                };
                MainPage.popup_background.BeginAnimation(UIElement.OpacityProperty, show_anim);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                MainPage.Download_panel_close();
            }
        }


        private void Map_panel_open_download_panel_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Download_panel_open();
        }

        private void Map_panel_open_settings_panel_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(Collectif.GetUrl.numberofurlgenere);
        }



    }
}
