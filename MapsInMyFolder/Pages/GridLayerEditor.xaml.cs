using CefSharp;
using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MapsInMyFolder
{
    /// <summary>
    /// Logique d'interaction pour GridLayerEditor.xaml
    /// </summary>
    public partial class GridLayerEditor : Page
    {
        public GridLayerEditor()
        {
            InitializeComponent();
        }

        private static ObservableCollection<Layers> _items;

        private void GetData()
        {
            _items = new ObservableCollection<Layers>();

            foreach (Layers layers in Layers.GetLayersList())
            {
                _items.Add(layers);
            }
            LayerGrid.ItemsSource = _items;
        }

        private void LayerGrid_Loaded(object sender, RoutedEventArgs e)
        {
            GetData();
            MapFigures = new MapFigures();
        }

        private void Layer_browser_LoadingStateChanged(object sender, CefSharp.LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                //layer_browser.GetMainFrame().EvaluateScriptAsync(LayerGetDefaultSelectByIdScript());
            }
        }
        private static MapFigures MapFigures;
        private void Mapviewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MapFigures.UpdateFiguresFromZoomLevel(mapviewer.TargetZoomLevel);
            AnimateLabel(ZoomLevelIndicator);
        }

        private bool isAnimating = false; // Indique si l'animation est en cours d'exécution
        private void AnimateLabel(Label label)
        {
            label.Visibility = Visibility.Visible;
            // Si l'animation est déjà en cours d'exécution, annule l'animation de disparition en cours et recommence l'animation d'apparition à la valeur d'opacité actuelle
            DoubleAnimation fadeInAnimation = new DoubleAnimation();

            if (isAnimating)
            {
                label.BeginAnimation(OpacityProperty, null);
                fadeInAnimation.From = label.Opacity;
                fadeInAnimation.To = 1.0;
            }
            else
            {
                fadeInAnimation.From = 0.0;
                fadeInAnimation.To = 1.0;
            }

            TimeSpan fadeInDuration = TimeSpan.FromSeconds(0.2);
            TimeSpan fadeOutDuration = TimeSpan.FromSeconds(0.5);

            fadeInAnimation.Duration = fadeInDuration;
            Storyboard storyboard = new Storyboard();
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = fadeOutDuration,

                // Démarre après l'animation d'apparition + 1 seconde
                BeginTime = fadeInDuration + TimeSpan.FromSeconds(0.5)
            };

            storyboard.Children.Add(fadeInAnimation);
            storyboard.Children.Add(fadeOutAnimation);
            Storyboard.SetTarget(fadeInAnimation, label);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
            Storyboard.SetTarget(fadeOutAnimation, label);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(OpacityProperty));

            void AnimationCompleted(object sender, EventArgs e)
            {
                isAnimating = false;
                fadeOutAnimation.Completed -= AnimationCompleted;
            }

            fadeOutAnimation.Completed += AnimationCompleted;

            storyboard.Begin();
            isAnimating = true;
        }

        public void LayerTilePreview_RequestUpdate()
        {
            var bbox = mapviewer.ViewRectToBoundingBox(new Rect(0, 0, mapviewer.ActualWidth, mapviewer.ActualHeight));
            Commun.Map.CurentView.NO_Latitude = bbox.North;
            Commun.Map.CurentView.NO_Longitude = bbox.West;
            Commun.Map.CurentView.SE_Latitude = bbox.South;
            Commun.Map.CurentView.SE_Longitude = bbox.East;

            layer_browser.ExecuteScriptAsyncWhenPageLoaded("UpdatePreview();");
            return;
        }

        private void mapLocationSearchBar_SearchLostFocusRequest(object sender, EventArgs e)
        {
            layer_browser.Focus();
        }

        private async void mapLocationSearchBar_SearchResultEvent(object sender, UserControls.SearchLocation.SearchResultEventArgs e)
        {
            MapPanel.SetLocation(searchResult, e.SearchResultLocation);
            if (e.MapViewerBoundingBox != null)
            {
                mapviewer.ZoomToBounds(e.MapViewerBoundingBox);
            }
            await Task.Delay((int)Settings.animations_duration_millisecond);
            LayerTilePreview_RequestUpdate();
        }
    }
}
