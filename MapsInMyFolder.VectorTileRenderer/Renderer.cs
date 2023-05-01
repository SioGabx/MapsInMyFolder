using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MapsInMyFolder.VectorTileRenderer
{
    public class Renderer
    {
        // private static Object cacheLock = new Object();

        enum VisualLayerType
        {
            Vector,
            Raster,
        }

        public class ICanvasCollisions
        {
            public Collisions ListOfEntitiesCollisions;
            public ICanvas bitmap;
            public ROptions Roptions;

            public ICanvasCollisions(Collisions ListOfEntitiesCollisions, ICanvas bitmap, ROptions Roptions)
            {
                this.ListOfEntitiesCollisions = ListOfEntitiesCollisions;
                this.bitmap = bitmap;
                this.Roptions = Roptions;
            }
        }

        public class TextElements
        {
            public Point geometry;
            public Brush style;
            public int hatchCode;
            public Rect rectangle;
            public bool doDraw;

            public TextElements(Point geometry, Brush style, int hatchCode, Rect rectangle, bool doDraw = true)
            {
                this.geometry = geometry;
                this.style = style;
                this.hatchCode = hatchCode;
                this.rectangle = rectangle;
                this.doDraw = doDraw;
            }
        }

        public class Collisions
        {
            public List<int> CollisionEntity = new List<int>();
            public List<TextElements> TextElementsList = new List<TextElements>();
        }

        public class ROptions
        {
            public int ImgPositionX;
            public int ImgPositionY;
            public int NbrTileHeightWidth;
            public bool GenerateCanvas;
            public double OverflowTextCorrectingValue;
            public double TextSizeMultiplicateur;
            public int TileSize;
            public int ImgCenterPositionX;
            public int ImgCenterPositionY;

            public ROptions(int ImgPositionX = 0, int ImgPositionY = 0, int ImgCenterPositionX = 0, int ImgCenterPositionY = 0, int NbrTileHeightWidth = 1, int TileSize = 256, bool GenerateCanvas = true, double OverflowTextCorrectingValue = 0.2, double TextSizeMultiplicateur = 1)
            {
                this.OverflowTextCorrectingValue = OverflowTextCorrectingValue;
                this.ImgPositionX = ImgPositionX;
                this.ImgPositionY = ImgPositionY;
                this.ImgCenterPositionX = ImgCenterPositionX;
                this.ImgCenterPositionY = ImgCenterPositionY;
                this.GenerateCanvas = GenerateCanvas;
                this.NbrTileHeightWidth = NbrTileHeightWidth;
                this.TileSize = TileSize;
                this.TextSizeMultiplicateur = TextSizeMultiplicateur;
            }
        }

        class VisualLayer
        {
            public VisualLayerType Type { get; set; }

            public Stream RasterStream { get; set; } = null;

            public VectorTileFeature VectorTileFeature { get; set; } = null;

            public List<List<Point>> Geometry { get; set; } = null;
            public List<List<Point>> OriginalGeometry { get; set; } = null;

            public Brush Brush { get; set; } = null;
        }

        //public async static Task<BitmapSource> RenderCached(string cachePath, Style style, ICanvas canvas, int x, int y, double zoom, double scale = 1, List<string> whiteListLayers = null)
        //{
        //    string layerString = whiteListLayers == null ? "" : string.Join(",-", whiteListLayers.ToArray());

        //    var bundle = new
        //    {
        //        style.Hash,
        //        scale,
        //        layerString,
        //    };

        //    lock (cacheLock)
        //    {
        //        if (!Directory.Exists(cachePath))
        //        {
        //            Directory.CreateDirectory(cachePath);
        //        }
        //    }

        //    var json = Newtonsoft.Json.JsonConvert.SerializeObject(bundle);
        //    var hash = Utils.Sha256(json).Substring(0, 12); // get 12 digits to avoid fs length issues

        //    var fileName = x + "x" + y + "-" + zoom + "-" + hash + ".png";
        //    var path = Path.Combine(cachePath, fileName);

        //    lock (cacheLock)
        //    {
        //        if (File.Exists(path))
        //        {
        //            return LoadBitmap(path);
        //        }
        //    }

        //    var bitmapa = await Render(style, canvas, x, y, zoom, scale, whiteListLayers, collisions:null);

        //    var bitmap = bitmapa.bitmap.FinishDrawing();
        //    // save to file in async fashion
        //    var _t = Task.Run(() =>
        //      {

        //          if (bitmap != null)
        //          {
        //              try
        //              {
        //                  lock (cacheLock)
        //                  {
        //                      if (File.Exists(path))
        //                      {
        //                          return;
        //                      }

        //                      using (var fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
        //                      {
        //                          BitmapEncoder encoder = new PngBitmapEncoder();
        //                          encoder.Frames.Add(BitmapFrame.Create(bitmap));
        //                          encoder.Save(fileStream);
        //                      }
        //                  }
        //              }
        //              catch (Exception)
        //              {
        //                  return;
        //              }
        //          }

        //      });

        //    return bitmap;
        //}

        //static BitmapSource LoadBitmap(string path)
        //{
        //    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //    {
        //        var fsBitmap = new BitmapImage();
        //        fsBitmap.BeginInit();
        //        fsBitmap.StreamSource = stream;
        //        fsBitmap.CacheOption = BitmapCacheOption.OnLoad;
        //        fsBitmap.EndInit();
        //        fsBitmap.Freeze();

        //        return fsBitmap;
        //    }
        //}

        public async static Task<MapsInMyFolder.VectorTileRenderer.Renderer.ICanvasCollisions> Render(Style style, ICanvas canvas, int x, int y, double zoom, double scale = 1, List<string> whiteListLayers = null, ROptions options = null, Collisions collisions = null)
        {
            Dictionary<Source, Stream> rasterTileCache = new Dictionary<Source, Stream>();
            Dictionary<Source, VectorTile> vectorTileCache = new Dictionary<Source, VectorTile>();
            Dictionary<string, List<VectorTileLayer>> categorizedVectorLayers = new Dictionary<string, List<VectorTileLayer>>();
            if (options == null)
            {
                options = new ROptions
                {
                    NbrTileHeightWidth = 1
                };
            }
            //ICanvasCollisions ReturncanvasCollisions = new ICanvasCollisions() { };
            if (collisions is null)
            {
                collisions = new Collisions();
            }

            double actualZoom = 0;
            if (options.TileSize == 0)
            {
                options.TileSize = 256;
            }
            if (options.TileSize < 1024)
            {
                var ratio = 1024 / options.TileSize;
                var zoomDelta = Math.Log(ratio, 2);

                actualZoom = zoom - zoomDelta;
            }
            //Debug.WriteLine("Zoom ini :" + zoom + "   Zoom act : " + actualZoom);
            double sizeX = options.TileSize;
            double sizeY = options.TileSize;
            if (options is null || options.GenerateCanvas)
            {
                canvas.StartDrawing(options.TileSize * options.NbrTileHeightWidth, options.TileSize * options.NbrTileHeightWidth);
                sizeX *= scale;
                sizeY *= scale;
            }

            var visualLayers = new List<VisualLayer>();

            foreach (var layer in style.Layers)
            {
                if (whiteListLayers != null && layer.Type != "background" && layer.SourceLayer != "")
                {
                    if (!whiteListLayers.Contains(layer.SourceLayer))
                    {
                        Debug.WriteLine("ignoring " + layer.SourceLayer);
                        continue;
                    }
                }
                if (layer.Source != null)
                {
                    if (layer.Source.Type == "vector")
                    {
                        if (!vectorTileCache.ContainsKey(layer.Source) && layer.Source.Provider is Sources.IVectorTileSource source)
                        {
                            var vectorTile = await source.GetVectorTile(x, y, (int)zoom);

                            if (vectorTile == null)
                            {
                                return null;
                                // throwing exceptions screws up the performance
                                throw new FileNotFoundException("Could not load tile : " + x + "," + y + "," + zoom + " of " + layer.SourceName);
                            }

                            // magic sauce! :p
                            if (vectorTile.IsOverZoomed)
                            {
                                canvas.ClipOverflow = false;
                            }

                            //Debug.WriteLine("Position de la tuile " + options.ImgPositionX + "/" + options.ImgPositionY + " : \n ImgPositionX" + sizeX * options.ImgPositionX + "\n ImgPositionY" + sizeX * options.ImgPositionY);
                            //canvas.ClipOverflow = true;

                            vectorTileCache[layer.Source] = vectorTile;

                            // normalize the points from 0 to size
                            foreach (var vectorLayer in vectorTile.Layers)
                            {
                                foreach (var feature in vectorLayer.Features)
                                {
                                    feature.OriginalGeometry = feature.Geometry;
                                    foreach (var geometry in feature.Geometry)
                                    {
                                        for (int i = 0; i < geometry.Count; i++)
                                        {
                                            var point = geometry[i];
                                            Double GeoPtx = point.X / feature.Extent * sizeX;
                                            Double GeoPty = point.Y / feature.Extent * sizeY;
                                            //Debug.WriteLine("tile.Layers ImgPositionX :" + options.ImgPositionX);
                                            //Debug.WriteLine("tile.Layers ImgPositionY :" + options.ImgPositionY);
                                            //Deplacement des points au millieu de la tuile

                                            //Debug.WriteLine("Origine x= " + GeoPtx + " / y=" + GeoPty);
                                            //Debug.WriteLine("sizeX = " + sizeX + " / ImgPositionX=" + options.ImgPositionX);
                                            //Debug.WriteLine("sizeY = " + sizeY + " / ImgPositionY=" + options.ImgPositionY);
                                            GeoPtx += sizeX * options.ImgPositionX;
                                            GeoPty += sizeY * options.ImgPositionY;

                                            //Debug.WriteLine("Point x= " + GeoPtx + " / y=" + GeoPty);
                                            geometry[i] = new Point(GeoPtx, GeoPty);
                                        }
                                    }
                                }
                            }

                            foreach (var tileLayer in vectorTile.Layers)
                            {
                                if (!categorizedVectorLayers.ContainsKey(tileLayer.Name))
                                {
                                    categorizedVectorLayers[tileLayer.Name] = new List<VectorTileLayer>();
                                }
                                categorizedVectorLayers[tileLayer.Name].Add(tileLayer);
                            }
                        }
                    }
                    else if (layer.Source.Type == "raster")
                    {
                        if (!rasterTileCache.ContainsKey(layer.Source))
                        {
                            if (layer.Source.Provider != null)
                            {
                                if (layer.Source.Provider is Sources.ITileSource)
                                {
                                    var tile = await layer.Source.Provider.GetTile(x, y, (int)zoom);

                                    if (tile == null)
                                    {
                                        continue;
                                        // throwing exceptions screws up the performance
                                        throw new FileNotFoundException("Could not load tile : " + x + "," + y + "," + zoom + " of " + layer.SourceName);
                                    }

                                    rasterTileCache[layer.Source] = tile;
                                }
                            }
                        }

                        if (rasterTileCache.ContainsKey(layer.Source))
                        {
                            if (style.ValidateLayer(layer, (int)zoom, null))
                            {
                                var brush = style.ParseStyle(layer, scale, new Dictionary<string, object>());

                                if (!brush.Paint.Visibility)
                                {
                                    continue;
                                }

                                visualLayers.Add(new VisualLayer()
                                {
                                    Type = VisualLayerType.Raster,
                                    RasterStream = rasterTileCache[layer.Source],
                                    Brush = brush,
                                });
                            }
                        }
                    }

                    if (categorizedVectorLayers.ContainsKey(layer.SourceLayer))
                    {
                        var tileLayers = categorizedVectorLayers[layer.SourceLayer];

                        foreach (var tileLayer in tileLayers)
                        {
                            foreach (var feature in tileLayer.Features)
                            {
                                //var geometry = localizeGeometry(feature.Geometry, sizeX, sizeY, feature.Extent);
                                var attributes = new Dictionary<string, object>(feature.Attributes)
                                {
                                    ["$type"] = feature.GeometryType,
                                    ["$id"] = layer.ID,
                                    ["$zoom"] = actualZoom
                                };

                                //if ((string)attributes["$type"] == "Point")
                                //{
                                //    if (attributes.ContainsKey("class"))
                                //    {
                                //        if ((string)attributes["class"] == "country")
                                //        {
                                //            if (layer.ID == "country_label")
                                //            {

                                //            }
                                //        }
                                //    }
                                //}

                                if (style.ValidateLayer(layer, actualZoom, attributes))
                                {
                                    var brush = style.ParseStyle(layer, scale, attributes);

                                    if (!brush.Paint.Visibility)
                                    {
                                        continue;
                                    }

                                    visualLayers.Add(new VisualLayer()
                                    {
                                        Type = VisualLayerType.Vector,
                                        VectorTileFeature = feature,
                                        Geometry = feature.Geometry,
                                        OriginalGeometry = feature.OriginalGeometry,
                                        Brush = brush,
                                    });
                                }
                            }
                        }
                    }
                }
                else if (layer.Type == "background")
                {
                    if (options.GenerateCanvas)
                    {
                        var brushes = style.GetStyleByType("background", actualZoom, scale);
                        foreach (var brush in brushes)
                        {
                            canvas.DrawBackground(brush);
                        }
                    }
                }
            }

            // defered rendering to preserve text drawing order
            foreach (var layer in visualLayers.OrderBy(item => item.Brush.ZIndex))
            {
                if (layer.Type == VisualLayerType.Vector)
                {
                    var feature = layer.VectorTileFeature;

                    var geometry = layer.Geometry;
                    var brush = layer.Brush;

                    var attributesDict = feature.Attributes.ToDictionary(key => key.Key, value => value.Value);

                    if (!brush.Paint.Visibility)
                    {
                        //continue; test
                    }

                    try
                    {
                        if (feature.GeometryType == "Point")
                        {
                            foreach (var point in geometry)
                            {
                                canvas.DrawPoint(point[0], brush);
                            }
                        }
                        else if (feature.GeometryType == "LineString")
                        {
                            if (options.GenerateCanvas)
                            {
                                foreach (var line in geometry)
                                {
                                    canvas.DrawLineString(line, brush);
                                }
                            }
                        }
                        else if (feature.GeometryType == "Polygon")
                        {
                            if (options.GenerateCanvas)
                            {
                                foreach (var polygon in geometry)
                                {
                                    canvas.DrawPolygon(polygon, brush);
                                }
                            }
                        }
                        else if (feature.GeometryType == "Unknown")
                        {
                            Debug.WriteLine("Draw unknown 2 " + feature.Attributes.ToString());
                            canvas.DrawUnknown(geometry, brush);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                else if (layer.Type == VisualLayerType.Raster)
                {
                    canvas.DrawImage(layer.RasterStream, layer.Brush);
                    layer.RasterStream.Close();
                }
                else
                {
                    Debug.WriteLine("Unknown layer type");
                }
            }

            foreach (var layer in visualLayers.OrderByDescending(item => item.Brush.ZIndex))
            {
                if (layer.Type == VisualLayerType.Vector)
                {
                    var feature = layer.VectorTileFeature;
                    var geometry = layer.Geometry;
                    var originalgeometry = layer.OriginalGeometry;
                    var brush = layer.Brush;

                    string hatch = brush.Layer.SourceLayer + brush.Text + brush.ZIndex.ToString() + brush.Paint + originalgeometry.ToString();
                    int hatchCode = hatch.GetHashCode();

                    var attributesDict = feature.Attributes.ToDictionary(key => key.Key, value => value.Value);

                    if (!brush.Paint.Visibility)
                    {
                        //continue; test
                    }

                    if (feature.GeometryType == "Point")
                    {
                        foreach (var point in geometry)
                        {
                            if (brush.Text != null)
                            {
                                //Debug.WriteLine("Write P " + brush.Text);
                                brush.ZIndex = 999;
                                collisions = canvas.DrawText(point[0], brush, options, collisions, hatchCode);
                            }
                        }
                    }
                    else if (feature.GeometryType == "LineString")
                    {
                        foreach (var line in geometry)
                        {
                            if (brush.Text != null)
                            {
                                //Debug.WriteLine("Write LS " + brush.Text);
                                collisions = canvas.DrawTextOnPath(line, brush, options, collisions, hatchCode);
                            }
                        }
                    }
                    else
                    {
                        foreach (var line in geometry)
                        {
                            if (brush.Text != null)
                            {
                                Debug.WriteLine("Write Ignore " + brush.Text);
                            }
                        }
                    }
                }
            }

            return new Renderer.ICanvasCollisions(collisions, canvas, options);
        }

        //private static List<List<Point>> LocalizeGeometry(List<List<Point>> coordinates, double sizeX, double sizeY, double extent)
        //{
        //    return coordinates.Select(list =>
        //    {
        //        return list.Select(point =>
        //        {
        //            Point newPoint = new Point(0, 0);

        //            var x = Utils.ConvertRange(point.X, 0, extent, 0, sizeX, false);
        //            var y = Utils.ConvertRange(point.Y, 0, extent, 0, sizeY, false);

        //            newPoint.X = x;
        //            newPoint.Y = y;

        //            return newPoint;
        //        }).ToList();
        //    }).ToList();
        //}

    }
}
