// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using MapsInMyFolder.Core.Layers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MapsInMyFolder.View.Controls.Map
{
    /// <summary>
    /// Loads and optionally caches map tile images for a MapTileLayer.
    /// </summary>
    public partial class TileImageLoader : ITileImageLoader
    {
        /// <summary>
        /// Maximum number of parallel tile loading tasks. The default value is 4.
        /// </summary>
        public static int MaxLoadTasks { get; set; } = 4;

        /// <summary>
        /// Default expiration time for cached tile images. Used when no expiration time
        /// was transmitted on download. The default value is one day.
        /// </summary>
        public static TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Maximum expiration time for cached tile images. A transmitted expiration time
        /// that exceeds this value is ignored. The default value is ten days.
        /// </summary>
        public static TimeSpan MaxCacheExpiration { get; set; } = TimeSpan.FromDays(10);

        /// <summary>
        /// The current TileSource, passed to the most recent LoadTiles call.
        /// </summary>
        public TileSource TileSource { get; private set; }

        private ConcurrentStack<Tile> pendingTiles;


        public Task SortedLoadTiles(IEnumerable<Tile> tiles, TileSource tileSource)
        {
            if (tiles.Any())
            {
                var centers = tiles
                    .GroupBy(x => x.ZoomLevel, (key, tiles) => new
                    {
                        key,
                        midX = tiles.Average(x => x.X),
                        midY = tiles.Average(x => x.Y)
                    })
                    .ToDictionary(x => x.key);
                static double sq(double x) => x * x;
                var sortedTiles = tiles
                    .OrderBy(x => x.ZoomLevel)
                    .ThenBy(x => sq(centers[x.ZoomLevel].midX - x.X) + sq(centers[x.ZoomLevel].midY - x.Y));
                return LoadTiles(sortedTiles, tileSource);
            }
            else
            {
                return LoadTiles(tiles, tileSource);
            }
        }


        /// <summary>
        /// Loads all pending tiles from the tiles collection.
        /// If tileSource.UriFormat starts with "http" and cacheName is a non-empty string,
        /// tile images will be cached in the TileImageLoader's Cache - if that is not null.
        /// </summary>
        public Task LoadTiles(IEnumerable<Tile> tiles, TileSource tileSource)
        {
            pendingTiles?.Clear(); // stop processing the current queue

            TileSource = tileSource;

            if (tileSource != null)
            {
                pendingTiles = new ConcurrentStack<Tile>(tiles.Where(tile => tile.Pending).Reverse());

                var numTasks = Math.Min(pendingTiles.Count, MaxLoadTasks);

                if (numTasks > 0)
                {
                    var tasks = Enumerable.Range(0, numTasks)
                        .Select(_ => Task.Run(() => LoadPendingTiles(pendingTiles, tileSource)));

                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        private static async Task LoadPendingTiles(ConcurrentStack<Tile> pendingTiles, TileSource tileSource)
        {
            while (pendingTiles.TryPop(out var tile))
            {
                tile.Pending = false;
                try
                {
                    await LoadTile(tile, tileSource).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"TileImageLoader: {tile.ZoomLevel}/{tile.XIndex}/{tile.Y}: {ex.Message}");
                }
            }
        }

        private static async Task LoadTile(Tile tile, TileSource tileSource)
        {
            var Image = tileSource.Layer.GetTile(tile.X, tile.Y, tile.ZoomLevel);
            await tile.Image.Dispatcher.InvokeAsync(() => tile.SetImage(Image));
        }
    }
}
