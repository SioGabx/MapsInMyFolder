﻿// XAML Map Control - https://github.com/ClemensFischer/XAML-Map-Control
// © 2022 Clemens Fischer
// Licensed under the Microsoft Public License (Ms-PL)

using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace MapsInMyFolder.MapControl
{
    public static partial class ImageLoader
    {
        public static Task<ImageSource> LoadImageAsync(Stream stream)
        {

            Debug.WriteLine("LoadImageAsync from stream task ");
            return Task.Run(() => LoadImage(stream));
        }

        public static Task<ImageSource> LoadImageAsync(byte[] buffer)
        {
            return Task.Run(() =>
            {
                using var stream = new MemoryStream(buffer);
                Commun.DebugMode.WriteLine("LoadImageAsync from buffer ");
                return LoadImage(stream);
            });
        }

        public static Task<ImageSource> LoadImageAsync(string path)
        {
            return Task.Run(() =>
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                using var stream = File.OpenRead(path);
                Commun.DebugMode.WriteLine("LoadImageAsync read file path " + path);
                return LoadImage(stream);
            });
        }

        private static ImageSource LoadImage(Stream stream)
        {
            var bitmapImage = new BitmapImage();
            try { 
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            }catch(System.NotSupportedException ex)
            {
                Debug.WriteLine("Erreur, ce format d'image n'est pas supporté : " + ex.Message);
            }catch(System.Exception ex)
            {
                Debug.WriteLine("Erreur lors du chargement de l'apperçu : " + ex.Message);
            }
            return bitmapImage;
        }
    }
}
