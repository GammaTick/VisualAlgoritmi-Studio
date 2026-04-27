using System;

namespace VisualAlgoritmi_Studio.Controls.Editor.Invalidation
{
    [Flags]
    internal enum EditorDirtyFlags
    {
        None = 0,
        TextBuffer = 1 << 0,
        Caret = 1 << 1,
        LineCount = 1 << 2,
        Selection = 1 << 3,
        Viewport = 1 << 4
    }
}
