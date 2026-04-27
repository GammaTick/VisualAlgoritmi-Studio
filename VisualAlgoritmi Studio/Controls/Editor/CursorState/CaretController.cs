using Avalonia.Media;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Editor.Text;

namespace VisualAlgoritmi_Studio.Controls.Editor.CursorState
{
    internal sealed class CaretController
    {
        private readonly TextBuffer _textBuffer;
        private readonly IGraphemeNavigator _graphemeNavigator;

        private int _version = 0;
        private int _preferredColumn = 0;
        private CharacterHit _caretCharacterHit;

        public int Line { get; private set; }
        public int Column { get; private set; }
        public int Version => _version;

        public CaretController(TextBuffer textBuffer, IGraphemeNavigator graphemeNavigator)
        {
            _textBuffer = textBuffer;
            _graphemeNavigator = graphemeNavigator;

            _caretCharacterHit = new CharacterHit(0);
        }

        private void IncreaseVersion()
        {
            unchecked
            {
                _version++;
            }
        }
        
        public void SetPosition(int line, int column)
        {
            if (Line == line && Column == column)
            {
                return;
            }

            int lineCount = _textBuffer.LineCount;
            line = Math.Clamp(line, 0, lineCount - 1);

            int lineLength = _textBuffer.GetLineLength(line);
            column = Math.Clamp(column, 0, lineLength);

            Line = line;
            Column = column;
            _preferredColumn = column;
            _caretCharacterHit = new CharacterHit(column);

            IncreaseVersion();
        }

        public void MoveRight()
        {
            if (Column == _textBuffer.GetLineLength(Line))
            {
                MoveDown(moveToLineStart: true);
                _preferredColumn = Column;
                return;
            }
            
            Column = _graphemeNavigator.GetNextIndex(Line, ref _caretCharacterHit);
            _preferredColumn = Column;
            _caretCharacterHit = new CharacterHit(Column);

            IncreaseVersion();  
        }

        public void MoveLeft()
        {
            if (Column == 0)
            {
                MoveUp(moveToLineEnd: true);
                _preferredColumn = Column;
                return;
            }

            Column = _graphemeNavigator.GetPreviousIndex(Line, ref _caretCharacterHit);
            _preferredColumn = Column;
            _caretCharacterHit = new CharacterHit(Column);
            
            IncreaseVersion();
        }

        public void MoveUp(bool moveToLineStart = false, bool moveToLineEnd = false)
        {
            if (Line == 0)
            {
                return;
            }

            int column;

            if (moveToLineStart)
            {
                column = 0;
            }
            else if (moveToLineEnd)
            {
                column = _textBuffer.GetLineLength(Line - 1);
            }
            else
            {
                int targetLine = Line - 1;
                int lineLength = _textBuffer.GetLineLength(targetLine);

                if (_preferredColumn >= lineLength)
                {
                    column = lineLength;
                }
                else
                {
                    column = _graphemeNavigator.SnapToBoundary(targetLine, _preferredColumn);
                }
            }

            Line--;
            Column = column;
            _caretCharacterHit = new CharacterHit(Column);

            IncreaseVersion();
        }

        public void MoveDown(bool moveToLineStart = false, bool moveToLineEnd = false)
        {
            if (Line == _textBuffer.LineCount - 1)
            {
                return;
            }

            int column;

            if (moveToLineStart)
            {
                column = 0;
            }
            else if (moveToLineEnd)
            {
                column = _textBuffer.GetLineLength(Line + 1);
            }
            else
            {
                int targetLine = Line + 1;
                int lineLength = _textBuffer.GetLineLength(targetLine);

                if (_preferredColumn >= lineLength)
                {
                    column = lineLength;
                }
                else
                {
                    column = _graphemeNavigator.SnapToBoundary(targetLine, _preferredColumn);
                }
            }

            Line++;
            Column = column;
            _caretCharacterHit = new CharacterHit(Column);

            IncreaseVersion();
        }
       
        public void MoveByCharCount(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            int absolutePosition = GetAbsoluteCaretPosition();
            int textLength = _textBuffer.SourceText.Length;
            int targetPosition = Math.Clamp(absolutePosition + amount, 0, textLength);

            if (targetPosition == absolutePosition)
            {
                return;
            }

            var lines = _textBuffer.SourceText.Lines;
            int lookupPosition = targetPosition == textLength && textLength > 0
                ? textLength - 1
                : targetPosition;

            var targetLine = lines.GetLineFromPosition(lookupPosition);

            Line = targetLine.LineNumber;

            if (targetPosition == textLength)
            {
                Column = targetLine.Span.Length;
            }
            else
            {
                Column = targetPosition - targetLine.Start;
            }

            _preferredColumn = Column;
            _caretCharacterHit = new CharacterHit(Column);

            IncreaseVersion();
        }

        public void MoveToLineStart()
        {
            Column = 0;
            _preferredColumn = Column;
            _caretCharacterHit = new CharacterHit(Column);
            IncreaseVersion();
        }

        public void MoveToLineEnd()
        {
            Column = _textBuffer.GetLineLength(Line);
            _preferredColumn = Column;
            _caretCharacterHit = new CharacterHit(Column);
            IncreaseVersion();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAbsoluteCaretPosition()
        {
            return _textBuffer.GetAbsolutePosition(Line, Column);
        }
    }
}
