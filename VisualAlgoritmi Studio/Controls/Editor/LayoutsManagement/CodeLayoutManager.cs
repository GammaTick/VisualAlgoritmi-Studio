using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Editor.SyntaxHighlighting;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;

namespace VisualAlgoritmi_Studio.Controls.Editor.LayoutsManagement
{
    internal sealed class CodeLayoutManager
    {
        private const int LargeViewportJumpThreshold = 24;

        private readonly CodeEditor _codeEditor;
        private readonly TextBuffer _textBuffer;
        private readonly ViewportManager _viewportManager;
        private readonly SyntaxHighlighterController _syntaxHighlighterController;

        private readonly List<LineLayout> _lineLayouts;

        public List<LineLayout> LineLayouts => _lineLayouts;

        public CodeLayoutManager(CodeEditor codeEditor,
            TextBuffer textBuffer,
            ViewportManager viewportManager,
            SyntaxHighlighterController syntaxHighlighterController)
        {
            _codeEditor = codeEditor;
            _textBuffer = textBuffer;
            _viewportManager = viewportManager;
            _syntaxHighlighterController = syntaxHighlighterController;

            _lineLayouts = new(4)
            {
                new LineLayout(0, CreateEmptyTextLayout())
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RefreshLine(int documentLine)
        {
            if (!_viewportManager.AreThereVisibleLines())
            {
                SynchWithViewport();
                return;
            }

            int localLine = ToLocalLine(documentLine);

            if ((uint)localLine >= (uint)_lineLayouts.Count)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            if (!_viewportManager.IsDocumentLineVisible(documentLine))
            {
                SynchWithViewport();
                return;
            }

            var newTextLine = _textBuffer.GetLine(documentLine);
            bool stateChanged = _syntaxHighlighterController.ApplyLexicalHighlightingForLine(documentLine);
            _lineLayouts[localLine] = new LineLayout(documentLine, CreateTextLayout(newTextLine));

            if (stateChanged)
            {
                PropagateHighlighting(documentLine + 1);
            }
        }

        /// <summary>
        /// Rebuilds TextLayouts only for specific document lines whose highlighting changed.
        /// Does NOT re-run highlighting — the cache is already up to date.
        /// </summary>
        public void RefreshLineLayouts(IReadOnlyList<int> documentLines)
        {
            if (!_viewportManager.AreThereVisibleLines() || documentLines.Count == 0) return;

            for (int i = 0; i < documentLines.Count; i++)
            {
                int documentLine = documentLines[i];
                if (!_viewportManager.IsDocumentLineVisible(documentLine)) continue;

                int localLine = ToLocalLine(documentLine);
                if ((uint)localLine >= (uint)_lineLayouts.Count) continue;

                var textLine = _textBuffer.GetLine(documentLine);
                _lineLayouts[localLine].TextLayout?.Dispose();
                _lineLayouts[localLine] = new LineLayout(documentLine, CreateTextLayout(textLine));
            }
        }

        private void PropagateHighlighting(int startDocumentLine)
        {
            (_, int lastVisibleLine) = _viewportManager.GetVisibleVerticalRange();
            int lastLine = Math.Min(lastVisibleLine, _textBuffer.LineCount - 1);

            for (int docLine = startDocumentLine; docLine <= lastLine; docLine++)
            {
                int local = ToLocalLine(docLine);

                if ((uint)local >= (uint)_lineLayouts.Count)
                {
                    break;
                }

                bool cascadeNeeded = _syntaxHighlighterController.ApplyLexicalHighlightingForLine(docLine);
                var textLine = _textBuffer.GetLine(docLine);

                _lineLayouts[local].TextLayout?.Dispose();
                _lineLayouts[local] = new LineLayout(docLine, CreateTextLayout(textLine));

                if (!cascadeNeeded)
                {
                    break;
                }
            }
        }

        public void RefreshRange(int startLine, int endLine)
        {
            if (startLine > endLine)
            {
                (startLine, endLine) = (endLine, startLine);
            }

            if (!_viewportManager.AreThereVisibleLines())
            {
                SynchWithViewport();
                return;
            }

            (int firstVisibleLine, int lastVisibleLine) = _viewportManager.GetVisibleVerticalRange();

            int visibleStart = Math.Max(startLine, firstVisibleLine);
            int visibleEnd = Math.Min(endLine, lastVisibleLine);

            if (visibleStart > visibleEnd)
            {
                return;
            }

            for (int documentLine = visibleStart; documentLine <= visibleEnd; documentLine++)
            {
                RefreshLine(documentLine);
            }
        }

        public void InsertLine(int documentLine, Microsoft.CodeAnalysis.Text.TextLine? initialTextLine = null)
        {
            _syntaxHighlighterController.ApplyLexicalHighlightingForLine(documentLine);

            if (!_viewportManager.AreThereVisibleLines())
            {
                SynchWithViewport();
                return;
            }

            int localLine = ToLocalLine(documentLine);

            TextLayout textLayout = initialTextLine.HasValue
                ? CreateTextLayout(initialTextLine.Value)
                : CreateEmptyTextLayout();

            _lineLayouts.Insert(localLine, new LineLayout(documentLine, textLayout));

            // Increment document line number for all subsequent lines to keep them in sync with the text buffer
            for (int i = localLine + 1; i < _lineLayouts.Count; i++)
            {
                _lineLayouts[i] = new LineLayout(_lineLayouts[i].DocumentLine + 1, _lineLayouts[i].TextLayout);
            }
        }

        public void DeleteLine(int documentLine)
        {
            if (!_viewportManager.AreThereVisibleLines())
            {
                SynchWithViewport();
                return;
            }

            int localLine = ToLocalLine(documentLine);

            if ((uint)localLine >= (uint)_lineLayouts.Count)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            _lineLayouts[localLine].TextLayout?.Dispose();
            _lineLayouts.RemoveAt(localLine);

            // Decrement document line number for all subsequent lines to keep them in sync with the text buffer
            for (int i = localLine; i < _lineLayouts.Count; i++)
            {
                _lineLayouts[i] = new LineLayout(_lineLayouts[i].DocumentLine - 1, _lineLayouts[i].TextLayout);
            }
        }

        public void RebuildFullLayout()
        {
            ClearLayouts();

            if (!_viewportManager.AreThereVisibleLines())
            {
                return;
            }

            (int firstVisibleLineIndex, int lastVisibleLineIndex) = _viewportManager.GetVisibleVerticalRange();

            for (int i = firstVisibleLineIndex; i <= lastVisibleLineIndex; i++)
            {
                _syntaxHighlighterController.ApplyLexicalHighlightingForLine(i);

                var textLine = _textBuffer.GetLine(i);
                var lineLayout = CreateTextLayout(textLine);

                _lineLayouts.Add(new LineLayout(i, lineLayout));
            }
        }

        public void RebuildFromLineToLastVisible(int startDocumentLine)
        {
            if (!_viewportManager.AreThereVisibleLines())
            {
                SynchWithViewport();
                return;
            }

            int localStartLine = ToLocalLine(startDocumentLine);

            if ((uint)localStartLine >= (uint)_lineLayouts.Count)
            {
                ThrowHelper.ThrowLineOutOfRange();
            }

            for (int i = localStartLine; i < _lineLayouts.Count; i++)
            {
                _lineLayouts[i].TextLayout?.Dispose();
            }

            _lineLayouts.RemoveRange(localStartLine, _lineLayouts.Count - localStartLine);

            (_, int lastVisibleLineIndex) = _viewportManager.GetVisibleVerticalRange();

            for (int i = startDocumentLine; i <= lastVisibleLineIndex; i++)
            {
                _syntaxHighlighterController.ApplyLexicalHighlightingForLine(i);

                var textLine = _textBuffer.GetLine(i);
                var lineLayout = CreateTextLayout(textLine);

                _lineLayouts.Add(new LineLayout(i, lineLayout));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RebuildLayoutsForRange(int firstDocumentLine, int lastDocumentLine)
        {
            ClearLayouts();

            for (int i = firstDocumentLine; i <= lastDocumentLine; i++)
            {
                var textLine = _textBuffer.GetLine(i);

                _syntaxHighlighterController.ApplyLexicalHighlightingForLine(i);

                var textLayout = CreateTextLayout(textLine);

                _lineLayouts.Add(new LineLayout(i, textLayout));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendLayouts(int startDocumentLine, int endDocumentLine)
        {
            for (int i = startDocumentLine; i <= endDocumentLine; i++)
            {
                var textLine = _textBuffer.GetLine(i);

                _syntaxHighlighterController.ApplyLexicalHighlightingForLine(i);

                var textLayout = CreateTextLayout(textLine);

                _lineLayouts.Add(new LineLayout(i, textLayout));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisposeRange(int startIndex, int count)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                _lineLayouts[i].TextLayout?.Dispose();
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SynchWithViewport()
        {
            if (!_viewportManager.AreThereVisibleLines())
            {
                if (_lineLayouts.Count > 0)
                {
                    ClearLayouts();
                }

                return;
            }

            (int firstVisibleLineIndex, int lastVisibleLineIndex) = _viewportManager.GetVisibleVerticalRange();

            if (_lineLayouts.Count == 0)
            {
                RebuildLayoutsForRange(firstVisibleLineIndex, lastVisibleLineIndex);
                return;
            }

            int firstRecordedDocumentLine = _lineLayouts[0].DocumentLine;
            int lastRecordedDocumentLine = _lineLayouts[^1].DocumentLine;

            bool hasNoOverlap =
                lastRecordedDocumentLine < firstVisibleLineIndex ||
                firstRecordedDocumentLine > lastVisibleLineIndex;

            if (hasNoOverlap)
            {
                RebuildLayoutsForRange(firstVisibleLineIndex, lastVisibleLineIndex);
                return;
            }

            int trimBottomCount = lastRecordedDocumentLine > lastVisibleLineIndex
                ? lastRecordedDocumentLine - lastVisibleLineIndex
                : 0;

            int trimTopCount = firstRecordedDocumentLine < firstVisibleLineIndex
                ? firstVisibleLineIndex - firstRecordedDocumentLine
                : 0;

            int appendBottomCount = lastRecordedDocumentLine < lastVisibleLineIndex
                ? lastVisibleLineIndex - lastRecordedDocumentLine
                : 0;

            int prependTopCount = firstRecordedDocumentLine > firstVisibleLineIndex
                ? firstRecordedDocumentLine - firstVisibleLineIndex
                : 0;

            int totalChangedLines =
                trimBottomCount +
                trimTopCount +
                appendBottomCount +
                prependTopCount;

            // Rebuild when:
            // 1. the viewport jump is large
            // 2. we would need to prepend at the top, because Insert(0, ...) is expensive
            if (totalChangedLines >= LargeViewportJumpThreshold || prependTopCount > 0)
            {
                RebuildLayoutsForRange(firstVisibleLineIndex, lastVisibleLineIndex);
                return;
            }

            // Trim bottom first so indices used by top trimming stay simple.
            if (trimBottomCount > 0)
            {
                int removeStartIndex = _lineLayouts.Count - trimBottomCount;

                DisposeRange(removeStartIndex, trimBottomCount);
                _lineLayouts.RemoveRange(removeStartIndex, trimBottomCount);
            }

            if (trimTopCount > 0)
            {
                DisposeRange(0, trimTopCount);
                _lineLayouts.RemoveRange(0, trimTopCount);
            }

            if (appendBottomCount > 0)
            {
                int newLastRecordedDocumentLine = _lineLayouts[^1].DocumentLine;
                AppendLayouts(newLastRecordedDocumentLine + 1, lastVisibleLineIndex);
            }
        }

        private TextLayout CreateTextLayout(Microsoft.CodeAnalysis.Text.TextLine textLine)
        {
            return new TextLayout(
                textLine.ToString(),
                _codeEditor.Typeface,
                _codeEditor.FontSize,
                _codeEditor.Foreground,
                TextAlignment.Left,
                maxWidth: double.PositiveInfinity,
                maxHeight: double.PositiveInfinity,
                lineHeight: _codeEditor.GetLineHeight(),
                textStyleOverrides: _syntaxHighlighterController.GetHighlightingForLine(textLine.LineNumber)
            );
        }

        private TextLayout CreateEmptyTextLayout()
        {
            return new TextLayout(
                string.Empty,
                _codeEditor.Typeface,
                _codeEditor.FontSize,
                _codeEditor.Foreground,
                TextAlignment.Left,
                maxWidth: double.PositiveInfinity,
                maxHeight: double.PositiveInfinity,
                lineHeight: _codeEditor.GetLineHeight()
            );
        }
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ToLocalLine(int documentLine)
        {
            // Use the first layout entry's DocumentLine as the offset base rather than the
            // viewport's current firstVisibleLine.  The two are identical when the layout is
            // synced, but during a mutation the viewport may already reflect the new buffer
            // while the layout still holds the pre-mutation document line numbers.  Deriving
            // the local index from the layout itself keeps the arithmetic correct regardless
            // of whether firstVisibleLine was clamped by a line-count change.
            if (_lineLayouts.Count == 0)
            {
                return -1;
            }

            return documentLine - _lineLayouts[0].DocumentLine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearLayouts()
        {
            foreach (var lineLayout in _lineLayouts)
            {
                lineLayout.TextLayout?.Dispose();
            }

            _lineLayouts.Clear();
        }

        internal struct LineLayout
        {
            public int DocumentLine;
            public TextLayout TextLayout;

            public LineLayout(int documentLine, TextLayout textLayout)
            {
                DocumentLine = documentLine;
                TextLayout = textLayout;
            }
        }

        private static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowLineOutOfRange()
            {
                throw new ArgumentOutOfRangeException("line");
            }
        }
    }
}