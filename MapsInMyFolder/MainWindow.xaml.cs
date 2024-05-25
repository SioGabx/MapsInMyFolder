using ModernWpf;
using ModernWpf.Media.Animation;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MapsInMyFolder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainPage MainPage = new MainPage();

        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }


        public void FrameBack(bool NoTransition = false)
        {
            AppTitleBar.Opacity = 1;
            AppTitleBar.IsEnabled = true;
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


        public void Init()
        {
            Debug.WriteLine("Version dotnet :" + Environment.Version.ToString());

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }



        private void Window_ContentRendered(object sender, EventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void Map_panel_open_download_panel_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Map_panel_open_settings_panel_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MainContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e?.Uri != null)
            {
                Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
                e.Cancel = true;
            }
        }
    }
}
