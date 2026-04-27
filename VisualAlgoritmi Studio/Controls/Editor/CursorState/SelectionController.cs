using System;
using System.Diagnostics;
using VisualAlgoritmi_Studio.Controls.Editor.Text;

namespace VisualAlgoritmi_Studio.Controls.Editor.CursorState
{
    internal sealed class SelectionController
    {
        private readonly TextBuffer _textBuffer;
        
        private int _version = 0;

        private int _anchorLine;
        private int _anchorColumn;

        private int _activeLine;
        private int _activeColumn;

        public int Version => _version;
        public bool HasSelection => _anchorLine != _activeLine || _anchorColumn != _activeColumn;

        public SelectionController(TextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        private void IncreaseVersion()
        {
            unchecked
            {
                _version++;
            }
        }

        public (int startLine, int startColumn, int endLine, int endColumn) GetNormalizedSelection()
        {
            if (_anchorLine > _activeLine || _anchorLine == _activeLine && _anchorColumn > _activeColumn)
            {
                return (_activeLine, _activeColumn, _anchorLine, _anchorColumn);
            }

            return (_anchorLine, _anchorColumn, _activeLine, _activeColumn);
        }  

        public void CollapseTo(int line, int column)
        {
            int lastLine = Math.Max(0, _textBuffer.LineCount - 1);

            line = Math.Clamp(line, 0, lastLine);
            column = Math.Clamp(column, 0, _textBuffer.GetLineLength(line));

            if (_anchorLine == line &&
                _anchorColumn == column &&
                _activeLine == line &&
                _activeColumn == column)
            {
                return;
            }

            _anchorLine = line;
            _anchorColumn = column;
            _activeLine = line;
            _activeColumn = column;

            IncreaseVersion();
        }

        public void SelectAll()
        {
            _anchorLine = 0;
            _anchorColumn = 0;
            _activeLine = _textBuffer.LineCount - 1;
            _activeColumn = _textBuffer.GetLineLength(_activeLine);
            IncreaseVersion();
        }

        public void BeginSelection(int caretLine, int caretColumn)
        {
            int lastLine = Math.Max(0, _textBuffer.LineCount - 1);

            caretLine = Math.Clamp(caretLine, 0, lastLine);
            caretColumn = Math.Clamp(caretColumn, 0, _textBuffer.GetLineLength(caretLine));

            _anchorLine = caretLine;
            _anchorColumn = caretColumn;
            _activeLine = caretLine;
            _activeColumn = caretColumn;

            IncreaseVersion();
        }

        public void ExtendTo(int caretLine, int caretColumn)
        {
            int lastLine = Math.Max(0, _textBuffer.LineCount - 1);
            caretLine = Math.Clamp(caretLine, 0, lastLine);
            caretColumn = Math.Clamp(caretColumn, 0, _textBuffer.GetLineLength(caretLine));

            if (_activeLine == caretLine && _activeColumn == caretColumn)
            {
                return;
            }

            _activeLine = caretLine;
            _activeColumn = caretColumn;

            IncreaseVersion();
        }

        public (int anchorLine, int anchorColumn, int activeLine, int activeColumn) GetRawPositions()
        {
            return (_anchorLine, _anchorColumn, _activeLine, _activeColumn);
        }

        public void SetRawPositions(int anchorLine, int anchorColumn, int activeLine, int activeColumn)
        {
            _anchorLine = anchorLine;
            _anchorColumn = anchorColumn;
            _activeLine = activeLine;
            _activeColumn = activeColumn;
            IncreaseVersion();
        }
    }
}
