using ClipperLib;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.VectorTileRenderer
{
    public class SkiaCanvas : ICanvas
    {
        int width;
        int height;

        WriteableBitmap bitmap;
        SKSurface surface;
        SKCanvas canvas;

        public bool ClipOverflow { get; set; } = false;
        private Rect clipRectangle;
        List<IntPoint> clipRectanglePath;

        ConcurrentDictionary<string, SKTypeface> fontPairs = new ConcurrentDictionary<string, SKTypeface>();
        private static readonly Object fontLock = new Object();

        List<Rect> textRectangles = new List<Rect>();

        public void StartDrawing(double width, double height)
        {
            this.width = (int)width;
            this.height = (int)height;

            bitmap = new WriteableBitmap(this.width, this.height, 96, 96, PixelFormats.Pbgra32, null);
            bitmap.Lock();
            var info = new SKImageInfo(this.width, this.height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            //var glInterface = GRGlInterface.CreateNativeGlInterface();
            //grContext = GRContext.Create(GRBackend.OpenGL, glInterface);

            //renderTarget = SkiaGL.CreateRenderTarget();
            //renderTarget.Width = this.width;
            //renderTarget.Height = this.height;


            surface = SKSurface.Create(info, bitmap.BackBuffer, bitmap.BackBufferStride);

            //surface = SKSurface.Create(grContext, renderTarget);
            canvas = surface.Canvas;

            double padding = -5;
            clipRectangle = new Rect(padding, padding, this.width - padding * 2, this.height - padding * 2);

            clipRectanglePath = new List<IntPoint>();
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top, (int)clipRectangle.Left));
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top, (int)clipRectangle.Right));
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom, (int)clipRectangle.Right));
            clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom, (int)clipRectangle.Left));

            //clipRectanglePath = new List<IntPoint>();
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top + 10, (int)clipRectangle.Left + 10));
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Top + 10, (int)clipRectangle.Right - 10));
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom - 10, (int)clipRectangle.Right - 10));
            //clipRectanglePath.Add(new IntPoint((int)clipRectangle.Bottom - 10, (int)clipRectangle.Left + 10));
        }

        public void DrawBackground(Brush style)
        {
            canvas.Clear(new SKColor(style.Paint.BackgroundColor.R, style.Paint.BackgroundColor.G, style.Paint.BackgroundColor.B, style.Paint.BackgroundColor.A));
        }

        SKStrokeCap ConvertCap(PenLineCap cap)
        {
            if (cap == PenLineCap.Flat)
            {
                return SKStrokeCap.Butt;
            }
            else if (cap == PenLineCap.Round)
            {
                return SKStrokeCap.Round;
            }

            return SKStrokeCap.Square;
        }

        //private double getAngle(double x1, double y1, double x2, double y2)
        //{
        //    double degrees;

        //    if (x2 - x1 == 0)
        //    {
        //        if (y2 > y1)
        //            degrees = 90;
        //        else
        //            degrees = 270;
        //    }
        //    else
        //    {
        //        // Calculate angle from offset.
        //        double riseoverrun = (y2 - y1) / (x2 - x1);
        //        double radians = Math.Atan(riseoverrun);
        //        degrees = radians * (180 / Math.PI);

        //        if ((x2 - x1) < 0 || (y2 - y1) < 0)
        //            degrees += 180;
        //        if ((x2 - x1) > 0 && (y2 - y1) < 0)
        //            degrees -= 180;
        //        if (degrees < 0)
        //            degrees += 360;
        //    }
        //    return degrees;
        //}

        //private double getAngleAverage(double a, double b)
        //{
        //    a = a % 360;
        //    b = b % 360;

        //    double sum = a + b;
        //    if (sum > 360 && sum < 540)
        //    {
        //        sum = sum % 180;
        //    }
        //    return sum / 2;
        //}

        double Clamp(double number, double min = 0, double max = 1)
        {
            return Math.Max(min, Math.Min(max, number));
        }

        List<List<Point>> ClipPolygon(List<Point> geometry) // may break polygons into multiple ones
        {
            Clipper c = new Clipper();

            var polygon = new List<IntPoint>();

            foreach (var point in geometry)
            {
                polygon.Add(new IntPoint((int)point.X, (int)point.Y));
            }

            c.AddPolygon(polygon, PolyType.ptSubject);

            c.AddPolygon(clipRectanglePath, PolyType.ptClip);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();

            bool success = c.Execute(ClipType.ctIntersection, solution, PolyFillType.pftNonZero, PolyFillType.pftEvenOdd);

            if (success && solution.Count > 0)
            {
                var result = solution.Select(s => s.Select(item => new Point(item.X, item.Y)).ToList()).ToList();
                return result;
            }

            return null;
        }

        List<Point> ClipLine(List<Point> geometry)
        {
            return LineClipper.ClipPolyline(geometry, clipRectangle);
        }

        SKPath GetPathFromGeometry(List<Point> geometry)
        {

            SKPath path = new SKPath
            {
                FillType = SKPathFillType.EvenOdd,
            };

            var firstPoint = geometry[0];

            path.MoveTo((float)firstPoint.X, (float)firstPoint.Y);
            foreach (var point in geometry.Skip(1))
            {
                var lastPoint = path.LastPoint;
                path.LineTo((float)point.X, (float)point.Y);
            }

            return path;
        }

        public void DrawLineString(List<Point> geometry, Brush style)
        {
            if (ClipOverflow)
            {
                geometry = ClipLine(geometry);
                if (geometry == null)
                {
                    return;
                }
            }

            var path = GetPathFromGeometry(geometry);
            if (path == null)
            {
                return;
            }

            SKPaint fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeCap = ConvertCap(style.Paint.LineCap),
                StrokeWidth = (float)style.Paint.LineWidth,
                Color = new SKColor(style.Paint.LineColor.R, style.Paint.LineColor.G, style.Paint.LineColor.B, (byte)Clamp(style.Paint.LineColor.A * style.Paint.LineOpacity, 0, 255)),
                IsAntialias = true,
            };

            if (style.Paint.LineDashArray.Count() > 0)
            {
                var effect = SKPathEffect.CreateDash(style.Paint.LineDashArray.Select(n => (float)n).ToArray(), 0);
                fillPaint.PathEffect = effect;
            }

            //Console.WriteLine(style.Paint.LineWidth);

            canvas.DrawPath(path, fillPaint);
        }

        SKTextAlign ConvertAlignment(TextAlignment alignment)
        {
            if (alignment == TextAlignment.Center)
            {
                return SKTextAlign.Center;
            }
            else if (alignment == TextAlignment.Left)
            {
                return SKTextAlign.Left;
            }
            else if (alignment == TextAlignment.Right)
            {
                return SKTextAlign.Right;
            }

            return SKTextAlign.Center;
        }

        SKPaint GetTextStrokePaint(Brush style)
        {
            var paint = new SKPaint()
            {
                IsStroke = true,
                StrokeWidth = (float)style.Paint.TextStrokeWidth,
                Color = new SKColor(style.Paint.TextStrokeColor.R, style.Paint.TextStrokeColor.G, style.Paint.TextStrokeColor.B, (byte)Clamp(style.Paint.TextStrokeColor.A * style.Paint.TextOpacity, 0, 255)),
                TextSize = (float)style.Paint.TextSize,
                IsAntialias = true,
                TextEncoding = SKTextEncoding.Utf32,
                TextAlign = ConvertAlignment(style.Paint.TextJustify),
                Typeface = GetFont(style.Paint.TextFont, style),
            };

            return paint;
        }

        SKPaint GetTextPaint(Brush style)
        {
            var paint = new SKPaint()
            {
                Color = new SKColor(style.Paint.TextColor.R, style.Paint.TextColor.G, style.Paint.TextColor.B, (byte)Clamp(style.Paint.TextColor.A * style.Paint.TextOpacity, 0, 255)),
                TextSize = (float)style.Paint.TextSize,
                IsAntialias = true,
                TextEncoding = SKTextEncoding.Utf32,
                TextAlign = ConvertAlignment(style.Paint.TextJustify),
                Typeface = GetFont(style.Paint.TextFont, style),
                HintingLevel = SKPaintHinting.Normal,
            };

            return paint;
        }

        string TransformText(string text, Brush style, Renderer.ROptions options)
        {
            if (text.Length == 0)
            {
                return "";
            }

            if (style.Paint.TextTransform == TextTransform.Uppercase)
            {
                text = text.ToUpper();
            }
            else if (style.Paint.TextTransform == TextTransform.Lowercase)
            {
                text = text.ToLower();
            }

            var paint = GetTextPaint(style);
            paint.TextSize = (float)(paint.TextSize);
          
            text = BreakText(text, paint, style, options);

            return text;
            //return Encoding.UTF32.GetBytes(newText);
        }

        string BreakText(string input, SKPaint paint, Brush style, Renderer.ROptions options)
        {
            //return input;
            var restOfText = input;
            var brokenText = "";
            do
            {
                var lineLength = paint.BreakText(restOfText, (float)(style.Paint.TextMaxWidth * style.Paint.TextSize));

                if (lineLength == restOfText.Length)
                {
                    // its the end
                    brokenText += restOfText.Trim();
                    break;
                }

                var lastSpaceIndex = restOfText.LastIndexOf(' ', (int)(lineLength - 1));
                if (lastSpaceIndex == -1 || lastSpaceIndex == 0)
                {
                    // no more spaces, probably ;)
                    brokenText += restOfText.Trim();
                    break;
                }

                int multiplieur = Math.Max(1,Convert.ToInt32(Math.Round(options.TextSizeMultiplicateur)));
                brokenText += restOfText.Substring(0, (int)lastSpaceIndex).Trim() + string.Concat(Enumerable.Repeat("\n", multiplieur));

                restOfText = restOfText.Substring((int)lastSpaceIndex, restOfText.Length - (int)lastSpaceIndex);

            } while (restOfText.Length > 0);

            return brokenText.Trim();
        }

        bool TextCollides(Rect rectangle)
        {
            foreach (var rect in textRectangles)
            {
                if (rect.IntersectsWith(rectangle))
                {
                    //ignore, add option to hide all text
                    return true;
                }
            }
            return false;
        }

        SKTypeface GetFont(string[] familyNames, Brush style)
        {
            lock (fontLock)
            {
                foreach (var name in familyNames)
                {
                    if (fontPairs.ContainsKey(name))
                    {
                        return fontPairs[name];
                    }

                    if (style.GlyphsDirectory != null)
                    {
                        // check file system for ttf
                        var newType = SKTypeface.FromFile(System.IO.Path.Combine(style.GlyphsDirectory, name + ".ttf"));
                        if (newType != null)
                        {
                            fontPairs[name] = newType;
                            return newType;
                        }

                        // check file system for otf
                        newType = SKTypeface.FromFile(System.IO.Path.Combine(style.GlyphsDirectory, name + ".otf"));
                        if (newType != null)
                        {
                            fontPairs[name] = newType;
                            return newType;
                        }
                    }

                    var typeface = SKTypeface.FromFamilyName(name);
                    if (typeface.FamilyName == name)
                    {
                        // gotcha!
                        fontPairs[name] = typeface;
                        return typeface;
                    }
                }

                // all options exhausted...
                // get the first one
                var fallback = SKTypeface.FromFamilyName(familyNames.First());
                fontPairs[familyNames.First()] = fallback;
                return fallback;
            }
        }

        //SKTypeface qualifyTypeface(string text, SKTypeface typeface)
        //{
        //    var glyphs = new ushort[typeface.CountGlyphs(text)];
        //    if (glyphs.Length < text.Length)
        //    {
        //        var fm = SKFontManager.Default;
        //        var charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;
        //        return fm.MatchCharacter(text[glyphs.Length]);
        //    }

        //    return typeface;
        //}

        void QualifyTypeface(Brush style, SKPaint paint)
        {
            var glyphs = new ushort[paint.Typeface.CountGlyphs(style.Text)];
            if (glyphs.Length < style.Text.Length)
            {
                var fm = SKFontManager.Default;
                var charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;
                var newTypeface = fm.MatchCharacter(style.Text[glyphs.Length]);

                if (newTypeface == null)
                {
                    return;
                }

                paint.Typeface = newTypeface;

                glyphs = new ushort[newTypeface.CountGlyphs(style.Text)];
                if (glyphs.Length < style.Text.Length)
                {
                    // still causing issues
                    // so we cut the rest
                    charIdx = (glyphs.Length > 0) ? glyphs.Length : 0;

                    style.Text = style.Text.Substring(0, charIdx);
                }
            }

        }

        public Renderer.Collisions DrawText(Point geometry, Brush style, Renderer.ROptions options, Renderer.Collisions collisions, int hatchCode)
        {

            if (style.Paint.TextOptional)
            {
                //return;
            }
            //style.ZIndex = 999;

            var paint = GetTextPaint(style);
           
            paint.TextSize = (float)(paint.TextSize * options.TextSizeMultiplicateur);
            QualifyTypeface(style, paint);

            var strokePaint = GetTextStrokePaint(style);
            var text = TransformText(style.Text, style, options);
            var allLines = text.Split('\n');

            //paint.Typeface = qualifyTypeface(text, paint.Typeface);

            // detect collisions
            if (allLines.Length > 0)
            {
                var biggestLine = allLines.OrderBy(line => line.Length).Last();
                var bytes = Encoding.UTF32.GetBytes(biggestLine);

                var width = (int)(paint.MeasureText(bytes));
                int left = (int)(geometry.X - width / 2);
                int top = (int)(geometry.Y - style.Paint.TextSize / 2 * allLines.Length);
                int height = (int)(style.Paint.TextSize * allLines.Length);

                var rectangle = new Rect(left, top, width, height);
                rectangle.Inflate(5, 5);

                if (ClipOverflow)
                {
                    if (!clipRectangle.Contains(rectangle))
                    {
                        //return collisions;
                    }
                }


                foreach (int coli in collisions.CollisionEntity)
                {
                    if (hatchCode == coli)
                    {
                        Debug.WriteLine("ignoring geometrie " + hatchCode);
                        return collisions;

                    }
                }




                if (TextCollides(rectangle))
                {
                    // collision detected
                    if (options.ImgPositionX == options.ImgCenterPositionX && options.ImgPositionY == options.ImgCenterPositionY)
                    {

                        collisions.CollisionEntity.Add(hatchCode);
                        return collisions;






                        //Debug.WriteLine("Colision detecté : " + style.Text + " \n L=" + left + "T=" + top + "W=" + width + "H=" + height);
                        //if (top > (256 * options.ImgCenterPositionY) && left > (256 * options.ImgCenterPositionX))
                        //{
                        //    if (geometry.X > (256 * options.ImgCenterPositionX + 20) && geometry.Y > (256 * options.ImgCenterPositionY) + 20)
                        //    {
                        //        double width_restante = left + width;
                        //        double height_restante = top + height;
                        //        if ((width_restante - 40 < 256 * (options.ImgPositionX + 1)) && (height_restante - 40 < 256 * (options.ImgCenterPositionY + 1)))
                        //        {
                        //            //Debug.WriteLine("Ajout texte " + hatchCode);
                        //            collisions.CollisionEntity.Add(hatchCode);
                        //            return collisions;
                        //        }
                        //    }
                        //}
                    }
                }
                textRectangles.Add(rectangle);

                //var list = new List<Point>()
                //{
                //    rectangle.TopLeft,
                //    rectangle.TopRight,
                //    rectangle.BottomRight,
                //    rectangle.BottomLeft,
                //};

                //var brush = new Brush();
                //brush.Paint = new Paint();
                //brush.Paint.FillColor = Color.FromArgb(150, 255, 0, 0);

                //this.DrawPolygon(list, brush);
            }

            int i = 0;
            foreach (var line in allLines)
            {
                var bytes = Encoding.UTF32.GetBytes(line);
                float lineOffset = (float)(i * style.Paint.TextSize) - ((float)(allLines.Length) * (float)style.Paint.TextSize) / 2 + (float)style.Paint.TextSize;
                var position = new SKPoint((float)geometry.X + (float)(style.Paint.TextOffset.X * style.Paint.TextSize), (float)geometry.Y + (float)(style.Paint.TextOffset.Y * style.Paint.TextSize) + lineOffset);



                if (style.Paint.TextStrokeWidth != 0)
                {
#pragma warning disable CS0618 // Le type ou le membre est obsolète
                    canvas.DrawText(bytes, position, strokePaint);
#pragma warning restore CS0618 // Le type ou le membre est obsolète
                }

#pragma warning disable CS0618 // Le type ou le membre est obsolète
                canvas.DrawText(bytes, position, paint);
#pragma warning restore CS0618 // Le type ou le membre est obsolète
                i++;
            }

            if (options.ImgPositionX == options.ImgCenterPositionX && options.ImgPositionY == options.ImgCenterPositionY)
            {
                collisions.CollisionEntity.Add(hatchCode);
            }
            return collisions;
            //todo ; draw text at the end
        }

        double GetPathLength(List<Point> path)
        {
            double distance = 0;
            for (var i = 0; i < path.Count - 2; i++)
            {
                distance += (path[i] - path[i + 1]).Length;
            }

            return distance;
        }

        double GetAbsoluteDiff2Angles(double x, double y, double c = Math.PI)
        {
            return c - Math.Abs((Math.Abs(x - y) % 2 * c) - c);
        }

        bool CheckPathSqueezing(List<Point> path, double textHeight)
        {
            //double maxCurve = 0;
            double previousAngle = 0;
            for (var i = 0; i < path.Count - 2; i++)
            {
                var vector = (path[i] - path[i + 1]);

                var angle = Math.Atan2(vector.Y, vector.X);
                var angleDiff = Math.Abs(GetAbsoluteDiff2Angles(angle, previousAngle));

                //var length = vector.Length / textHeight;
                //var curve = angleDiff / length;
                //maxCurve = Math.Max(curve, maxCurve);


                if (angleDiff > Math.PI / 3)
                {
                    return true;
                }

                previousAngle = angle;
            }

            return false;

            //return 0;

            //return maxCurve;
        }

        void DebugRectangle(Rect rectangle, Color color)
        {
            var list = new List<Point>()
            {
                rectangle.TopLeft,
                rectangle.TopRight,
                rectangle.BottomRight,
                rectangle.BottomLeft,
            };

            var brush = new Brush();
            brush.Paint = new Paint();
            brush.Paint.FillColor = color;

            this.DrawPolygon(list, brush);
        }

        public Renderer.Collisions DrawTextOnPath(List<Point> geometry, Brush style, Renderer.ROptions options, Renderer.Collisions collisions, int hatchCode)
        {
            style.Paint.TextSize = style.Paint.TextSize * options.TextSizeMultiplicateur;
            // buggggyyyyyy
            // requires an amazing collision system to work :/
            // --
            //return;

            //if (ClipOverflow)
            //{
            //return collisions;


            geometry = ClipLine(geometry);
            if (geometry == null)
            {
                return collisions;
            }
            //}

            var path = GetPathFromGeometry(geometry);
            var text = TransformText(style.Text, style, options);

            var pathSqueezed = CheckPathSqueezing(geometry, style.Paint.TextSize);

            if (pathSqueezed)
            {
                return collisions;
            }

            //text += " : " + bending.ToString("F");

            var bounds = path.Bounds;

            var left = bounds.Left - style.Paint.TextSize;
            var top = bounds.Top - style.Paint.TextSize;
            var right = bounds.Right + style.Paint.TextSize;
            var bottom = bounds.Bottom + style.Paint.TextSize;

            var rectangle = new Rect(left, top, right - left, bottom - top);

            //if (rectangle.Left <= 0 || rectangle.Right >= width || rectangle.Top <= 0 || rectangle.Bottom >= height)
            //{
            //    debugRectangle(rectangle, Color.FromArgb(128, 255, 100, 100));
            //    // bounding box (much bigger) collides with edges
            //    return;
            //}


            foreach (int coli in collisions.CollisionEntity)
            {
                if (hatchCode == coli)
                {
                    //Debug.WriteLine("Ignore " + hatchCode);
                    return collisions;
                }
            }


            double width_restante = left + width;
            double height_restante = top + height;

            if (TextCollides(rectangle) && true)
            {

                //if (options.ImgPositionX == options.ImgCenterPositionX && options.ImgPositionY == options.ImgCenterPositionY)
                //{
                //    double vwidth = right - left;
                //    double height = bottom - top;
                //    Debug.WriteLine("Colision detecté ligne : " + style.Text + " \n L=" + left + "T=" + top + "W=" + width + "H=" + height);
                //    if (top > (256 * options.ImgCenterPositionY) && left > (256 * options.ImgCenterPositionX))
                //    {
                //        Debug.WriteLine("a");
                //        if (bounds.Left > (256 * options.ImgCenterPositionX) && bounds.Top > (256 * options.ImgCenterPositionY))
                //        {

                //            Debug.WriteLine("b");
                //            if ((width_restante < 256 * (options.ImgPositionX + 1)) && (height_restante < 256 * (options.ImgCenterPositionY + 1)))
                //            {
                //                collisions.CollisionEntity.Add(new Renderer.Collisions.Entities(new Point(top, left), style, geometry));
                //            }
                //        }
                //    }
                //    return collisions;
                //}
                if (options.ImgPositionX == options.ImgCenterPositionX && options.ImgPositionY == options.ImgCenterPositionY)
                {
                    //Debug.WriteLine("Ajout dans collisions la ligne " + hatchCode);
                    collisions.CollisionEntity.Add(hatchCode);
                    return collisions;
                }

            }
            textRectangles.Add(rectangle);

            if (style.Text.Length * style.Paint.TextSize * options.OverflowTextCorrectingValue >= GetPathLength(geometry))
            {
                //debugRectangle(rectangle, Color.FromArgb(128, 100, 100, 255));
                // exceeds estimated path length

                if (options.ImgPositionX == options.ImgCenterPositionX && options.ImgPositionY == options.ImgCenterPositionY)
                {
                    //Debug.WriteLine("Ajout dans collisions la ligne (execd estim) " + hatchCode);
                    collisions.CollisionEntity.Add(hatchCode);
                    return collisions;
                }
                return collisions;
            }


            style.Paint.TextSize = style.Paint.TextSize * 1;

            var offset = new SKPoint((float)style.Paint.TextOffset.X, (float)style.Paint.TextOffset.Y);
            var bytes = Encoding.UTF32.GetBytes(text);
            if (style.Paint.TextStrokeWidth != 0)
            {

#pragma warning disable CS0618 // Le type ou le membre est obsolète
                canvas.DrawTextOnPath(bytes, path, offset, GetTextStrokePaint(style));
#pragma warning restore CS0618 // Le type ou le membre est obsolète
            }
#pragma warning disable CS0618 // Le type ou le membre est obsolète
            canvas.DrawTextOnPath(bytes, path, offset, GetTextPaint(style));
#pragma warning restore CS0618 // Le type ou le membre est obsolète
            if (options.ImgPositionX == options.ImgCenterPositionX && options.ImgPositionY == options.ImgCenterPositionY)
            {
                collisions.CollisionEntity.Add(hatchCode);
            }
            return collisions;
        }

        public void DrawPoint(Point geometry, Brush style)
        {
            if (style.Paint.IconImage != null)
            {
                // draw icon here
            }
        }

        public void DrawPolygon(List<Point> geometry, Brush style)
        {
            List<List<Point>> allGeometries = null;
            if (ClipOverflow)
            {
                allGeometries = ClipPolygon(geometry);
            }
            else
            {
                allGeometries = new List<List<Point>>() { geometry };
            }

            if (allGeometries == null)
            {
                return;
            }

            foreach (var geometryPart in allGeometries)
            {
                var path = GetPathFromGeometry(geometryPart);
                if (path == null)
                {
                    return;
                }

                SKPaint fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    StrokeCap = ConvertCap(style.Paint.LineCap),
                    Color = new SKColor(style.Paint.FillColor.R, style.Paint.FillColor.G, style.Paint.FillColor.B, (byte)Clamp(style.Paint.FillColor.A * style.Paint.FillOpacity, 0, 255)),
                    IsAntialias = true,
                };

                canvas.DrawPath(path, fillPaint);
            }

        }


        static SKImage ToSKImage(BitmapSource bitmap)
        {
            var info = new SKImageInfo(bitmap.PixelWidth, bitmap.PixelHeight);
            var image = SKImage.Create(info);
            using (var pixmap = image.PeekPixels())
            {
                ToSKPixmap(bitmap, pixmap);
            }
            return image;
        }

        static void ToSKPixmap(BitmapSource bitmap, SKPixmap pixmap)
        {
            if (pixmap.ColorType == SKImageInfo.PlatformColorType)
            {
                var info = pixmap.Info;
                var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
                converted.CopyPixels(new Int32Rect(0, 0, info.Width, info.Height), pixmap.GetPixels(), info.BytesSize, info.RowBytes);
            }
            else
            {
                // we have to copy the pixels into a format that we understand
                // and then into a desired format
                using (var tempImage = ToSKImage(bitmap))
                {
                    tempImage.ReadPixels(pixmap, 0, 0);
                }
            }
        }

        public void DrawImage(Stream imageStream, Brush style)
        {
            //var bitmapImage = new BitmapImage();
            //bitmapImage.BeginInit();
            //bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //bitmapImage.StreamSource = imageStream;
            //bitmapImage.DecodePixelWidth = this.width;
            //bitmapImage.DecodePixelHeight = this.height;
            //bitmapImage.EndInit();

            //var image = toSKImage(bitmapImage);

            //canvas.DrawImage(image, new SKPoint(0, 0));
            SKData sd = SKData.Create(imageStream);
            using (SKImage image = SKImage.FromEncodedData(sd))
            {
                canvas.DrawImage(image, new SKPoint(0, 0));
            }
        }

        public void DrawUnknown(List<List<Point>> geometry, Brush style)
        {

        }




        public BitmapSource FinishDrawing()
        {
            //using (var paint = new SKPaint())
            //{
            //    paint.Color = new SKColor(255, 255, 255, 255);
            //    paint.Style = SKPaintStyle.Fill;
            //    paint.TextSize = 24;
            //    paint.IsAntialias = true;

            //    var bytes = Encoding.UTF32.GetBytes("HELLO WORLD");
            //    canvas.DrawText(bytes, new SKPoint(10, 10), paint);
            //}


            //surface.Canvas.Flush();
            //grContext.


            bitmap.AddDirtyRect(new Int32Rect(0, 0, this.width, this.height));
            bitmap.Unlock();
            bitmap.Freeze();


            return bitmap;

        }
    }
}


