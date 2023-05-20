using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{
    public partial class TileGenerator
    {
        public async Task<HttpResponse> GetTile(int layerID, string urlBase, int TileX, int TileY, int zoom)
        {
            //Debug.WriteLine("TileX :" + TileX);
            Layers Layer = Layers.GetLayerById(layerID);
            if (Layer is null)
            {
                return HttpResponse.HttpResponseError;
            }

            if (!TileGeneratorSettings.SupportedFileType.Contains(Layer.class_format.ToLower()))
            {
                Debug.WriteLine("Le format peut ne pas être supporté actuellement : " + Layer.class_format);
            }

            try
            {
                string url = Collectif.GetUrl.FromTileXYZ(urlBase, TileX, TileY, zoom, layerID, Collectif.GetUrl.InvokeFunction.getTile);
                if (string.IsNullOrWhiteSpace(url))
                {
                    return HttpResponse.HttpResponseError;
                }
                Uri uri = new Uri(url);
                return await Collectif.ByteDownloadUri(uri, layerID, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Erreur GetTile : " + ex.Message);
                return HttpResponse.HttpResponseError;
            }
        }
    }
}
