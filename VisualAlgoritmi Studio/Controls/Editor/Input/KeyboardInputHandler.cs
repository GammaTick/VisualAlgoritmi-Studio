using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;
using VisualAlgoritmi_Studio.RoslynCore;

namespace VisualAlgoritmi_Studio.Controls.Editor.Input
{
    internal sealed class KeyboardInputHandler
    {
        private readonly TextMutationSyncPipeline _textMutationSyncPipeline;
        private readonly CaretController _caretController;
        private readonly SelectionController _selectionController;
        private readonly TextBuffer _textBuffer;
        private readonly ViewportManager _viewportManager;
        private readonly IGraphemeNavigator _graphemeNavigator;
        private readonly UndoRedoManager _undoRedoManager;
        private readonly CodeAnalysisSession _codeAnalysisSession;
        private readonly Func<IClipboard?> _getClipboard;

        public KeyboardInputHandler(TextMutationSyncPipeline textMutationSyncPipeline, 
            CaretController caretController,
            SelectionController selectionController,
            TextBuffer textBuffer,
            ViewportManager viewportManager,
            IGraphemeNavigator graphemeNavigator,
            UndoRedoManager undoRedoManager,
            CodeAnalysisSession codeAnalysisSession,
            Func<IClipboard?> getClipboard)
        {
            _textMutationSyncPipeline = textMutationSyncPipeline;
            _caretController = caretController;
            _selectionController = selectionController;
            _textBuffer = textBuffer;
            _viewportManager = viewportManager;
            _graphemeNavigator = graphemeNavigator;
            _undoRedoManager = undoRedoManager;
            _codeAnalysisSession = codeAnalysisSession;
            _getClipboard = getClipboard;
        }

        public void HandleTextInput(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

            DeleteSelectedText();

            var oldSourceText = _textBuffer.SourceText;

            var textInsertionChange = _textBuffer.InsertText(_caretController.Line, _caretController.Column, text);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, textInsertionChange, () =>
            {
                _caretController.MoveByCharCount(text.Length);
            });
            
            _undoRedoManager.AddToBatch(textInsertionChange);

            if (text == "}")
            {
                TryApplyClosingBraceDeIndent();
            }

            _undoRedoManager.EndBatch(_caretController, _selectionController);
        }

        public async Task HandleKeyPress(KeyEventArgs e)
        {
            bool handled = false;

            if (e.KeyModifiers != KeyModifiers.None)
            {
                handled = await HandleModifiedKeys(e);
            }
            
            if (!handled)
            {
                handled = HandleSimpleKeybinds(e);
            }

            e.Handled = handled;
        }

        private bool HandleSimpleKeybinds(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Back:
                    Backspace();
                    return true;

                case Key.Enter:
                    Enter();
                    return true;

                case Key.Right:
                    MoveCaret(CaretDirection.Right);
                    return true;

                case Key.Left:
                    MoveCaret(CaretDirection.Left);
                    return true;

                case Key.Up:
                    MoveCaret(CaretDirection.Up);
                    return true;

                case Key.Down:
                    MoveCaret(CaretDirection.Down);
                    return true;

                case Key.Tab:
                    Tab();
                    return true;

                case Key.Home:
                    _caretController.MoveToLineStart();
                    return true;

                case Key.End:
                    _caretController.MoveToLineEnd();
                    return true;
            }

