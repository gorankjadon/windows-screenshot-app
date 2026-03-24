using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GstackScreenshot;

namespace GstackScreenshot.Tests
{
    internal static class Program
    {
        private static int _failures;

        [STAThread]
        private static int Main()
        {
            Run("Hotkey parse round-trip", TestHotkeyParseRoundTrip);
            Run("Hotkey parse rejects missing modifier", TestHotkeyRejectsMissingModifier);
            Run("Settings store round-trip", TestSettingsStoreRoundTrip);
            Run("Settings store falls back on corrupt JSON", TestSettingsStoreCorruptJsonFallback);
            Run("Writable path resolver picks the first usable candidate without creating a placeholder file", TestWritablePathResolver);
            Run("Capture service creates the save folder when needed", TestCaptureServiceCreatesSaveFolder);
            Run("Capture service adds a suffix when the timestamp collides", TestCaptureServiceAddsCollisionSuffix);
            Run("Capture service surfaces invalid save targets cleanly", TestCaptureServiceRejectsInvalidSaveTarget);
            Run("Hotkey manager restores previous bindings on registration failure", TestHotkeyManagerRestoresPreviousBindings);
            Run("Settings workflow restores previous hotkeys when save fails", TestSettingsWorkflowRestoresPreviousHotkeysOnSaveFailure);

            Console.WriteLine(_failures == 0 ? "ALL TESTS PASSED" : (_failures + " TEST(S) FAILED"));
            return _failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("[PASS] " + name);
            }
            catch (Exception ex)
            {
                _failures++;
                Console.WriteLine("[FAIL] " + name + ": " + ex.Message);
            }
        }

        private static void TestHotkeyParseRoundTrip()
        {
            var gesture = HotkeyGesture.Parse("Ctrl+Alt+S");
            AssertEqual("Ctrl+Alt+S", gesture.ToString(), "Shortcut should round-trip cleanly.");
        }

