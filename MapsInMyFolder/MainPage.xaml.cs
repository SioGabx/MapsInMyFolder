﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2211:Les champs non constants ne doivent pas être visibles", Justification = "for access everywhere")]
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
            DrawRectangleCelectionArroundPushpin();
            UpdatePushpinPositionAndDrawRectangle();
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
