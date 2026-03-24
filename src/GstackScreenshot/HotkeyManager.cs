using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace GstackScreenshot
{
    internal enum CaptureMode
    {
        Region,
        FullScreen,
        ActiveWindow
    }

    internal interface IHotkeyPlatform
    {
        bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);
        void UnregisterHotKey(IntPtr handle, int id);
    }

    internal sealed class NativeHotkeyPlatform : IHotkeyPlatform
    {
        public bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key)
        {
            return NativeMethods.RegisterHotKey(handle, id, modifiers, key);
        }

        public void UnregisterHotKey(IntPtr handle, int id)
        {
            NativeMethods.UnregisterHotKey(handle, id);
        }
    }

    internal sealed class HotkeyManager : IDisposable, IHotkeyRegistrationService
    {
        private readonly HotkeyWindow _window;
        private readonly Dictionary<int, CaptureMode> _registrations;
        private readonly IHotkeyPlatform _platform;
        private Dictionary<CaptureMode, HotkeyGesture> _activeHotkeys;

        public HotkeyManager(IHotkeyPlatform platform = null)
        {
            _window = new HotkeyWindow();
            _window.HotkeyPressed += OnHotkeyPressed;
            _registrations = new Dictionary<int, CaptureMode>();
            _activeHotkeys = new Dictionary<CaptureMode, HotkeyGesture>();
            _platform = platform ?? new NativeHotkeyPlatform();
        }

        public event EventHandler<CaptureMode> CaptureRequested;

        public void Register(AppSettings settings)
        {
            var requestedHotkeys = BuildHotkeys(settings);
            var previousHotkeys = new Dictionary<CaptureMode, HotkeyGesture>(_activeHotkeys);

            UnregisterCurrent();

            try
            {
                RegisterInternal(requestedHotkeys);
            }
            catch (Exception registrationException)
            {
                try
                {
                    RegisterInternal(previousHotkeys);
                }
                catch (Exception restoreException)
                {
                    throw new InvalidOperationException(
                        registrationException.Message + " Previous hotkeys could not be restored: " + restoreException.Message,
                        registrationException);
                }

                throw;
            }
        }

        public void UnregisterAll()
        {
            UnregisterCurrent();
            _activeHotkeys.Clear();
        }

        public void Dispose()
        {
            UnregisterAll();
            _window.HotkeyPressed -= OnHotkeyPressed;
            _window.Dispose();
        }

        private static Dictionary<CaptureMode, HotkeyGesture> BuildHotkeys(AppSettings settings)
        {
            var hotkeys = new Dictionary<CaptureMode, HotkeyGesture>
            {
                { CaptureMode.Region, HotkeyGesture.Parse(settings.RegionHotkey) },
                { CaptureMode.FullScreen, HotkeyGesture.Parse(settings.FullScreenHotkey) },
                { CaptureMode.ActiveWindow, HotkeyGesture.Parse(settings.ActiveWindowHotkey) }
            };

            var duplicates = hotkeys
                .GroupBy(pair => pair.Value)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.ToString())
                .ToArray();

            if (duplicates.Length > 0)
            {
                throw new InvalidOperationException("Shortcuts must be unique. Duplicate: " + string.Join(", ", duplicates));
            }

            return hotkeys;
        }

        private void RegisterInternal(Dictionary<CaptureMode, HotkeyGesture> hotkeys)
        {
            var temporaryRegistrations = new Dictionary<int, CaptureMode>();
            int id = 1;

            try
            {
                foreach (var mode in new[] { CaptureMode.Region, CaptureMode.FullScreen, CaptureMode.ActiveWindow })
                {
                    HotkeyGesture gesture;
                    if (!hotkeys.TryGetValue(mode, out gesture))
                    {
                        continue;
                    }

                    if (!_platform.RegisterHotKey(_window.Handle, id, (uint)gesture.Modifiers, (uint)gesture.Key))
                    {
                        throw new Win32Exception("Unable to register shortcut " + gesture + ". Another app may already be using it.");
                    }

                    temporaryRegistrations[id] = mode;
                    id++;
                }
            }
            catch
            {
                foreach (var registration in temporaryRegistrations)
                {
                    _platform.UnregisterHotKey(_window.Handle, registration.Key);
                }

                throw;
            }

            _registrations.Clear();
            foreach (var registration in temporaryRegistrations)
            {
                _registrations[registration.Key] = registration.Value;
            }

            _activeHotkeys = new Dictionary<CaptureMode, HotkeyGesture>(hotkeys);
        }

        private void UnregisterCurrent()
        {
            foreach (var id in _registrations.Keys.ToArray())
            {
                _platform.UnregisterHotKey(_window.Handle, id);
            }

            _registrations.Clear();
        }

        private void OnHotkeyPressed(object sender, int id)
        {
            CaptureMode mode;
            if (_registrations.TryGetValue(id, out mode))
            {
                var handler = CaptureRequested;
                if (handler != null)
                {
                    handler(this, mode);
                }
            }
        }

        private sealed class HotkeyWindow : NativeWindow, IDisposable
        {
            public HotkeyWindow()
            {
                CreateHandle(new CreateParams());
            }

            public event EventHandler<int> HotkeyPressed;

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == NativeMethods.WmHotkey)
                {
                    var handler = HotkeyPressed;
                    if (handler != null)
                    {
                        handler(this, m.WParam.ToInt32());
                    }
                }

                base.WndProc(ref m);
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }
    }
}
