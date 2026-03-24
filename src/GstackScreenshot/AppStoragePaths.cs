using System;
using System.Collections.Generic;
using System.IO;

namespace GstackScreenshot
{
    internal static class AppStoragePaths
    {
        public static string ResolveLogPath()
        {
            return ResolveWritableFilePath(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GstackScreenshot", "logs", "app.log"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GstackScreenshot", "logs", "app.log"),
                Path.Combine(Path.GetTempPath(), "GstackScreenshot", "logs", "app.log"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log")
            });
        }

        public static string ResolveSettingsPath()
        {
            return ResolveWritableFilePath(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GstackScreenshot", "settings.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GstackScreenshot", "settings.json"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GstackScreenshot", "settings.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json")
            });
        }

        internal static string ResolveWritableFilePath(IEnumerable<string> candidatePaths)
        {
            foreach (var candidate in candidatePaths)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                try
                {
                    var directory = Path.GetDirectoryName(candidate);
                    EnsureDirectoryIsWritable(directory);
                    return candidate;
                }
                catch
                {
                }
            }

            throw new UnauthorizedAccessException("Could not find a writable location for app storage.");
        }

        private static void EnsureDirectoryIsWritable(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                directory = AppDomain.CurrentDomain.BaseDirectory;
            }

            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (var stream = new FileStream(probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                }
            }
            finally
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
        }
    }
}