            return false;
        }

        private async Task<bool> HandleModifiedKeys(KeyEventArgs e)
        {
            var mods = e.KeyModifiers;

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
            {
                if (mods.HasFlag(KeyModifiers.Meta))
                {
                    return await HandleCtrlCombinations(e);
                }
            }
            else
            {               
                if (mods.HasFlag(KeyModifiers.Control))
                {
                    return await HandleCtrlCombinations(e);
                }
            }

            if (mods.HasFlag(KeyModifiers.Shift))
            {
                return HandleShiftCombination(e);
            }

            return false;
        }

        private async Task<bool> HandleCtrlCombinations(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.C:
                    await CopyAsync();
                    return true;

                case Key.V:
                    await PasteAsync();
                    return true;

                case Key.X:
                    await CutAsync();
                    return true;

                case Key.A:
                    SelectAllText();
                    return true;

                case Key.Z:
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        Redo();
                    }
                    else
                    {
                        Undo();
                    }

                    return true;
                
                case Key.Y:
                    Redo();
                    return true;

                case Key.Oem2:
                    ToggleComment();
                    return true;
            }

            return false;
        }

        private bool HandleShiftCombination(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    ExtendSelection(CaretDirection.Right);
                    return true;

                case Key.Left:
                    ExtendSelection(CaretDirection.Left);
                    return true;

                case Key.Up:
                    ExtendSelection(CaretDirection.Up);
                    return true;

                case Key.Down:
                    ExtendSelection(CaretDirection.Down);
                    return true;
            }

            return false;
        }

        private void Backspace()
        {
            if (_selectionController.HasSelection)
            {
                DeleteSelectedText();
                return;
            }

            int caretLine = _caretController.Line;
            int caretColumn = _caretController.Column;

            if (caretColumn == 0 && caretLine == 0)
            {
                return;
            }

            _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

            if (caretColumn == 0)
            {
                var oldSourceText = _textBuffer.SourceText;

                int previousLineLength = _textBuffer.GetLineLength(caretLine - 1);

                var lineMergeChange = _textBuffer.MergeLineWithPrevious(caretLine);

                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, lineMergeChange, () =>
                {
                    _caretController.SetPosition(caretLine - 1, previousLineLength);
                });

                _undoRedoManager.AddToBatch(lineMergeChange);
            }
            else
            {
                var oldSourceText = _textBuffer.SourceText;

                int startIndexOfCharToDelete = _graphemeNavigator.GetPreviousIndex(caretLine, caretColumn);

                var backspaceChange = _textBuffer.DeleteRange(caretLine, startIndexOfCharToDelete, caretLine, caretColumn);

                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, backspaceChange, () =>
                {
                    _caretController.SetPosition(caretLine, startIndexOfCharToDelete);
                });

                _undoRedoManager.AddToBatch(backspaceChange);
            }

            _undoRedoManager.EndBatch(_caretController, _selectionController);
        }

        private void MoveCaret(CaretDirection direction)
        {
            MoveCaretInternal(direction);
            _selectionController.CollapseTo(_caretController.Line, _caretController.Column);
            _viewportManager.EnsureCaretIsVisible(_caretController);
        }

        private void Enter()
        {
            _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

            DeleteSelectedText();

            var oldSourceText = _textBuffer.SourceText;

            var newLineInsertionChange = _textBuffer.InsertNewLineAtPosition(_caretController.Line, _caretController.Column);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, newLineInsertionChange, () =>
            {
                _caretController.SetPosition(_caretController.Line + 1, 0);
            });

            string smartIndent = GetSmartIndent();

            oldSourceText = _textBuffer.SourceText;
            var indentationInsertionChange = _textBuffer.InsertText(_caretController.Line, 0, smartIndent);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, indentationInsertionChange, () =>
            {
                _caretController.MoveByCharCount(smartIndent.Length);
            });

            _undoRedoManager.AddToBatch(newLineInsertionChange);
            _undoRedoManager.AddToBatch(indentationInsertionChange);
            _undoRedoManager.EndBatch(_caretController, _selectionController);
        }

        private void Tab()
        {
            _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

            if (_selectionController.HasSelection)
            {
                if (IsSelectionSingleLine())
                {
                    DeleteSelectedText();
                }
                else
                {
                    IndentSelection();
                    _undoRedoManager.EndBatch(_caretController, _selectionController);
                    return;
                }
            }

            var oldSourceText = _textBuffer.SourceText;

            var tabInsertionChange = _textBuffer.InsertText(_caretController.Line, _caretController.Column, "    ");

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, tabInsertionChange, () =>
            {
                _caretController.MoveByCharCount(4);
            });

            _undoRedoManager.AddToBatch(tabInsertionChange);
            _undoRedoManager.EndBatch(_caretController, _selectionController);
        }

        private void IndentSelection()
        {
            (int startLine, _, int endLine, _) = _selectionController.GetNormalizedSelection();
            (int anchorLine, int anchorColumn, int activeLine, int activeColumn) = _selectionController.GetRawPositions();

            // Capture indent info before any mutations so the checks are based on original positions
            bool anchorHasCodeBefore = anchorColumn > _textBuffer.GetLineIndentEndColumn(anchorLine);
            bool activeHasCodeBefore = activeColumn > _textBuffer.GetLineIndentEndColumn(activeLine);

            for (int line = startLine; line <= endLine; line++)
            {
                var oldSourceText = _textBuffer.SourceText;
                var change = _textBuffer.InsertText(line, 0, "    ");
                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, change);
                _undoRedoManager.AddToBatch(change);
            }

            int newAnchorColumn = anchorHasCodeBefore ? anchorColumn + 4 : anchorColumn;
            int newActiveColumn = activeHasCodeBefore ? activeColumn + 4 : activeColumn;

            _selectionController.SetRawPositions(anchorLine, newAnchorColumn, activeLine, newActiveColumn);
            _caretController.SetPosition(activeLine, newActiveColumn);
            _viewportManager.EnsureCaretIsVisible(_caretController);
        }

        private async Task CopyAsync()
        {
            IClipboard? clipboard = _getClipboard();

            if (clipboard is null)
            {
                Trace.WriteLine("[KeyboardInputHandler] Clipboard is not available in Copy method.");
                return;
            }

            if (!_selectionController.HasSelection)
            {
                string currentLineText = _textBuffer.GetLine(_caretController.Line).ToString() + Environment.NewLine;
                await clipboard.SetTextAsync(currentLineText);
                return;
            }

            (int startLine, int startColumn, int endLine, int endColumn) = _selectionController.GetNormalizedSelection();

            string selectedText = _textBuffer.GetRange(startLine, startColumn, endLine, endColumn);

            if (string.IsNullOrEmpty(selectedText))
            {
                return;
            }

            await clipboard.SetTextAsync(selectedText);
        }

        private async Task CutAsync()
        {
            IClipboard? clipboard = _getClipboard();

            if (clipboard is null)
            {
                Trace.WriteLine("[KeyboardInputHandler] Clipboard is not available in Cut method.");
                return;
            }

            if (_selectionController.HasSelection)
            {
                (int startLine, int startColumn, int endLine, int endColumn) = _selectionController.GetNormalizedSelection();

                string selectedText = _textBuffer.GetRange(startLine, startColumn, endLine, endColumn);

                if (string.IsNullOrEmpty(selectedText))
                {
                    return;
                }

                await clipboard.SetTextAsync(selectedText);

                _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);
                DeleteSelectedText();
                _undoRedoManager.EndBatch(_caretController, _selectionController);
            }
            else
            {
                int caretLine = _caretController.Line;

                string lineText = _textBuffer.GetLine(caretLine).ToString() + Environment.NewLine;
                await clipboard.SetTextAsync(lineText);

                _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

                var oldSourceText = _textBuffer.SourceText;
                var lineDeleteChange = _textBuffer.DeleteLine(caretLine);

                int newLine = Math.Min(caretLine, _textBuffer.LineCount - 1);
                int newColumn = Math.Min(_caretController.Column, _textBuffer.GetLineLength(newLine));

                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, lineDeleteChange, () =>
                {
                    _caretController.SetPosition(newLine, newColumn);
                });

                _undoRedoManager.AddToBatch(lineDeleteChange);
                _undoRedoManager.EndBatch(_caretController, _selectionController);
            }
        }

        private async Task PasteAsync()        
        {
            IClipboard? clipboard = _getClipboard();

            if (clipboard is null)
            {
                Trace.WriteLine("[KeyboardInputHandler] Clipboard is not available in Paste method.");
                return;
            }

            string? clipboardText = await ClipboardExtensions.TryGetTextAsync(clipboard);

            if (string.IsNullOrEmpty(clipboardText))
            {
                return;
            }

            _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

            DeleteSelectedText();

            var oldSourceText = _textBuffer.SourceText;

            var textInsertionChange = _textBuffer.InsertText(_caretController.Line, _caretController.Column, clipboardText);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, textInsertionChange, () =>
            {
                _caretController.MoveByCharCount(clipboardText.Length);
            });

            _undoRedoManager.AddToBatch(textInsertionChange);
            _undoRedoManager.EndBatch(_caretController, _selectionController);
        }   

        private void SelectAllText()
        {
            _selectionController.BeginSelection(0, 0);

            int lastLineIndex = _textBuffer.LineCount - 1;
            int lastColumnIndex = _textBuffer.GetLineLength(lastLineIndex);

            _selectionController.ExtendTo(lastLineIndex, lastColumnIndex);
        }

        public void ToggleComment()
        {
            ExecuteCommentAction(ToggleCommentForLine, ToggleCommentForSelection);
        }

        public void ForceComment()
        {
            ExecuteCommentAction(ForceCommentLine, ForceCommentSelection);
        }

        public void ForceUncomment()
        {
            ExecuteCommentAction(ForceUncommentLine, ForceUncommentSelection);
        }

        private void ExecuteCommentAction(Action<int> singleLineAction, Action<int, int> selectionAction)
        {
            _undoRedoManager.BeginBatch(_caretController, _selectionController, _textBuffer.SourceText);

            if (_selectionController.HasSelection)
            {
                (int startLine, _, int endLine, _) = _selectionController.GetNormalizedSelection();

                if (startLine == endLine)
                {
                    singleLineAction(startLine);
                }
                else
                {
                    selectionAction(startLine, endLine);
                }
            }
            else
            {
                singleLineAction(_caretController.Line);
            }

            _undoRedoManager.EndBatch(_caretController, _selectionController);
        }

        private void ToggleCommentForLine(int line)
        {
            int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);

            // Whitespace-only / empty line
            if (firstNonWhitespaceColumn < 0)
            {
                InsertCommentOnLine(line, 0);
                return;
            }

            if (IsLineCommented(line))
            {
                RemoveCommentFromLine(line, firstNonWhitespaceColumn);
                return;
            }

            InsertCommentOnLine(line, firstNonWhitespaceColumn);
        }

        private void ForceCommentLine(int line)
        {
            int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);
            int insertColumn = firstNonWhitespaceColumn < 0 ? 0 : firstNonWhitespaceColumn;
            InsertCommentOnLine(line, insertColumn);
        }

        private void ForceUncommentLine(int line)
        {
            if (!IsLineCommented(line))
            {
                return;
            }

            int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);
            RemoveCommentFromLine(line, firstNonWhitespaceColumn);
        }

        private void InsertCommentOnLine(int line, int column)
        {
            var oldSourceText = _textBuffer.SourceText;
            var insertionChange = _textBuffer.InsertText(line, column, "//");
            _undoRedoManager.AddToBatch(insertionChange);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, insertionChange, () =>
            {
                if (_caretController.Line == line && _caretController.Column >= column)
                {
                    _caretController.SetPosition(_caretController.Line, _caretController.Column + 2);
                }
            });
        }

        private void RemoveCommentFromLine(int line, int firstNonWhitespaceColumn)
        {
            var oldSourceText = _textBuffer.SourceText;
            var deletionChange = _textBuffer.DeleteRange(line, firstNonWhitespaceColumn, line, firstNonWhitespaceColumn + 2);
            _undoRedoManager.AddToBatch(deletionChange);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, deletionChange, () =>
            {
                if (_caretController.Line == line && _caretController.Column >= firstNonWhitespaceColumn + 2)
                {
                    _caretController.SetPosition(_caretController.Line, _caretController.Column - 2);
                }
            });
        }

        private void ToggleCommentForSelection(int startLine, int endLine)
        {
            bool shouldComment = ShouldCommentSelection(startLine, endLine);

            var (anchorLine, anchorColumn, activeLine, activeColumn) =
                _selectionController.GetRawPositions();

            for (int line = startLine; line <= endLine; line++)
            {
                int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);

                // For selections: skip empty / whitespace-only lines
                if (firstNonWhitespaceColumn < 0)
                {
                    continue;
                }

                if (shouldComment)
                {
                    InsertCommentInSelection(line, firstNonWhitespaceColumn,
                        anchorLine, ref anchorColumn, activeLine, ref activeColumn);
                }
                else
                {
                    if (!IsLineCommented(line))
                    {
                        continue;
                    }

                    RemoveCommentInSelection(line, firstNonWhitespaceColumn,
                        anchorLine, ref anchorColumn, activeLine, ref activeColumn);
                }
            }

            _selectionController.SetRawPositions(anchorLine, anchorColumn, activeLine, activeColumn);
            _caretController.SetPosition(activeLine, activeColumn);
        }

        private void ForceCommentSelection(int startLine, int endLine)
        {
            var (anchorLine, anchorColumn, activeLine, activeColumn) =
                _selectionController.GetRawPositions();

            for (int line = startLine; line <= endLine; line++)
            {
                int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);

                if (firstNonWhitespaceColumn < 0)
                {
                    continue;
                }

                InsertCommentInSelection(line, firstNonWhitespaceColumn,
                    anchorLine, ref anchorColumn, activeLine, ref activeColumn);
            }

            _selectionController.SetRawPositions(anchorLine, anchorColumn, activeLine, activeColumn);
            _caretController.SetPosition(activeLine, activeColumn);
        }

        private void ForceUncommentSelection(int startLine, int endLine)
        {
            var (anchorLine, anchorColumn, activeLine, activeColumn) =
                _selectionController.GetRawPositions();

            for (int line = startLine; line <= endLine; line++)
            {
                if (!IsLineCommented(line))
                {
                    continue;
                }

                int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);
                RemoveCommentInSelection(line, firstNonWhitespaceColumn,
                    anchorLine, ref anchorColumn, activeLine, ref activeColumn);
            }

            _selectionController.SetRawPositions(anchorLine, anchorColumn, activeLine, activeColumn);
            _caretController.SetPosition(activeLine, activeColumn);
        }

        private void InsertCommentInSelection(int line, int firstNonWhitespaceColumn,
            int anchorLine, ref int anchorColumn, int activeLine, ref int activeColumn)
        {
            var oldSourceText = _textBuffer.SourceText;
            var insertionChange = _textBuffer.InsertText(line, firstNonWhitespaceColumn, "//");
            _undoRedoManager.AddToBatch(insertionChange);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(
                oldSourceText, insertionChange, shouldClearSelection: false);

            if (line == anchorLine && anchorColumn >= firstNonWhitespaceColumn)
            {
                anchorColumn += 2;
            }

            if (line == activeLine && activeColumn >= firstNonWhitespaceColumn)
            {
                activeColumn += 2;
            }
        }

        private void RemoveCommentInSelection(int line, int firstNonWhitespaceColumn,
            int anchorLine, ref int anchorColumn, int activeLine, ref int activeColumn)
        {
            var oldSourceText = _textBuffer.SourceText;
            var deletionChange = _textBuffer.DeleteRange(
                line, firstNonWhitespaceColumn, line, firstNonWhitespaceColumn + 2);
            _undoRedoManager.AddToBatch(deletionChange);

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(
                oldSourceText, deletionChange, shouldClearSelection: false);

            if (line == anchorLine && anchorColumn >= firstNonWhitespaceColumn + 2)
            {
                anchorColumn -= 2;
            }
            else if (line == anchorLine && anchorColumn > firstNonWhitespaceColumn)
            {
                anchorColumn = firstNonWhitespaceColumn;
            }

            if (line == activeLine && activeColumn >= firstNonWhitespaceColumn + 2)
            {
                activeColumn -= 2;
            }
            else if (line == activeLine && activeColumn > firstNonWhitespaceColumn)
            {
                activeColumn = firstNonWhitespaceColumn;
            }
        }

        private bool ShouldCommentSelection(int startLine, int endLine)
        {
            for (int line = startLine; line <= endLine; line++)
            {
                int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);

                // Ignore empty / whitespace-only lines
                if (firstNonWhitespaceColumn < 0)
                {
                    continue;
                }

                if (!IsLineCommented(line))
                {
                    return true;
                }
            }

            return false;
        }

        private int GetFirstNonWhitespaceColumn(int line)
        {
            var textLine = _textBuffer.GetLine(line);
            int start = textLine.Span.Start;
            int end = textLine.Span.End;
            var sourceText = _textBuffer.SourceText;

            int i = start;

            while (i < end)
            {
                char c = sourceText[i];

                if (c != ' ' && c != '\t')
                {
                    return i - start;
                }

                i++;
            }

            return -1;
        }

        private bool IsLineCommented(int line)
        {
            int firstNonWhitespaceColumn = GetFirstNonWhitespaceColumn(line);

            if (firstNonWhitespaceColumn < 0)
            {
                return false;
            }

            var textLine = _textBuffer.GetLine(line);
            int absoluteIndex = textLine.Span.Start + firstNonWhitespaceColumn;
            int end = textLine.Span.End;
            var sourceText = _textBuffer.SourceText;

            return absoluteIndex + 1 < end
                && sourceText[absoluteIndex] == '/'
                && sourceText[absoluteIndex + 1] == '/';
        }

        private bool IsSelectionSingleLine()
        {
            if (!_selectionController.HasSelection)
            {
                return false;
            }

            (int startLine, _, int endLine, _) = _selectionController.GetNormalizedSelection();

            return startLine == endLine;
        }

        private void ExtendSelection(CaretDirection direction)
        {
            if (!_selectionController.HasSelection)
            {
                _selectionController.BeginSelection(_caretController.Line, _caretController.Column);
            }

            MoveCaretInternal(direction);

            _selectionController.ExtendTo(_caretController.Line, _caretController.Column);
        }

        private void MoveCaretInternal(CaretDirection direction)
        {
            switch (direction)
            {
                case CaretDirection.Right:
                    _caretController.MoveRight();
                    break;

                case CaretDirection.Left:
                    _caretController.MoveLeft();
                    break;

                case CaretDirection.Up:
                    _caretController.MoveUp();
                    break;

                case CaretDirection.Down:
                    _caretController.MoveDown();
                    break;
            }
        }

        private string GetSmartIndent()
        {
            var fastTree = _codeAnalysisSession.GetFastSyntaxTree();

            int caretAbsolutePosition = _caretController.GetAbsoluteCaretPosition();

            var token = fastTree.GetRoot().FindToken(caretAbsolutePosition, findInsideTrivia: true);

            if (token.IsKind(SyntaxKind.StringLiteralToken) ||
                token.IsKind(SyntaxKind.InterpolatedStringTextToken) ||
                token.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                return string.Empty;
            }

            int previousLine = _caretController.Line - 1;

            if (previousLine < 0)
            {
                return string.Empty;
            }

            int indentCount = _textBuffer.GetLineIndentEndColumn(previousLine);
            string baseIndent = new(' ', indentCount);

            // If the previous line ends with '{', indent one level deeper.
            // Check the last non-whitespace character to avoid false positives from
            // '{' inside strings or earlier on the line (e.g. Console.WriteLine("{0}")).
            string prevLineText = _textBuffer.GetLine(previousLine).ToString();
            char lastNonWs = '\0';
            for (int i = prevLineText.Length - 1; i >= 0; i--)
            {
                if (prevLineText[i] != ' ' && prevLineText[i] != '\t')
                {
                    lastNonWs = prevLineText[i];
                    break;
                }
            }

            if (lastNonWs == '{')
            {
                return baseIndent + "    ";
            }

            return baseIndent;
        }

        private void TryApplyClosingBraceDeIndent()
        {
            int currentLine = _caretController.Line;
            string lineText = _textBuffer.GetLine(currentLine).ToString();

            // Only re-indent when the line contains nothing but optional leading spaces + '}'
            if (lineText.TrimStart() != "}")
            {
                return;
            }

            int currentIndent = lineText.Length - lineText.TrimStart().Length;

            // Use the Roslyn fast tree to locate the matching '{'
            var fastTree = _codeAnalysisSession.GetFastSyntaxTree();

            // Caret is positioned after the '}', so the '}' is one position behind
            int closeBraceAbsolutePos = _caretController.GetAbsoluteCaretPosition() - 1;
            var closeToken = fastTree.GetRoot().FindToken(closeBraceAbsolutePos);

            if (!closeToken.IsKind(SyntaxKind.CloseBraceToken))
            {
                return;
            }

            var parent = closeToken.Parent;
            if (parent == null)
            {
                return;
            }

            // The parent node owns both the OpenBraceToken and CloseBraceToken as direct children
            SyntaxToken openBrace = default;
            foreach (var childToken in parent.ChildTokens())
            {
                if (childToken.IsKind(SyntaxKind.OpenBraceToken))
                {
                    openBrace = childToken;
                    break;
                }
            }

            if (openBrace == default)
            {
                return;
            }

            int openBraceLine = _textBuffer.SourceText.Lines.GetLineFromPosition(openBrace.SpanStart).LineNumber;
            int correctIndent = _textBuffer.GetLineIndentEndColumn(openBraceLine);

            if (currentIndent == correctIndent)
            {
                return;
            }

            int lineStartAbsolutePos = _textBuffer.GetAbsolutePosition(currentLine, 0);
            var oldSourceText = _textBuffer.SourceText;
            var reindentChange = _textBuffer.ReplaceSpan(lineStartAbsolutePos, currentIndent, new string(' ', correctIndent));

            _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, reindentChange, () =>
            {
                _caretController.SetPosition(currentLine, correctIndent + 1);
            });

            _undoRedoManager.AddToBatch(reindentChange);
        }

        private void DeleteSelectedText()
        {
            if (!_selectionController.HasSelection)
            {
                return;
            }

            (int startLine, int startColumn, int endLine, int endColumn) = _selectionController.GetNormalizedSelection();

            var oldSourceText = _textBuffer.SourceText;

            var rangeDeletionChange = _textBuffer.DeleteRange(startLine, startColumn, endLine, endColumn);

            if (_undoRedoManager.HasActiveBatch)
            {
                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, rangeDeletionChange, () =>
                {
                    _caretController.SetPosition(startLine, startColumn);
                }); 

                _undoRedoManager.AddToBatch(rangeDeletionChange);
            }   
            else
            {
                var caretBefore = CreateCaretSnapshot();
                var selectionBefore = CreateSelectionSnapshot();

                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, rangeDeletionChange, () =>
                {
                    _caretController.SetPosition(startLine, startColumn);
                }); 

                var caretAfter = CreateCaretSnapshot();
                var selectionAfter = CreateSelectionSnapshot();

                string oldText = oldSourceText.ToString(rangeDeletionChange.Span);
                _undoRedoManager.RecordSingleChange(rangeDeletionChange, oldText, caretBefore, caretAfter, selectionBefore, selectionAfter);
            }
        }

        public void Undo()
        {
            if (!_undoRedoManager.TryPopUndo(out UndoRedoManager.UndoItem item))
            {
                return;
            }          

            for (int i = item.Changes.Length - 1; i >= 0; i--)
            {
                var change = item.Changes[i];
                var textChange = change.textChange;

                if (textChange.NewText == null)
                {
                    continue;
                }

                var inverse = new TextChange(
                    new TextSpan(textChange.Span.Start, textChange.NewText.Length),
                    change.oldText
                );

                var oldSourceText = _textBuffer.SourceText;

                _textBuffer.ApplyChange(inverse);

                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, inverse);
            }

            _caretController.SetPosition(item.CaretBefore.Line, item.CaretBefore.Column);
            _selectionController.SetRawPositions(
                item.SelectionBefore.AnchorLine, item.SelectionBefore.AnchorColumn,
                item.SelectionBefore.ActiveLine, item.SelectionBefore.ActiveColumn);
            _viewportManager.EnsureCaretIsVisible(_caretController);

            _undoRedoManager.PushRedo(item);
        }

        public void Redo()
        {
            if (!_undoRedoManager.TryPopRedo(out UndoRedoManager.UndoItem item))
            {
                return;
            }

            for (int i = 0; i < item.Changes.Length; i++)
            {
                var change = item.Changes[i];
                var textChange = change.textChange;

                _textBuffer.ApplyChange(textChange);

                var oldSourceText = _textBuffer.SourceText;

                _textMutationSyncPipeline.SynchronizeEditorAfterTextChange(oldSourceText, textChange);
            }

            _caretController.SetPosition(item.CaretAfter.Line, item.CaretAfter.Column);
            _selectionController.SetRawPositions(
                item.SelectionAfter.AnchorLine, item.SelectionAfter.AnchorColumn,
                item.SelectionAfter.ActiveLine, item.SelectionAfter.ActiveColumn);
            _viewportManager.EnsureCaretIsVisible(_caretController);

            _undoRedoManager.PushUndo(item);
        }

        private UndoRedoManager.CaretSnapshot CreateCaretSnapshot()
        {
            return new UndoRedoManager.CaretSnapshot(_caretController.Line, _caretController.Column);
        }

        private UndoRedoManager.SelectionSnapshot CreateSelectionSnapshot()
        {
            var (anchorLine, anchorColumn, activeLine, activeColumn) = _selectionController.GetRawPositions();
            return new UndoRedoManager.SelectionSnapshot(anchorLine, anchorColumn, activeLine, activeColumn);
        }

        private enum CaretDirection
        {
            Right,
            Left,
            Up,
            Down
        }
    }
}
