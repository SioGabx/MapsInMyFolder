// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Core.Layers;

namespace MapsInMyFolder.View.Controls.Map
{
    /// <summary>
    /// Provides the download Uri or ImageSource of map tiles.
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(TileSourceConverter))]
    public class TileSource
    {
        public Layer Layer { get; set; }
        public bool ShowBorders { get; set; }
    }
}
