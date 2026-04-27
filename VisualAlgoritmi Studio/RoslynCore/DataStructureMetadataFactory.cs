using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.List;
using VisualAlgoritmi_Studio.DotNetInternals;
using VisualAlgoritmi_Studio.Visualization;
using System.Collections;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.ArrayList;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.LinkedList;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.Queue;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.Stack;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    internal static class DataStructureMetadataFactory
    {
        public static DataStructureMetadata Create(VisualizedDataStructure dataStructure)
        {
            string? originalTypeMetadataName;
            Type visualDataStructureRuntimeType;
            string? canvasNamespace;

            switch (dataStructure)
            {
                case VisualizedDataStructure.ArrayList:
                    originalTypeMetadataName = TypeMetadataNames.ArrayList;
                    visualDataStructureRuntimeType = typeof(VisualArrayList);
                    canvasNamespace = typeof(ArrayListCanvas).Namespace;
                    break;

                case VisualizedDataStructure.List:
                    originalTypeMetadataName = TypeMetadataNames.List;
                    visualDataStructureRuntimeType = typeof(VisualList<>);
                    canvasNamespace = typeof(ListCanvas).Namespace;
                    break;

                case VisualizedDataStructure.LinkedList:
                    originalTypeMetadataName = TypeMetadataNames.LinkedList;
                    visualDataStructureRuntimeType = typeof(VisualLinkedList<>);
                    canvasNamespace = typeof(LinkedListCanvas).Namespace;
                    break;

                case VisualizedDataStructure.Queue:
                    originalTypeMetadataName = TypeMetadataNames.Queue;
                    visualDataStructureRuntimeType = typeof(VisualQueue<>);
                    canvasNamespace = typeof(QueueCanvas).Namespace;
                    break;

                case VisualizedDataStructure.Stack:
                    originalTypeMetadataName = TypeMetadataNames.Stack;
                    visualDataStructureRuntimeType = typeof(VisualStack<>);
                    canvasNamespace = typeof(StackCanvas).Namespace;
                    break;

                default:
                    ThrowHelper.ThrowDataStructureIdArgumentOutOfRange();
                    throw null!;
            }

            if (originalTypeMetadataName == null || canvasNamespace == null)
            {
                ThrowHelper.ThrowMetadataNull();
            }

            string dataStructureName = dataStructure.ToString();

            return new DataStructureMetadata(
                visualDataStructureRuntimeType,
                originalTypeMetadataName,
                "Visual" + dataStructureName,
                canvasNamespace
            );
        }

        internal static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowDataStructureIdArgumentOutOfRange()
            {
                throw new InvalidOperationException("dataStructureId");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowMetadataNull()
            {
                throw new InvalidOperationException("Metadata values cannot be null.");
            }
        }
    }
}