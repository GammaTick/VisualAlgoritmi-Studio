using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;

namespace VisualAlgoritmi_Studio.Controls.Editor.Invalidation
{
    internal sealed class EditorStateTracker
    {
        private readonly TextBuffer _textBuffer;
        private readonly CaretController _caretController;
        private readonly SelectionController _selectionController;
        private readonly ViewportManager _viewportManager;

        private int _previousTextBufferVersion;
        private int _previousCaretVersion;
        private int _previousSelectionVersion;
        private int _previousLineCount;
        private int _previousViewportVersion;

        public EditorStateTracker(TextBuffer textBuffer, CaretController caretController,
            SelectionController selectionController, ViewportManager viewportManager)
        {
            _textBuffer = textBuffer;
            _caretController = caretController;
            _selectionController = selectionController;
            _viewportManager = viewportManager;

            Snapshot();
        }

        public EditorDirtyFlags ComputeDirtyFlags()
        {
            EditorDirtyFlags flags = EditorDirtyFlags.None;

            if (_previousTextBufferVersion != _textBuffer.Version)
            {
                flags |= EditorDirtyFlags.TextBuffer;
            }

            if (_previousCaretVersion != _caretController.Version)
            {
                flags |= EditorDirtyFlags.Caret;
            }

            if (_previousSelectionVersion != _selectionController.Version)
            {
                flags |= EditorDirtyFlags.Selection;
            }

            if (_previousLineCount != _textBuffer.LineCount)
            {
                flags |= EditorDirtyFlags.LineCount;
            }

            if (_previousViewportVersion != _viewportManager.Version)
            {
                flags |= EditorDirtyFlags.Viewport;
            }

            return flags;
        }

        public void Snapshot()
        {
            _previousTextBufferVersion = _textBuffer.Version;
            _previousCaretVersion = _caretController.Version;
            _previousSelectionVersion = _selectionController.Version;
            _previousLineCount = _textBuffer.LineCount;
            _previousViewportVersion = _viewportManager.Version;
        }
    }
}
