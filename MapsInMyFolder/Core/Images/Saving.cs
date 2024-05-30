using NetVips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Images
{
    public static class Saving
    {
        public static VOption GetSaveVOption(string SaveFileFormat, int Quality, int TileSize)
        {
            Quality = Math.Max(Quality, 1);
            return SaveFileFormat switch
            {
                "png" => new VOption {
                    { "compression", Quality },
                    { "interlace", true },
                    { "strip", true },
                },
                "jpeg" => new VOption {
                    { "Q", Quality },
                    { "interlace", true },
                    { "optimize_coding", true },
                    { "strip", true },
                },
                "tiff" => new VOption {
                    { "Q", Quality },
                    { "tileWidth", TileSize },
                    { "tileHeight", TileSize },
                    { "compression", "jpeg" },
                    { "interlace", true },
                    { "tile", true },
                    { "pyramid", true },
                    { "bigtif", true }
                },
                _ => new VOption(),
            };
        }



    }
}
