// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Core.Layers;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;

namespace MapsInMyFolder.View.Controls.Map
{
    /// <summary>
    /// MapBase with default input event handling.
    /// </summary>
    public class Map : MapBase
    {
        public static readonly DependencyProperty MouseWheelZoomDeltaProperty = DependencyProperty.Register(
            nameof(MouseWheelZoomDelta), typeof(double), typeof(Map), new PropertyMetadata(1d));

        public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.Register(
            nameof(ManipulationMode), typeof(ManipulationModes), typeof(Map), new PropertyMetadata(ManipulationModes.All));

        private Point? mousePosition;

        static Map()
        {
            IsManipulationEnabledProperty.OverrideMetadata(typeof(Map), new FrameworkPropertyMetadata(true));
        }

        public Map()
        {
            ManipulationStarted += OnManipulationStarted;
            ManipulationDelta += OnManipulationDelta;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseMove += OnMouseMove;
            MouseWheel += OnMouseWheel;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
        }
        
        public void SetMapLayer(Layer FrontLayer, Layer BackLayer)
        {
            Debug.WriteLine("SetMapLayer");
            foreach (var item in Children)
            {
                Debug.WriteLine(item.GetType());
                if (item is MapTileLayer Mtl && Mtl.LayerIndex > 0)
                {
                    if (FrontLayer.HasTransparency)
                    {
                        Mtl.TileSource = new TileSource() { Layer = FrontLayer };
                    }
                    else
                    {
                        Mtl.TileSource = new TileSource();
                    }

                    Debug.WriteLine("--");
                    break;
                }
            }
            if (FrontLayer.HasTransparency && !(MapLayer is MapTileLayer MlIsMtl && MlIsMtl.TileSource.Layer == BackLayer))
            {
                MapLayer = new MapTileLayer()
                {
                    LayerIndex = 0,
                    TileSource = new TileSource() { Layer = BackLayer },
                    SourceName = BackLayer.Identifier + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    MaxZoomLevel = BackLayer.MaxZoom,
                    MinZoomLevel = BackLayer.MinZoom,
                    Description = BackLayer.Description
                };
            }
            else if (!FrontLayer.HasTransparency && !(MapLayer is MapTileLayer MlIsMtl2 && MlIsMtl2.TileSource.Layer == FrontLayer))
            {
                MapLayer = new MapTileLayer()
                {
                    LayerIndex = 0,
                    TileSource = new TileSource() { Layer = FrontLayer },
                    SourceName = FrontLayer.Identifier + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                    MaxZoomLevel = FrontLayer.MaxZoom,
                    MinZoomLevel = FrontLayer.MinZoom,
                    Description = FrontLayer.Description
                };
            }

        }



        /// <summary>
        /// Gets or sets the amount by which the ZoomLevel property changes during a MouseWheel event.
        /// The default value is 1.
        /// </summary>
        public double MouseWheelZoomDelta
        {
            get { return (double)GetValue(MouseWheelZoomDeltaProperty); }
            set { SetValue(MouseWheelZoomDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that specifies how the map control handles manipulations.
        /// </summary>
        public ManipulationModes ManipulationMode
        {
            get { return (ManipulationModes)GetValue(ManipulationModeProperty); }
            set { SetValue(ManipulationModeProperty, value); }
        }

        private void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            Manipulation.SetManipulationMode(this, ManipulationMode);
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            TransformMap(e.ManipulationOrigin,
                e.DeltaManipulation.Translation,
                e.DeltaManipulation.Rotation,
                e.DeltaManipulation.Scale.LengthSquared / 2d);
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                mousePosition = e.GetPosition(this);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mousePosition.HasValue)
            {
                mousePosition = null;
                ReleaseMouseCapture();
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if (CaptureMouse())
                {
                    mousePosition = e.GetPosition(this);
                }
            }
        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
            {
                if (mousePosition.HasValue)
                {
                    mousePosition = null;
                    ReleaseMouseCapture();
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (mousePosition.HasValue)
            {
                var position = e.GetPosition(this);
                var translation = position - mousePosition.Value;
                mousePosition = position;

                TranslateMap(translation);
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoomLevel = TargetZoomLevel + (MouseWheelZoomDelta * Math.Sign(e.Delta));

            ZoomMap(e.GetPosition(this), MouseWheelZoomDelta * Math.Round(zoomLevel / MouseWheelZoomDelta));
        }
    }
}
