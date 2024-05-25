using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapsInMyFolder.View.Modules
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

        string last_input;
        public string SearchGetText()
        {
            string searchText = null;
            if (layer_searchbar.Text != "searchLayerPlaceholder")
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
                    if ((last_input != SearchValue || IsIgnoringLastInput) && SearchValue != null)
                    {
                        last_input = SearchValue;
                        Debug.WriteLine("Search: " + SearchValue);
                    }
                }));
            });
        }


        private void Layer_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (layer_searchbar.Text == "searchLayerPlaceholder")
            {
                layer_searchbar.Text = "";
                layer_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
        }

        private string lastSearch = "searchLayerPlaceholder";
        private void Layer_searchbar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    lastSearch = "";
                    SearchLayerStart();
                }
                catch { }
            }
            if (e.Key == Key.Escape)
            {
            }
        }

        private void Layer_searchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(layer_searchbar.Text))
            {
                layer_searchbar.Text = "searchLayerPlaceholder";
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