        private static void TestHotkeyRejectsMissingModifier()
        {
            bool threw = false;

            try
            {
                HotkeyGesture.Parse("S");
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Assert(threw, "Shortcut parsing should reject keys without modifiers.");
        }

        private static void TestSettingsStoreRoundTrip()
        {
            var tempRoot = CreateTempRoot();
            try
            {
                var settingsPath = Path.Combine(tempRoot, "settings.json");
                var store = new SettingsStore(settingsPath);
                var settings = CreateSettings(Path.Combine(tempRoot, "shots"), "Ctrl+Alt+S", "Ctrl+Alt+F", "Ctrl+Alt+A");

                store.Save(settings);
                var loaded = store.Load();

                AssertEqual(settings.SaveFolder, loaded.SaveFolder, "Save folder should persist.");
                AssertEqual(settings.RegionHotkey, loaded.RegionHotkey, "Region hotkey should persist.");
                AssertEqual(settings.FullScreenHotkey, loaded.FullScreenHotkey, "Full-screen hotkey should persist.");
                AssertEqual(settings.ActiveWindowHotkey, loaded.ActiveWindowHotkey, "Active-window hotkey should persist.");
                Assert(new FileInfo(settingsPath).Length > 0, "Settings file should contain JSON after save.");
            }
            finally
            {
                CleanupTempRoot(tempRoot);
            }
        }

        private static void TestSettingsStoreCorruptJsonFallback()
        {
            var tempRoot = CreateTempRoot();
            try
            {
                var settingsPath = Path.Combine(tempRoot, "settings.json");
                File.WriteAllText(settingsPath, "not valid json");

                var store = new SettingsStore(settingsPath);
                var loaded = store.Load();

                AssertEqual(SettingsStore.GetDefaultSaveFolder(), loaded.SaveFolder, "Corrupt settings should fall back to defaults.");
                AssertEqual("Ctrl+Alt+S", loaded.RegionHotkey, "Default region hotkey should be restored.");
            }
            finally
            {
                CleanupTempRoot(tempRoot);
            }
        }

        private static void TestWritablePathResolver()
        {
            var tempRoot = CreateTempRoot();
            try
            {
                var expected = Path.Combine(tempRoot, "usable", "state.json");
                var resolved = AppStoragePaths.ResolveWritableFilePath(new[]
                {
                    null,
                    string.Empty,
                    expected
                });

                AssertEqual(expected, resolved, "Resolver should choose the first writable candidate.");
                Assert(Directory.Exists(Path.GetDirectoryName(expected)), "Resolver should prepare the target directory.");
                Assert(!File.Exists(expected), "Resolver should not create a placeholder file at the final path.");
            }
            finally
            {
                CleanupTempRoot(tempRoot);
            }
        }

        private static void TestCaptureServiceCreatesSaveFolder()
        {
            var tempRoot = CreateTempRoot();
            try
            {
                var saveFolder = Path.Combine(tempRoot, "new-folder", "nested");
                var service = new CaptureService(delegate { return new DateTime(2026, 3, 23, 18, 5, 0); });

                using (var bitmap = CreateBitmap())
                {
                    var path = service.SavePng(bitmap, saveFolder);
                    Assert(Directory.Exists(saveFolder), "SavePng should create the destination folder.");
                    Assert(File.Exists(path), "SavePng should write a PNG file to the destination folder.");
                }
            }
            finally
            {
                CleanupTempRoot(tempRoot);
            }
        }

        private static void TestCaptureServiceAddsCollisionSuffix()
        {
            var tempRoot = CreateTempRoot();
            try
            {
                var saveFolder = Path.Combine(tempRoot, "shots");
                var fixedTime = new DateTime(2026, 3, 23, 18, 6, 0);
                var service = new CaptureService(delegate { return fixedTime; });

                string firstPath;
                string secondPath;

                using (var firstBitmap = CreateBitmap())
                {
                    firstPath = service.SavePng(firstBitmap, saveFolder);
                }

                using (var secondBitmap = CreateBitmap())
                {
                    secondPath = service.SavePng(secondBitmap, saveFolder);
                }

                AssertEqual("Screenshot_2026-03-23_18-06-00.png", Path.GetFileName(firstPath), "First file should use the base timestamp name.");
                AssertEqual("Screenshot_2026-03-23_18-06-00_1.png", Path.GetFileName(secondPath), "Second file should get a suffix when the timestamp collides.");
            }
            finally
            {
                CleanupTempRoot(tempRoot);
            }
        }

        private static void TestCaptureServiceRejectsInvalidSaveTarget()
        {
            var tempRoot = CreateTempRoot();
            try
            {
                var occupiedPath = Path.Combine(tempRoot, "not-a-folder");
                File.WriteAllText(occupiedPath, "occupied");
                var service = new CaptureService(delegate { return new DateTime(2026, 3, 23, 18, 7, 0); });
                bool threw = false;

                try
                {
                    using (var bitmap = CreateBitmap())
                    {
                        service.SavePng(bitmap, occupiedPath);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    threw = ex.Message.IndexOf("Unable to save screenshot", StringComparison.Ordinal) >= 0;
                }

                Assert(threw, "Invalid save targets should surface a friendly save failure.");
            }
            finally
            {
                CleanupTempRoot(tempRoot);
            }
        }

        private static void TestHotkeyManagerRestoresPreviousBindings()
        {
            var fakePlatform = new FakeHotkeyPlatform();
            using (var manager = new HotkeyManager(fakePlatform))
            {
                var original = CreateSettings("C:\\shots", "Ctrl+Alt+S", "Ctrl+Alt+F", "Ctrl+Alt+A");
                manager.Register(original);
                var originalSnapshot = fakePlatform.CurrentBindings.OrderBy(value => value).ToArray();

                fakePlatform.FailOnAttempt = 5;
                var updated = CreateSettings("C:\\shots", "Ctrl+Alt+Q", "Ctrl+Alt+W", "Ctrl+Alt+E");

                bool threw = false;
                try
                {
                    manager.Register(updated);
                }
                catch (Exception)
                {
                    threw = true;
                }

                Assert(threw, "Hotkey registration should fail when the platform rejects a binding.");
                var restoredSnapshot = fakePlatform.CurrentBindings.OrderBy(value => value).ToArray();
                AssertSequenceEqual(originalSnapshot, restoredSnapshot, "Previous hotkeys should be restored after a failed update.");
            }
        }

        private static void TestSettingsWorkflowRestoresPreviousHotkeysOnSaveFailure()
        {
            var fakeStore = new FakeSettingsStore { ThrowOnSave = true };
            var fakeHotkeys = new FakeHotkeyRegistrationService();
            var workflow = new SettingsWorkflow(fakeStore, fakeHotkeys);
            var current = CreateSettings("C:\\shots", "Ctrl+Alt+S", "Ctrl+Alt+F", "Ctrl+Alt+A");
            var next = CreateSettings("C:\\shots", "Ctrl+Alt+Q", "Ctrl+Alt+W", "Ctrl+Alt+E");

            bool threw = false;
            try
            {
                workflow.Apply(current, next);
            }
            catch (Exception)
            {
                threw = true;
            }

            Assert(threw, "Settings workflow should throw when persistence fails.");
            AssertEqual(2.ToString(), fakeHotkeys.RegisteredSettings.Count.ToString(), "Hotkeys should be registered twice when save fails.");
            AssertEqual(current.RegionHotkey, fakeHotkeys.RegisteredSettings[1].RegionHotkey, "Previous hotkeys should be restored after save failure.");
        }

        private static AppSettings CreateSettings(string saveFolder, string region, string fullScreen, string activeWindow)
        {
            return new AppSettings
            {
                SaveFolder = saveFolder,
                RegionHotkey = region,
                FullScreenHotkey = fullScreen,
                ActiveWindowHotkey = activeWindow
            };
        }

        private static Bitmap CreateBitmap()
        {
            var bitmap = new Bitmap(4, 3);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.CadetBlue);
            }

            return bitmap;
        }

