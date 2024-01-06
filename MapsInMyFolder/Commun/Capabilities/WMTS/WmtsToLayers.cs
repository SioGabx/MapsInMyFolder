using Esprima.Ast;
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
            "EPSG:3857",    //WGS 84 / Pseudo-Mercator -- Spherical Mercator, Google Maps, OpenStreetMap, Bing, ArcGIS, ESRI https://epsg.io/3857
            "EPSG:900913",  //Google Maps Global Mercator -- Spherical Mercator (unofficial - used in open source projects / OSGEO)
            "EPSG:3587",    //mistype of Spherical Mercator EPSG:3857 (mistype)
            "EPSG:54004",   //Mercator -- Spherical Mercator (unofficial deprecated ESRI)
            "EPSG:41001",   //WGS84 / Simple Mercator -- Spherical Mercator (unofficial deprecated OSGEO / Tile Map Service)
            "EPSG:102113",  //WGS 1984 Major Auxiliary Web Mercator -- Spherical Mercator (unofficial deprecated ESRI)
            "EPSG:102100",  //WGS 1984 Web Mercator Major Auxiliary Sphere -- Spherical Mercator (unofficial deprecated ESRI)
            "EPSG:3785",    //Popular Visualisation CRS / Mercator -- Spherical Mercator (deprecated EPSG code wrongly defined)
        };

        public static async Task<IEnumerable<Layers>> ParseAsync()
        {
             var distUrl = "http://wxs.ign.fr/an7nvfzojv5wa96dsga5nk8w/geoportail/wmts?SERVICE=WMTS&REQUEST=GetCapabilities";
           // var distUrl = @"C:\Users\franc\Documents\SharpDevelop Projects\WMTSCapabilitiesParser\WMTSCapabilitiesParser.Test\datagrandest.xml";
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
                Layer.TilesFormat = LayerCapabilities.LayerFormat.TrimStart("image/"); ;
                Layer.Style = LayerCapabilities.LayerStyle;
                Layer.TileUrl = LayerCapabilities.UrlTemplate;
                Layer.SiteUrl = new Uri(LayerCapabilities.UrlTemplate).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                Layer.SiteName = new Uri(LayerCapabilities.UrlTemplate).Host.Split('.').SkipLast(1).Last();
                Layer.Tags = string.Join(';', LayerCapabilities.LayerKeyWords);
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
                if (string.IsNullOrEmpty(MatrixSetIndentifier))
                {
                    continue; //ignore non supported CRS
                }
                var SelectedMatrix = LayerCapabilities.TileMatrixSets.Where(MatrixSet => MatrixSet.Identifier == MatrixSetIndentifier).FirstOrDefault();
                SelectedMatrix ??= LayerCapabilities.TileMatrixSets.FirstOrDefault();


                int MinZoom = int.MaxValue;
                int MaxZoom = int.MinValue;
                SelectedMatrix.TileMatrixes.ToList().ForEach(TileMatrix =>
                {
                    if (TileMatrix?.Zoom is null)
                    {
                        return;
                    }
                    if (int.TryParse(TileMatrix.Zoom, out int tileMatrixLevel))
                    {
                        MinZoom = Math.Min(MinZoom, tileMatrixLevel);
                        MaxZoom = Math.Max(MaxZoom, tileMatrixLevel);
                    }
                });

                Layer.MinZoom = MinZoom;
                Layer.MaxZoom = MaxZoom;
                Layer.Description += $"\n\nTileMatrixSet : {SelectedMatrix.Identifier} / {SelectedMatrix.SupportedCrs}";

                var TileMatrixSet = SelectedMatrix.Identifier;
                Layer.TileUrl = Layer.TileUrl.Replace("{TileMatrixSet}", TileMatrixSet);
                Layer.TileUrl = Layer.TileUrl.Replace("{TileMatrix}", (SelectedMatrix?.TileMatrixes?.FirstOrDefault()?.LevelPrefix ?? string.Empty) + "{TileMatrix}");

                var boundingBox = LayerCapabilities?.WGS84BoundingBox;
                if (boundingBox.NortheastY == 0 && boundingBox.NortheastX == 0 && boundingBox.SouthwestY == 0 && boundingBox.SouthwestX == 0)
                {
                    if (SelectedMatrix.TileMatrixes.Count > 0)
                    {
                        var x = SelectedMatrix.TileMatrixes.LastOrDefault();
                        var zoom = int.Parse(x.Level);
                        var NO = Collectif.TileToCoordonnees(x.MinTileCol, x.MinTileRow, zoom);
                        var SE = Collectif.TileToCoordonnees(x.MaxTileCol, x.MaxTileRow, zoom);
                        boundingBox.NortheastY = NO.Latitude;
                        boundingBox.NortheastX = NO.Longitude;
                        boundingBox.SouthwestY = SE.Latitude;
                        boundingBox.SouthwestX = SE.Longitude;
                    }
                    else
                    {
                        boundingBox = null;
                    }
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




                LayersList.Add(Layer);
            }
            return LayersList;
        }

    }
}
