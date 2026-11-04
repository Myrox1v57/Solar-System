using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Solar_System.Scene
{
    /// <summary>
    /// Loads pictures from Assets/Textures. A missing or broken file must never crash
    /// the app: Load returns null, the file name goes to the log, and the caller uses
    /// a solid color instead.
    /// </summary>
    public static class TextureLoader
    {
        private static readonly string textureFolder =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Textures");

        /// <summary>Returns a brush showing the picture, or null if it could not be loaded.</summary>
        public static ImageBrush Load(string fileName)
        {
            string path = Path.Combine(textureFolder, fileName);
            if (!File.Exists(path))
            {
                AppLog.Warning("Texture missing, using a plain color instead: " + fileName);
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad; // read it now, don't keep the file open
                image.EndInit();
                image.Freeze();

                // Keep the brush settings at their defaults. With TileMode.Tile, WPF first redraws
                // the picture at a lower quality, which makes every texture blurry.
                var brush = new ImageBrush(image);
                brush.Freeze();
                return brush;
            }
            catch (Exception ex)
            {
                AppLog.Error("Texture could not be read, using a plain color instead: " + fileName, ex);
                return null;
            }
        }
    }
}
