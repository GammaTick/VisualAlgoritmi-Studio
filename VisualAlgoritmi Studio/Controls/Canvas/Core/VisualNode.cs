using Avalonia;
using Avalonia.Media.TextFormatting;
using System;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Core
{
    public class VisualNode : IDisposable
    {
        public TextLayout Layout { get; }
        public Point CellLocation { get; set; }
        public double CellHeight { get; }
        public double CellWidth { get; }

        private bool _disposed;

        public VisualNode(TextLayout layout, Point cellLocation, double cellHeight, double cellWidth)
        {
            Layout = layout;
            CellLocation = cellLocation;
            CellHeight = cellHeight;
            CellWidth = cellWidth;
        }

        public double EndX => CellLocation.X + CellWidth;

        public virtual Point GetTextLocation()
        {
            // Multiline TextLayout components bounded by a maxWidth will automatically center
            // texts inside that bounding space. Since the canvases apply an 8px padding inward
            // (16px total subtracted from CellWidth for the constraint width), we simply set the 
            // layout's starting origin to this padding to perfectly center the entire constrained block.
            double paddingX = 8;
            double x = CellLocation.X + paddingX;
            double y = CellLocation.Y + (CellHeight - Layout.Height) * 0.5;

            return new Point(x, y);
        }

        public virtual VisualNode Clone()
        {
            return new VisualNode(Layout, CellLocation, CellHeight, CellWidth);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Layout.Dispose();
            _disposed = true;
        }
    }
}
