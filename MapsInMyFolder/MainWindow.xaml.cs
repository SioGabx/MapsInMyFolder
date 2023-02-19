﻿using System;
using System.Windows;
using CefSharp;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using ModernWpf;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MapsInMyFolder.Commun;
using ModernWpf.Media.Animation;

namespace MapsInMyFolder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Les champs non constants ne doivent pas être visibles", Justification = "for access everywhere")]
        public static MainWindow _instance;
        public MainWindow()
        {
            _instance = this;
            InitializeComponent();
            MainContentFrame.Navigate(MainPage);
            TileGeneratorSettings.handler.ServerCertificateCustomValidationCallback += (_, _, _, _) => true;
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
            if (!MainContentFrame.CanGoBack) { return; }
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
            PrepareDownloadPage.default_filename = Curent.Layer.class_name.Trim().Replace(" ", "_").ToLowerInvariant();
            PrepareDownloadPage.Init();
            MainContentFrame.Navigate(PrepareDownloadPage);
        }
        public void FrameLoad_CustomOrEditLayers(int Layerid, int prefilLayerId = -1)
        {
            Popup_opening(false);
            CustomOrEditLayersPage CustomOrEditLayersPage = new CustomOrEditLayersPage
            {
                LayerId = Layerid
            };
            CustomOrEditLayersPage.Init_CustomOrEditLayersWindow(prefilLayerId);

            MainContentFrame.Navigate(CustomOrEditLayersPage);
            TileGeneratorSettings.AcceptJavascriptPrint = true;
        }

        //SQLiteConnection global_conn;
        public async void Init()
        {
            this.Title = "MapsInMyFolder";
            TitleTextBox.Text = "MapsInMyFolder";
            //Settings.SaveSettings();
            //Settings.LoadSettingsAsync();
            Database.DB_Download();
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", Settings.user_agent);
            TileGeneratorSettings.HttpClient.DefaultRequestHeaders.Add("User-Agent", Settings.user_agent);
            MainPage.mapviewer.AnimationDuration = Settings.animations_duration;
            Debug.WriteLine("Version dotnet :" + Environment.Version.ToString());
            Javascript JavascriptLocationInstance = Javascript.JavascriptInstance;
            JavascriptLocationInstance.LocationChanged += (o, e) => MainPage.MapViewerSetSelection(Javascript.JavascriptInstance.Location, Javascript.JavascriptInstance.ZoomToNewLocation);
            Network.IsNetworkNowAvailable += (o, e) => CheckIfReadyToStartDownloadAfterNetworkChange();
            Database.RefreshPanels += (o, e) => RefreshAllPanels();
            Update.NewUpdateFoundEvent += (o, e) => Application.Current.Dispatcher.Invoke(NewUpdateFoundEvent);


            if (await Update.CheckIfNewerVersionAvailableOnGithub())
            {
                Debug.WriteLine("Une nouvelle mise à jour est disponible : Version " + Update.UpdateRelease.Tag_name);
            }
        }

        public void NewUpdateFoundEvent()
        {
            Action Callback = () => Update.StartUpdating();
            MainPage.CreateNotification("MapsInMyFolder", $"Une nouvelle version de l'application ({Update.UpdateRelease.Tag_name}) est disponible. Cliquez ici pour mettre à jour.", Callback);
        }


        public static void RefreshAllPanels()
        {
            _instance.Init();
            _instance.MainPage.MapLoad();
            _instance.MainPage.Init_layer_panel();
            _instance.MainPage.ReloadPage();
            _instance.MainPage.SearchLayerStart();
            _instance.MainPage.Init_download_panel();
            _instance.MainPage.Set_current_layer(Settings.layer_startup_id);
        }


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            MainPage.Set_current_layer(Settings.layer_startup_id);
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
            DoubleAnimation hide_anim = new DoubleAnimation(0d, Settings.animations_duration / 1.3)
            {
                EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
            };
            hide_anim.Completed += (s, e) => MainPage.popup_background.Visibility = Visibility.Hidden;
            MainPage.popup_background.BeginAnimation(OpacityProperty, hide_anim);
        }

        public void Popup_opening(bool ReduceOpacity = true)
        {
            AppTitleBar.Opacity = 0.5;
            MainPage.Download_panel_close();
            if (ReduceOpacity)
            {
                MainPage.popup_background.Visibility = Visibility.Visible;
                DoubleAnimation show_anim = new DoubleAnimation(1, Settings.animations_duration * 1)
                {
                    EasingFunction = new PowerEase { EasingMode = EasingMode.EaseInOut }
                };
                MainPage.popup_background.BeginAnimation(OpacityProperty, show_anim);
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
            SettingsWindow settingsModalWindow = new SettingsWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            settingsModalWindow.ShowDialog();
            settingsModalWindow.Focus();
        }
    }
}
