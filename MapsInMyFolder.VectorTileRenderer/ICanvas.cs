using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.VectorTileRenderer
{
    public interface ICanvas
    {
        bool ClipOverflow { get; set; }

        void StartDrawing(double sizeX, double sizeY);

        void DrawBackground(Brush style);

        void DrawLineString(List<Point> geometry, Brush style);

        void DrawPolygon(List<Point> geometry, Brush style);

        void DrawPoint(Point geometry, Brush style);
        void DrawTextOnCanvas(Renderer.Collisions textElements, Renderer.ROptions options);

        Renderer.Collisions DrawText(Point geometry, Brush style, Renderer.ROptions options, Renderer.Collisions collisions, int hatchCode);

        Renderer.Collisions DrawTextOnPath(List<Point> geometry, Brush style, Renderer.ROptions options, Renderer.Collisions collisions, int hatchCode);

        void DrawImage(Stream imageStream, Brush style);

        void DrawUnknown(List<List<Point>> geometry, Brush style);

        BitmapSource FinishDrawing();
    }
}
