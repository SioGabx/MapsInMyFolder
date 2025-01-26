using MapsInMyFolder.Core.Downloader;
using MapsInMyFolder.Core.Generic.Extensions;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Layers
{


    public enum SavingFormat { png, jpeg, pbf }
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


        public string Path { get; set; }
        public string Identifier { get; set; }
        public bool IsFavorite
        {
            get { return isFavorite; }
            set { isFavorite = value; OnPropertyChanged(); }
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string Countries { get; set; }
        public string TileUrlSchema { get; set; }
        public string ProviderName { get; set; }
        public string ProviderUrl { get; set; }
        public int MinZoom { get; set; }
        public int MaxZoom { get; set; }
        public SavingFormat TileSavingFormat { get; set; }
        public string Style { get; set; }
        public string Script { get; set; }
        public Display Visibility
        {
            get { return visibility; }
            set { visibility = value; OnPropertyChanged(); }
        }
        private Display visibility;

        public int TileSize { get; set; }
        public string Version { get; set; }
        public string Area { get; set; }
        public SolidColorBrush BackColor { get; set; }
        public string UserAgent { get; set; }

        public bool IsAtScale { get; set; }

        public bool ShowTileBorder { get; set; }
        public bool ShowTileLocation { get; set; }

        public TilesImages Tiles { get; private set; }

        public Layer()
        {
            Path = "/";
            Identifier = "OpenStreetMap";
            Name = "OpenStreetMap";
            Description = string.Empty;
            Tags = string.Empty;
            TileSize = 256;
            Countries = "All";
            TileUrlSchema = "http://tile.openstreetmap.org/{z}/{x}/{y}.png";
            Script = string.Empty;
            Style = string.Empty;
            ProviderName = "OpenStreetMap";
            ProviderUrl = "openstreetmap.org";
            MinZoom = 0;
            MaxZoom = 19;
            TileSavingFormat = SavingFormat.jpeg;
            Visibility = Display.visible;
            IsFavorite = false;
            BackColor = "#E6E6E6".ConvertHexValueToSolidColorBrush();
            Tiles = TilesImages.Create(this);

            UserAgent = null;
        }
        public static Layer Default = new Layer();
        private bool isFavorite;

        public bool HasTransparency => TileSavingFormat == SavingFormat.png;
    }
}
