using MapsInMyFolder.Core.Generic.Extensions;
using MapsInMyFolder.Core.Images;
using MapsInMyFolder.Core.Layers;
using MapsInMyFolder.Properties;
using NetVips;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MapsInMyFolder.Core.Downloader
{
    public class TilesImages : IDisposable
    {
        public static TilesImages Create(Layer layer)
        {
            Type ClassType = typeof(TilesImages);
            switch (layer.TilesFormat)
            {
                case Format.png:
                    ClassType = typeof(PNGTilesImages);
                    break;
                case Format.jpeg:
                    ClassType = typeof(JPEGTilesImages);
                    break;
                case Format.pbf:
                    throw new NotImplementedException();
            }
            return (TilesImages)Activator.CreateInstance(ClassType, layer);
        }

        private readonly Layer _layer;
        private readonly TilesClient _client; 
        public TilesImages(Layer layer)
        {
            _layer = layer;
            _client = new TilesClient(layer);
        }

        public virtual byte[] GetEmpty(HttpResponseMessage Message)
        {
            string ErrorMessage = $"[{(int)Message.StatusCode}]\n{Message.ReasonPhrase}";
            return GetEmpty(ErrorMessage);
        }
        public virtual byte[] GetEmpty(string Message)
        {
            const int BorderSize = 1;
            if (string.IsNullOrEmpty(Message)) { return null; }
            var MapBackground = Properties.Settings.Default.MapBackground;
            var ImageFormat = _layer.HasTransparency ? "png" : "jpeg";
            double[] color = new double[] { MapBackground.R, MapBackground.G, MapBackground.B, _layer.HasTransparency ? 60 : 255 };
            var BorderTileSize = _layer.TileSize - (BorderSize * 2);
            using (Image text = NetVips.Image.Text(Message.Wrap(20), null, null, null, Enums.Align.Centre, null, 100, 5, null, true))
            using (Image background = NetVips.Image.Black(BorderTileSize, BorderTileSize))
            {
                int offsetX = (int)Math.Floor((double)(BorderTileSize - text.Width) / 2);
                int offsetY = (int)Math.Floor((double)(BorderTileSize - text.Height) / 2);

                using (Image image = background.Linear(color, color))
                using (Image SrgbImage = image.Copy(interpretation: Enums.Interpretation.Srgb))

                using (Image finalImage = SrgbImage.Composite2(
                    text,
                    _layer.HasTransparency ? Enums.BlendMode.Xor : Enums.BlendMode.Atop,
                    offsetX,
                    offsetY)
                )
                using (Image GravityFinalImage = finalImage.Gravity(
                    Enums.CompassDirection.Centre,
                    _layer.TileSize,
                    _layer.TileSize,
                    _layer.HasTransparency ? Enums.Extend.Background : Enums.Extend.Black,
                    _layer.HasTransparency ? new double[] { 0, 0, 0, 255 } : null)
                )
                {
                    GravityFinalImage.WriteToFile(@"C:\Users\franc\Downloads\dff\" + new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() + "." + ImageFormat);
                    return GravityFinalImage.WriteToBuffer("." + ImageFormat, Saving.GetSaveVOption(ImageFormat, 100, _layer.TileSize));
                }

            }
        }

        public async Task<HttpResponseMessage> Query(string url)
        {
            return await _client.SendRequestAutoRetry(url, Settings.Default.MaxRequestErrorRetry, TimeSpan.FromSeconds(3));
        }
        public virtual async Task<ImageSource> GetDisplayTileFromUrl(int X, int Y, int ZoomLevel)
        {
            var RemoteTile = await GetTileFromUrl(X, Y, ZoomLevel);
            var Content = RemoteTile.Content;
            var ResponseMessage = RemoteTile.ResponseMessage;

            if (Content is null)
            {
                Debug.WriteLine($"[{(int)ResponseMessage.StatusCode}]\n{ResponseMessage.ReasonPhrase}");
                Debug.WriteLine(ResponseMessage.Headers.Location);
                Content = GetEmpty(ResponseMessage);
            }

            var Image = Content.ToImageSource();
            bool ShowTileLocation = Settings.Default.DebugMode || _layer.ShowTileLocation;
            if (_layer.ShowTileLocation || ShowTileLocation)
            {
                return Image.AddBorder(1, ShowTileLocation ? $"X : {X}\nY : {Y}\nZoom : {ZoomLevel}\n" : null);
            }

            return Image;
        }

        public virtual async Task<(byte[] Content, HttpResponseMessage ResponseMessage)> GetTileFromUrl(int X, int Y, int ZoomLevel)
        {
            string url = GetUrl(X, Y, ZoomLevel);
            HttpResponseMessage ResponseMessage = await Query(url);
            if (ResponseMessage.IsSuccessStatusCode)
            {
                return (await ResponseMessage.Content.ReadAsByteArrayAsync(), ResponseMessage);
            }
            return (null, ResponseMessage);
        }

        public virtual string GetUrl(int X, int Y, int ZoomLevel)
        {
            var url = _layer.TileUrl;
            url = url.Replace("{x}", X.ToString());
            url = url.Replace("{y}", Y.ToString());
            url = url.Replace("{z}", ZoomLevel.ToString());
            return url;
        }


        public void Dispose()
        {
            ((IDisposable)_client).Dispose();
            GC.SuppressFinalize(this);
        }
    }


    public class PNGTilesImages : TilesImages
    {
        public PNGTilesImages(Layer layer) : base(layer)
        {
        }
    }

    public class JPEGTilesImages : TilesImages
    {
        public JPEGTilesImages(Layer layer) : base(layer)
        {
        }
    }
}
