using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GstackScreenshot
{
    internal sealed class ScreenshotApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly SettingsStore _settingsStore;
        private readonly HotkeyManager _hotkeyManager;
        private readonly CaptureService _captureService;
        private readonly SettingsWorkflow _settingsWorkflow;

        private AppSettings _settings;
        private bool _isCapturing;

        public ScreenshotApplicationContext()
        {
            AppLogger.Info("Constructing application context.");
            _settingsStore = new SettingsStore();
            _captureService = new CaptureService();
            _hotkeyManager = new HotkeyManager();
            _settingsWorkflow = new SettingsWorkflow(_settingsStore, _hotkeyManager);
            _hotkeyManager.CaptureRequested += OnCaptureRequested;

            AppLogger.Info("Loading persisted settings.");
            _settings = _settingsStore.Load();

            AppLogger.Info("Creating tray icon.");
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Gstack Screenshot",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };

            AppLogger.Info("Registering initial hotkeys.");
            TryRegisterHotkeys();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hotkeyManager.CaptureRequested -= OnCaptureRequested;
                _hotkeyManager.Dispose();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            base.Dispose(disposing);
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Capture Region", null, delegate { BeginCapture(CaptureMode.Region); });
            menu.Items.Add("Capture Full Screen", null, delegate { BeginCapture(CaptureMode.FullScreen); });
            menu.Items.Add("Capture Active Window", null, delegate { BeginCapture(CaptureMode.ActiveWindow); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Settings", null, delegate { ShowSettings(); });
            menu.Items.Add("Exit", null, delegate { ExitThread(); });
            return menu;
        }

        private void TryRegisterHotkeys()
        {
            try
            {
                _hotkeyManager.Register(_settings);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Initial hotkey registration failed.", ex);
                MessageBox.Show(
                    "Default shortcuts could not be registered. Choose different shortcuts in Settings.",
                    "Gstack Screenshot",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ShowSettings();
            }
        }

        private void OnCaptureRequested(object sender, CaptureMode mode)
        {
            BeginCapture(mode);
        }

        private void BeginCapture(CaptureMode mode)
        {
            if (_isCapturing)
            {
                AppLogger.Info("Capture ignored because another capture is already in progress.");
                return;
            }

            try
            {
                _isCapturing = true;
                AppLogger.Info("Starting capture: " + mode);
                Rectangle bounds;

                switch (mode)
                {
                    case CaptureMode.Region:
                        bounds = CaptureRegion();
                        break;
                    case CaptureMode.FullScreen:
                        bounds = _captureService.GetVirtualScreenBounds();
                        break;
                    case CaptureMode.ActiveWindow:
                        var activeBounds = _captureService.GetActiveWindowBounds();
                        if (!activeBounds.HasValue)
                        {
                            AppLogger.Info("Active window capture failed because no active window bounds were available.");
                            ShowError("Unable to find an active window to capture.");
                            return;
                        }

                        bounds = activeBounds.Value;
                        break;
                    default:
                        return;
                }

                AppLogger.Info(string.Format("Capturing bounds: {0},{1} {2}x{3}", bounds.Left, bounds.Top, bounds.Width, bounds.Height));
                using (var bitmap = _captureService.Capture(bounds))
                {
                    var savedPath = _captureService.SavePng(bitmap, _settings.SaveFolder);
                    AppLogger.Info("Saved screenshot to " + savedPath);

                    try
                    {
                        _captureService.CopyToClipboard(bitmap);
                        AppLogger.Info("Copied screenshot to clipboard.");
                    }
                    catch (ExternalException ex)
                    {
                        AppLogger.Error("Clipboard copy failed after saving screenshot.", ex);
                        ShowError("Screenshot saved, but the clipboard is busy.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                AppLogger.Info("Capture canceled by user.");
            }
            catch (Win32Exception ex)
            {
                AppLogger.Error("Win32 failure during capture.", ex);
                ShowError("Screenshot failed: " + ex.Message);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Unexpected failure during capture.", ex);
                ShowError("Screenshot failed: " + ex.Message);
            }
            finally
            {
                _isCapturing = false;
            }
        }

        private Rectangle CaptureRegion()
        {
            using (var form = new RegionSelectionForm())
            {
                if (form.ShowDialog() != DialogResult.OK || !form.SelectedBounds.HasValue)
                {
                    throw new OperationCanceledException();
                }

                return form.SelectedBounds.Value;
            }
        }

        private void ShowSettings()
        {
            using (var form = new SettingsForm(_settings))
            {
                if (form.ShowDialog() != DialogResult.OK || form.Result == null)
                {
                    AppLogger.Info("Settings dialog canceled.");
                    return;
                }

                try
                {
                    _settings = _settingsWorkflow.Apply(_settings, form.Result);
                    AppLogger.Info("Settings applied successfully.");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Settings update failed.", ex);
                    ShowError("Unable to save settings: " + ex.Message);
                }
            }
        }

        private void ShowError(string message)
        {
            _notifyIcon.BalloonTipTitle = "Gstack Screenshot";
            _notifyIcon.BalloonTipText = message;
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
            _notifyIcon.ShowBalloonTip(4000);
        }
    }
}
