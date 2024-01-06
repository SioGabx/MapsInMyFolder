using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace MapsInMyFolder.Commun.Capabilities
{
    public partial class WMTSParser
    {
        public class WmtsTileMatrix
        {
            // See 07-057r7_Web_Map_Tile_Service_Standard.pdf, section 6.1.a, page 8:
            // "standardized rendering pixel size" is 0.28 mm

            public WmtsTileMatrix(string identifier, string level,string levelPrefix, double scaleDenominator, int tileWidth, int tileHeight, int matrixWidth, int matrixHeight)
            {
                Identifier = identifier;
                Scale = 1 / (scaleDenominator * 0.00028); // 0.28 mm
                TileWidth = tileWidth;
                TileHeight = tileHeight;
                MatrixWidth = matrixWidth;
                MatrixHeight = matrixHeight;
                Level = level;
                LevelPrefix = levelPrefix;
            }

            public string Identifier { get; }
            public string Level { get; }
            public string LevelPrefix { get; }
            public double Scale { get; }
            public int TileWidth { get; }
            public int TileHeight { get; }
            public int MatrixWidth { get; }
            public int MatrixHeight { get; }
            public int MinTileRow { get; set; }
            public int MaxTileRow { get; set; }
            public int MinTileCol { get; set; }
            public int MaxTileCol { get; set; }
            public string Zoom { get; set; } = null;

            public static WmtsTileMatrix ReadTileMatrix(XElement tileMatrixElement)
            {
                var identifier = tileMatrixElement.Element(ows + "Identifier")?.Value;

                if (string.IsNullOrEmpty(identifier))
                {
                    throw new ArgumentException("No Identifier element found in TileMatrix.");
                }

                var valueString = tileMatrixElement.Element(wmts + "ScaleDenominator")?.Value;

                if (string.IsNullOrEmpty(valueString) ||
                    !double.TryParse(valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out double scaleDenominator))
                {
                    throw new ArgumentException($"No ScaleDenominator element found in TileMatrix \"{identifier}\".");
                }

                valueString = tileMatrixElement.Element(wmts + "TopLeftCorner")?.Value;
                string[] topLeftCornerStrings;

                if (string.IsNullOrEmpty(valueString) ||
                    (topLeftCornerStrings = valueString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Length < 2 ||
                    !double.TryParse(topLeftCornerStrings[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double left) ||
                    !double.TryParse(topLeftCornerStrings[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double top))
                {
                    throw new ArgumentException($"No TopLeftCorner element found in TileMatrix \"{identifier}\".");
                }

                valueString = tileMatrixElement.Element(wmts + "TileWidth")?.Value;

                if (string.IsNullOrEmpty(valueString) || !int.TryParse(valueString, out int tileWidth))
                {
                    throw new ArgumentException($"No TileWidth element found in TileMatrix \"{identifier}\".");
                }

                valueString = tileMatrixElement.Element(wmts + "TileHeight")?.Value;

                if (string.IsNullOrEmpty(valueString) || !int.TryParse(valueString, out int tileHeight))
                {
                    throw new ArgumentException($"No TileHeight element found in TileMatrix \"{identifier}\".");
                }

                valueString = tileMatrixElement.Element(wmts + "MatrixWidth")?.Value;

                if (string.IsNullOrEmpty(valueString) || !int.TryParse(valueString, out int matrixWidth))
                {
                    throw new ArgumentException($"No MatrixWidth element found in TileMatrix \"{identifier}\".");
                }

                valueString = tileMatrixElement.Element(wmts + "MatrixHeight")?.Value;

                if (string.IsNullOrEmpty(valueString) || !int.TryParse(valueString, out int matrixHeight))
                {
                    throw new ArgumentException($"No MatrixHeight element found in TileMatrix \"{identifier}\".");
                }

                var SplittedIdentifier = identifier.Split(":");
                var Level = SplittedIdentifier.Last();
                var LevelPrefix = identifier.TrimEnd(Level);

                return new WmtsTileMatrix(
                    identifier, Level, LevelPrefix, scaleDenominator, tileWidth, tileHeight, matrixWidth, matrixHeight);
            }
        }
    }
}