using System.Threading.Tasks;

namespace MapsInMyFolder.VectorTileRenderer.Sources
{
    public interface IVectorTileSource : ITileSource
    {
        Task<VectorTile> GetVectorTile(int x, int y, int zoom);
    }
}
