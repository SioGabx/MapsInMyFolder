using MapsInMyFolder.Core.Generic.Extensions;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Layers
{
    public static class LayerExtensions
    {
        public static ImageSource GetTile(this Layer Layer, int X, int Y, int Z)
        {
            return Downloader.Tiles.GetEmpty(Layer, "Not implemented").ToImageSource();
        }

    }
}
