using MapsInMyFolder.Core.Layers;

namespace MapsInMyFolder.Core.Downloader
{
    public static class Tiles
    {

        public static byte[] GetEmpty(this Layer Layer, string Message)
        {
            const int BorderSize = 1;
            var MapBackground = Properties.Settings.Default.MapBackground;
            double[] color = new double[] { MapBackground.R, MapBackground.G, MapBackground.B };

            return null;
        }

    }
}
