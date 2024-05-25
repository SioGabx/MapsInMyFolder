using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Generic.Extensions
{
    public static class ByteExtensions
    {
        public static ImageSource ToImageSource(this byte[] buffer)
        {
            using var stream = new MemoryStream(buffer);
            return stream.ToImageSource();
        }

        public static Task<ImageSource> ToImageSourceAsync(this byte[] buffer)
        {
            return Task.Run(() => buffer.ToImageSource());
        }
    }
}
