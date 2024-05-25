namespace MapsInMyFolder.Core.Layers
{
    public enum Format { png, jpeg }



    public class Layer
    {
        public int LayerId { get; }
        public int TileSize { get; }


        public Layer()
        {
            LayerId = 0;
            TileSize = 256;
        }
        public static Layer Default = new Layer();
    }
}
