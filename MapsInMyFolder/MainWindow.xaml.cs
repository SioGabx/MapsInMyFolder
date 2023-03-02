using System;
using System.Windows;
using CefSharp;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using ModernWpf;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MapsInMyFolder.Commun;
using ModernWpf.Media.Animation;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using System.IO;

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
            if (Layers.Curent.class_tile_url is null)
            {
                Message.NoReturnBoxAsync("Une erreur s'est produite lors du chargement du calque.");
                return;
            }
            Popup_opening(false);
            PrepareDownloadPage.default_filename = Layers.Curent.class_name.Trim().Replace(" ", "_").ToLowerInvariant();
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
        public void Init()
        {
            this.Title = "MapsInMyFolder";
            TitleTextBox.Text = "MapsInMyFolder";
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", Settings.user_agent);
            TileGeneratorSettings.HttpClient.DefaultRequestHeaders.Add("User-Agent", Settings.user_agent);
            MainPage.mapviewer.AnimationDuration = TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond);
            Debug.WriteLine("Version dotnet :" + Environment.Version.ToString());
            Javascript JavascriptLocationInstance = Javascript.JavascriptInstance;
            JavascriptLocationInstance.LocationChanged += (o, e) => MainPage.MapViewerSetSelection(Javascript.JavascriptInstance.Location, Javascript.JavascriptInstance.ZoomToNewLocation);
            Network.IsNetworkNowAvailable += (o, e) => CheckIfReadyToStartDownloadAfterNetworkChange();
            Database.RefreshPanels += (o, e) => RefreshAllPanels();
            Javascript.JavascriptActionEvent += JavascriptActionEvent;
            Update.NewUpdateFoundEvent += (o, e) =>
            {
                try {
                    Application.Current.Dispatcher.Invoke(ApplicationUpdateFoundEvent); 
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            };  
            Database.NewUpdateFoundEvent += (o, e) =>
            {
                try { 
                    Application.Current.Dispatcher.Invoke(DatabaseUpdateFoundEvent); 
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            };
        }


        public void JavascriptActionEvent(object sender, Javascript.JavascriptAction javascriptAction)
        {
            switch (javascriptAction)
            {
                case Javascript.JavascriptAction.refreshMap:
                    MainPage.RefreshMap();
                    break;
                default:
                    Debug.WriteLine("Cette JSEvent action n'existe pas");
                    break;
            }
        }

        public void ApplicationUpdateFoundEvent()
        {
            Notification ApplicationUpdateNotification = new NText($"Une nouvelle version de l'application ({Update.UpdateRelease.Tag_name}) est disponible. Cliquez ici pour mettre à jour.", "MapsInMyFolder", Update.StartUpdating)
            {
                NotificationId = "ApplicationUpdateNotification",
                DisappearAfterAMoment = false,
                IsPersistant = true,
            };
            ApplicationUpdateNotification.Register();
        }

        public void DatabaseUpdateFoundEvent()
        {
            int ActualUserVersion = Database.ExecuteScalarSQLCommand("PRAGMA user_version");
            string Message;
            if (ActualUserVersion == -1)
            {
                //no database found
                return;
            }
            if (ActualUserVersion == 0)
            {
                Message = "Une base de données de calques est disponible en ligne. Voullez-vous la télécharger ?";
            }
            else
            {
                Message = "Une nouvelle version de la base de donnée est disponible. Cliquez ici pour mettre à jour.";
            }

            Notification DatabaseUpdateNotification = new NText(Message, "MapsInMyFolder", Database.StartUpdating)
            {
                NotificationId = "DatabaseUpdateNotification",
                DisappearAfterAMoment = false,
                IsPersistant = true,
            };
            DatabaseUpdateNotification.Register();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            IHighlightingDefinition customHighlighting;
            using (Stream s = Collectif.ReadResourceStream("ScriptEditorTheme.xshd"))
            {
                if (s == null)
                {
                    Collectif.GetAllManifestResourceNames();
                    throw new InvalidOperationException("Le theme de l'editeur de script n'as pas été trouvé");
                }

                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                        HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            // and register it in the HighlightingManager
            HighlightingManager.Instance.RegisterHighlighting("MIMF_JavaScript", new string[] { ".js" }, customHighlighting);

            if (Settings.search_application_update_on_startup && await Update.CheckIfNewerVersionAvailableOnGithub())
            {
                Debug.WriteLine("Une nouvelle mise à jour est disponible : Version " + Update.UpdateRelease.Tag_name);
            }

            if (Settings.search_database_update_on_startup)
            {
                Database.CheckIfNewerVersionAvailable();
            }
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


        public void Popup_closing()
        {
            AppTitleBar.Opacity = 1;
            DoubleAnimation hide_anim = new DoubleAnimation(0d, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond / 1.3))
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
                DoubleAnimation show_anim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond))
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
