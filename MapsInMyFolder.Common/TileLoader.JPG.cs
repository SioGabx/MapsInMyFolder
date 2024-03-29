﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun
{
    public partial class TileLoader
    {
        public async Task<HttpResponse> GetTile(int layerID, TileProperty tilesUrl)
        {
            try
            {
                string url = tilesUrl.url;
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
