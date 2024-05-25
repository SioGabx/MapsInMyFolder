using MapsInMyFolder.Core.Downloader;
using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Navigation;

namespace MapsInMyFolder.Core.Layers
{


    public enum Format { png, jpeg }
    public enum Display { visible, hidden }
    public partial class Layer : IDisposable
    {
        public int LayerId { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string Country { get; set; }
        public string TileUrl { get; set; }
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }
        public int MinZoom { get; set; }
        public int MaxZoom { get; set; }
        public Format TilesFormat { get; set; }
        public string Style { get; set; }
        public string Script { get; set; }
        public Display Visibility { get; set; }
        public int TileSize { get; set; }
        public string UserAgent { get; set; }

        public bool ShowTileBorder { get; set; }
        public bool ShowTileLocation { get; set; }

        public Client Client { get; private set; }

        public Layer()
        {
            LayerId = 0;
            Identifier = "OpenStreetMap";
            Name = "OpenStreetMap";
            Description = string.Empty;
            Tags = string.Empty;
            TileSize = 256;
            Country = "All";
            TileUrl = "http://tile.openstreetmap.org/{z}/{x}/{y}.png";
            Script = string.Empty;
            Style = string.Empty;
            SiteName = "OpenStreetMap";
            SiteUrl = "openstreetmap.org";
            MinZoom = 0;
            MaxZoom = 19;
            TilesFormat = Format.jpeg;
            Visibility = Display.visible;

            UserAgent = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

            Client = new Client(this);
        }
        public static Layer Default = new Layer()
        {


        };

        public bool HasTransparency => TilesFormat == Format.png;
    }
}
