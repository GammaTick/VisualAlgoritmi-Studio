using System;
using Microsoft.CodeAnalysis.Text;
using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.LayoutsManagement;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;
using VisualAlgoritmi_Studio.RoslynCore;

namespace VisualAlgoritmi_Studio.Controls.Editor.Input
{
    internal class TextMutationSyncPipeline 
    {
        private readonly TextBuffer _textBuffer;
        private readonly CaretController _caretController;
        private readonly SelectionController _selectionController;
        private readonly CodeLayoutManager _codeLayoutManager;
        private readonly ViewportManager _viewportManager;
        private readonly CodeAnalysisSession _codeAnalysisSession;
        private readonly Action<int, int>? _onLineCountChanged;

        public TextMutationSyncPipeline(TextBuffer textBuffer,
            CaretController caretController,
            SelectionController selectionController,
            CodeLayoutManager codeLayoutManager,
            ViewportManager viewportManager,
            CodeAnalysisSession codeAnalysisSession,
            Action<int, int>? onLineCountChanged = null)
        {
            _textBuffer = textBuffer;
            _caretController = caretController;
            _selectionController = selectionController;
            _viewportManager = viewportManager; 
            _codeLayoutManager = codeLayoutManager;
            _codeAnalysisSession = codeAnalysisSession;
            _onLineCountChanged = onLineCountChanged;
        }

        public void SynchronizeEditorAfterTextChange(SourceText oldSourceText, TextChange? textChange, Action? caretAfter = null, bool shouldClearSelection = true)
        {
            if (!textChange.HasValue)
            {
                return;
            }

            TextChange change = textChange.Value;

            _codeAnalysisSession.SetPendingSourceText(_textBuffer.SourceText);

            caretAfter?.Invoke();

            if (shouldClearSelection)
            {
                _selectionController.CollapseTo(_caretController.Line, _caretController.Column);
            }

            if (_viewportManager.AreThereVisibleLines())
            {
                int startPosition = change.Span.Start;
                int endPosition = change.Span.End;

                int startLine = oldSourceText.Lines.GetLineFromPosition(startPosition).LineNumber;
                int endLine = oldSourceText.Lines.GetLineFromPosition(endPosition).LineNumber;

                if (startLine > endLine)
                {
                    (startLine, endLine) = (endLine, startLine);
                }

                int lineDelta = _textBuffer.SourceText.Lines.Count - oldSourceText.Lines.Count;

                if (lineDelta != 0)
                {
                    _onLineCountChanged?.Invoke(startLine, lineDelta);
                }

                if (lineDelta == 0) // only a single line was changed just refresh the line
                {
                    // If the line is visible refresh it, otherwise leave it to the SynchWithViewport method
                    // to remove it
                    if (_viewportManager.IsDocumentLineVisible(startLine))
                    {
                        _codeLayoutManager.RefreshLine(startLine);
                    }
                }
                else if (lineDelta > 0) // lines were added
                {
                    if (_viewportManager.IsDocumentLineVisible(startLine))
                    {
                        _codeLayoutManager.RefreshLine(startLine);
                    }

                    (int firstVisibleLine, int lastVisibleLine) = _viewportManager.GetVisibleVerticalRange();
                    int clampedInsertStart = Math.Max(startLine + 1, firstVisibleLine);
                    int clampedInsertEnd   = Math.Min(startLine + lineDelta, lastVisibleLine);

                    for (int i = clampedInsertStart; i <= clampedInsertEnd; i++)
                    {
                        if (_textBuffer.GetLineLength(i) == 0)
                        {
                            _codeLayoutManager.InsertLine(i);
                        }
                        else
                        {
                            _codeLayoutManager.InsertLine(i, _textBuffer.GetLine(i));
                        }
                    }
                }
                else // lines were removed
                {
                    if (_viewportManager.IsDocumentLineVisible(startLine))
                    {
                        _codeLayoutManager.RefreshLine(startLine);
                    }

                    // Query the visible range clamped to the OLD line count.
                    // GetVisibleVerticalRange() has already detected the new buffer size and
                    // will clamp lastVisibleLine to newLineCount-1.  That makes clampedDeleteEnd
                    // smaller than endLine (old coordinates), so the loop under-deletes layout
                    // entries and SynchWithViewport has to do a full rebuild every time.
                    // Using the pre-mutation line count keeps the bounds consistent with the
                    // layout, which has not yet been updated.
                    (int firstVisibleLine, int lastVisibleLine) = _viewportManager.ComputeVisibleRangeForLineCount(oldSourceText.Lines.Count);
                    int clampedDeleteEnd  = Math.Min(endLine, lastVisibleLine);
                    int clampedDeleteStop = Math.Max(endLine + lineDelta, firstVisibleLine - 1);

                    for (int i = clampedDeleteEnd; i > clampedDeleteStop; i--)
                    {
                        _codeLayoutManager.DeleteLine(i);
                    }
                }
            }
            
            _viewportManager.EnsureCaretIsVisible(_caretController);
            _codeLayoutManager.SynchWithViewport();
        }
    }
}
