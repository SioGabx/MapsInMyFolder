using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using CefSharp;
using MapsInMyFolder.MapControl;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;
using System.Data.SQLite;
using System.Timers;
using System.Windows.Input;
using System.Threading.Tasks;
using MapsInMyFolder.Commun;
using System.Windows.Controls;
using System.Net.Http;

namespace MapsInMyFolder
{
    public partial class MainPage : System.Windows.Controls.Page
    {
        readonly Search_result_engine_class search_temp_engine_class = new Search_result_engine_class();

        public List<string> Search(string search)
        {
            try
            {
                List<string> return_list_of_adresse = new List<string>();
                string encoded_search = System.Web.HttpUtility.UrlEncode(search.Trim());
                String url = "https://nominatim.openstreetmap.org/search.php?q=" + encoded_search + "&polygon_geojson=1&limit=10&format=xml&email=siogabx@siogabx.fr";
                using (HttpClient client = new HttpClient())
                {
                    using HttpResponseMessage response = client.GetAsync(url).Result;
                    using Stream responseStream = response.Content.ReadAsStream();

                    response.EnsureSuccessStatusCode();
                    using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(responseStream))
                    {
                        search_temp_engine_class.Search_result_list.Clear();
                        reader.MoveToContent();
                        int id_search = 0;
                        while (reader.Read())
                        {
                            string adresse = reader.GetAttribute("display_name");
                            if (!string.IsNullOrEmpty(adresse))
                            {
                                string lat = reader.GetAttribute("lat");
                                string lon = reader.GetAttribute("lon");
                                string boundingbox = reader.GetAttribute("boundingbox");
                                return_list_of_adresse.Add(adresse);
                                search_temp_engine_class.Add(new Search_result_engine_class(id_search, adresse, lat, lon, boundingbox), id_search);
                                id_search++;
                            }
                        }
                    }
                }
                return return_list_of_adresse;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        private void Map_searchbar_GotFocus(object sender, RoutedEventArgs e)
        {
            if (map_searchbar.Text == "Rechercher un lieux...")
            {
                map_searchbar.Text = "";
            }
            SearchStart();
            map_searchbar_suggestion.Visibility = Visibility.Visible;
            map_searchbar_overflow.Visibility = Visibility.Hidden;
        }

        private void Map_searchbar_LostFocus(object sender, RoutedEventArgs e)
        {
            map_searchbar_suggestion.Visibility = Visibility.Hidden;
            map_searchbar_overflow.Visibility = Visibility.Visible;

            if (string.IsNullOrWhiteSpace(map_searchbar.Text))
            {
                search_result.Visibility = Visibility.Hidden;
                map_searchbar.Text = "Rechercher un lieux...";
                map_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
            }
        }

        readonly System.Timers.Timer Map_searchbar_timer = new System.Timers.Timer(500);
        private void Map_searchbar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (map_searchbar.Text != "Rechercher un lieux...")
            {
                search_result.Visibility = Visibility.Hidden;
                map_searchbar.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#BCBCBC");
            }
            if (string.IsNullOrWhiteSpace(map_searchbar.Text))
            {
                map_searchbar_suggestion.Height = 0;
                map_searchbar_suggestion.ItemsSource = new List<string>();
            }
            Map_searchbar_timer.Stop();
            Map_searchbar_timer.Elapsed += Map_searchbar_Timer_Elapsed_StartSearch;
            Map_searchbar_timer.AutoReset = false;
            Map_searchbar_timer.Enabled = true;
        }

        void Map_searchbar_Timer_Elapsed_StartSearch(object source, EventArgs e)
        {
            SearchStart();
        }

