using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class StreamExtensions
    {
        public static ImageSource ToImageSource(this Stream stream)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
    }
}
