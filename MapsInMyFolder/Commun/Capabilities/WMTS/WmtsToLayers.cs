using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapsInMyFolder.Commun.Capabilities
{
    public partial class WMTSParser
    {
        public static readonly List<string> SupportedCRS = new List<string>() {
            "EPSG:3857", //https://epsg.io/3857
            "EPSG:900913", //old name for 3857
        };

        public static async Task<IEnumerable<Layers>> ParseAsync()
        {
            // var distUrl = "http://wxs.ign.fr/an7nvfzojv5wa96dsga5nk8w/geoportail/wmts?SERVICE=WMTS&REQUEST=GetCapabilities";
            var distUrl = @"C:\Users\franc\Documents\SharpDevelop Projects\WMTSCapabilitiesParser\WMTSCapabilitiesParser.Test\datagrandest.xml";
            //var distUrl = @"C:\Users\franc\Documents\SharpDevelop Projects\WMTSCapabilitiesParser\WMTSCapabilitiesParser.Test\datagrandest_v2.xml";


            var Capabilities = await WMTSParser.ReadCapabilitiesAsync(new Uri(distUrl.Trim('"')));
            var LayersList = new List<Layers>();
            foreach (var LayerCapabilities in Capabilities)
            {
                Layers Layer = Layers.Empty();
                Layer.Id = LayersList.Count + 1;
                Layer.Identifier = LayerCapabilities.LayerIdentifier;
                Layer.Name = LayerCapabilities.LayerTitle;
                Layer.Description = LayerCapabilities.LayerAbstract;
                Layer.TilesFormat = LayerCapabilities.LayerFormat;
                Layer.Style = LayerCapabilities.LayerStyle;
                Layer.TileUrl = LayerCapabilities.UrlTemplate;

                var boundingBox = LayerCapabilities?.WGS84BoundingBox;
                if (boundingBox.NortheastY == 0 && boundingBox.NortheastX == 0 && boundingBox.SouthwestY == 0 && boundingBox.SouthwestX == 0)
                {
                    boundingBox = null;
                }
                if (boundingBox != null)
                {
                    var Rectangle = new Dictionary<string, object>()
                    {
                        { "NO_Lat", boundingBox.NortheastY.ToString()},
                        { "NO_Long", boundingBox.NortheastX.ToString()},
                        { "SE_Lat", boundingBox.SouthwestY.ToString()},
                        { "SE_Long",  boundingBox.SouthwestX.ToString()}
                    };
                    Layer.BoundaryRectangles = $"[{JsonConvert.SerializeObject(Rectangle, Formatting.Indented)}]";
                }

                Layer.SiteUrl = new Uri(LayerCapabilities.UrlTemplate).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                Layer.SiteName = new Uri(LayerCapabilities.UrlTemplate).Host.Split('.').SkipLast(1).Last();
                string MatrixSetIndentifier = string.Empty;

                foreach (var CRS in SupportedCRS)
                {
                    foreach (var MatrixSet in LayerCapabilities.TileMatrixSets)
                    {
                        if (CRS == MatrixSet.SupportedCrs)
                        {
                            MatrixSetIndentifier = MatrixSet.Identifier;
                            break;
                        }
                    }
                }

                var SelectedMatrix = LayerCapabilities.TileMatrixSets.Where(MatrixSet => MatrixSet.Identifier == MatrixSetIndentifier).FirstOrDefault();
                SelectedMatrix ??= LayerCapabilities.TileMatrixSets.FirstOrDefault();
                Layer.MinZoom = SelectedMatrix.MinZoom;
                Layer.MaxZoom = SelectedMatrix.MaxZoom;
                Layer.Description += $"\n\nTileMatrixSet : {SelectedMatrix.Identifier} / {SelectedMatrix.SupportedCrs}";

                var TileMatrixSet = SelectedMatrix.Identifier;
                Layer.TileUrl = Layer.TileUrl.Replace("{TileMatrixSet}", TileMatrixSet);
                Layer.TileUrl = Layer.TileUrl.Replace("{TileMatrix}", (SelectedMatrix?.TileMatrixes?.FirstOrDefault()?.LevelPrefix ?? string.Empty) + "{TileMatrix}");

                LayersList.Add(Layer);
            }
            return LayersList;
        }

    }
}
