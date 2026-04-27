using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Text;

namespace VisualAlgoritmi_Studio.Controls.Editor.Viewport
{
    internal sealed class ViewportManager
    {
        private readonly CodeEditor _codeEditor;
        private readonly TextBuffer _textBuffer;

        private int _version;
        private (int _firstVisibleLineIndex, int _lastVisibleLineIndex) _visibleLineRange = (0, 0);
        private bool _isChangeDirty = true;
        private int _lastCachedLineCount = -1;

        public double ScrollY { get; private set; } = 0;
        public double ScrollX { get; private set; } = 0;
        public int Version => _version;
        public double VerticalOffsetWithinFirstLine
        {
            get
            {
                double lineHeight = _codeEditor.GetLineHeight();

                if (lineHeight <= 0)
                {
                    return 0;
                }

                return ScrollY % lineHeight;
            }
        }

        public event EventHandler? VisibleRangeChanged;

        public ViewportManager(CodeEditor codeEditor, TextBuffer textBuffer)
        {
            _codeEditor = codeEditor;
            _textBuffer = textBuffer;
        }

        private void IncreaseVersion()
        {
            unchecked
            {
                _version++;
            }
        }

        public void InvalidateVisibleRange()
        {
            _isChangeDirty = true;

            var previousVisibleRange = _visibleLineRange;

            ReCalculateVisibleVerticalRange();

            if (previousVisibleRange != _visibleLineRange)
            {
                VisibleRangeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public (int firstVisibleLineIndex, int lastVisibleLineIndex) GetVisibleVerticalRange()
        {
            if (!_isChangeDirty)
            {
                if (_lastCachedLineCount != _textBuffer.LineCount)
                {
                    _isChangeDirty = true;
                    _lastCachedLineCount = _textBuffer.LineCount;

                    InvalidateVisibleRange();
                }

                return _visibleLineRange;
            }

            InvalidateVisibleRange();
            
            return _visibleLineRange;
        }

        /// <summary>
        /// Computes the visible line range for a specific line count, bypassing the cache.
        /// Use this when the <see cref="TextBuffer"/> has already advanced to a new version
        /// but the caller still needs the range clamped against the previous line count
        /// (e.g. the pre-mutation layout in <see cref="TextMutationSyncPipeline"/>).
        /// </summary>
        public (int firstVisibleLineIndex, int lastVisibleLineIndex) ComputeVisibleRangeForLineCount(int lineCount)
        {
            if (lineCount <= 0)
            {
                return (-1, -1);
            }

            double lineHeight = _codeEditor.GetLineHeight();
            int firstVisibleLine = (int)Math.Floor(ScrollY / lineHeight);
            int visibleLineCount = GetVisibleLineCount(lineHeight);

            if (visibleLineCount <= 0)
            {
                return (-1, -1);
            }

            int lastVisibleLine = firstVisibleLine + visibleLineCount - 1;
            int lastLineIndex = lineCount - 1;

            return (
                Math.Clamp(firstVisibleLine, 0, lastLineIndex),
                Math.Clamp(lastVisibleLine, 0, lastLineIndex)
            );
        }

        public bool AreThereVisibleLines()
        {
            (int firstVisibleLineIndex, int lastVisibleLineIndex) = GetVisibleVerticalRange();

            return firstVisibleLineIndex != -1 && lastVisibleLineIndex != -1;
        }

        private void ReCalculateVisibleVerticalRange()
        {
            if (!_isChangeDirty)
            {
                return;
            }

            double lineHeight = _codeEditor.GetLineHeight();

            if (_textBuffer.LineCount <= 0)
            {
                _visibleLineRange = (-1, -1);
                _isChangeDirty = false;
                return;
            }

            int firstVisibleLine = (int)Math.Floor(ScrollY / lineHeight);

            int visibleLineCount = GetVisibleLineCount(lineHeight);

            if (visibleLineCount <= 0)
            {
                _visibleLineRange = (-1, -1);
                _isChangeDirty = false;
                return;
            }

            int lastVisibleLine = firstVisibleLine + visibleLineCount - 1;

            int lastLineIndex = _textBuffer.LineCount - 1;

            int clampedFirstVisible = Math.Clamp(firstVisibleLine, 0, lastLineIndex);
            int clampedLastVisible = Math.Clamp(lastVisibleLine, 0, lastLineIndex);

            _visibleLineRange = (clampedFirstVisible, clampedLastVisible);
            _isChangeDirty = false;
        }

        public bool IsDocumentLineVisible(int line)
        {
            (int firstVisibleLineIndex, int lastVisibleLineIndex) = GetVisibleVerticalRange();

            if (line < firstVisibleLineIndex || line > lastVisibleLineIndex)
            {              
                return false;
            }

            return true;
        }

        public static bool IsDocumentLineVisible(int line, (int firstVisibleLineIndex, int lastVisibleLineIndex) cachedVisibleRange)
        {
            if (line < cachedVisibleRange.firstVisibleLineIndex || line > cachedVisibleRange.lastVisibleLineIndex)
            {
                return false;
            }

            return true;
        }

        public void EnsureCaretIsVisible(CaretController caretController)
        {
            double lineHeight = _codeEditor.GetLineHeight();
            double viewportHeight = _codeEditor.ViewportHeight;

            if (viewportHeight <= 0 || lineHeight <= 0)
            {
                return;
            }

            int caretLine = caretController.Line;
            double caretTop = caretLine * lineHeight;
            double caretBottom = caretTop + lineHeight;
            double viewportBottom = ScrollY + viewportHeight;
            double newScrollY = ScrollY;

            if (caretTop < ScrollY)
            {
                newScrollY = caretTop;
            }
            else if (caretBottom > viewportBottom)
            {
                newScrollY = caretBottom - viewportHeight;
            }

            newScrollY = Math.Clamp(newScrollY, 0, _codeEditor.MaxScrollY);

            if (newScrollY.Equals(ScrollY))
            {
                return;
            }

            ScrollY = newScrollY;
            InvalidateVisibleRange();
            IncreaseVersion();
        }

        public void ScrollByLines(int lines)
        {
            double lineHeight = _codeEditor.GetLineHeight();
            double maxScrollY = _codeEditor.MaxScrollY;

            if (maxScrollY <= 0)
            {
                return;
            }

            ScrollY = Math.Clamp(ScrollY + lines * lineHeight, 0, maxScrollY);

            InvalidateVisibleRange();
            IncreaseVersion();
        }

        public void ScrollToY(double newScrollY)
        {
            ScrollY = Math.Clamp(newScrollY, 0, _codeEditor.MaxScrollY);

            InvalidateVisibleRange();
            IncreaseVersion();
        }

        public void ScrollToX(double newScrollX)
        {
            ScrollX = Math.Clamp(newScrollX, 0, _codeEditor.MaxScrollX);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetVisibleLineCount(double lineHeight)
        {
            if (lineHeight <= 0)
            {
                return 0;
            }

            double viewportHeight = _codeEditor.GetCodeAreaHeight();

            if (viewportHeight <= 0)
            {
                return 0;
            }

            return (int)Math.Ceiling((viewportHeight + VerticalOffsetWithinFirstLine) / lineHeight);
        }
    }
}
