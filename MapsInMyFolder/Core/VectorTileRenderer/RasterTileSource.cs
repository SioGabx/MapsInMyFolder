using System.IO;

namespace MapsInMyFolder.Core.VectorTileRenderer.Sources
{
    public class RasterTileSource : ITileSource
    {
        public string Path { get; }

        public RasterTileSource(string path)
        {
            Path = path;
        }

        public Stream GetTile(int x, int y, int zoom)
        {
            var qualifiedPath = Path
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString());

            return File.Open(qualifiedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