        private static string CreateTempRoot()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "GstackScreenshotTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            return tempRoot;
        }

        private static void CleanupTempRoot(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(message + " Expected '" + expected + "' but got '" + actual + "'.");
            }
        }

        private static void AssertSequenceEqual(string[] expected, string[] actual, string message)
        {
            if (expected.Length != actual.Length)
            {
                throw new InvalidOperationException(message + " Expected length " + expected.Length + " but got " + actual.Length + ".");
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (!string.Equals(expected[i], actual[i], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(message + " Mismatch at index " + i + ": expected '" + expected[i] + "' but got '" + actual[i] + "'.");
                }
            }
        }

        private sealed class FakeSettingsStore : IAppSettingsStore
        {
            public bool ThrowOnSave { get; set; }

            public AppSettings Load()
            {
                return AppSettings.CreateDefault();
            }

            public void Save(AppSettings settings)
            {
                if (ThrowOnSave)
                {
                    throw new IOException("Disk is full.");
                }
            }
        }

        private sealed class FakeHotkeyRegistrationService : IHotkeyRegistrationService
        {
            public List<AppSettings> RegisteredSettings { get; private set; }

            public FakeHotkeyRegistrationService()
            {
                RegisteredSettings = new List<AppSettings>();
            }

            public void Register(AppSettings settings)
            {
                RegisteredSettings.Add(CreateSettings(settings.SaveFolder, settings.RegionHotkey, settings.FullScreenHotkey, settings.ActiveWindowHotkey));
            }
        }

        private sealed class FakeHotkeyPlatform : IHotkeyPlatform
        {
            private readonly Dictionary<int, string> _registeredBindings;
            private int _attemptCount;

            public FakeHotkeyPlatform()
            {
                _registeredBindings = new Dictionary<int, string>();
            }

            public int FailOnAttempt { get; set; }

            public IEnumerable<string> CurrentBindings
            {
                get { return _registeredBindings.Values; }
            }

            public bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key)
            {
                _attemptCount++;
                if (FailOnAttempt > 0 && _attemptCount == FailOnAttempt)
                {
                    return false;
                }

                _registeredBindings[id] = modifiers + ":" + key;
                return true;
            }

            public void UnregisterHotKey(IntPtr handle, int id)
            {
                _registeredBindings.Remove(id);
            }
        }
    }
}
