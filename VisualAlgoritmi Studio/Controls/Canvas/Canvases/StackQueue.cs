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
    public class StackCanvas : VisualizerCanvasBase<VisualNode>
    {
        private static readonly SolidColorBrush Fill = new(Color.FromArgb(80, 0, 120, 215));
        private static readonly SolidColorBrush Stroke = new(Color.FromArgb(200, 0, 120, 215));
        private static readonly Pen Pen = new(Stroke, 1);
        private const double RectangleWidth = 120;
        private const double RectangleHeight = 80;
        private const double RectangleSpacing = 10;
        private const double CornerRadius = 4;
        private const double ResetBottomPadding = 10;

        public override void ResetView()
        {
            SetView(
                GetCenteredResetOffsetX(),
                GetBottomAlignedResetOffsetY(),
                1.0
            );
        }

        protected override void StepForwardCore(List<VisualNode> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps)
        {
            foreach (var operation in canvasOps)
            {
                switch (operation)
                {
                    case PushOperation pushOperation:
                        Push(previousVisualState, pushOperation.Value);
                        break;

                    case PopOperation:
                        Pop(previousVisualState);
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

        private double GetCenteredResetOffsetX()
        {
            if (Bounds.Width <= 0)
            {
                return 0;
            }

            GetStackHorizontalBounds(out double stackLeft, out double stackRight);

            double stackWidth = stackRight - stackLeft;
            double targetLeft = (Bounds.Width - stackWidth) * 0.5;

            return stackLeft - targetLeft;
        }

        private double GetBottomAlignedResetOffsetY()
        {
            if (Bounds.Height <= 0)
            {
                return 0;
            }

            double stackBottom = GetStackBottom();
            double targetBottom = Bounds.Height - ResetBottomPadding;

            return stackBottom - targetBottom;
        }

        private void GetStackHorizontalBounds(out double stackLeft, out double stackRight)
        {
            stackLeft = RectangleSpacing;
            stackRight = RectangleSpacing + RectangleWidth;

            if (_visibleElements.Count == 0)
            {
                return;
            }

            stackLeft = _visibleElements[0].CellLocation.X;
            stackRight = _visibleElements[0].EndX;

            for (int i = 1; i < _visibleElements.Count; i++)
            {
                var element = _visibleElements[i];
                stackLeft = Math.Min(stackLeft, element.CellLocation.X);
                stackRight = Math.Max(stackRight, element.EndX);
            }
        }

        private double GetStackBottom()
        {
            double stackBottom = RectangleSpacing + RectangleHeight;

            for (int i = 0; i < _visibleElements.Count; i++)
            {
                var element = _visibleElements[i];
                stackBottom = Math.Max(stackBottom, element.CellLocation.Y + element.CellHeight);
            }

            return stackBottom;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(List<VisualNode> previousVisualState, string value)
        {
            double yOffset = previousVisualState.Count == 0
                ? RectangleSpacing
                : previousVisualState[^1].CellLocation.Y - RectangleHeight - RectangleSpacing;

            var textLayout = CreateFittedTextLayout(value);

            var node = new VisualNode(
                textLayout,
                new Point(10, yOffset),
                RectangleHeight,
                RectangleWidth
            );

            previousVisualState.Add(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Pop(List<VisualNode> previousVisualState)
        {
            if (previousVisualState.Count > 0)
            {
                previousVisualState.RemoveAt(previousVisualState.Count - 1);
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

                if (element.CellLocation.Y > viewportBottom
                    || element.CellLocation.Y + element.CellHeight < viewportTop
                    || element.CellLocation.X > viewportRight
                    || element.EndX < viewportLeft)
                {
                    continue;
                }

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
