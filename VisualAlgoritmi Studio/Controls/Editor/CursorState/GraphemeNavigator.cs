using Avalonia.Media;
using System.Globalization;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Editor.LayoutsManagement;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;

namespace VisualAlgoritmi_Studio.Controls.Editor.CursorState
{
    internal sealed class GraphemeNavigator: IGraphemeNavigator
    {
        private readonly TextBuffer _textBuffer;
        private readonly CodeLayoutManager _codeLayoutManager;
        private readonly ViewportManager _viewportManager;

        public GraphemeNavigator(TextBuffer textBuffer, CodeLayoutManager codeLayoutManager, ViewportManager viewportManager)
        {
            _textBuffer = textBuffer;
            _codeLayoutManager = codeLayoutManager;
            _viewportManager = viewportManager;
        }

        // =============================
        // PUBLIC API
        // =============================

        public int GetNextIndex(int line, ref CharacterHit characterHit)
        {
            if (_viewportManager.IsDocumentLineVisible(line))
            {
                return GetNextWithLayouts(line, ref characterHit);
            }

            return GetNextWithStringInfo(line, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
        }

        public int GetPreviousIndex(int line, ref CharacterHit characterHit)
        {
            if (_viewportManager.IsDocumentLineVisible(line))
            {
                return GetPreviousWithLayouts(line, ref characterHit);
            }

            return GetPreviousWithStringInfo(line, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
        }

        public int GetPreviousIndex(int line, int column)
        {
            if (_viewportManager.IsDocumentLineVisible(line))
            {
                var hit = new CharacterHit(column, 0);
                return GetPreviousWithLayouts(line, ref hit);
            }

            return GetPreviousWithStringInfo(line, column);
        }

        public int SnapToBoundary(int line, int column)
        {
            if (_viewportManager.IsDocumentLineVisible(line))
            {
                return SnapWithLayouts(line, column);
            }

            return SnapWithStringInfo(line, column);
        }

        // =============================
        // LAYOUT BASED
        // =============================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNextWithLayouts(int line, ref CharacterHit characterHit)
        {
            if (!_viewportManager.AreThereVisibleLines())
            {
                return GetNextWithStringInfo(line, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
            }

            (int firstVisibleLineIndex, _) = _viewportManager.GetVisibleVerticalRange();

            int localLine = line - firstVisibleLineIndex;

            var layout = _codeLayoutManager.LineLayouts[localLine].TextLayout;

            var textLine = layout.TextLines[0];

            characterHit = textLine.GetNextCaretCharacterHit(characterHit);

            return characterHit.FirstCharacterIndex + characterHit.TrailingLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPreviousWithLayouts(int line, ref CharacterHit characterHit)
        {
            if (!_viewportManager.AreThereVisibleLines())
            {
                return GetPreviousWithStringInfo(line, characterHit.FirstCharacterIndex + characterHit.TrailingLength);
            }

            (int firstVisibleLineIndex, _) = _viewportManager.GetVisibleVerticalRange();

            int localLine = line - firstVisibleLineIndex;

            var layout = _codeLayoutManager.LineLayouts[localLine].TextLayout;

            var textLine = layout.TextLines[0];

            characterHit = textLine.GetPreviousCaretCharacterHit(characterHit);

            return characterHit.FirstCharacterIndex + characterHit.TrailingLength;
        }

        private int SnapWithLayouts(int line, int column)
        {
            if (column <= 0)
            {
                return 0;
            }

            if (!_viewportManager.AreThereVisibleLines())
            {
                return SnapWithStringInfo(line, column);
            }

            (int firstVisibleLineIndex, _) = _viewportManager.GetVisibleVerticalRange();

            int localLine = line - firstVisibleLineIndex;

            var layout = _codeLayoutManager.LineLayouts[localLine].TextLayout;

            var textLine = layout.TextLines[0];

            var hit = new CharacterHit(column, 0);

            var prev = textLine.GetPreviousCaretCharacterHit(hit);
            var next = textLine.GetNextCaretCharacterHit(hit);

            int prevIndex = prev.FirstCharacterIndex + prev.TrailingLength;
            int nextIndex = next.FirstCharacterIndex + next.TrailingLength;

            int distPrev = column - prevIndex;
            int distNext = nextIndex - column;

            if (distPrev == distNext)
            {
                return column;
            }

            if (distPrev < distNext)
            {
                return prevIndex;
            }

            return nextIndex;
        }

        // =============================
        // STRINGINFO FALLBACK
        // =============================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetNextWithStringInfo(int line, int column)
        {
            int lineLength = _textBuffer.GetLineLength(line);

            if (lineLength == 0)
            {
                return 0;
            }

            if (column >= lineLength)
            {
                return lineLength;
            }

            var textLine = _textBuffer.GetLine(line);

            TextElementEnumerator enumerator =
                StringInfo.GetTextElementEnumerator(textLine.ToString());

            while (enumerator.MoveNext())
            {
                int elementIndex = enumerator.ElementIndex;

                if (elementIndex > column)
                {
                    return elementIndex;
                }
            }

            return lineLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPreviousWithStringInfo(int line, int column)
        {
            int lineLength = _textBuffer.GetLineLength(line);

            if (lineLength == 0)
            {
                return 0;
            }

            if (column <= 0)
            {
                return 0;
            }

            if (column > lineLength)
            {
                column = lineLength;
            }

            var textLine = _textBuffer.GetLine(line);

            TextElementEnumerator enumerator =
                StringInfo.GetTextElementEnumerator(textLine.ToString());

            int previousBoundary = 0;

            while (enumerator.MoveNext())
            {
                int elementIndex = enumerator.ElementIndex;

                if (elementIndex >= column)
                {
                    break;
                }

                previousBoundary = elementIndex;
            }

            return previousBoundary;
        }

        private int SnapWithStringInfo(int line, int column)
        {
            int lineLength = _textBuffer.GetLineLength(line);

            if (lineLength == 0)
            {
                return 0;
            }

            if (column <= 0)
            {
                return 0;
            }

            if (column >= lineLength)
            {
                return lineLength;
            }

            var textLine = _textBuffer.GetLine(line).ToString();

            TextElementEnumerator e =
                StringInfo.GetTextElementEnumerator(textLine);

            int prevBoundary = 0;
            int nextBoundary = lineLength;

            while (e.MoveNext())
            {
                int start = e.ElementIndex;

                if (start == column)
                {
                    return column;
                }

                if (start < column)
                {
                    prevBoundary = start;
                    continue;
                }

                nextBoundary = start;
                break;
            }

            int distToPrev = column - prevBoundary;
            int distToNext = nextBoundary - column;

            if (distToPrev <= distToNext)
            {
                return prevBoundary;
            }

            return nextBoundary;
        }
    }
}