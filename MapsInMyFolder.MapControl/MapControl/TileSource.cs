﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Commun;
using System;
using System.Threading.Tasks;

using System.Windows.Media;

namespace MapsInMyFolder.MapControl
{
    /// <summary>
    /// Provides the download Uri or ImageSource of map tiles.
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(TileSourceConverter))]
    public class TileSource
    {
        private string uriFormat;

        /// <summary>
        /// Gets or sets the format string to produce tile request Uris.
        /// </summary>
        public string UriFormat
        {
            get { return uriFormat; }
            set
            {
                uriFormat = value?.Replace("{c}", "{s}"); // for backwards compatibility since 5.4.0

                if (Subdomains == null && uriFormat != null && uriFormat.Contains("{s}"))
                {
                    Subdomains = new string[] { "a", "b", "c" }; // default OpenStreetMap subdomains
                }
            }
        }

        /// <summary>
        /// Gets or sets an array of request subdomain names that are replaced for the {s} format specifier.
        /// </summary>
        public string[] Subdomains { get; set; }
        public int LayerID { get; set; }

        /// <summary>
        /// Gets the image Uri for the specified tile indices and zoom level.
        /// </summary>
        public virtual Uri GetUri(int x, int y, int zoomLevel)
        {
            Uri uri = null;

            if (UriFormat != null)
            {
                var uriString = Collectif.GetUrl.FromTileXYZ(UriFormat, x, y, zoomLevel, LayerID, Javascript.InvokeFunction.getTile);

                if (Subdomains != null && Subdomains.Length > 0)
                {
                    uriString = uriString.Replace("{s}", Subdomains[(x + y) % Subdomains.Length]);
                }

                uri = new Uri(uriString, UriKind.RelativeOrAbsolute);
            }

            return uri;
        }

        /// <summary>
        /// Loads a tile ImageSource asynchronously from GetUri(x, y, zoomLevel).
        /// </summary>
        public virtual Task<ImageSource> LoadImageAsync(int x, int y, int zoomLevel, TileSource tileSource)
        {
            var uri = GetUri(x, y, zoomLevel);
            if (uri is null)
            {
                return Task.FromResult((ImageSource)null);
            }
            else
            {
                return ImageLoader.LoadImageAsync(uri, x, y, zoomLevel, tileSource);
            }
            //return uri != null ? ImageLoader.LoadImageAsync(uri, x,y, zoomLevel, tileSource) : Task.FromResult((ImageSource)null);
        }
    }

    public class TmsTileSource : TileSource
    {
        public override Uri GetUri(int x, int y, int zoomLevel)
        {
            return base.GetUri(x, (1 << zoomLevel) - 1 - y, zoomLevel);
        }
    }
}
