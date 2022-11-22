using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {

        public static MainPage _instance;
        public MainPage()
        {
            _instance = this;
            InitializeComponent();
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isInitialised)
            {
                Preload();
                Init();
            }
        }

        bool isInitialised = false;
       public void Preload()
        {
            ReloadPage();
            MapLoad();
            Draw_rectangle_selection_arround_pushpin();
            Pushpin_stop_mooving();
        }
        void Init()
        {
           
            Init_download_panel();
            Init_layer_panel();
            isInitialised = true;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
            
        }


        private void Download_panel_close_button_Click(object sender, RoutedEventArgs e)
        {
            Download_panel_close();            
        }



        private void Layer_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (layer_searchbar.Text == "Rechercher un calque, un site...")
            {
                layer_searchbar.Text = "";
            }
            //SearchStart();
        }

        private void Layer_searchbar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    last_search = "";
                    SearchLayerStart();
                    layer_browser.Focus();
                }
                catch { }
            }
            if (e.Key == Key.Escape)
            {
                layer_browser.Focus();
            }
        }

        private void Layer_searchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(layer_searchbar.Text))
            {
                layer_searchbar.Text = "Rechercher un calque, un site...";
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
            else
            {
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }


        private void Layer_searchbar_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SearchLayerStart();
        }

       
    }
}
