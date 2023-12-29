using CefSharp;
using ICSharpCode.AvalonEdit.Highlighting;
using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using ModernWpf;
using ModernWpf.Media.Animation;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Xml;

namespace MapsInMyFolder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; set; }
        private static bool FrameCanGoBack = false;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            MainContentFrame.Navigate(MainPage);
            Tiles.handler.ServerCertificateCustomValidationCallback += (_, _, _, _) => true;
        }

        public MainPage MainPage = new MainPage();

        public void FrameBack(bool NoTransition = false)
        {
            AppTitleBar.Opacity = 1;
            AppTitleBar.IsEnabled = true;
            if (!MainContentFrame.CanGoBack) { return; }
            FrameCanGoBack = true;
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
            if (Layers.Current.TileUrl is null)
            {
                Message.NoReturnBoxAsync("An error occurred while loading the layer : Tile URL is not defined");
                return;
            }
            Popup_opening(false);
            PrepareDownloadPage PrepareDownloadPage = new PrepareDownloadPage
            {
                defaultFilename = Layers.Current.Name.Trim().Replace(" ", "_").ToLowerInvariant()
            };
            PrepareDownloadPage.Init();
            MainContentFrame.Navigate(PrepareDownloadPage);
        }

        public void FrameLoad_CustomOrEditLayers(int Layerid, CustomOrEditLayersPage.EditingMode EditMode)
        {
            Popup_opening(false);
            CustomOrEditLayersPage CustomOrEditLayersPage = new CustomOrEditLayersPage
            {
                LayerId = Layerid,
                EditMode = EditMode
            };
            CustomOrEditLayersPage.Init_CustomOrEditLayersWindow();

            MainContentFrame.Navigate(CustomOrEditLayersPage);
            Tiles.AcceptJavascriptPrint = true;
        }

        public void Init()
        {
            TitleTextBox.Text = this.Title = "MapsInMyFolder";
            LightInit();
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            Debug.WriteLine("Version dotnet :" + Environment.Version.ToString());
            Javascript JavascriptLocationInstance = Javascript.instance;
            JavascriptLocationInstance.LocationChanged += (o, e) => MainPage.MapViewerSetSelection(Javascript.instance.Location, Javascript.instance.ZoomToNewLocation);
            Network.IsNetworkNowAvailable += (o, e) => NetworkIsBack();
            Database.RefreshPanels += (o, e) => RefreshAllPanels();
            Javascript.JavascriptActionEvent += JavascriptActionEvent;
            Update.NewUpdateFoundEvent += (o, e) =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(ApplicationUpdateFoundEvent);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            };

            Database.NewUpdateFoundEvent += (_, _) =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(DatabaseUpdateFoundEvent);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            };
        }

        public void LightInit()
        {
            ImageLoader.HttpClient.DefaultRequestHeaders.AddChangeIfExist("User-Agent", Settings.user_agent);
            Tiles.HttpClient.DefaultRequestHeaders.AddChangeIfExist("User-Agent", Settings.user_agent);
            MainPage.mapviewer.AnimationDuration = TimeSpan.FromMilliseconds(Settings.animations_duration_millisecond);
        }

        public void JavascriptActionEvent(object sender, Javascript.JavascriptAction javascriptAction)
        {
            switch (javascriptAction)
            {
                case Javascript.JavascriptAction.refreshMap:
                    MainPage.RefreshMap();
                    break;
                case Javascript.JavascriptAction.clearCache:
                    Layers.ClearCache((int)(sender), false);
                    break;
                default:
                    Debug.WriteLine("Cette JSEvent action n'existe pas");
                    break;
            }
        }

        public static void ApplicationUpdateFoundEvent()
        {
            Notification ApplicationUpdateNotification = new NText(Languages.GetWithArguments("updateMessageNewVersionAvailable", Update.UpdateRelease.Tag_name) + " " + Languages.Current["updateMessageNewVersionClickToUpdate"], "MapsInMyFolder", "MainPage", Update.StartUpdating)
            {
                NotificationId = "ApplicationUpdateNotification",
                DisappearAfterAMoment = false,
                IsPersistant = true,
            };
            ApplicationUpdateNotification.Register();
        }

        public static void DatabaseUpdateFoundEvent()
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
                Message = Languages.Current["databaseMessageAvailableOnline"];
            }
            else
            {
                Message = Languages.Current["databaseMessageNewAvailableOnline"];
            }

            Notification DatabaseUpdateNotification = new NText(Message, "MapsInMyFolder", "MainPage", Database.StartUpdating)
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
            LoadAvalonEditThemes();
            if (Settings.search_application_update_on_startup && await Update.CheckIfNewerVersionAvailableOnGithub())
            {
                Debug.WriteLine("Une nouvelle mise à jour est disponible : Version " + Update.UpdateRelease.Tag_name);
            }

            if (Settings.search_database_update_on_startup)
            {
                await Database.CheckIfNewerVersionAvailable();
            }
        }

        public void NetworkIsBack()
        {
            MainPage.RefreshMap();
            MainPage.SetBBOXPreviewRequestUpdate();
            Downloader.CheckIfReadyToStartDownload();
        }

        public static void LoadAvalonEditThemes()
        {
            static void LoadHighlighting(string Name, string FileName, string StyleExtension)
            {
                using (Stream s = Collectif.ReadResourceStream(@"Commun\Styles\EditorTheme\" + FileName))
                {
                    if (s == null)
                    {
                        Collectif.GetAllManifestResourceNames();
                        throw new InvalidOperationException("Le theme de l'editeur de script n'as pas été trouvé");
                    }

                    using (XmlReader reader = new XmlTextReader(s))
                    {
                        IHighlightingDefinition customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        HighlightingManager.Instance.RegisterHighlighting(Name, new string[] { StyleExtension }, customHighlighting);
                    }
                }
            }
            // and register it in the HighlightingManager

            LoadHighlighting("MIMF_JavaScript", "Javascript.xshd", ".js");
            LoadHighlighting("MIMF_Json", "Json.xshd", ".json");
        }

        public static void RefreshAllPanels()
        {
            Instance.LightInit();
            Instance.MainPage.MapLoad();
            Instance.MainPage.LayerPanel.Init();
            Instance.MainPage.InitDownloadPanel();
            Instance.MainPage.SetCurrentLayer(Layers.Current.Id);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            MainPage.SetCurrentLayer(Layers.StartupLayerId);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
            Javascript.EngineStopAll();
            Cef.Shutdown();
        }

        public void Popup_opening(bool ReduceOpacity = true)
        {
            AppTitleBar.Opacity = 0.5;
            AppTitleBar.IsEnabled = false;
            MainPage.DownloadPanelClose();
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
                MainPage.DownloadPanelClose();
            }
            if (e.Key == Key.Down)
            {
                GridLayerEditor editor = new GridLayerEditor();
                MainContentFrame.Navigate(editor);
            }
        }

        private void Map_panel_open_download_panel_Click(object sender, RoutedEventArgs e)
        {
            MainPage.DownloadPanelOpen();
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

        private void MainContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back && !FrameCanGoBack)
            {
                e.Cancel = true;
            }
            if (e.NavigationMode == NavigationMode.New && e?.Uri != null)
            {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
                e.Cancel = true;
            }
            FrameCanGoBack = false;
        }
    }
}
