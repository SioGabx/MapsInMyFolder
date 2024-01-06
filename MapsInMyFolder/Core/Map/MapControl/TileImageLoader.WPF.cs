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

        /// <summary>
        /// An ObjectCache instance used to cache tile image data. The default value is MemoryCache.Default.
        /// </summary>
        public static ObjectCache Cache { get; set; } = MemoryCache.Default;

        private static async Task LoadCachedTile(Tile tile, string uri, string cacheKey, int LayerId)
        {
            var cacheItem = Cache.Get(cacheKey) as Tuple<byte[], DateTime>;
            var buffer = cacheItem?.Item1;

            if (cacheItem == null || cacheItem.Item2 < DateTime.UtcNow)
            {
                Layers layers = Layers.GetLayerById(LayerId) ?? Layers.Empty();
                try
                {
                    var response = await TileLoader.GetImageAsync(uri, tile.XIndex, tile.Y, tile.ZoomLevel, layers, layers.TilesFormat, Collectif.GetSaveTempDirectory(layers.Name, layers.Identifier, tile.ZoomLevel)).ConfigureAwait(false);
                    if (response != null && response.Buffer != null && response.ResponseMessage != null && response.ResponseMessage.IsSuccessStatusCode)
                    {
                        buffer = response.Buffer;
                        cacheItem = Tuple.Create(buffer, GetExpiration(response.ResponseMessage.Headers.CacheControl?.MaxAge));
                        Cache.Set(cacheKey, cacheItem, new CacheItemPolicy { AbsoluteExpiration = cacheItem.Item2 });
                    }
                    else if (Settings.map_view_error_tile)
                    {
                        buffer = Collectif.GetEmptyImageBufferFromText(response, LayerId, layers.TilesFormat);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("on top " + ex.Message);
                }
            }

            if (buffer?.Length > 0)
            {
                //Loading LoadCachedTile
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
