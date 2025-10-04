using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

// By: SpinxDev 2025 xD
namespace UIEdit.Utils
{
    public static class ImageUtils
    {
        #region Injection Properties
        #endregion

        #region Constructor
        #endregion

        #region Methods
        /// <summary>
        /// Creates a thumbnail image from the specified image path, resizing it to fit within the given maximum width and height.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static BitmapImage CreateThumbnail(string imagePath, int maxWidth = 100, int maxHeight = 100)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    return CreateErrorThumbnail();
                }

                var fullImage = Core.GetImageSourceFromFileName(imagePath);
                if (fullImage == null)
                {
                    return CreateErrorThumbnail();
                }

                if (fullImage is BitmapImage bitmapImage && bitmapImage.UriSource != null)
                {
                    return LoadThumbnailFromFile(bitmapImage.UriSource.LocalPath, maxWidth, maxHeight);
                }

                return fullImage as BitmapImage ?? CreateErrorThumbnail();
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageUtils.CreateThumbnail", $"Erro ao criar thumbnail para {imagePath}", ex);
                return CreateErrorThumbnail();
            }
        }

        /// <summary>
        /// Loads a thumbnail image from the specified file path, resizing it to fit within the given maximum width and height.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        private static BitmapImage LoadThumbnailFromFile(string imagePath, int maxWidth, int maxHeight)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    return CreateErrorThumbnail();
                }

                var fileInfo = new FileInfo(imagePath);
                if (fileInfo.Length == 0)
                {
                    return CreateErrorThumbnail();
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.DecodePixelWidth = maxWidth;
                bitmap.DecodePixelHeight = maxHeight;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                if (bitmap.CanFreeze) bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageUtils.LoadThumbnailFromFile", $"Erro ao carregar thumbnail de {imagePath}", ex);
                return CreateErrorThumbnail();
            }
        }

        /// <summary>
        /// Creates a default error thumbnail image.
        /// </summary>
        /// <returns></returns>
        public static BitmapImage CreateErrorThumbnail()
        {
            try
            {
                using (var memory = new MemoryStream())
                {
                    Properties.Resources.errorpic.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = memory;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    if (bitmap.CanFreeze) bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                return new BitmapImage();
            }
        }

        /// <summary>
        /// Checks if the given file path has an image file extension (.dds, .tga, .png, .jpg, .jpeg, .bmp).
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool IsImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".dds" || extension == ".tga" || extension == ".png" ||
                   extension == ".jpg" || extension == ".jpeg" || extension == ".bmp";
        }

        /// <summary>
        /// Converts a .dds or .tga file path to a .png file path.
        /// </summary>
        /// <param name="originalPath"></param>
        /// <returns></returns>
        public static string GetPngPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath)) return originalPath;

            return originalPath.Replace(".dds", ".png").Replace(".DDS", ".png")
                              .Replace(".tga", ".png").Replace(".TGA", ".png");
        }

        /// <summary>
        /// Checks if an image file exists at the specified path, considering both the original and .png versions.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static bool ImageExists(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return false;

            if (!Path.IsPathRooted(imagePath))
            {
                return File.Exists(imagePath);
            }

            if (File.Exists(imagePath)) return true;

            var pngPath = GetPngPath(imagePath);
            return File.Exists(pngPath);
        }

        /// <summary>
        /// Gets detailed information about the image file at the specified path.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static ImageInfo GetImageInfo(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    return new ImageInfo { Exists = false };
                }

                var fileInfo = new FileInfo(imagePath);
                var pngPath = GetPngPath(imagePath);

                int width = 0, height = 0;
                try
                {
                    using (var img = Image.FromFile(File.Exists(pngPath) ? pngPath : imagePath))
                    {
                        width = img.Width;
                        height = img.Height;
                    }
                }
                catch
                {
                }

                return new ImageInfo
                {
                    Exists = true,
                    FileName = fileInfo.Name,
                    FullPath = imagePath,
                    Size = fileInfo.Length,
                    Width = width,
                    Height = height,
                    Extension = fileInfo.Extension.ToUpper(),
                    LastModified = fileInfo.LastWriteTime
                };
            }
            catch (Exception ex)
            {
                Logger.LogError("ImageUtils.GetImageInfo", $"Erro ao obter informa??es da imagem {imagePath}", ex);
                return new ImageInfo { Exists = false };
            }
        }

        /// <summary>
        /// Converts an absolute path to a relative path based on the given surfaces directory.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="surfacesPath"></param>
        /// <returns></returns>
        public static string ConvertToRelativePath(string fullPath, string surfacesPath)
        {
            if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(surfacesPath))
            {
                return fullPath;
            }

            try
            {
                var uri1 = new Uri(surfacesPath + Path.DirectorySeparatorChar);
                var uri2 = new Uri(fullPath);
                var relativeUri = uri1.MakeRelativeUri(uri2);
                return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', '\\');
            }
            catch
            {
                return Path.GetFileName(fullPath);
            }
        }

        /// <summary>
        /// Converts a relative path to an absolute path based on the given surfaces directory.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="surfacesPath"></param>
        /// <returns></returns>
        public static string ConvertToAbsolutePath(string relativePath, string surfacesPath)
        {
            if (string.IsNullOrEmpty(relativePath) || string.IsNullOrEmpty(surfacesPath))
            {
                return relativePath;
            }

            try
            {
                if (relativePath.StartsWith("\\"))
                {
                    relativePath = relativePath.Substring(1);
                }

                return Path.Combine(surfacesPath, relativePath);
            }
            catch
            {
                return relativePath;
            }
        }
        #endregion
    }

    public class ImageInfo
    {
        public bool Exists { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public long Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Extension { get; set; }
        public DateTime LastModified { get; set; }
        public string SizeFormatted => Size > 1024 ? $"{Size / 1024} KB" : $"{Size} bytes";
        public string Dimensions => Width > 0 && Height > 0 ? $"{Width}x{Height}" : "Desconhecido";
    }
}
