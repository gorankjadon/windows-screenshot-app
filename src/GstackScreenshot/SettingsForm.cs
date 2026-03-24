using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GstackScreenshot
{
    internal sealed class SettingsForm : Form
    {
        private readonly TextBox _saveFolderTextBox;
        private readonly TextBox _regionHotkeyTextBox;
        private readonly TextBox _fullScreenHotkeyTextBox;
        private readonly TextBox _activeWindowHotkeyTextBox;
        private readonly Button _saveButton;

        public SettingsForm(AppSettings settings)
        {
            Text = "Gstack Screenshot Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 250);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

            _saveFolderTextBox = CreateTextBox(settings.SaveFolder);
            _regionHotkeyTextBox = CreateTextBox(settings.RegionHotkey);
            _fullScreenHotkeyTextBox = CreateTextBox(settings.FullScreenHotkey);
            _activeWindowHotkeyTextBox = CreateTextBox(settings.ActiveWindowHotkey);

            table.Controls.Add(CreateLabel("Save folder"), 0, 0);
            table.Controls.Add(_saveFolderTextBox, 1, 0);
            var browseButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
            browseButton.Click += OnBrowseClicked;
            table.Controls.Add(browseButton, 2, 0);

            table.Controls.Add(CreateLabel("Region hotkey"), 0, 1);
            table.Controls.Add(_regionHotkeyTextBox, 1, 1);
            table.SetColumnSpan(_regionHotkeyTextBox, 2);

            table.Controls.Add(CreateLabel("Full screen hotkey"), 0, 2);
            table.Controls.Add(_fullScreenHotkeyTextBox, 1, 2);
            table.SetColumnSpan(_fullScreenHotkeyTextBox, 2);

            table.Controls.Add(CreateLabel("Active window hotkey"), 0, 3);
            table.Controls.Add(_activeWindowHotkeyTextBox, 1, 3);
            table.SetColumnSpan(_activeWindowHotkeyTextBox, 2);

            var hint = new Label
            {
                AutoSize = true,
                Text = "Use shortcuts like Ctrl+Alt+S. Shortcuts must be unique.",
                Dock = DockStyle.Fill
            };
            table.Controls.Add(hint, 0, 4);
            table.SetColumnSpan(hint, 3);

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill
            };
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            _saveButton = new Button { Text = "Save", AutoSize = true };
            _saveButton.Click += OnSaveClicked;
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(_saveButton);
            table.Controls.Add(buttons, 0, 5);
            table.SetColumnSpan(buttons, 3);

            Controls.Add(table);
            AcceptButton = _saveButton;
            CancelButton = cancelButton;
        }

        public AppSettings Result { get; private set; }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };
        }

        private static TextBox CreateTextBox(string value)
        {
            return new TextBox
            {
                Text = value ?? string.Empty,
                Dock = DockStyle.Fill
            };
        }

        private void OnBrowseClicked(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose where PNG screenshots should be saved.";
                dialog.SelectedPath = Directory.Exists(_saveFolderTextBox.Text)
                    ? _saveFolderTextBox.Text
                    : SettingsStore.GetDefaultSaveFolder();

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _saveFolderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                var result = new AppSettings
                {
                    SaveFolder = _saveFolderTextBox.Text.Trim(),
                    RegionHotkey = HotkeyGesture.Parse(_regionHotkeyTextBox.Text).ToString(),
                    FullScreenHotkey = HotkeyGesture.Parse(_fullScreenHotkeyTextBox.Text).ToString(),
                    ActiveWindowHotkey = HotkeyGesture.Parse(_activeWindowHotkeyTextBox.Text).ToString()
                };

                if (string.IsNullOrWhiteSpace(result.SaveFolder))
                {
                    throw new InvalidOperationException("Choose a save folder.");
                }

                Directory.CreateDirectory(result.SaveFolder);

                if (result.RegionHotkey == result.FullScreenHotkey ||
                    result.RegionHotkey == result.ActiveWindowHotkey ||
                    result.FullScreenHotkey == result.ActiveWindowHotkey)
                {
                    throw new InvalidOperationException("Each screenshot shortcut must be unique.");
                }

                Result = result;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Invalid Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
