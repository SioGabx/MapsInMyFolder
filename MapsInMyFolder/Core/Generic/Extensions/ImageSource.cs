using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class BitmapSourceExtensions
    {
        public static ImageSource AddBorder(this ImageSource ImageSource, int Thickness, string Text = null)
        {
            if (ImageSource is BitmapSource bitmapSource)
            {
                // Draw a Rectangle
                Brush couleur = Brushes.Black;
                DrawingVisual dVisual = new DrawingVisual();
                using (DrawingContext dc = dVisual.RenderOpen())
                {
                    dc.DrawImage(bitmapSource, new System.Windows.Rect(0, 0, bitmapSource.PixelWidth, bitmapSource.PixelHeight));
                    if (!string.IsNullOrWhiteSpace(Text))
                    {
                        dc.DrawText(
                            new FormattedText(Text,
                                System.Globalization.CultureInfo.CurrentCulture,
                                System.Windows.FlowDirection.LeftToRight,
                                new Typeface("Arial"),
                                12,
                                couleur,
                                150),
                            new System.Windows.Point(5, 5));
                    }
                    Pen pen = new Pen(couleur, Thickness);
                    dc.DrawRectangle(Brushes.Transparent, pen, new System.Windows.Rect(0, 0, bitmapSource.PixelWidth, bitmapSource.PixelHeight));
                }
                RenderTargetBitmap targetBitmap = new RenderTargetBitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, 96, 96, PixelFormats.Default);
                targetBitmap.Render(dVisual);
                WriteableBitmap wBitmap = new WriteableBitmap(targetBitmap);
                return wBitmap.ToImageSource();
            }
            return null;
        }
    }
}
