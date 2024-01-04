using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MapsInMyFolder.Commun.Capabilities
{
    public partial class WMTSParser
    {
        public class WmtsTileMatrixSet
        {
            public string Identifier { get; }
            public string SupportedCrs { get; }
            public IList<WmtsTileMatrix> TileMatrixes { get; }
            public int MinZoom { get; }
            public int MaxZoom { get; }

            public WmtsTileMatrixSet(string identifier, string supportedCrs, IEnumerable<WmtsTileMatrix> tileMatrixes, int minZoom, int maxZoom)
            {
                if (string.IsNullOrEmpty(identifier))
                {
                    throw new ArgumentException("The identifier argument must not be null or empty.", nameof(identifier));
                }

                if (string.IsNullOrEmpty(supportedCrs))
                {
                    throw new ArgumentException("The supportedCrs argument must not be null or empty.", nameof(supportedCrs));
                }

                if (tileMatrixes == null || !tileMatrixes.Any())
                {
                    throw new ArgumentException("The tileMatrixes argument must not be null or an empty collection.", nameof(tileMatrixes));
                }

                Identifier = identifier;
                SupportedCrs = supportedCrs;
                TileMatrixes = tileMatrixes.OrderBy(m => m.Scale).ToList();

                MinZoom = minZoom;
                MaxZoom = maxZoom;
            }

            internal static WmtsTileMatrixSet ReadTileMatrixSet(XElement tileMatrixSetElement)
            {
                var identifier = tileMatrixSetElement.Element(ows + "Identifier")?.Value;

                if (string.IsNullOrEmpty(identifier))
                {
                    throw new ArgumentException("No Identifier element found in TileMatrixSet.");
                }

                var supportedCrs = tileMatrixSetElement.Element(ows + "SupportedCRS")?.Value;

                if (string.IsNullOrEmpty(supportedCrs))
                {
                    throw new ArgumentException($"No SupportedCRS element found in TileMatrixSet \"{identifier}\".");
                }

                const string urnPrefix = "urn:ogc:def:crs:EPSG:";

                if (supportedCrs.StartsWith(urnPrefix)) // e.g. "urn:ogc:def:crs:EPSG:6.18:3857")
                {
                    var crs = supportedCrs.Substring(urnPrefix.Length).Split(':');

                    if (crs.Length > 1)
                    {
                        supportedCrs = "EPSG:" + crs[1];
                    }
                }

                var tileMatrixes = new List<WmtsTileMatrix>();

                foreach (var tileMatrixElement in tileMatrixSetElement.Elements(wmts + "TileMatrix"))
                {
                    WmtsTileMatrix Matrix = WmtsTileMatrix.ReadTileMatrix(tileMatrixElement);
                    tileMatrixes.Add(Matrix);
                }

                int MinZoom = int.MaxValue;
                int MaxZoom = int.MinValue;
                tileMatrixes.ForEach(TileMatrix =>
                {
                    if (int.TryParse(TileMatrix.Level, out int tileMatrixLevel))
                    {
                        MinZoom = Math.Min(MinZoom, tileMatrixLevel);
                        MaxZoom = Math.Max(MaxZoom, tileMatrixLevel);
                    }
                });

                if (tileMatrixes.Count <= 0)
                {
                    throw new ArgumentException($"No TileMatrix elements found in TileMatrixSet \"{identifier}\".");
                }

                return new WmtsTileMatrixSet(identifier, supportedCrs, tileMatrixes, MinZoom, MaxZoom);
            }
        }
    }
}

