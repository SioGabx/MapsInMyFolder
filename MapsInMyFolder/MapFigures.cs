using MapsInMyFolder.Commun;
using MapsInMyFolder.MapControl;
using System.Collections.Generic;

namespace MapsInMyFolder
{

    public class MapFigures
    {
        private List<Figure> mapPolygons = new List<Figure>();

        public class Figure
        {
            public MapPolygon polygon;
            public int MinZoom;
            public int MaxZoom;
            public double StrokeThickness;
            public Location NO;
            public Location SE;
            public string Name;
            public string Color;
            public Figure(MapPolygon polygon, string Name, Location NO, Location SE, int MinZoom, int MaxZoom, double StrokeThickness, string Color)
            {
                this.polygon = polygon;
                this.MinZoom = MinZoom;
                this.MaxZoom = MaxZoom;
                this.NO = NO;
                this.SE = SE;
                this.Name = Name;
                this.StrokeThickness = StrokeThickness;
                this.Color = Color;
            }
        }

        public void DrawFigure(MapItemsControl map, Figure figure)
        {
            mapPolygons.Add(figure);
            map.Items.Add(figure.polygon);
        }

        public void RemoveFigure(MapItemsControl map, Figure figure)
        {
            mapPolygons.Remove(figure);
            map.Items.Remove(figure.polygon);
        }

        public void ClearFigures(MapItemsControl map)
        {
            Figure[] mapPolygonsCopy = mapPolygons.ToArray();
            mapPolygons.Clear();
            foreach (Figure figure in mapPolygonsCopy)
            {
                map.Items.Remove(figure.polygon);
            }
        }

        public void UpdateFiguresFromZoomLevel(double ZoomLevel)
        {
            foreach(Figure figure in mapPolygons)
            {
                if (figure.MinZoom != -1 && figure.MinZoom > ZoomLevel)
                {
                    figure.polygon.Visibility = System.Windows.Visibility.Collapsed;
                    continue;
                }
                
                if (figure.MaxZoom != -1 && figure.MaxZoom < ZoomLevel)
                {
                    figure.polygon.Visibility = System.Windows.Visibility.Collapsed;
                    continue;
                }
                figure.polygon.Visibility = System.Windows.Visibility.Visible;
            }
        }

        public static IEnumerable<Figure> GetFiguresFromJsonString(string json)
        {
            foreach (Dictionary<string, string> ElementsForRectangleSelection in Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>())
            {
                MapPolygon Rectangle = new MapPolygon();

                if (!ElementsForRectangleSelection.TryGetValue("NO_Lat", out string NO_Lat_str)) continue;
                if (!ElementsForRectangleSelection.TryGetValue("NO_Long", out string NO_Long_str)) continue;
                if (!ElementsForRectangleSelection.TryGetValue("SE_Lat", out string SE_Lat_str)) continue;
                if (!ElementsForRectangleSelection.TryGetValue("SE_Long", out string SE_Long_str)) continue;

                ElementsForRectangleSelection.TryGetValue("Name", out string Name);
                ElementsForRectangleSelection.TryGetValue("MinZoom", out string MinZoomStr);
                ElementsForRectangleSelection.TryGetValue("MaxZoom", out string MaxZoomStr);
                ElementsForRectangleSelection.TryGetValue("StrokeThickness", out string StrokeThicknessStr);
                ElementsForRectangleSelection.TryGetValue("Color", out string Color);
               
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
                Rectangle.Locations = new List<Location>() { NO, new Location(SE.Latitude, NO.Longitude), SE, new Location(NO.Latitude, SE.Longitude) };

                yield return new Figure(Rectangle, Name, NO, SE, MinZoom, MaxZoom, StrokeThickness, Color);
            }
        }


        public void DrawFigureOnMapItemsControlFromJsonString(MapItemsControl mapviewerRectangles, string FiguresJsonString)
        {
            ClearFigures(mapviewerRectangles);
            foreach (MapFigures.Figure Figure in MapFigures.GetFiguresFromJsonString(FiguresJsonString))
            {
                MapPolygon polygon = Figure.polygon;
                polygon.Stroke = Collectif.HexValueToSolidColorBrush(Figure.Color, "#000");
                polygon.StrokeThickness = Figure.StrokeThickness;
                polygon.IsHitTestVisible = false;
                Figure.polygon = polygon;
                DrawFigure(mapviewerRectangles, Figure);
            }
        }





    }

}
