using CefSharp;
using CefSharp.Wpf;
using MapsInMyFolder.Commun;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace MapsInMyFolder.UserControls
{
    /// <summary>
    /// Logique d'interaction pour SearchLayer.xaml
    /// </summary>
    public partial class SearchLayer : UserControl
    {
        public SearchLayer()
        {
            InitializeComponent();
        }

        // Dependency property to control the visibility of the button
        public static readonly DependencyProperty LinkedLayerBrowserProperty =
            DependencyProperty.Register(
                "LinkedLayerBrowser",
                typeof(ChromiumWebBrowser),
                typeof(SearchLayer),
                new PropertyMetadata(null));

        public ChromiumWebBrowser LinkedLayerBrowser
        {
            get { return (ChromiumWebBrowser)GetValue(LinkedLayerBrowserProperty); }
            set { SetValue(LinkedLayerBrowserProperty, value); }
        }




        string last_input;
        public string SearchGetText()
        {
            string searchText = null;
            if (layer_searchbar.Text != Languages.Current["searchLayerPlaceholder"])
            {
                searchText = layer_searchbar.Text.Replace("'", "’").Trim();
            }
            return searchText;
        }

        public async void SearchLayerStart(bool IsIgnoringLastInput = false)
        {
            await Task.Run(async () =>
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    string SearchValue = SearchGetText();
                    if ((last_input != SearchValue || IsIgnoringLastInput) && SearchValue != null && LinkedLayerBrowser?.IsInitialized == true)
                    {
                        last_input = SearchValue;
                        Debug.WriteLine("Search: " + SearchValue);
                        LinkedLayerBrowser?.ExecuteScriptAsync("searchAndUpdatePreview", SearchValue);
                    }
                }));
            });
        }


        private void Layer_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (layer_searchbar.Text == Languages.Current["searchLayerPlaceholder"])
            {
                layer_searchbar.Text = "";
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private string lastSearch = Languages.Current["searchLayerPlaceholder"];
        private void Layer_searchbar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    lastSearch = "";
                    SearchLayerStart();
                    LinkedLayerBrowser.Focus();
                }
                catch { }
            }
            if (e.Key == Key.Escape)
            {
                LinkedLayerBrowser.Focus();
            }
        }

        private void Layer_searchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(layer_searchbar.Text))
            {
                layer_searchbar.Text = Languages.Current["searchLayerPlaceholder"];
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
            else
            {
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private void Layer_searchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchLayerStart();
        }
    }
}
