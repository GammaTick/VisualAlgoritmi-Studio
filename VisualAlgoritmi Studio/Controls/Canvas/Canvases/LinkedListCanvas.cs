using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Canvas.Operations;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;
using Avalonia.Input;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Canvases
{
    public class LinkedListCanvas : VisualizerCanvasBase<LinkedListVisualNode>
    {
        private static readonly SolidColorBrush Fill = new(Color.FromArgb(80, 0, 120, 215));
        private static readonly SolidColorBrush Stroke = new(Color.FromArgb(200, 0, 120, 215));
        private static readonly SolidColorBrush MarkerBrush = new(Colors.Black);

        private static readonly Pen Pen = new(Stroke, 1);

        private static readonly Pen ArrowPen = new(Stroke, 4)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        private static readonly Pen DividerPen = new(
            new SolidColorBrush(Color.FromArgb(100, 0, 120, 215)),
            1
        );

        private static TextLayout? HeadTextLayout;
        private static TextLayout? TailTextLayout;

        private const double RectangleWidth = 240;
        private const double RectangleHeight = 100;
        private const double RectangleSpacing = 60;
        private const double CornerRadius = 4;

        private const double MarkerFontSize = 16;
        private const double MarkerVerticalGap = 10;

        private const double LinkTextFontSize = 16;
        private const double LinkTextHeight = 30;
        private const double LinkTextPaddingX = 10;
        private const double LinkTextPaddingY = 4;

        private const double ArrowGap = 8;
        private const double ArrowHeadLength = 12;
        private const double ArrowHeadHalfHeight = 8;

        private bool _displayExtraInfo;

        public LinkedListCanvas()
        {
            _displayExtraInfo = App.Settings.DisplayExtraLinkedListInfo;

            HeadTextLayout ??= CreateMarkerLayout("Head");
            TailTextLayout ??= CreateMarkerLayout("Tail");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            bool isShortcutPressed = OperatingSystem.IsMacOS()
                ? e.KeyModifiers.HasFlag(KeyModifiers.Meta)
                : e.KeyModifiers.HasFlag(KeyModifiers.Control);

            if (e.Key == Key.L && isShortcutPressed)
            {
                e.Handled = true;
                _displayExtraInfo = !_displayExtraInfo;
               
                App.Settings.DisplayExtraLinkedListInfo = _displayExtraInfo;
                App.Settings.Save();

                InvalidateVisual();
            }
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindNodeIndex(List<LinkedListVisualNode> visualState, int nodeId)
        {
            for (int i = 0; i < visualState.Count; i++)
            {
                if (visualState[i].NodeId == nodeId)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"Visual node with NodeId {nodeId} not found.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Insert(List<LinkedListVisualNode> previousVisualState, int index, int nodeId, string value)
        {
            double xOffset = index == 0
                ? RectangleSpacing
                : previousVisualState[index - 1].EndX + RectangleSpacing;

            string nextValue = index == previousVisualState.Count
                ? "null"
                : previousVisualState[index].Value;

            var node = new LinkedListVisualNode(
                CreateValueTextLayout(value),
                CreateLinkTextLayout($"next: {nextValue}"),
                new Point(xOffset, RectangleSpacing),
                RectangleHeight,
                RectangleWidth,
                nodeId,
                value
            );

            previousVisualState.Insert(index, node);

            for (int i = index + 1; i < previousVisualState.Count; i++)
            {
                previousVisualState[i].CellLocation = new Point(
                    previousVisualState[i].CellLocation.X + RectangleWidth + RectangleSpacing,
                    previousVisualState[i].CellLocation.Y
                );
            }

            UpdateNodeLinksAt(previousVisualState, index - 1);
            UpdateNodeLinksAt(previousVisualState, index + 1);
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

            UpdateNodeLinksAt(previousVisualState, index - 1);
            UpdateNodeLinksAt(previousVisualState, index);
        }

        private void UpdateNodeLinksAt(List<LinkedListVisualNode> visualState, int index)
        {
            if (index < 0 || index >= visualState.Count)
            {
                return;
            }

            string previousValue = index == 0
                ? "null"
                : visualState[index - 1].Value;

            string nextValue = index == visualState.Count - 1
                ? "null"
                : visualState[index + 1].Value;

            LinkedListVisualNode oldNode = visualState[index];

            visualState[index] = oldNode.CopyWithLinkLayouts(
                CreateLinkTextLayout($"next: {nextValue}")
            );
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
                {
                    break;
                }

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

                if (element.CellLocation.X > viewportRight)
                {
                    break;
                }

                if (!IsElementVisible(element))
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

                DrawNodeText(context, element);
            }

            if (!_displayExtraInfo)
            {
                return;
            }

            if (IsElementVisible(0))
            {
                LinkedListVisualNode firstElement = _visibleElements[0];

                HeadTextLayout!.Draw(
                    context,
                    new Point(
                        firstElement.CellLocation.X,
                        firstElement.CellLocation.Y - MarkerVerticalGap - HeadTextLayout.Height
                    )
                );
            }

            if (IsElementVisible(_visibleElements.Count - 1))
            {
                LinkedListVisualNode lastElement = _visibleElements[^1];

                TailTextLayout!.Draw(
                    context,
                    new Point(
                        lastElement.CellLocation.X + lastElement.CellWidth - TailTextLayout.Width,
                        lastElement.CellLocation.Y - MarkerVerticalGap - TailTextLayout.Height
                    )
                );
            }
        }

        private bool IsElementVisible(int index)
        {
            if (index < 0 || index >= _visibleElements.Count)
            {
                return false;
            }

            return IsElementVisible(_visibleElements[index]);
        }

        private bool IsElementVisible(VisualNode element)
        {
            Rect viewport = ViewportBounds;

            return element.EndX >= viewport.Left
                && element.CellLocation.X <= viewport.Right
                && element.CellLocation.Y <= viewport.Bottom
                && element.CellLocation.Y + element.CellHeight >= viewport.Top;
        }

        private void DrawNodeText(DrawingContext context, LinkedListVisualNode element)
        {
            if (!_displayExtraInfo)
            {
                element.Layout.Draw(
                    context,
                    element.GetTextLocation()
                );
                return;
            }

            double bottomDividerY = element.CellLocation.Y + element.CellHeight - LinkTextHeight;

            context.DrawLine(
                DividerPen,
                new Point(element.CellLocation.X, bottomDividerY),
                new Point(element.EndX, bottomDividerY)
            );

            var textLocation = element.GetTextLocation();

            element.Layout.Draw(
                context,
                new Point(
                    textLocation.X,
                    textLocation.Y - LinkTextHeight * 0.5
                )
            );

            element.NextLayout.Draw(
                context,
                new Point(
                    element.CellLocation.X + LinkTextPaddingX,
                    element.CellLocation.Y + element.CellHeight - LinkTextHeight + LinkTextPaddingY
                )
            );
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

        private TextLayout CreateValueTextLayout(string text)
        {
            const double paddingX = 12;

            double maxWidth = RectangleWidth - paddingX * 2;
            double maxHeight = RectangleHeight - LinkTextHeight * 2;

            return CreateFittedTextLayout(text, maxWidth, maxHeight);
        }

        private TextLayout CreateLinkTextLayout(string text)
        {
            return BuildLayout(
                text,
                LinkTextFontSize,
                TextTrimming.CharacterEllipsis,
                RectangleWidth - LinkTextPaddingX * 2,
                LinkTextHeight
            );
        }

        private TextLayout CreateFittedTextLayout(string text, double maxWidth, double maxHeight)
        {
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

        private static TextLayout CreateMarkerLayout(string text)
        {
            return new TextLayout(
                text,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
                MarkerFontSize,
                MarkerBrush,
                TextAlignment.Left,
                TextWrapping.NoWrap,
                TextTrimming.None,
                null,
                FlowDirection.LeftToRight,
                RectangleWidth,
                34,
                double.NaN,
                0,
                1,
                null,
                null
            );
        }
    }

    public class LinkedListVisualNode : VisualNode
    {
        public int NodeId { get; }
        public string Value { get; }

        public TextLayout NextLayout { get; }

        public LinkedListVisualNode(
            TextLayout valueLayout,
            TextLayout nextLayout,
            Point cellLocation,
            double cellHeight,
            double cellWidth,
            int nodeId,
            string value)
            : base(valueLayout, cellLocation, cellHeight, cellWidth)
        {
            NextLayout = nextLayout;
            NodeId = nodeId;
            Value = value;
        }

        public LinkedListVisualNode CopyWithLinkLayouts(TextLayout nextLayout)
        {
            return new LinkedListVisualNode(
                Layout,
                nextLayout,
                CellLocation,
                CellHeight,
                CellWidth,
                NodeId,
                Value
            );
        }

        public override VisualNode Clone()
        {
            return new LinkedListVisualNode(
                Layout,
                NextLayout,
                CellLocation,
                CellHeight,
                CellWidth,
                NodeId,
                Value
            );
        }
    }
}