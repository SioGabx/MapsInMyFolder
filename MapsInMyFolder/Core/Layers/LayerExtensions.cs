using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Properties;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Layers
{
    public static class LayerExtensions
    {
        public static async Task<ImageSource> GetTile(this Layer Layer, int X, int Y, int Z)
        {
            //var Image = Downloader.Tiles.GetEmpty(Layer, $"Not implemented").ToImageSource();
            string Url = Layer.GetFormatedUrl(X, Y, Z);
            var ImageByte = await Downloader.Tiles.GetTileFromUrl(Layer, Url);
            var Image = ImageByte.ToImageSource();

            bool ShowTileLocation = Settings.Default.DebugMode || Layer.ShowTileLocation;
            if (Layer.ShowTileLocation || ShowTileLocation)
            {
                return Image.AddBorder(1, ShowTileLocation ? $"X : {X}\nY : {Y}\nZoom : {Z}\n" : null);
            }

            return Image;
        }

        public static string GetFormatedUrl(this Layer Layer, int X, int Y, int Z)
        {
            var url = Layer.TileUrl;
            url = url.Replace("{x}", X.ToString());
            url = url.Replace("{y}", Y.ToString());
            url = url.Replace("{z}", Z.ToString());
            return url;
        }

    }
}
