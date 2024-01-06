using Jint;
using MapsInMyFolder.Commun;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MapsInMyFolder
{
    public static class GetUrl
    {
        public static (Dictionary<string, object> DefaultCallValue, Dictionary<string, string> ResultCallValue) CallFunctionAndGetResult(string urlbase, string Script, int Tilex, int Tiley, int z, int LayerID, Javascript.InvokeFunction InvokeFunction)
        {
            var location_topleft = Collectif.TileToCoordonnees(Tilex, Tiley, z);
            var location_bottomright = Collectif.TileToCoordonnees(Tilex + 1, Tiley + 1, z);
            var (Latitude, Longitude) = Collectif.GetCenterBetweenTwoPoints(location_topleft, location_bottomright);

            Dictionary<string, object> arguments = new Dictionary<string, object>()
                {
                      { "x",  Tilex.ToString() },
                      { "y",  Tiley.ToString() },
                      { "z",  z.ToString() },
                      { "TileRow",  Tiley.ToString() },
                      { "TileCol",  Tilex.ToString() },
                      { "TileMatrix",  z.ToString() },
                      { "zoom",  z.ToString() },
                      { "lat",  Latitude.ToString() },
                      { "lng",  Longitude.ToString()},

                      { "t_lat",  location_topleft.Latitude.ToString() },
                      { "t_lng",  location_topleft.Longitude.ToString()},
                      { "b_lat",  location_bottomright.Latitude.ToString() },
                      { "b_lng",  location_bottomright.Longitude.ToString()},

                      { "layerid",  LayerID.ToString() },
                      { "url",  urlbase },
                };

            if (!string.IsNullOrEmpty(Script))
            {
                Jint.Native.JsValue JavascriptMainResult = null;
                try
                {
                    JavascriptMainResult = Javascript.ExecuteScript(Script, new Dictionary<string, object>(arguments), LayerID, InvokeFunction);
                }
                catch (Exception ex)
                {
                    Javascript.Functions.PrintError(ex.Message);
                }
                if (JavascriptMainResult is not null && JavascriptMainResult.IsObject())
                {
                    object JavascriptMainResultObject = JavascriptMainResult.ToObject();
                    var JavascriptMainResultJson = JsonConvert.SerializeObject(JavascriptMainResultObject);
                    var JavascriptMainResultDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(JavascriptMainResultJson);
                    return (arguments, JavascriptMainResultDictionary);
                }
            }
            return (null, null);
        }

        public static string FromTileXYZ(string urlbase, int Tilex, int Tiley, int z, Layers TileLayer, Javascript.InvokeFunction InvokeFunction)
        {
            if (TileLayer is null)
            {
                return string.Empty;
            }
            string Script = TileLayer.Script;
            var ValuesDictionnary = CallFunctionAndGetResult(urlbase, Script, Tilex, Tiley, z, TileLayer.Id, InvokeFunction);
            if (ValuesDictionnary.ResultCallValue is null)
            {
                return string.Empty;
            }

            string finalurl;
            if (string.IsNullOrEmpty(urlbase))
            {
                finalurl = TileLayer.TileUrl;
            }
            else
            {
                finalurl = urlbase;
            }
            if (ValuesDictionnary.ResultCallValue.TryGetValue("url", out string urlResult))
            {
                if (!string.IsNullOrEmpty(urlResult))
                {
                    finalurl = urlResult;
                }
            }

            foreach (var JavascriptReplacementVar in ValuesDictionnary.ResultCallValue)
            {
                string replacementValue = string.Empty;
                try
                {
                    if (JavascriptReplacementVar.Value is null)
                    {
                        replacementValue = "null";
                    }
                    else
                    {
                        replacementValue = JavascriptReplacementVar.Value;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    replacementValue = "null";
                }
                finally
                {
                    finalurl = finalurl.Replace("{" + JavascriptReplacementVar.Key + "}", replacementValue);
                }
            }
            return finalurl;
        }

        public static List<int> NextNumberFromPara(int x, int y, int x_Max, int y_Max)
        {
            int return_x;
            int return_y;
            if (x < x_Max - 1)
            {
                return_x = x + 1;
                return_y = y;
            }
            else if (y < y_Max - 1)
            {
                return_x = 0;
                return_y = y + 1;
            }
            else
            {
                return_x = 0;
                return_y = 0;
            }
            return new List<int>() { return_x, return_y };
        }

        public static List<TileProperty> GetListOfUrlFromLocation(Dictionary<string, double> location, int z, string urlbase, int LayerID, int downloadid, string varContext)
        {
            try
            {
                Layers calque = Layers.Convert.Copy(Layers.GetLayerById(LayerID));
                Layers.Add((int)Layers.ReservedId.TempLayerGeneric, calque);

                var deserializedArray = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(varContext);

                foreach (var dictionary in deserializedArray)
                {
                    foreach (var kvp in dictionary)
                    {
                        string key = kvp.Key;
                        object value = kvp.Value;

                        if (value != null && !string.IsNullOrEmpty(key))
                        {
                            Javascript.Functions.SetVar(key, value, false, (int)Layers.ReservedId.TempLayerGeneric);
                        }
                    }
                }

                Javascript.EngineDeleteById((int)Layers.ReservedId.TempLayerGeneric);

                List<TileProperty> tilesUrls = GenrateListOfUrlFromLocation(location, z, urlbase, (int)Layers.ReservedId.TempLayerGeneric, downloadid, calque.TilesFormat);

                return tilesUrls;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                Layers.RemoveLayerById(-1);
                Javascript.Functions.ClearVar(-1);
            }

            return new List<TileProperty>();
        }


        public static List<TileProperty> GenrateListOfUrlFromLocation(Dictionary<string, double> location, int z, string urlbase, int LayerID, int downloadid, string format)
        {
            var NO_tile = Collectif.CoordonneesToTile(location["NO_Latitude"], location["NO_Longitude"], z);
            var SE_tile = Collectif.CoordonneesToTile(location["SE_Latitude"], location["SE_Longitude"], z);
            int NO_x = NO_tile.X;
            int NO_y = NO_tile.Y;
            int SE_x = SE_tile.X;
            int SE_y = SE_tile.Y;

            List<TileProperty> list_of_url_to_download = new List<TileProperty>();
            int Download_X_tile = 0;
            int Download_Y_tile = 0;
            int max_x = Math.Abs(SE_x - NO_x) + 1;
            int max_y = Math.Abs(SE_y - NO_y) + 1;
            for (int i = 0; i < max_y; i++)
            {
                for (int a = 0; a < max_x; a++)
                {
                    int tuileX = NO_x + Download_X_tile;
                    int tuileY = NO_y + Download_Y_tile;
                    Layers tempLayer = Layers.GetLayerById(LayerID);
                    string url = FromTileXYZ(urlbase, tuileX, tuileY, z, tempLayer, Javascript.InvokeFunction.getTile);
                    TileProperty Tile = new TileProperty(url, tuileX, tuileY, z, Status.waitfordownloading, downloadid, format);
                    if (format == "pbf")
                    {
                        Tile.SetNeighbour(tempLayer, urlbase);
                    }
                    list_of_url_to_download.Add(Tile);
                    List<int> next_num_list = NextNumberFromPara(Download_X_tile, Download_Y_tile, max_x, max_y);
                    Download_X_tile = next_num_list[0];
                    Download_Y_tile = next_num_list[1];
                }
            }
            return list_of_url_to_download;
        }
    }
}
