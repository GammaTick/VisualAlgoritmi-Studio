using Avalonia;
using Avalonia.Media.TextFormatting;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Structures.LinkedList
{
    public class LinkedListVisualNode : VisualNode
    {
        public int NodeId { get; }

        public LinkedListVisualNode(TextLayout layout, Point cellLocation, double cellHeight, double cellWidth, int nodeId)
            : base(layout, cellLocation, cellHeight, cellWidth)
        {
            NodeId = nodeId;
        }

        public override VisualNode Clone()
        {
            return new LinkedListVisualNode(Layout, CellLocation, CellHeight, CellWidth, NodeId);
        }
    }
}
