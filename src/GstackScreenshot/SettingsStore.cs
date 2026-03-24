using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace GstackScreenshot
{
    internal sealed class SettingsStore : IAppSettingsStore
    {
        private readonly string _settingsPath;

        public SettingsStore(string settingsPath = null)
        {
            if (string.IsNullOrWhiteSpace(settingsPath))
            {
                _settingsPath = AppStoragePaths.ResolveSettingsPath();
                return;
            }

            var directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _settingsPath = settingsPath;
        }

        public AppSettings Load()
        {
            if (!File.Exists(_settingsPath))
            {
                return AppSettings.CreateDefault();
            }

            try
            {
                using (var stream = File.OpenRead(_settingsPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    var settings = serializer.ReadObject(stream) as AppSettings;
                    return Sanitize(settings ?? AppSettings.CreateDefault());
                }
            }
            catch
            {
                return AppSettings.CreateDefault();
            }
        }

        public void Save(AppSettings settings)
        {
            var sanitized = Sanitize(settings);
            Directory.CreateDirectory(sanitized.SaveFolder);

            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = _settingsPath + ".tmp-" + Guid.NewGuid().ToString("N");

            try
            {
                using (var stream = File.Create(tempPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(AppSettings));
                    serializer.WriteObject(stream, sanitized);
                }

                if (File.Exists(_settingsPath))
                {
                    File.Replace(tempPath, _settingsPath, null);
                }
                else
                {
                    File.Move(tempPath, _settingsPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public static string GetDefaultSaveFolder()
        {
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            return Path.Combine(pictures, "Screenshots");
        }

        private static AppSettings Sanitize(AppSettings settings)
        {
            if (settings == null)
            {
                settings = AppSettings.CreateDefault();
            }

            if (string.IsNullOrWhiteSpace(settings.SaveFolder))
            {
                settings.SaveFolder = GetDefaultSaveFolder();
            }

            if (string.IsNullOrWhiteSpace(settings.RegionHotkey))
            {
                settings.RegionHotkey = "Ctrl+Alt+S";
            }

            if (string.IsNullOrWhiteSpace(settings.FullScreenHotkey))
            {
                settings.FullScreenHotkey = "Ctrl+Alt+F";
            }

            if (string.IsNullOrWhiteSpace(settings.ActiveWindowHotkey))
            {
                settings.ActiveWindowHotkey = "Ctrl+Alt+A";
            }

            return settings;
        }
    }
}
