// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Commun;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace MapsInMyFolder.MapControl
{
    //public class TileLoaderSettings
    //{
    //    public static TileGenerator TileLoaderGenerator = new();
    //    //public static void Set(string user_agent)
    //    //{
    //    //    MIMF_TileLoader_TG.HttpClient.
    //    //}

    //    public static class Set
    //    {
    //        public static void UserAgent(string user_agent)
    //        {
    //            //TileLoaderGenerator.HttpClient.DefaultRequestHeaders.Add("User-Agent", user_agent);
    //        }
    //        public static void BaseUrl(string baseUrl)
    //        {
    //            TileLoaderGenerator.BaseUrl = baseUrl;
    //        }

    //    }

    //    public static TileGenerator Get()
    //    {
    //        return TileLoaderGenerator;
    //    }

    //}

    public partial class TileImageLoader
    {
        /// <summary>
        /// Default folder path where an ObjectCache instance may save cached data, i.e. C:\ProgramData\MapControl\TileCache
        /// </summary>
        ///
        public static string DefaultCacheFolder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MapControl", "TileCache"); }
        }
        //public static TileGenerator MIMF_TileLoader_TG = new TileGenerator();
        //public static TileGeneratorSettings TileLoaderSettings = new();
        /// <summary>
        /// An ObjectCache instance used to cache tile image data. The default value is MemoryCache.Default.
        /// </summary>
        public static ObjectCache Cache { get; set; } = MemoryCache.Default;

        //private static async Task LoadCachedTile(Tile tile, string uri, string cacheKey, int LayerId)
        //{
        //    var cacheItem = Cache.Get(cacheKey) as Tuple<byte[], DateTime>;
        //    var buffer = cacheItem?.Item1;

        //    if (cacheItem == null || cacheItem.Item2 < DateTime.UtcNow)
        //    {
        //        Layers layers = Layers.GetLayerById(LayerId) ?? Layers.Empty();
        //        try
        //        {
        //            var response = await TileGeneratorSettings.TileLoaderGenerator.GetImageAsync(uri, tile.XIndex, tile.Y, tile.ZoomLevel, LayerId, null, Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifiant, tile.ZoomLevel)).ConfigureAwait(false);
        //            if (!((response is null) || (response.Buffer is null) || (response.ResponseMessage is null)) && response.ResponseMessage.IsSuccessStatusCode) // download succeeded
        //            {
        //                buffer = response.Buffer;
        //                cacheItem = Tuple.Create(buffer, GetExpiration(response.ResponseMessage.Headers.CacheControl?.MaxAge));
        //                Cache.Set(cacheKey, cacheItem, new CacheItemPolicy { AbsoluteExpiration = cacheItem.Item2 });
        //            }
        //            else if (Settings.map_view_error_tile)
        //            {
        //                buffer = Collectif.GetEmptyImageBufferFromText(response);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine("on top " + ex.Message);
        //        }
        //    }

        //    if (buffer?.Length > 0)
        //    {
        //        DebugMode.WriteLine("Loading LoadCachedTile image LayerId=" + LayerId);
        //        var image = await ImageLoader.LoadImageAsync(buffer).ConfigureAwait(false);
        //        await tile.Image.Dispatcher.InvokeAsync(() => tile.SetImage(image));
        //    }
        //}

        private static async Task LoadCachedTile(Tile tile, string uri, string cacheKey, int LayerId)
        {
            var cacheItem = Cache.Get(cacheKey) as Tuple<byte[], DateTime>;
            var buffer = cacheItem?.Item1;

            if (cacheItem == null || cacheItem.Item2 < DateTime.UtcNow)
            {
                Layers layers = Layers.GetLayerById(LayerId) ?? Layers.Empty();
                try
                {
                    var response = await TileGeneratorSettings.TileLoaderGenerator.GetImageAsync(uri, tile.XIndex, tile.Y, tile.ZoomLevel, LayerId, null, Collectif.GetSaveTempDirectory(layers.class_name, layers.class_identifiant, tile.ZoomLevel)).ConfigureAwait(false);
                    if (response != null && response.Buffer != null && response.ResponseMessage != null && response.ResponseMessage.IsSuccessStatusCode)
                    {
                        buffer = response.Buffer;
                        cacheItem = Tuple.Create(buffer, GetExpiration(response.ResponseMessage.Headers.CacheControl?.MaxAge));
                        Cache.Set(cacheKey, cacheItem, new CacheItemPolicy { AbsoluteExpiration = cacheItem.Item2 });
                    }
                    else if (Settings.map_view_error_tile)
                    {
                        buffer = Collectif.GetEmptyImageBufferFromText(response);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("on top " + ex.Message);
                }
            }

            if (buffer?.Length > 0)
            {
                DebugMode.WriteLine("Loading LoadCachedTile image LayerId=" + LayerId);
                var image = await ImageLoader.LoadImageAsync(buffer).ConfigureAwait(false);
                await tile.Image.Dispatcher.InvokeAsync(() => tile.SetImage(image));
            }
        }


        private static async Task LoadTile(Tile tile, TileSource tileSource)
        {
            var image = await tileSource.LoadImageAsync(tile.XIndex, tile.Y, tile.ZoomLevel, tileSource).ConfigureAwait(false);

            await tile.Image.Dispatcher.InvokeAsync(() => tile.SetImage(image));
        }
    }
}
