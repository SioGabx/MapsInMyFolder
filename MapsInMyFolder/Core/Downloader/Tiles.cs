using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Layers;
using MapsInMyFolder.Properties;
using NetVips;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapsInMyFolder.Core.Downloader
{
    public static class Tiles
    {
        public static byte[] GetEmpty(this Layer Layer, HttpResponseMessage Message)
        {
            string ErrorMessage = $"[{(int)Message.StatusCode}]\n{Message.ReasonPhrase}";
            return GetEmpty(Layer, ErrorMessage);
        }
        public static byte[] GetEmpty(this Layer Layer, string Message)
        {
            const int BorderSize = 1;
            if (string.IsNullOrEmpty(Message)) { return null; }
            var MapBackground = Properties.Settings.Default.MapBackground;
            var ImageFormat = Layer.HasTransparency ? "png" : "jpeg";
            double[] color = new double[] { MapBackground.R, MapBackground.G, MapBackground.B, Layer.HasTransparency ? 60 : 255 };
            var BorderTileSize = Layer.TileSize - (BorderSize * 2);
            using (NetVips.Image text = NetVips.Image.Text(Message.Wrap(20), null, null, null, Enums.Align.Centre, null, 100, 5, null, true))
            using (NetVips.Image background = NetVips.Image.Black(BorderTileSize, BorderTileSize))
            {
                int offsetX = (int)Math.Floor((double)(BorderTileSize - text.Width) / 2);
                int offsetY = (int)Math.Floor((double)(BorderTileSize - text.Height) / 2);

                using (Image image = background.Linear(color, color))
                using (NetVips.Image SrgbImage = image.Copy(interpretation: Enums.Interpretation.Srgb))

                using (NetVips.Image finalImage = SrgbImage.Composite2(
                    text,
                    Layer.HasTransparency ? Enums.BlendMode.Xor : Enums.BlendMode.Atop,
                    offsetX,
                    offsetY)
                )
                using (NetVips.Image GravityFinalImage = finalImage.Gravity(
                    Enums.CompassDirection.Centre,
                    Layer.TileSize,
                    Layer.TileSize,
                    Layer.HasTransparency ? Enums.Extend.Background : Enums.Extend.Black,
                    Layer.HasTransparency ? new double[] { 0, 0, 0, 255 } : null)
                )
                {
                    GravityFinalImage.WriteToFile(@"C:\Users\franc\Downloads\dff\" + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + "." + ImageFormat);
                    return GravityFinalImage.WriteToBuffer("." + ImageFormat, Core.Downloader.ImageSaving.GetSaveVOption(ImageFormat, 100, Layer.TileSize));
                }

            }
        }

        public static async Task<byte[]> GetTileFromUrl(this Layer Layer, string url)
        {
            var Response = await Layer.Client.SendRequestAutoRetry(url, Settings.Default.MaxRequestErrorRetry, TimeSpan.FromSeconds(3));
            if (!Response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[{(int)Response.StatusCode}]\n{Response.ReasonPhrase}");
                Debug.WriteLine(url);
                return GetEmpty(Layer, Response);
            }
            return await Response.Content.ReadAsByteArrayAsync();
        }
    }
}
