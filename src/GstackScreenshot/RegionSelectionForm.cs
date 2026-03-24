using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GstackScreenshot
{
    internal sealed class RegionSelectionForm : Form
    {
        private readonly Rectangle _virtualScreen;
        private Point _dragStart;
        private Point _currentPoint;
        private bool _isDragging;

        public RegionSelectionForm()
        {
            _virtualScreen = SystemInformation.VirtualScreen;
            Bounds = _virtualScreen;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            BackColor = Color.Black;
            Opacity = 0.30;
        }

        public Rectangle? SelectedBounds { get; private set; }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }

            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _isDragging = true;
            _dragStart = e.Location;
            _currentPoint = e.Location;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                _currentPoint = e.Location;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!_isDragging)
            {
                base.OnMouseUp(e);
                return;
            }

            _isDragging = false;
            _currentPoint = e.Location;
            var selection = GetSelectionRectangle();

            if (selection.Width < 2 || selection.Height < 2)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            SelectedBounds = new Rectangle(
                selection.Left + _virtualScreen.Left,
                selection.Top + _virtualScreen.Top,
                selection.Width,
                selection.Height);

            DialogResult = DialogResult.OK;
            Close();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!_isDragging)
            {
                return;
            }

            var selection = GetSelectionRectangle();
            using (var fillBrush = new SolidBrush(Color.FromArgb(50, Color.White)))
            using (var outlinePen = new Pen(Color.White, 2))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillRectangle(fillBrush, selection);
                e.Graphics.DrawRectangle(outlinePen, selection);
            }
        }

        private Rectangle GetSelectionRectangle()
        {
            var left = Math.Min(_dragStart.X, _currentPoint.X);
            var top = Math.Min(_dragStart.Y, _currentPoint.Y);
            var width = Math.Abs(_dragStart.X - _currentPoint.X);
            var height = Math.Abs(_dragStart.Y - _currentPoint.Y);
            return new Rectangle(left, top, width, height);
        }
    }
}
