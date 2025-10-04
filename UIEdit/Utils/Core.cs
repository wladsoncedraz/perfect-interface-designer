using FreeImageAPI;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

// By: SpinxDev 2025 xD
namespace UIEdit.Utils
{
    public class Core
    {
        #region Injection Properties
        #endregion

        #region Constructor
        #endregion

        #region Methods
        /// <summary>
        /// Sets the minimum and maximum working set sizes for the specified process.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="minimumWorkingSetSize"></param>
        /// <param name="maximumWorkingSetSize"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr handle, int minimumWorkingSetSize, int maximumWorkingSetSize);

        /// <summary>
        /// Frees up memory by forcing garbage collection and reducing the working set size of the current process.
        /// </summary>
        public static void ClearMemory()
        {
            GC.Collect();
            SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
        }

        /// <summary>
        /// Gets an ImageSource from a file name, converting DDS or TGA files to PNG if necessary.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static ImageSource GetImageSourceFromFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return new BitmapImage();
            var pngFileName = fileName.Replace(".dds", ".png").
                Replace(".DDS", ".png").
                Replace(".tga", ".png").
                Replace(".TGA", ".png");

            if (!File.Exists(pngFileName) && File.Exists(fileName))
            {
                try
                {
                    using (var ms = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var dib = FreeImage.LoadFromStream(ms);
                        if (!dib.IsNull)
                        {
                            try
                            {
                                FreeImage.Save(FREE_IMAGE_FORMAT.FIF_PNG, dib, pngFileName, FREE_IMAGE_SAVE_FLAGS.PNG_Z_NO_COMPRESSION);
                            }
                            catch { }
                            dib.SetNull();
                        }
                    }
                }
                catch (DllNotFoundException)
                {
                }
                catch (Exception)
                {
                }
                ClearMemory();
            }
            if (File.Exists(pngFileName))
            {
                try
                {
                    var fi = new FileInfo(pngFileName);
                    if (fi.Length > 0)
                    {
                        var img = new BitmapImage();
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.UriSource = new Uri(pngFileName, UriKind.Absolute);
                        img.EndInit();
                        if (img.CanFreeze) img.Freeze();
                        return img;
                    }
                }
                catch
                {
                    try { File.Delete(pngFileName); } catch { }
                }
            }
            using (var memory = new MemoryStream())
            {
                Properties.Resources.errorpic.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                if (bitmapImage.CanFreeze) bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        /// <summary>
        /// Stretches an image from the specified file name to the target width and height using a 9-slice scaling technique.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="targetWidth"></param>
        /// <param name="targetHeight"></param>
        /// <returns></returns>
        public static ImageSource TrueStretchImage(string fileName, double targetWidth, double targetHeight)
        {
            if (string.IsNullOrEmpty(fileName)) return new BitmapImage();
            if (targetWidth == 0 || targetHeight == 0) return new BitmapImage();

            try
            {
                var fname = System.IO.Path.GetFileName(fileName) ?? string.Empty;
                if (fname.IndexOf("透明", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    fname.IndexOf("transparent", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var blank = new Bitmap((int)targetWidth, (int)targetHeight);
                    using (var ms0 = new MemoryStream())
                    {
                        blank.Save(ms0, ImageFormat.Png); ms0.Position = 0;
                        var bi0 = new BitmapImage();
                        bi0.BeginInit(); bi0.StreamSource = ms0; bi0.CacheOption = BitmapCacheOption.OnLoad; bi0.EndInit();
                        if (bi0.CanFreeze) bi0.Freeze();
                        return bi0;
                    }
                }
            }
            catch { }
            GetImageSourceFromFileName(fileName);
            var pngFileName = fileName.Replace(".dds", ".png").
                Replace(".DDS", ".png").
                Replace(".tga", ".png").
                Replace(".TGA", ".png");
            if (!File.Exists(pngFileName)) return new BitmapImage();
            var sourceImage = Image.FromFile(pngFileName);
            var targetImage = new BitmapImage();

            var img = new Bitmap((int)targetWidth, (int)targetHeight);
            using (var g = Graphics.FromImage(img))
            {
                var topLeftAngle = new Rectangle(0, 0, sourceImage.Width / 2, sourceImage.Height / 2 + 1);
                var topRightAngle = new Rectangle(sourceImage.Width / 2, 0, sourceImage.Width / 2, sourceImage.Height / 2);
                var bottomLeftAngle = new Rectangle(0, sourceImage.Height / 2, sourceImage.Width / 2, sourceImage.Height / 2);
                var bottomRighttAngle = new Rectangle(sourceImage.Width / 2, sourceImage.Height / 2, sourceImage.Width / 2, sourceImage.Height / 2);
                var dstTopLeftAngle = topLeftAngle;
                var dstTopRightAngle = new Rectangle((int)targetWidth - sourceImage.Width / 2, 0, sourceImage.Width / 2, sourceImage.Height / 2);
                var dstBottomLeftAngle = new Rectangle(0, (int)targetHeight - sourceImage.Height / 2, sourceImage.Width / 2, sourceImage.Height / 2);
                var dstBottomRightAngle = new Rectangle((int)targetWidth - sourceImage.Width / 2, (int)targetHeight - sourceImage.Height / 2, sourceImage.Width / 2, sourceImage.Height / 2);

                g.DrawImage(sourceImage, dstTopLeftAngle, topLeftAngle, GraphicsUnit.Pixel);
                g.DrawImage(sourceImage, dstTopRightAngle, topRightAngle, GraphicsUnit.Pixel);
                g.DrawImage(sourceImage, dstBottomLeftAngle, bottomLeftAngle, GraphicsUnit.Pixel);
                g.DrawImage(sourceImage, dstBottomRightAngle, bottomRighttAngle, GraphicsUnit.Pixel);

                var topTextureImg = new Bitmap(1, sourceImage.Height == 1 ? 1 : sourceImage.Height / 2);
                using (var tg = Graphics.FromImage(topTextureImg))
                {
                    tg.DrawImage(
                        sourceImage,
                        new Rectangle(0, 0, 1, sourceImage.Height / 2),
                        new Rectangle(sourceImage.Width / 2, 0, sourceImage.Width / 2 + sourceImage.Width % 2, sourceImage.Height / 2),
                        GraphicsUnit.Pixel
                        );
                }
                var topBrush = new TextureBrush(topTextureImg);
                var topBorderRect = new Rectangle(sourceImage.Width / 2, 0, (int)targetWidth - sourceImage.Width + 2, sourceImage.Height / 2);
                g.FillRectangle(topBrush, topBorderRect);

                var bottomTextureImg = new Bitmap(1, sourceImage.Height == 1 ? 1 : sourceImage.Height / 2);
                using (var tg = Graphics.FromImage(bottomTextureImg))
                {
                    tg.DrawImage(
                        sourceImage,
                        new Rectangle(0, 0, 1, sourceImage.Height / 2),
                        new Rectangle(sourceImage.Width / 2, sourceImage.Height / 2, 1, sourceImage.Height / 2),
                        GraphicsUnit.Pixel
                        );
                }
                for (var x = sourceImage.Width / 2; x < (int)targetWidth - sourceImage.Width / 2; x++)
                {
                    var bottomBorderRect = new Rectangle(x, (int)targetHeight - sourceImage.Height / 2, 1, sourceImage.Height / 2);
                    g.DrawImage(bottomTextureImg, bottomBorderRect);
                }

                var leftTextureImg = new Bitmap(sourceImage.Width == 1 ? 1 : sourceImage.Width / 2, 1);
                using (var tg = Graphics.FromImage(leftTextureImg))
                {
                    tg.DrawImage(
                        sourceImage,
                        new Rectangle(0, 0, sourceImage.Width / 2, 1),
                        new Rectangle(0, sourceImage.Height / 2, sourceImage.Width / 2, sourceImage.Height / 2 + 1),
                        GraphicsUnit.Pixel
                        );
                }
                var leftBrush = new TextureBrush(leftTextureImg);
                var leftBorderRect = new Rectangle(0, sourceImage.Height / 2, sourceImage.Width / 2, (int)targetHeight - sourceImage.Height);
                g.FillRectangle(leftBrush, leftBorderRect);

                var rightTextureImg = new Bitmap(sourceImage.Width == 1 ? 1 : sourceImage.Width / 2, 1);
                using (var tg = Graphics.FromImage(rightTextureImg))
                {
                    tg.DrawImage(
                        sourceImage,
                        new Rectangle(0, 0, sourceImage.Width / 2, 1),
                        new Rectangle(sourceImage.Width / 2, sourceImage.Height / 2, sourceImage.Width / 2, sourceImage.Height / 2),
                        GraphicsUnit.Pixel
                        );
                }
                for (var y = sourceImage.Height / 2; y < (int)targetHeight - sourceImage.Height / 2; y++)
                {
                    var rightBorderRect = new Rectangle((int)targetWidth - sourceImage.Width / 2, y, sourceImage.Width / 2, 1);
                    g.DrawImage(rightTextureImg, rightBorderRect);
                }

                var centerWidth = (int)targetWidth - sourceImage.Width;
                var centerHeight = (int)targetHeight - sourceImage.Height;
                if (centerWidth > 0 && centerHeight > 0)
                {
                    var centerTextureImg = new Bitmap(1, 1);
                    using (var tg = Graphics.FromImage(centerTextureImg))
                    {
                        tg.DrawImage(
                            sourceImage,
                            new Rectangle(0, 0, 1, 1),
                            new Rectangle(sourceImage.Width / 2, sourceImage.Height / 2, 1, 1),
                            GraphicsUnit.Pixel
                        );
                    }
                    using (var centerBrush = new TextureBrush(centerTextureImg))
                    {
                        var centerRect = new Rectangle(sourceImage.Width / 2, sourceImage.Height / 2, centerWidth, centerHeight);
                        g.FillRectangle(centerBrush, centerRect);
                    }
                }
            }
            using (var ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                targetImage.BeginInit();
                targetImage.StreamSource = ms;
                targetImage.CacheOption = BitmapCacheOption.OnLoad;
                targetImage.EndInit();
                ms.Close();
            }
            return targetImage;
        }

        /// <summary>
        /// Parses a color string in the format "R,G,B,A" and returns a SolidColorBrush.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static SolidColorBrush GetColorBrushFromString(string color)
        {
            if (color == null) return new SolidColorBrush(Colors.Transparent);
            var ret = new SolidColorBrush();
            var dyes = color.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            byte alfa, r, g, b;
            Byte.TryParse(dyes[3], NumberStyles.Integer, Thread.CurrentThread.CurrentCulture, out alfa);
            Byte.TryParse(dyes[0], NumberStyles.Integer, Thread.CurrentThread.CurrentCulture, out r);
            Byte.TryParse(dyes[1], NumberStyles.Integer, Thread.CurrentThread.CurrentCulture, out g);
            Byte.TryParse(dyes[2], NumberStyles.Integer, Thread.CurrentThread.CurrentCulture, out b);
            ret.Color = dyes.Length == 4
                ? Color.FromArgb(alfa, r, g, b)
                : Colors.White;
            return ret;
        }
        #endregion
    }
}
