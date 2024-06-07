using System.IO;

namespace MapsInMyFolder.Core.VectorTileRenderer.Sources
{
    public interface ITileSource
    {
        Stream GetTile(int x, int y, int zoom);
    }
}
