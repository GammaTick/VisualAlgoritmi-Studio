using Avalonia;
using System.Collections.Generic;

namespace VisualAlgoritmi_Studio.Controls.Editor.Invalidation
{
    internal sealed class EditorRenderCache
    {
        public (double X, double Y) CaretPosInCodeLayout = (0, 0);
        public bool IsCaretInVisibleArea = true;

        public List<Rect> CachedSelectionRects = [];

        public double EndOfLineCellWidth = 0;
    }
}
