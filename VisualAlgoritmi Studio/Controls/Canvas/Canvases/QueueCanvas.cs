using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Canvas.Operations;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Canvases
{
    public class QueueCanvas : VisualizerCanvasBase<VisualNode>
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
                    case EnqueueOperation enqueueOperation:
                        Enqueue(previousVisualState, enqueueOperation.Value);
                        break;

                    case DequeueOperation :
                        Dequeue(previousVisualState);
                        break;

                    case ClearOperation:
                        previousVisualState.Clear();
                        break;

                    case CreationFromCollectionOperation creationFromCollectionOperation:
                        CreationFromCollection(previousVisualState, creationFromCollectionOperation.Values);
                        break;

                    case CapacitySetOperation:
                        // Capacity changes will be handled in a future version
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Enqueue(List<VisualNode> previousVisualState, string value)
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
        private void Dequeue(List<VisualNode> previousVisualState)
        {
            if (previousVisualState.Count == 0)
            {
                return;
            }

            double removedNodeWidth = previousVisualState[0].CellWidth + RectangleSpacing;

            previousVisualState.RemoveAt(0);

            for (int i = 0; i < previousVisualState.Count; i++)
            {
                previousVisualState[i].CellLocation = new Point(
                    previousVisualState[i].CellLocation.X - removedNodeWidth,
                    previousVisualState[i].CellLocation.Y
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreationFromCollection(List<VisualNode> previousVisualState, IReadOnlyList<string> values)
        {
            foreach (var value in values)
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
