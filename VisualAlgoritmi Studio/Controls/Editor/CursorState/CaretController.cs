using Avalonia.Media;
using System;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Editor.Text;

namespace VisualAlgoritmi_Studio.Controls.Editor.CursorState
{
    internal sealed class CaretController
    {
        private readonly TextBuffer _textBuffer;
        private readonly IGraphemeNavigator _graphemeNavigator;

        private int _line = 0;
        private int _column = 0;
        private int _preferredColumn = 0;
        private CharacterHit _caretCharacterHit;
        private int _version = 0;

        public int Line => _line;
        public int Column => _column;
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
            if (_line == line && _column == column)
            {
                return;
            }

            int lineCount = _textBuffer.LineCount;
            line = Math.Clamp(line, 0, lineCount - 1);

            int lineLength = _textBuffer.GetLineLength(line);
            column = Math.Clamp(column, 0, lineLength);

            _line = line;
            _column = column;
            _preferredColumn = column;
            _caretCharacterHit = new CharacterHit(column);

            IncreaseVersion();
        }

        public void MoveRight()
        {
            if (_column == _textBuffer.GetLineLength(_line))
            {
                MoveDown(moveToLineStart: true);
                _preferredColumn = _column;
                return;
            }
            
            _column = _graphemeNavigator.GetNextIndex(Line, ref _caretCharacterHit);
            _preferredColumn = _column;
            _caretCharacterHit = new CharacterHit(_column);

            IncreaseVersion();  
        }

        public void MoveLeft()
        {
            if (_column == 0)
            {
                MoveUp(moveToLineEnd: true);
                _preferredColumn = Column;
                return;
            }

            _column = _graphemeNavigator.GetPreviousIndex(Line, ref _caretCharacterHit);
            _preferredColumn = _column;
            _caretCharacterHit = new CharacterHit(_column);
            
            IncreaseVersion();
        }

        public void MoveUp(bool moveToLineStart = false, bool moveToLineEnd = false)
        {
            if (_line == 0)
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

            _line--;
            _column = column;
            _caretCharacterHit = new CharacterHit(_column);

            IncreaseVersion();
        }

        public void MoveDown(bool moveToLineStart = false, bool moveToLineEnd = false)
        {
            if (_line == _textBuffer.LineCount - 1)
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

            _line++;
            _column = column;
            _caretCharacterHit = new CharacterHit(_column);

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

            _line = targetLine.LineNumber;

            if (targetPosition == textLength)
            {
                _column = targetLine.Span.Length;
            }
            else
            {
                _column = targetPosition - targetLine.Start;
            }

            _preferredColumn = _column;
            _caretCharacterHit = new CharacterHit(_column);

            IncreaseVersion();
        }

        public void MoveToLineStart()
        {
            _column = 0;
            _preferredColumn = _column;
            _caretCharacterHit = new CharacterHit(_column);
            IncreaseVersion();
        }

        public void MoveToLineEnd()
        {
            _column = _textBuffer.GetLineLength(_line);
            _preferredColumn = _column;
            _caretCharacterHit = new CharacterHit(_column);
            IncreaseVersion();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetAbsoluteCaretPosition()
        {
            return _textBuffer.GetAbsolutePosition(_line, _column);
        }
    }
}