        string last_search = "";
        async void SearchStart(Boolean selectfirst = false)
        {
            string text = "";
            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (SendOrPostCallback)async delegate
            {
                text = map_searchbar.Text.Trim();
                if (text != "" && text != last_search)
                {
                    last_search = text;
                    List<string> list_of_search_result_from_task = new List<string>();
                    Task search_task = Task.Run(() => list_of_search_result_from_task = Search(text));
                    await search_task;
                    if (list_of_search_result_from_task.Count > 0)
                    {
                        map_searchbar_suggestion.Foreground = System.Windows.Media.Brushes.White;
                        map_searchbar_suggestion.ItemsSource = list_of_search_result_from_task;
                        map_searchbar_suggestion.Height = list_of_search_result_from_task.Count * 35;
                        if (selectfirst)
                        {
                            Set_selection(0);
                        }
                    }
                    else
                    {
                        map_searchbar_suggestion.Height = 35;
                        map_searchbar_suggestion.ItemsSource = new List<string> { "Aucun resultat" };
                        map_searchbar_suggestion.Foreground = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#5A5A5A");
                    }
                }
            }, null);
        }

        private void Map_searchbar_suggestion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = map_searchbar_suggestion.SelectedIndex;
            Set_selection(index);
        }

        void Set_selection(int index)
        {
            if (index >= 0)
            {
                Search_result_engine_class selected_search_engine = search_temp_engine_class.GetEngineById(index);
                if (selected_search_engine != null)
                {
                    map_searchbar.Text = selected_search_engine.display_name;
                    search_result.Visibility = Visibility.Visible;
                    MapPanel.SetLocation(search_result, new Location(Convert.ToDouble(selected_search_engine.lat), Convert.ToDouble(selected_search_engine.lon)));
                    if (!string.IsNullOrEmpty(selected_search_engine.boundingbox))
                    {
                        string[] boundingbox = selected_search_engine.boundingbox.Split(',');
                        mapviewer.ZoomToBounds(new BoundingBox(Convert.ToDouble(boundingbox[0]),
                                                               Convert.ToDouble(boundingbox[2]),
                                                               Convert.ToDouble(boundingbox[1]),
                                                               Convert.ToDouble(boundingbox[3])));
                        LayerTilePreview_RequestUpdate();
                    }
                }
            }
        }

        private void Map_searchbar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    last_search = "";
                    SearchStart(true);
                    layer_browser.Focus();
                }
                catch { }
            }
            if (e.Key == Key.Escape)
            {
                layer_browser.Focus();
            }
        }

        private void Search_result_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            last_search = "";
            SearchStart(true);
        }
    }

    public class Search_result_engine_class
    {
        public int id;
        public string display_name;
        public string lat;
        public string lon;
        public string boundingbox;
        public List<Dictionary<int, Search_result_engine_class>> Search_result_list = new List<Dictionary<int, Search_result_engine_class>>();

        public Search_result_engine_class(int id = -1, string display_name = "", string lat = "", string lon = "", string boundingbox = "")
        {
            if (id != -1)
            {
                this.id = id;
                this.display_name = display_name;
                this.lat = lat;
                this.lon = lon;
                this.boundingbox = boundingbox;
            }
        }

        public int Add(Search_result_engine_class engine, int id)
        {
            int number_of_engine_in_list = Search_result_list.Count + 1;
            Dictionary<int, Search_result_engine_class> temp_dictionnary = new Dictionary<int, Search_result_engine_class>
            {
                { id, engine }
            };
            Search_result_list.Add(temp_dictionnary);
            return number_of_engine_in_list;
        }

        public List<Search_result_engine_class> GetEngineList()
        {
            List<Search_result_engine_class> SearchList = new List<Search_result_engine_class>();

            foreach (Dictionary<int, Search_result_engine_class> search_dictionnary in Search_result_list)
            {
                Search_result_engine_class value = search_dictionnary.Values.First();
                SearchList.Add(value);
            }
            return SearchList;
        }

        public Search_result_engine_class GetEngineById(int id)
        {
            foreach (Dictionary<int, Search_result_engine_class> search_dictionnary in Search_result_list)
            {
                try
                {
                    if (search_dictionnary.Keys.First() == id)
                    {
                        Search_result_engine_class return_search_result_engine_class = search_dictionnary[id];
                        return return_search_result_engine_class;
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("Erreur : l'id n'existe pas.");
                }
            }
            return null;
        }
    }
}
