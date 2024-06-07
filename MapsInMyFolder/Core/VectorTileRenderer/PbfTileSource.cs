using Mapbox.Vector.Tile;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace MapsInMyFolder.Core.VectorTileRenderer.Sources
{
    public class PbfTileSource : IVectorTileSource
    {
        public string Path { get; set; } = "";
        public Stream Stream { get; set; } = null;

        public PbfTileSource(string path)
        {
            Path = path;
        }

        public PbfTileSource(Stream stream)
        {
            Stream = stream;
        }

        public Stream GetTile(int x, int y, int zoom)
        {
            var qualifiedPath = Path
                .Replace("{x}", x.ToString())
                .Replace("{y}", y.ToString())
                .Replace("{z}", zoom.ToString());
            return File.Open(qualifiedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public VectorTile GetVectorTile(int x, int y, int zoom)
        {
            if (Path != "")
            {
                Debug.WriteLine("Download tile from pbf tile source");
                using (var stream = GetTile(x, y, zoom))
                {
                    return UnzipStream(stream);
                }
            }
            else if (Stream != null)
            {
                return UnzipStream(Stream);
            }

            return null;
        }

        private static VectorTile UnzipStream(Stream stream)
        {
            return LoadStream(stream);
        }

        private static VectorTile LoadStream(Stream stream)
        {
            var mbLayers = VectorTileParser.Parse(stream);
            return BaseTileToVector(mbLayers);
        }

        static string ConvertGeometryType(Tile.GeomType type)
        {
            if (type == Tile.GeomType.LineString)
            {
                return "LineString";
            }
            else if (type == Tile.GeomType.Point)
            {
                return "Point";
            }
            else if (type == Tile.GeomType.Polygon)
            {
                return "Polygon";
            }
            else
            {
                return "Unknown";
            }
        }

        private static VectorTile BaseTileToVector(IEnumerable<Mapbox.Vector.Tile.VectorTileLayer> vectorTileLayers)
        {
            var result = new VectorTile();

            foreach (var lyr in vectorTileLayers)
            {
                var vectorLayer = new VectorTileLayer
                {
                    Name = lyr.Name,
                };

                for (int i = 0; i < lyr.VectorTileFeatures.Count; i++)
                {
                    Mapbox.Vector.Tile.VectorTileFeature feat = lyr.VectorTileFeatures[i];

                    var vectorFeature = new VectorTileFeature
                    {
                        Extent = 1,
                        GeometryType = ConvertGeometryType(feat.GeometryType),
                        Attributes = feat.Attributes.ToDictionary(element => element.Key, element => element.Value)
                    };

                    var vectorGeometry = new List<List<Point>>();

                    foreach (var points in feat.Geometry)
                    {
                        var vectorPoints = new List<Point>();

                        foreach (var coordinate in points)
                        {
                            var dX = coordinate.X / (double)lyr.Extent;
                            var dY = coordinate.Y / (double)lyr.Extent;

                            vectorPoints.Add(new Point(dX, dY));
                        }

                        vectorGeometry.Add(vectorPoints);
                    }

                    vectorFeature.Geometry = vectorGeometry;
                    vectorLayer.Features.Add(vectorFeature);
                }

                result.Layers.Add(vectorLayer);
            }

            return result;
        }
    }
}
