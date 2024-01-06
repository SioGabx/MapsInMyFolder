using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MapsInMyFolder.Commun.Capabilities
{
    public partial class WMTSParser
    {
        private static readonly XNamespace ows = "http://www.opengis.net/ows/1.1";
        private static readonly XNamespace wmts = "http://www.opengis.net/wmts/1.0";
        private static readonly XNamespace xlink = "http://www.w3.org/1999/xlink";

        public string LayerIdentifier { get; private set; } = string.Empty;
        public string LayerTitle { get; private set; } = string.Empty;
        public string LayerAbstract { get; private set; } = string.Empty;
        public string LayerFormat { get; private set; } = string.Empty;
        public string LayerStyle { get; private set; } = string.Empty;
        public string[] LayerKeyWords { get; private set; } = new string[0];
        public string UrlTemplate { get; private set; }
        public BoundingBox WGS84BoundingBox { get; private set; }
        public List<WmtsTileMatrixSet> TileMatrixSets { get; private set; }

        private static async Task<IEnumerable<WMTSParser>> ReadCapabilitiesAsync(Uri capabilitiesUri)
        {
            IEnumerable<WMTSParser> capabilities;

            if (capabilitiesUri.IsAbsoluteUri && (capabilitiesUri.Scheme == "http" || capabilitiesUri.Scheme == "https"))
            {
                var Response = await Commun.Collectif.ByteDownloadUri(capabilitiesUri, (int)Layers.ReservedId.TempLayerGeneric, true).ConfigureAwait(false);
                if (Response?.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    using (var stream = Collectif.ByteArrayToStream(Response.Buffer))
                    {
                        capabilities = ReadCapabilities(XDocument.Load(stream).Root, capabilitiesUri.ToString());
                    }
                }

                HttpClient client = new HttpClient();
                using (var stream = await client.GetStreamAsync(capabilitiesUri))
                {
                    capabilities = ReadCapabilities(XDocument.Load(stream).Root, capabilitiesUri.ToString());
                }
            }
            else
            {
                capabilities = ReadCapabilities(XDocument.Load(capabilitiesUri.ToString()).Root, string.Empty);
            }

            return capabilities;
        }

        private static List<WMTSParser> ReadCapabilities(XElement capabilitiesElement, string capabilitiesUrl)
        {
            var contentsElement = (capabilitiesElement?.Element(wmts + "Contents")) ?? throw new ArgumentException("Contents element not found.");
            List<WMTSParser> ListOfWMTSParser = new List<WMTSParser>();
            IEnumerable<XElement> LayerList = contentsElement.Elements(wmts + "Layer");
            foreach (XElement layerElement in LayerList)
            {

                string LayerIdentifier = layerElement.Element(ows + "Identifier")?.Value ?? string.Empty;
                string LayerTitle = layerElement.Element(ows + "Title")?.Value ?? string.Empty;
                string LayerAbstract = layerElement.Element(ows + "Abstract")?.Value ?? string.Empty;

                List<string> ListOfKeywords = new List<string>();
                XElement Keywords = layerElement.Element(ows + "Keywords");
                foreach(XElement keyWord in Keywords?.Elements(ows + "Keyword"))
                {
                    string Word = keyWord?.Value;
                    if (!string.IsNullOrWhiteSpace(Word))
                    {
                        ListOfKeywords.Add(Word);
                    }
                }

                XElement boundingBoxElement = layerElement.Element(ows + "WGS84BoundingBox");
                XElement LowerCorner = boundingBoxElement?.Element(ows + "LowerCorner");
                XElement UpperCorner = boundingBoxElement?.Element(ows + "UpperCorner");
                BoundingBox boundingBox = BoundingBox.ParseBBox(LowerCorner, UpperCorner);

                var styleElement = layerElement.Elements(wmts + "Style").FirstOrDefault(s => s.Attribute("isDefault")?.Value == "true");

                styleElement ??= layerElement.Elements(wmts + "Style").FirstOrDefault();

                string StyleName = styleElement?.Element(ows + "Identifier")?.Value ?? string.Empty;
                var urlTemplate = ReadUrlTemplate(capabilitiesElement, layerElement, LayerIdentifier, StyleName, capabilitiesUrl);

                var tileMatrixSetIds = layerElement.Elements(wmts + "TileMatrixSetLink").Select(l => l.Element(wmts + "TileMatrixSet")?.Value).Where(v => !string.IsNullOrEmpty(v));

                var tileMatrixSets = new List<WmtsTileMatrixSet>();
                foreach (var tileMatrixSetId in tileMatrixSetIds)
                {
                    var tileMatrixSetElement = contentsElement
                        .Elements(wmts + "TileMatrixSet")
                        .FirstOrDefault(s => s.Element(ows + "Identifier")?.Value == tileMatrixSetId);


                    if (tileMatrixSetElement == null)
                    {
                        continue;
                    }
                    WmtsTileMatrixSet MatrixSet = WmtsTileMatrixSet.ReadTileMatrixSet(tileMatrixSetElement);

                    var TilesMatrixSetLink = layerElement?.Elements(wmts + "TileMatrixSetLink");
                    foreach(var TileMatrixSetLink in TilesMatrixSetLink)
                    {

                        var CurrentTileMatrixSetId = TileMatrixSetLink?.Element(wmts + "TileMatrixSet")?.Value;
                        if (CurrentTileMatrixSetId == tileMatrixSetId)
                        {
                            var TileMatrixSetLimits = TileMatrixSetLink?.Element(wmts + "TileMatrixSetLimits");
                            foreach(var Matrix in MatrixSet.TileMatrixes) { 
                                var TileMatrixLimits = TileMatrixSetLimits?.Elements(wmts + "TileMatrixLimits").Where(element=> element?.Element(wmts + "TileMatrix")?.Value == (Matrix.LevelPrefix + Matrix.Level))?.FirstOrDefault();
                                if (TileMatrixLimits is null)
                                {
                                    continue;
                                }
                                
                                _ = int.TryParse(TileMatrixLimits?.Element(wmts + "MinTileRow")?.Value, out int MinTileRow);
                                _ = int.TryParse(TileMatrixLimits?.Element(wmts + "MaxTileRow")?.Value, out int MaxTileRow);
                                _ = int.TryParse(TileMatrixLimits?.Element(wmts + "MinTileCol")?.Value, out int MinTileCol);
                                _ = int.TryParse(TileMatrixLimits?.Element(wmts + "MaxTileCol")?.Value, out int MaxTileCol);
                                Matrix.MinTileRow = MinTileRow;
                                Matrix.MaxTileRow = MaxTileRow;
                                Matrix.MinTileCol = MinTileCol;
                                Matrix.MaxTileCol = MaxTileCol;
                                Matrix.Zoom = Matrix.Level;
                            }
                        }
                    }

                    tileMatrixSets.Add(MatrixSet);
                }



                ListOfWMTSParser.Add(new WMTSParser()
                {
                    LayerIdentifier = LayerIdentifier,
                    LayerTitle = LayerTitle,
                    LayerAbstract = LayerAbstract,
                    UrlTemplate = urlTemplate.urlTemplate,
                    LayerFormat = urlTemplate.layerFormat,
                    LayerStyle = StyleName,
                    TileMatrixSets = tileMatrixSets,
                    WGS84BoundingBox = boundingBox,
                    LayerKeyWords = ListOfKeywords.ToArray(),
                });
            }
            return ListOfWMTSParser;
        }

        private static (string urlTemplate, string layerFormat) ReadUrlTemplate(XElement capabilitiesElement, XElement layerElement, string layer, string style, string capabilitiesUrl)
        {
            const string formatPng = "image/png";
            const string formatJpg = "image/jpeg";

            string urlTemplate = null;
            string layerFormat = formatJpg;

            var resourceUrls = layerElement
                .Elements(wmts + "ResourceURL")
                .Where(r => r.Attribute("resourceType")?.Value == "tile" &&
                            r.Attribute("format")?.Value != null &&
                            r.Attribute("template")?.Value != null)
                .ToLookup(r => r?.Attribute("format")?.Value,
                          r => r?.Attribute("template")?.Value);

            if (resourceUrls.Any())
            {
                IEnumerable<string> urlTemplates;

                if (resourceUrls.Contains(formatPng))
                {
                    layerFormat = formatPng;
                    urlTemplates = resourceUrls[formatPng];
                }
                else if (resourceUrls.Contains(formatJpg))
                {
                    layerFormat = formatJpg;
                    urlTemplates = resourceUrls[formatJpg];
                }
                else
                {
                    layerFormat = formatJpg;
                    urlTemplates = resourceUrls.First();
                }
                urlTemplate = urlTemplates?.First();

                urlTemplate = urlTemplate?.Replace("{Style}", style);
                urlTemplate = urlTemplate?.Replace("{style}", style);

                UriBuilder uriBuilder = new UriBuilder(urlTemplate);
                uriBuilder.Path = uriBuilder.Path.Replace("//", "/");
                urlTemplate = uriBuilder.Uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.Unescaped);
            }
            else
            {
                if (capabilitiesElement is null)
                {
                    return (string.Empty, string.Empty);
                }
                urlTemplate = capabilitiesElement
                    .Elements(ows + "OperationsMetadata")
                    .Elements(ows + "Operation")
                    .Where(o => o.Attribute("name")?.Value == "GetTile")
                    .Elements(ows + "DCP")
                    .Elements(ows + "HTTP")
                    .Elements(ows + "Get")
                    .Where(g => g.Elements(ows + "Constraint")
                                 .Any(con => con.Attribute("name")?.Value == "GetEncoding" &&
                                             con.Element(ows + "AllowedValues")?.Element(ows + "Value")?.Value == "KVP"))
                    .Select(g => g.Attribute(xlink + "href")?.Value)
                    .Where(h => !string.IsNullOrEmpty(h))
                    .Select(h => h?.Split('?')[0])
                    .FirstOrDefault() ?? string.Empty;

                if (urlTemplate == null && capabilitiesUrl != null && capabilitiesUrl.Contains("Request=GetCapabilities", StringComparison.OrdinalIgnoreCase))
                {
                    urlTemplate = capabilitiesUrl.Split('?')[0];
                }

                if (urlTemplate != null)
                {
                    var formats = layerElement.Elements(wmts + "Format").Select(f => f.Value);

                    var format = formats.Contains(formatPng) ? formatPng
                               : formats.Contains(formatJpg) ? formatJpg
                               : formats.FirstOrDefault();

                    if (string.IsNullOrEmpty(format))
                    {
                        format = formatPng;
                    }
                    layerFormat = format;

                    if (string.IsNullOrEmpty(style))
                    {
                        style = string.Empty;
                    }
                    else
                    {
                        style = "&Style=" + style;
                    }

                    urlTemplate += "?Service=WMTS"
                        + "&Request=GetTile"
                        + "&Version=1.0.0"
                        + "&Layer=" + layer
                        + style
                        + "&Format=" + format
                        + "&TileMatrixSet={TileMatrixSet}"
                        + "&TileMatrix={TileMatrix}"
                        + "&TileRow={TileRow}"
                        + "&TileCol={TileCol}";
                }
            }

            if (string.IsNullOrEmpty(urlTemplate))
            {
                throw new ArgumentException($"No ResourceURL element in Layer \"{layer}\" and no GetTile KVP Operation Metadata found.");
            }

            return (urlTemplate, layerFormat);
        }
    }
}
