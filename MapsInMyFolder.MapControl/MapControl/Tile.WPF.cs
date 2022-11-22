// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Windows.Threading;

namespace MapsInMyFolder.MapControl
{
    public partial class Tile
    {
        public void SetImage(ImageSource image, bool fadeIn = true)
        {
            try
            {
                Pending = false;

                if (image != null && fadeIn && MapBase.ImageFadeDuration > TimeSpan.Zero)
                {
                    if (image is BitmapSource bitmap && !bitmap.IsFrozen && bitmap.IsDownloading)
                    {
                        bitmap.DownloadCompleted += BitmapDownloadCompleted;
                        bitmap.DownloadFailed += BitmapDownloadFailed;
                    }
                    else
                    {
                        FadeIn();
                    }
                }
                else
                {
                    Image.Opacity = 1d;
                }


                //Image.Source = image;


                #region drawborder
                if (Commun.Settings.map_show_tile_border || Commun.Settings.is_in_debug_mode)
                {
                    BitmapSource bImage = image as BitmapSource;
                    if (bImage != null)
                    {
                        // Draw a Rectangle
                        Brush couleur = Brushes.Black;
                        DrawingVisual dVisual = new DrawingVisual();
                        using (DrawingContext dc = dVisual.RenderOpen())
                        {
                            dc.DrawImage(bImage, new System.Windows.Rect(0, 0, bImage.PixelWidth, bImage.PixelHeight));
                            if (Commun.Settings.is_in_debug_mode)
                            {
                                string TileLocationText = "TileX : " + this.X.ToString() + "\n" +
                                                                             "TileY : " + this.Y.ToString() + "\n" +
                                                                             "Zoom  : " + this.ZoomLevel.ToString();
                                dc.DrawText(new FormattedText(TileLocationText, System.Globalization.CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), 12, couleur, 150), new System.Windows.Point(05, 05));
                            }

                            Pen pen = new Pen(couleur, 1);
                            dc.DrawRectangle(Brushes.Transparent, pen, new System.Windows.Rect(0, 0, image.Width, image.Height));
                        }
                        RenderTargetBitmap targetBitmap = new RenderTargetBitmap(Convert.ToInt32(image.Width), Convert.ToInt32(image.Height), 96, 96, PixelFormats.Default);
                        targetBitmap.Render(dVisual);
                        WriteableBitmap wBitmap = new WriteableBitmap(targetBitmap);
                        Image.Source = wBitmap;
                    }
                }
                else
                {
                    Image.Source = image;
                }
                #endregion




            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error tile.wpf : " + ex.Message);
            }
        }

        private void BitmapDownloadCompleted(object sender, EventArgs e)
        {
            var bitmap = (BitmapSource)sender;

            bitmap.DownloadCompleted -= BitmapDownloadCompleted;
            bitmap.DownloadFailed -= BitmapDownloadFailed;

            FadeIn();
        }

        private void BitmapDownloadFailed(object sender, ExceptionEventArgs e)
        {
            var bitmap = (BitmapSource)sender;

            bitmap.DownloadCompleted -= BitmapDownloadCompleted;
            bitmap.DownloadFailed -= BitmapDownloadFailed;

            Image.Source = null;
        }
    }
}
