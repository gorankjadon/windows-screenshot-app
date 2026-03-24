using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GstackScreenshot
{
    internal sealed class CaptureService
    {
        private readonly Func<DateTime> _nowProvider;

        public CaptureService(Func<DateTime> nowProvider = null)
        {
            _nowProvider = nowProvider ?? delegate { return DateTime.Now; };
        }

        public Rectangle GetVirtualScreenBounds()
        {
            return SystemInformation.VirtualScreen;
        }

        public Rectangle? GetActiveWindowBounds()
        {
            var handle = NativeMethods.GetForegroundWindow();
            if (handle == IntPtr.Zero || !NativeMethods.IsWindowVisible(handle))
            {
                return null;
            }

            NativeMethods.Rect rect;
            if (NativeMethods.DwmGetWindowAttribute(handle, NativeMethods.DwmaExtendedFrameBounds, out rect, Marshal.SizeOf(typeof(NativeMethods.Rect))) == 0)
            {
                return NormalizeRectangle(NativeMethods.ToRectangle(rect));
            }

            return NativeMethods.GetWindowRect(handle, out rect)
                ? NormalizeRectangle(NativeMethods.ToRectangle(rect))
                : (Rectangle?)null;
        }

        public Bitmap Capture(Rectangle bounds)
        {
            var normalized = NormalizeRectangle(bounds);
            if (normalized.Width <= 0 || normalized.Height <= 0)
            {
                throw new InvalidOperationException("Capture area must be greater than zero.");
            }

            var bitmap = new Bitmap(normalized.Width, normalized.Height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(normalized.Left, normalized.Top, 0, 0, normalized.Size, CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        public string SavePng(Bitmap bitmap, string saveFolder)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }

            if (string.IsNullOrWhiteSpace(saveFolder))
            {
                throw new InvalidOperationException("Save folder is not configured.");
            }

            try
            {
                Directory.CreateDirectory(saveFolder);
            }
            catch (Exception ex)
            {
                throw CreateSaveFailure(saveFolder, ex);
            }

            var baseName = "Screenshot_" + _nowProvider().ToString("yyyy-MM-dd_HH-mm-ss");

            for (int suffix = 0; suffix < 1000; suffix++)
            {
                var fileName = suffix == 0
                    ? baseName + ".png"
                    : baseName + "_" + suffix + ".png";
                var path = Path.Combine(saveFolder, fileName);
                var created = false;

                try
                {
                    using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        created = true;
                        bitmap.Save(stream, ImageFormat.Png);
                    }

                    return path;
                }
                catch (IOException ex)
                {
                    if (!created && File.Exists(path))
                    {
                        continue;
                    }

                    TryDelete(path);
                    throw CreateSaveFailure(saveFolder, ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    TryDelete(path);
                    throw CreateSaveFailure(saveFolder, ex);
                }
                catch (ExternalException ex)
                {
                    TryDelete(path);
                    throw CreateSaveFailure(saveFolder, ex);
                }
            }

            throw new InvalidOperationException("Unable to save screenshot because the destination folder already contains too many screenshots for the current second.");
        }

        public void CopyToClipboard(Image image)
        {
            Clipboard.SetImage(image);
        }

        private static InvalidOperationException CreateSaveFailure(string saveFolder, Exception innerException)
        {
            return new InvalidOperationException(
                "Unable to save screenshot to '" + saveFolder + "'. " + innerException.Message,
                innerException);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static Rectangle NormalizeRectangle(Rectangle rect)
        {
            var left = Math.Min(rect.Left, rect.Right);
            var top = Math.Min(rect.Top, rect.Bottom);
            var right = Math.Max(rect.Left, rect.Right);
            var bottom = Math.Max(rect.Top, rect.Bottom);
            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }
}
