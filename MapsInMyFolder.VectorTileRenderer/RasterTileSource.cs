using System.IO;
using System.Threading.Tasks;

namespace MapsInMyFolder.VectorTileRenderer.Sources
{
    public class RasterTileSource : ITileSource
    {
        public string Path { get; private set; }

        public RasterTileSource(string path)
        {
            this.Path = path;
        }

#pragma warning disable CS1998 // Cette méthode async n'a pas d'opérateur 'await' et elle s'exécutera de façon synchrone
        public async Task<Stream> GetTile(int x, int y, int zoom)
#pragma warning restore CS1998 // Cette méthode async n'a pas d'opérateur 'await' et elle s'exécutera de façon synchrone
        {
            var qualifiedPath = Path
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString());

            return File.Open(qualifiedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}
