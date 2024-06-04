using MapsInMyFolder.Core.Downloader;
using MapsInMyFolder.Properties;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;

namespace MapsInMyFolder.Core.Layers
{


    public enum Format { png, jpeg, pbf }
    public enum Display { visible, hidden }
    public partial class Layer : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int LayerId { get; set; }
        public string Identifier { get; set; }
        public bool IsFavorite
        {
            get { return isFavorite; }
            set { isFavorite = value; OnPropertyChanged(); }
        }
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
        public int Version { get; set; }
        public string Area { get; set; }
        public Color BackColor { get; set; }
        public string UserAgent { get; set; }

        public bool ShowTileBorder { get; set; }
        public bool ShowTileLocation { get; set; }

        public TilesImages Tiles { get; private set; }

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
            IsFavorite = false;
            BackColor = Color.FromRgb(230, 230, 230); //#E6E6E6
            Tiles = TilesImages.Create(this);

            UserAgent = null;
        }
        public static Layer Default = new Layer();
        private bool isFavorite;

        public bool HasTransparency => TilesFormat == Format.png;


    }
}
