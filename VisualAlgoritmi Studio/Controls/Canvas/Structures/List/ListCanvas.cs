using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Structures.List
{
    public class ListCanvas : VisualizerCanvasBase<VisualNode>
    {
        private static readonly SolidColorBrush Fill = new(Color.FromArgb(80, 0, 120, 215));
        private static readonly SolidColorBrush Stroke = new(Color.FromArgb(200, 0, 120, 215));
        private static readonly Pen Pen = new(Stroke, 1);
        private const double RectangleWidth = 120;
        private const double RectangleHeight = 80;
        private const double RectangleSpacing = 10;
        private const double CornerRadius = 4;

        protected override void StepForwardCore(List<VisualNode> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps)
        {
            foreach (var operation in canvasOps)
            {
                switch (operation)
                {
                    case AddOperation addOperation:
                        Add(previousVisualState, addOperation.Value);
                        break;

                    case AddRangeOperation addRangeOperation:
                        AddRange(previousVisualState, addRangeOperation.Values);
                        break;

                    case InsertOperation insertOperation:
                        Insert(previousVisualState, insertOperation.Index, insertOperation.Value);
                        break;

                    case InsertRangeOperation insertRangeOperation:
                        InsertRange(previousVisualState, insertRangeOperation.StartIndex, insertRangeOperation.Values);
                        break;

                    case RemoveOperation removeOperation:
                        Remove(previousVisualState, removeOperation.Index);
                        break;

                    case RemoveRangeOperation removeRangeOperation:
                        RemoveRange(previousVisualState, removeRangeOperation.StartIndex, removeRangeOperation.Count);
                        break;

                    case ClearOperation:
                        previousVisualState.Clear();
                        break;

                    case SetOperation setOperation:
                        Set(previousVisualState, setOperation.Index, setOperation.Value);
                        break;

                    case CapacitySetOperation:
                        // Capacity changes do not affect the visual state of the list, so we can ignore this operation.
                        break;

                    case SnapshotOperation snapshotOperation:
                        Snapshot(previousVisualState, snapshotOperation.Values);
                        break;

                    case ReverseOperation reverseOperation:
                        Reverse(previousVisualState, reverseOperation.StartIndex, reverseOperation.Count);
                        break;
                }
            }
        }

        protected override void StepBackCore(List<VisualNode> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps)
        {
            
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(List<VisualNode> previousVisualState, string value)
        {
            double xOffset = previousVisualState.Count == 0 
                ? RectangleSpacing 
                : previousVisualState[^1].EndX + RectangleSpacing;

            var textLayout = CreateFittedTextLayout(value);

            var node = new VisualNode(
                textLayout,
                new Point(xOffset, 10),
                RectangleHeight,
                RectangleWidth
            );

            previousVisualState.Add(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddRange(List<VisualNode> previousVisualState, IEnumerable<string> values)
        {
            double xOffset = previousVisualState.Count == 0 
                ? RectangleSpacing 
                : previousVisualState[^1].EndX + RectangleSpacing;

            foreach (var value in values)
            {
                var textLayout = CreateFittedTextLayout(value);

                var node = new VisualNode(
                    textLayout,
                    new Point(xOffset, RectangleSpacing),
                    RectangleHeight,
                    RectangleWidth
                );

                previousVisualState.Add(node);

                xOffset += RectangleWidth + RectangleSpacing;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Insert(List<VisualNode> previousVisualState, int index, string value)
        {
            double xOffset = index == 0 
                ? RectangleSpacing 
                : previousVisualState[index - 1].EndX + RectangleSpacing;

            var textLayout = CreateFittedTextLayout(value);

            var node = new VisualNode(
                textLayout,
                new Point(xOffset, RectangleSpacing),
                RectangleHeight,
                RectangleWidth
            );

            previousVisualState.Insert(index, node);

            for (int i = index + 1; i < previousVisualState.Count; i++)
            {
                previousVisualState[i].CellLocation = new Point(
                    previousVisualState[i].CellLocation.X + RectangleWidth + RectangleSpacing,
                    previousVisualState[i].CellLocation.Y
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InsertRange(List<VisualNode> previousVisualState, int index, IEnumerable<string> values)
        {
            double xOffset = index == 0 
                ? RectangleSpacing 
                : previousVisualState[index - 1].EndX + RectangleSpacing;

            var nodesToInsert = new List<VisualNode>();

            foreach (var value in values)
            {
                var textLayout = CreateFittedTextLayout(value);

                var node = new VisualNode(
                    textLayout,
                    new Point(xOffset, RectangleSpacing),
                    RectangleHeight,
                    RectangleWidth
                );

                nodesToInsert.Add(node);

                xOffset += RectangleWidth + RectangleSpacing;
            }

            previousVisualState.InsertRange(index, nodesToInsert);

            for (int i = index + nodesToInsert.Count; i < previousVisualState.Count; i++)
            {
                previousVisualState[i].CellLocation = new Point(
                    previousVisualState[i].CellLocation.X + (RectangleWidth + RectangleSpacing) * nodesToInsert.Count,
                    previousVisualState[i].CellLocation.Y
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Remove(List<VisualNode> previousVisualState, int index)
        {
            double removedNodeWidth = previousVisualState[index].CellWidth + RectangleSpacing;

            previousVisualState.RemoveAt(index);

            for (int i = index; i < previousVisualState.Count; i++)
            {
                previousVisualState[i].CellLocation = new Point(
                    previousVisualState[i].CellLocation.X - removedNodeWidth,
                    previousVisualState[i].CellLocation.Y
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveRange(List<VisualNode> previousVisualState, int index, int count)
        {
            double removedNodesWidth = (RectangleWidth + RectangleSpacing) * count;

            previousVisualState.RemoveRange(index, count);

            for (int i = index; i < previousVisualState.Count; i++)
            {
                previousVisualState[i].CellLocation = new Point(
                    previousVisualState[i].CellLocation.X - removedNodesWidth,
                    previousVisualState[i].CellLocation.Y
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(List<VisualNode> previousVisualState, int index, string value)
        {
            double xOffset = previousVisualState[index].CellLocation.X;

            var textLayout = CreateFittedTextLayout(value);

            var node = new VisualNode(
                textLayout,
                new Point(xOffset, 10),
                RectangleHeight,
                RectangleWidth
            );

            previousVisualState[index] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Snapshot(List<VisualNode> previousVisualState, IReadOnlyList<string> values)
        {
            previousVisualState.Clear();

            double xOffset = 10;

            foreach (var value in values)
            {
                var textLayout = CreateFittedTextLayout(value);

                var node = new VisualNode(
                    textLayout,
                    new Point(xOffset, 10),
                    RectangleHeight,
                    RectangleWidth
                );

                previousVisualState.Add(node);

                xOffset += RectangleWidth + 10;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reverse(List<VisualNode> previousVisualState, int index, int count)
        {
            int left = index;
            int right = index + count - 1;

            while (left < right)
            {
                var temp = previousVisualState[left];
                previousVisualState[left] = previousVisualState[right];
                previousVisualState[right] = temp;

                left++;
                right--;
            }

            for (int i = index; i < index + count; i++)
            {
                double xOffset = RectangleSpacing + i * (RectangleWidth + RectangleSpacing);
                previousVisualState[i].CellLocation = new Point(xOffset, RectangleSpacing);
            }
        }

        public override void RenderCore(DrawingContext context)
        {
            var viewport = ViewportBounds;
            double viewportLeft = viewport.Left;
            double viewportRight = viewport.Right;
            double viewportTop = viewport.Top;
            double viewportBottom = viewport.Bottom;

            for (int i = 0; i < _visibleElements.Count; i++)
            {
                var element = _visibleElements[i];

                // Elements are laid out left-to-right, so we can break early.
                if (element.CellLocation.X > viewportRight)
                    break;

                // Skip elements entirely to the left or outside vertically.
                if (element.EndX < viewportLeft
                    || element.CellLocation.Y > viewportBottom
                    || element.CellLocation.Y + element.CellHeight < viewportTop)
                    continue;

                var rect = new Rect(
                    element.CellLocation,
                    new Size(element.CellWidth, element.CellHeight)
                );

                context.DrawRectangle(
                    Fill,
                    Pen,
                    rect,
                    CornerRadius,
                    CornerRadius
                );

                element.Layout.Draw(
                    context,
                    element.GetTextLocation()
                );
            }         
        }

        protected TextLayout CreateFittedTextLayout(string text)
        {
            const double paddingX = 8;
            const double paddingY = 6;

            double maxWidth = RectangleWidth - paddingX * 2;
            double maxHeight = RectangleHeight - paddingY * 2;

            var layout = BuildLayout(
                text,
                DefaultFontSize,
                TextTrimming.None,
                maxWidth,
                double.PositiveInfinity
            );

            if (Fits(layout, maxWidth, maxHeight))
            {
                return BuildLayout(text, DefaultFontSize, TextTrimming.None, maxWidth, maxHeight);
            }

            double scale = maxHeight / layout.Height;

            double newSize = Math.Floor(DefaultFontSize * scale);

            if (newSize < MinimumFontSize)
            {
                newSize = MinimumFontSize;
            }

            layout = BuildLayout(text, newSize, TextTrimming.None, maxWidth, maxHeight);

            if (Fits(layout, maxWidth, maxHeight))
            {
                return layout;
            }

            return BuildLayout(text, MinimumFontSize, TextTrimming.CharacterEllipsis, maxWidth, maxHeight);
        } 
    }
}
