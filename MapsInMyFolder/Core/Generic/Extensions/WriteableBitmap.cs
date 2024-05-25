using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class WriteableBitmapExtensions
    {
        public static ImageSource ToImageSource(this WriteableBitmap writeableBitmap)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                // Use PngBitmapEncoder (you can choose a different encoder if needed)
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                encoder.Save(stream);

                return stream.ToImageSource();
            }
        }
    }
}
