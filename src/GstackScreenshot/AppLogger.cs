using System;
using System.IO;
using System.Text;

namespace GstackScreenshot
{
    internal static class AppLogger
    {
        private static readonly object Sync = new object();
        private static readonly string LogPath = AppStoragePaths.ResolveLogPath();

        public static string CurrentLogPath
        {
            get { return LogPath; }
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                lock (Sync)
                {
                    var directory = Path.GetDirectoryName(LogPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine("[{0:u}] {1}: {2}", DateTime.UtcNow, level, message);
                        if (exception != null)
                        {
                            writer.WriteLine(exception.ToString());
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
