using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Text;

namespace VisualAlgoritmi_Studio.Controls.Editor.Text
{
    internal sealed class UndoRedoManager
    {
        private readonly RingStack<UndoItem> _undo = new();
        private readonly RingStack<UndoItem> _redo = new();

        private List<(TextChange textChange, string oldText)>? _activeChanges;
        private SourceText? _oldSourceText;
        private SourceText? _currentSourceText;
        private CaretSnapshot _activeCaretBefore;
        private SelectionSnapshot _activeSelectionBefore;

        public bool HasActiveBatch => _activeChanges != null;

        internal readonly struct UndoItem
        {
            public readonly (TextChange textChange, string oldText)[] Changes;

            public readonly CaretSnapshot CaretBefore;
            public readonly CaretSnapshot CaretAfter;
            public readonly SelectionSnapshot SelectionBefore;
            public readonly SelectionSnapshot SelectionAfter;

            public UndoItem((TextChange textChange, string oldText)[] changes,
                CaretSnapshot caretBefore, CaretSnapshot caretAfter,
                SelectionSnapshot selectionBefore, SelectionSnapshot selectionAfter)
            {
                Changes = changes;
                CaretBefore = caretBefore;
                CaretAfter = caretAfter;
                SelectionBefore = selectionBefore;
                SelectionAfter = selectionAfter;
            }
        }

        internal readonly struct CaretSnapshot
        {
            public readonly int Line;
            public readonly int Column;

            public CaretSnapshot(int line, int column)
            {
                Line = line;
                Column = column;
            }
        }

        internal readonly struct SelectionSnapshot
        {
            public readonly int AnchorLine;
            public readonly int AnchorColumn;
            public readonly int ActiveLine;
            public readonly int ActiveColumn;

            public SelectionSnapshot(int anchorLine, int anchorColumn, int activeLine, int activeColumn)
            {
                AnchorLine = anchorLine;
                AnchorColumn = anchorColumn;
                ActiveLine = activeLine;
                ActiveColumn = activeColumn;
            }
        }

        public void BeginBatch(CaretController caretController, SelectionController selectionController, SourceText oldSourceText)
        {
            _activeCaretBefore = new CaretSnapshot(
                caretController.Line,
                caretController.Column);

            var (anchorLine, anchorColumn, activeLine, activeColumn) = selectionController.GetRawPositions();
            _activeSelectionBefore = new SelectionSnapshot(anchorLine, anchorColumn, activeLine, activeColumn);

            _oldSourceText = oldSourceText;
            _currentSourceText = oldSourceText;

            _activeChanges = [];
        }

        public void AddToBatch(TextChange? textChange)
        {
            if (!textChange.HasValue)
            {
                return;
            }

            if (_activeChanges == null)
            {
                return;
            }

            var textChangeValue = textChange!.Value;

            var oldText = _currentSourceText!.ToString(textChangeValue.Span);
            _activeChanges.Add((textChangeValue, oldText));

            _currentSourceText = _currentSourceText.WithChanges(textChangeValue);
        }

        public void EndBatch(CaretController caretController, SelectionController selectionController)
        {
            if (_activeChanges == null || _activeChanges.Count == 0)
            {
                _activeChanges = null;
                _currentSourceText = null;
                return;
            }

            var caretAfter = new CaretSnapshot(
                caretController.Line,
                caretController.Column);

            var (anchorLine, anchorColumn, activeLine, activeColumn) = selectionController.GetRawPositions();
            var selectionAfter = new SelectionSnapshot(anchorLine, anchorColumn, activeLine, activeColumn);

            _undo.Push(new UndoItem(
                [.. _activeChanges],
                _activeCaretBefore,
                caretAfter,
                _activeSelectionBefore,
                selectionAfter));

            _redo.Clear();
            _activeChanges = null;
            _currentSourceText = null;
        }

        public void RecordSingleChange(TextChange? textChange, string oldText,
            CaretSnapshot caretBefore, CaretSnapshot caretAfter,
            SelectionSnapshot selectionBefore, SelectionSnapshot selectionAfter)
        {
            if (!textChange.HasValue)
            {
                return;
            }

            _undo.Push(new UndoItem([(textChange.Value, oldText)], caretBefore, caretAfter, selectionBefore, selectionAfter));
            _redo.Clear();
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }

        public bool TryPopUndo(out UndoItem item)
        {
            return _undo.TryPop(out item);
        }

        public bool TryPopRedo(out UndoItem item)
        {
            return _redo.TryPop(out item);
        }

        public void PushRedo(UndoItem item) => _redo.Push(item);
        public void PushUndo(UndoItem item) => _undo.Push(item);

        public bool TryPeekUndo(out UndoItem item)
        {
            return _undo.TryPeek(out item);
        }

        private class RingStack<T>
        {
            private static readonly int MAX_BUFFER_SIZE = 4096;

            private T[] _buffer;
            private int _head;
            private int _count;

            public RingStack()
            {
                _buffer = new T[512];
            }

            public int Count => _count;

            public void Push(T value)
            {
                _buffer[_head] = value;
                _head++;

                if (_count < _buffer.Length)
                {
                    _count++;
                }

                int bufferLength = _buffer.Length;

                if (_head == bufferLength)
                {
                    if (bufferLength < MAX_BUFFER_SIZE)
                    {
                        Resize(Math.Min(bufferLength * 2, MAX_BUFFER_SIZE));
                    }
                    else
                    {
                        // Wrap around — oldest entry is overwritten
                        _head = 0;
                        _count = bufferLength;
                    }
                }
            }

            private void Resize(int newCapacity)
            {
                T[] newBuffer = new T[newCapacity];
                Array.Copy(_buffer, newBuffer, _buffer.Length);
                _buffer = newBuffer;
            }

            public bool TryPop([MaybeNullWhen(false)] out T item)
            {
                if (_count == 0)
                {
                    item = default;
                    return false;
                }

                _head = (_head - 1 + _buffer.Length) % _buffer.Length;
                item = _buffer[_head];

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    _buffer[_head] = default!;
                }

                _count--;

                return true;
            }

            public bool TryPeek([MaybeNullWhen(false)] out T item)
            {
                if (_count == 0)
                {
                    item = default;
                    return false;
                }

                int index = (_head - 1 + _buffer.Length) % _buffer.Length;
                item = _buffer[index];
                return true;
            }

            public void Clear()
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _head = 0;
                _count = 0;
            }
        }
    }
}