using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System.Collections.Generic;

namespace MapsInMyFolder
{
    public class MapFigures
    {
        private readonly List<Figure> mapPolygons = new List<Figure>();

        public class Figure
        {
            public MapPolygon Polygon { get; set; }
            public int MinZoom { get; set; }
            public int MaxZoom { get; set; }
            public double StrokeThickness { get; set; }
            public Location NO { get; set; }
            public Location SE { get; set; }
            public string Name { get; set; }
            public string Color { get; set; }

            public Figure(MapPolygon polygon, string name, Location NO, Location SE, int minZoom, int maxZoom, double strokeThickness, string color)
            {
                Polygon = polygon;
                MinZoom = minZoom;
                MaxZoom = maxZoom;
                StrokeThickness = strokeThickness;
                this.NO = NO;
                this.SE = SE;
                Name = name;
                Color = color;
            }


            public bool Contains(Location OtherNO, Location OtherSE)
            {
                if (OtherNO.Latitude >= NO.Latitude &&
                    OtherNO.Longitude >= NO.Longitude &&
                    OtherSE.Latitude <= SE.Latitude &&
                    OtherSE.Longitude <= SE.Longitude)
                {
                    return true;
                }

                return false;
            }
        }

        public void DrawFigure(MapItemsControl map, Figure figure)
        {
            mapPolygons.Add(figure);
            map.Items.Add(figure.Polygon);
        }

        public void RemoveFigure(MapItemsControl map, Figure figure)
        {
            mapPolygons.Remove(figure);
            map.Items.Remove(figure.Polygon);
        }

        public void ClearFigures(MapItemsControl map)
        {
            Figure[] mapPolygonsCopy = mapPolygons.ToArray();
            mapPolygons.Clear();
            foreach (Figure figure in mapPolygonsCopy)
            {
                map.Items.Remove(figure.Polygon);
            }
        }

        public void UpdateFiguresFromZoomLevel(double zoomLevel)
        {
            foreach (Figure figure in mapPolygons)
            {
                if (figure.MinZoom != -1 && figure.MinZoom > zoomLevel)
                {
                    figure.Polygon.Visibility = System.Windows.Visibility.Collapsed;
                    continue;
                }

                if (figure.MaxZoom != -1 && figure.MaxZoom < zoomLevel)
                {
                    figure.Polygon.Visibility = System.Windows.Visibility.Collapsed;
                    continue;
                }
                figure.Polygon.Visibility = System.Windows.Visibility.Visible;
            }
        }

        public static IEnumerable<Figure> GetFiguresFromJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                yield break;
            }

            List<Dictionary<string, string>> rectangleSelection;
            try
            {
                rectangleSelection = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                Javascript.Functions.PrintError(ex.Message);
                yield break;
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                Javascript.Functions.PrintError(ex.Message);
                yield break;
            }

            foreach (Dictionary<string, string> elementsForRectangleSelection in rectangleSelection)
            {
                MapPolygon rectangle = new MapPolygon();

                if (!elementsForRectangleSelection.TryGetValue("NO_Lat", out string NO_Lat_str)) continue;
                if (!elementsForRectangleSelection.TryGetValue("NO_Long", out string NO_Long_str)) continue;
                if (!elementsForRectangleSelection.TryGetValue("SE_Lat", out string SE_Lat_str)) continue;
                if (!elementsForRectangleSelection.TryGetValue("SE_Long", out string SE_Long_str)) continue;

                elementsForRectangleSelection.TryGetValue("Name", out string name);
                elementsForRectangleSelection.TryGetValue("MinZoom", out string MinZoomStr);
                elementsForRectangleSelection.TryGetValue("MaxZoom", out string MaxZoomStr);
                elementsForRectangleSelection.TryGetValue("StrokeThickness", out string StrokeThicknessStr);
                elementsForRectangleSelection.TryGetValue("Color", out string color);

                if (!double.TryParse(NO_Lat_str, out double NO_Lat)) continue;
                if (!double.TryParse(NO_Long_str, out double NO_Long)) continue;
                if (!double.TryParse(SE_Lat_str, out double SE_Lat)) continue;
                if (!double.TryParse(SE_Long_str, out double SE_Long)) continue;

                if (!int.TryParse(MinZoomStr, out int MinZoom))
                {
                    MinZoom = -1;
                }

                if (!double.TryParse(StrokeThicknessStr, out double StrokeThickness))
                {
                    StrokeThickness = 1;
                }
                if (!int.TryParse(MaxZoomStr, out int MaxZoom))
                {
                    MaxZoom = -1;
                };

                Location NO = new Location(NO_Lat, NO_Long);
                Location SE = new Location(SE_Lat, SE_Long);
                rectangle.Locations = new List<Location>() { NO, new Location(SE.Latitude, NO.Longitude), SE, new Location(NO.Latitude, SE.Longitude) };
                if (MapSelectable.IsZeroWidthRectangle(NO, SE))
                {
                    yield break;
                }
                yield return new Figure(rectangle, name, NO, SE, MinZoom, MaxZoom, StrokeThickness, color);
            }
        }

        public void DrawFigureOnMapItemsControlFromJsonString(MapItemsControl mapviewerRectangles, string figuresJsonString, double zoomLevel)
        {
            ClearFigures(mapviewerRectangles);
            foreach (Figure figure in GetFiguresFromJsonString(figuresJsonString))
            {
                MapPolygon polygon = figure.Polygon;
                polygon.Stroke = Collectif.HexValueToSolidColorBrush(figure.Color, "#000");
                polygon.StrokeThickness = figure.StrokeThickness;
                polygon.IsHitTestVisible = false;
                figure.Polygon = polygon;
                DrawFigure(mapviewerRectangles, figure);
            }
            UpdateFiguresFromZoomLevel(zoomLevel);
        }
    }
}
