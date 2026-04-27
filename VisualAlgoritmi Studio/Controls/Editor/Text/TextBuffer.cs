using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace VisualAlgoritmi_Studio.Controls.Editor.Text
{
    internal sealed class TextBuffer
    {
        private const int MAX_LINES_COUNT = 3_000;

        private SourceText _sourceText = SourceText.From(string.Empty, Encoding.UTF8);

        private int _version = 0;

        /// <summary>
        /// Raised when a text mutation is rejected because it would exceed the maximum line count.
        /// </summary>
        public event Action? MaxLinesReached;

        public int TextLength => _sourceText.Length;
        public int Version => _version;
        public int LineCount => _sourceText.Lines.Count;
        public SourceText SourceText => _sourceText;

        public void IncreaseVersion()
        {
            unchecked
            {
                _version++;
            }
        }

        public int GetAbsolutePosition(int line, int column)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            var lineSpan = _sourceText.Lines[line].Span;

            if ((uint)column > (uint)lineSpan.Length)
            {
                ThrowHelper.ThrowColumnOutOfRange();
            }

            return lineSpan.Start + column;
        }

        public TextChange DeleteLine(int line)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            TextSpan lineSpan;

            if (LineCount == 1)
            {
                lineSpan = new TextSpan(0, _sourceText.Length);
            }
            else if (line < LineCount - 1)
            {
                lineSpan = _sourceText.Lines[line].SpanIncludingLineBreak;
            }
            else 
            {
                int currentLineEnd = _sourceText.Lines[line].End;
                int previousLineEnd = _sourceText.Lines[line - 1].End;
                lineSpan = new TextSpan(previousLineEnd, currentLineEnd - previousLineEnd);
            }

            var change = new TextChange(lineSpan, string.Empty);

            _sourceText = _sourceText.WithChanges(change);

            IncreaseVersion();
            
            return change;
        }

        public TextChange DeleteRange(int startLine, int startColumn, int endLine, int endColumn)
        {
            Normalize(ref startLine, ref startColumn, ref endLine, ref endColumn);

            if ((uint)startLine >= (uint)LineCount || (uint)endLine >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            var startLineSpan = _sourceText.Lines[startLine].Span;
            var endLineSpan = _sourceText.Lines[endLine].Span;

            if ((uint)startColumn > (uint)startLineSpan.Length || (uint)endColumn > (uint)endLineSpan.Length)
            {
                ThrowHelper.ThrowColumnOutOfRange();
            }

            int startPosition = startLineSpan.Start + startColumn;
            int endPosition = endLineSpan.Start + endColumn;

            var bulkSpan = new TextSpan(startPosition, endPosition - startPosition);
            var change = new TextChange(bulkSpan, string.Empty);

            _sourceText = _sourceText.WithChanges(change);

            IncreaseVersion();

            return change;
        }

        public TextChange ReplaceSpan(int start, int length, string newText)
        {
            if ((uint)start > (uint)_sourceText.Length || (uint)length > (uint)(_sourceText.Length - start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            if (newText == null)
            {
                ThrowHelper.ThrowTextArgumentNullException();
            }

            var change = new TextChange(new TextSpan(start, length), newText);

            _sourceText = _sourceText.WithChanges(change);

            IncreaseVersion();

            return change;
        }

        public TextLine GetLine(int line)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            return _sourceText.Lines[line];
        }

        public int GetLineIndentEndColumn(int line)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            var textLine = _sourceText.Lines[line];
            int start = textLine.Start; // The absolute position where the line begins
            int length = textLine.Span.Length;

            int i = 0;

            // We check (start + i) to look at the correct position in the buffer
            while (i < length && _sourceText[start + i] == ' ')
            {
                i++;
            }

            return i;
        }

        public int GetLineLength(int line)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            return _sourceText.Lines[line].Span.Length;
        }

        public string GetText()
        {
            return _sourceText.ToString();
        }

        public string GetRange(int startLine, int startColumn, int endLine, int endColumn)
        {
            Normalize(ref startLine, ref startColumn, ref endLine, ref endColumn);

            if ((uint)startLine >= (uint)LineCount || (uint)endLine >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            var startLineSpan = _sourceText.Lines[startLine].Span;
            var endLineSpan = _sourceText.Lines[endLine].Span;

            if ((uint)startColumn > (uint)startLineSpan.Length
                || (uint)endColumn > (uint)endLineSpan.Length)
            {
                ThrowHelper.ThrowColumnOutOfRange();
            }

            int absoluteStart = startLineSpan.Start + startColumn;
            int absoluteEnd = endLineSpan.Start + endColumn;

            var bulkSpan = new TextSpan(absoluteStart, absoluteEnd - absoluteStart);

            return _sourceText.ToString(bulkSpan);
        }

        public TextChange? InsertNewLineAtPosition(int line, int column)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            var lineSpan = _sourceText.Lines[line].Span;

            if ((uint)column > (uint)lineSpan.Length)
            {
                ThrowHelper.ThrowColumnOutOfRange();
            }

            if (_sourceText.Lines.Count >= MAX_LINES_COUNT)
            {
                MaxLinesReached?.Invoke();
                return null;
            }

            int absolutePosition = lineSpan.Start + column;
            var textChange = new TextChange(new TextSpan(absolutePosition, 0), Environment.NewLine);

            _sourceText = _sourceText.WithChanges(textChange);

            IncreaseVersion();

            return textChange;
        }

        public TextChange? InsertText(int line, int column, string text)
        {
            if (text == null)
            {
                ThrowHelper.ThrowTextArgumentNullException();
            }

            if (text.Length == 0)
            {
                return null;
            }

            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            var lineSpan = _sourceText.Lines[line].Span;

            if ((uint)column > (uint)lineSpan.Length)
            {
                ThrowHelper.ThrowColumnOutOfRange();
            }

            int absolutePosition = lineSpan.Start + column;
            var change = new TextChange(new TextSpan(absolutePosition, 0), text);

            _sourceText = _sourceText.WithChanges(change);

            IncreaseVersion();
            return change;
        }

        public TextChange? MergeLineWithPrevious(int line)
        {
            if ((uint)line >= (uint)LineCount)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            if (line == 0)
            {
                return null;
            }

            var textLine = _sourceText.Lines[line - 1];
            int start = textLine.End;
            int lineBreakLength = textLine.EndIncludingLineBreak - textLine.End;

            var change = new TextChange(new TextSpan(start, lineBreakLength), string.Empty);
            _sourceText = _sourceText.WithChanges(change);

            IncreaseVersion();
            return change;
        }

        public TextChange SetText(string text)
        {
            if (text is null)
            {
                ThrowHelper.ThrowTextArgumentNullException();
            }

            var fullSpan = new TextSpan(0, _sourceText.Length);

            var change = new TextChange(fullSpan, text);

            var tempSourceText = _sourceText.WithChanges(change);

            if (tempSourceText.Lines.Count > MAX_LINES_COUNT)
            {
                var lastAllowedLine = tempSourceText.Lines[MAX_LINES_COUNT - 1];

                var allowedSpan = new TextSpan(0, lastAllowedLine.End);

                var truncatedText = tempSourceText.GetSubText(allowedSpan);

                tempSourceText = truncatedText;
            }

            _sourceText = tempSourceText;

            IncreaseVersion();
            return change;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Normalize(ref int sl, ref int sc, ref int el, ref int ec)
        {
            if (sl > el || sl == el && sc > ec)
            {
                (sl, el) = (el, sl);
                (sc, ec) = (ec, sc);
            }
        }

        public void ApplyChange(TextChange textChange)
        {
            _sourceText = _sourceText.WithChanges(textChange);

            IncreaseVersion();
        }

        private static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowLineOutOfRange()
            {
                throw new ArgumentOutOfRangeException("line");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowColumnOutOfRange()
            {        
                throw new ArgumentOutOfRangeException("column");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowTextArgumentNullException()
            {
                throw new ArgumentNullException("text");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowArgumentOutOfRangeException()
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}