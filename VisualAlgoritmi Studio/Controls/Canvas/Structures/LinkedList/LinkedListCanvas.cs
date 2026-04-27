using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Structures.LinkedList
{
    public class LinkedListCanvas : VisualizerCanvasBase<LinkedListVisualNode>
    {
        private static readonly SolidColorBrush Fill = new(Color.FromArgb(80, 0, 120, 215));
        private static readonly SolidColorBrush Stroke = new(Color.FromArgb(200, 0, 120, 215));
        private static readonly Pen Pen = new(Stroke, 1);
        private static readonly Pen ArrowPen = new(Stroke, 4)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        private const double RectangleWidth = 120;
        private const double RectangleHeight = 80;
        private const double RectangleSpacing = 60;
        private const double CornerRadius = 4;
        private const double ArrowGap = 8;
        private const double ArrowHeadLength = 12;
        private const double ArrowHeadHalfHeight = 8;

        protected override void StepForwardCore(List<LinkedListVisualNode> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps)
        {
            foreach (var operation in canvasOps)
            {
                switch (operation)
                {
                    case AddFirst addFirst:
                        Insert(previousVisualState, 0, addFirst.NodeId, addFirst.Value);
                        break;

                    case AddLast addLast:
                        Insert(previousVisualState, previousVisualState.Count, addLast.NodeId, addLast.Value);
                        break;

                    case AddAfter addAfter:
                        {
                            int targetIndex = FindNodeIndex(previousVisualState, addAfter.TargetNodeId);
                            Insert(previousVisualState, targetIndex + 1, addAfter.NewNodeId, addAfter.Value);
                        }
                        break;

                    case AddBefore addBefore:
                        {
                            int targetIndex = FindNodeIndex(previousVisualState, addBefore.TargetNodeId);
                            Insert(previousVisualState, targetIndex, addBefore.NewNodeId, addBefore.Value);
                        }
                        break;

                    case RemoveNode removeNode:
                        {
                            int targetIndex = FindNodeIndex(previousVisualState, removeNode.NodeId);
                            Remove(previousVisualState, targetIndex);
                        }
                        break;

                    case ClearOperation:
                        previousVisualState.Clear();
                        break;
                }
            }
        }

        protected override void StepBackCore(List<LinkedListVisualNode> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps)
        {
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindNodeIndex(List<LinkedListVisualNode> visualState, int nodeId)
        {
            for (int i = 0; i < visualState.Count; i++)
            {
                if (visualState[i].NodeId == nodeId)
                    return i;
            }

            throw new InvalidOperationException($"Visual node with NodeId {nodeId} not found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Insert(List<LinkedListVisualNode> previousVisualState, int index, int nodeId, string value)
        {
            double xOffset = index == 0 
                ? RectangleSpacing 
                : previousVisualState[index - 1].EndX + RectangleSpacing;

            var textLayout = CreateFittedTextLayout(value);

            var node = new LinkedListVisualNode(
                textLayout,
                new Point(xOffset, RectangleSpacing),
                RectangleHeight,
                RectangleWidth,
                nodeId
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
        private void Remove(List<LinkedListVisualNode> previousVisualState, int index)
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
        
        public override void RenderCore(DrawingContext context)
        {
            var viewport = ViewportBounds;
            double viewportLeft = viewport.Left;
            double viewportRight = viewport.Right;
            double viewportTop = viewport.Top;
            double viewportBottom = viewport.Bottom;

            for (int i = 0; i < _visibleElements.Count - 1; i++)
            {
                var current = _visibleElements[i];
                var next = _visibleElements[i + 1];

                double arrowStartX = current.EndX + ArrowGap;
                if (arrowStartX > viewportRight)
                    break;

                double arrowTipX = next.CellLocation.X - ArrowGap;
                double arrowY = current.CellLocation.Y + current.CellHeight * 0.5;

                if (arrowTipX < viewportLeft
                    || arrowY < viewportTop
                    || arrowY > viewportBottom)
                {
                    continue;
                }

                DrawArrow(context, current, next);
            }

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

        private static void DrawArrow(DrawingContext context, LinkedListVisualNode startNode, LinkedListVisualNode endNode)
        {
            double startX = startNode.EndX + ArrowGap;
            double tipX = endNode.CellLocation.X - ArrowGap;

            if (tipX <= startX)
            {
                return;
            }

            double arrowY = startNode.CellLocation.Y + startNode.CellHeight * 0.5;
            double availableLength = tipX - startX;
            double headLength = Math.Min(ArrowHeadLength, availableLength * 0.5);
            double headHalfHeight = Math.Min(ArrowHeadHalfHeight, headLength * 0.75);
            double shaftEndX = tipX - headLength;

            var geometry = new StreamGeometry();

            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(new Point(startX, arrowY), false);
                geometryContext.LineTo(new Point(shaftEndX, arrowY));
                geometryContext.EndFigure(false);

                geometryContext.BeginFigure(new Point(shaftEndX, arrowY - headHalfHeight), false);
                geometryContext.LineTo(new Point(tipX, arrowY));
                geometryContext.LineTo(new Point(shaftEndX, arrowY + headHalfHeight));
                geometryContext.EndFigure(false);
            }

            context.DrawGeometry(null, ArrowPen, geometry);
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
