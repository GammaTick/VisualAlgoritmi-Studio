using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;

namespace VisualAlgoritmi_Studio.Controls.Editor.LayoutsManagement
{
    internal sealed class LineNumbersLayoutManager
    {
        private static readonly double DefaultLineNumbersLayoutWidth = 40;
        private static readonly double LineNumberSeparatorPadding = 10;
        private static readonly Pen LineNumberSeparatorPen = new(new SolidColorBrush(Color.Parse("#5B5B5B")), 1);

        private readonly CodeEditor _codeEditor;
        private readonly ViewportManager _viewportManager;

        private TextLayout _lineNumbersLayout = null!;

        public LineNumbersLayoutManager(CodeEditor codeEditor, ViewportManager viewportManager)
        {
            _codeEditor = codeEditor;
            _viewportManager = viewportManager;

            RebuildLayout();
        }

        public void RebuildLayout()
        {
            StringBuilder lines = new();

            if (!_viewportManager.AreThereVisibleLines())
            {
                _lineNumbersLayout = CreateEmptyTextLayout();
                return;
            }

            (int firstVisibleLineIndex, int lastVisibleLineIndex) = _viewportManager.GetVisibleVerticalRange();

            // Convert from 0-based line indexes (used internally) to 1-based line numbers
            // for display in the line number margin.
            firstVisibleLineIndex++;
            lastVisibleLineIndex++;

            for (int i = firstVisibleLineIndex; i <= lastVisibleLineIndex; i++)
            {
                lines.AppendLine(i.ToString());
            }

            _lineNumbersLayout = new TextLayout(
                lines.ToString(),
                _codeEditor.Typeface,
                _codeEditor.FontSize,
                _codeEditor.LineNumbersForeground,
                TextAlignment.Right,
                maxWidth: DefaultLineNumbersLayoutWidth,
                lineHeight: _codeEditor.GetLineHeight()
            );
        }

        private TextLayout CreateEmptyTextLayout()
        {
            return new TextLayout(
                string.Empty,
                _codeEditor.Typeface,
                _codeEditor.FontSize,
                _codeEditor.LineNumbersForeground,
                TextAlignment.Right,
                maxWidth: DefaultLineNumbersLayoutWidth,
                lineHeight: _codeEditor.GetLineHeight()
            );
        }

        public void Draw(DrawingContext context)
        {
            double editorContentMargin = _codeEditor.ContentMargin;

            double lineNumbersHeight = Math.Max(0, _codeEditor.GetCodeAreaHeight());
            var lineNumbersClip = new Avalonia.Rect(editorContentMargin, editorContentMargin, DefaultLineNumbersLayoutWidth, lineNumbersHeight);

            using (context.PushClip(lineNumbersClip))
            {
                double y = editorContentMargin - _viewportManager.VerticalOffsetWithinFirstLine;
                _lineNumbersLayout!.Draw(context, new Avalonia.Point(editorContentMargin, y));
            }

            double x = editorContentMargin + DefaultLineNumbersLayoutWidth + LineNumberSeparatorPadding;

            context.DrawLine(LineNumberSeparatorPen, new Avalonia.Point(x, 0), new Avalonia.Point(x, _codeEditor.Bounds.Height));
        }

        public double GetLayoutEndX()
        {
            return _codeEditor.ContentMargin + DefaultLineNumbersLayoutWidth + LineNumberSeparatorPadding * 2;
        }
    }
}
