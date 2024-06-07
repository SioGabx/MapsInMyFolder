namespace MapsInMyFolder.Core.VectorTileRenderer.Sources
{
    public interface IVectorTileSource : ITileSource
    {
        VectorTile GetVectorTile(int x, int y, int zoom);
    }
}
