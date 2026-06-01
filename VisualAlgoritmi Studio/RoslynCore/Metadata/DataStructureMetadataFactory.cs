using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Visualization;
using VisualAlgoritmi.Runtime.Collections;

namespace VisualAlgoritmi_Studio.RoslynCore.Metadata
{
    internal static class DataStructureMetadataFactory
    {
        public static DataStructureMetadata Create(VisualizedDataStructure dataStructure)
        {
            string? originalTypeMetadataName;
            Type visualDataStructureRuntimeType;

            switch (dataStructure)
            {
                case VisualizedDataStructure.ArrayList:
                    originalTypeMetadataName = TypeMetadataNames.ArrayList;
                    visualDataStructureRuntimeType = typeof(VisualArrayList);
                    break;

                case VisualizedDataStructure.List:
                    originalTypeMetadataName = TypeMetadataNames.List;
                    visualDataStructureRuntimeType = typeof(VisualList<>);
                    break;

                case VisualizedDataStructure.LinkedList:
                    originalTypeMetadataName = TypeMetadataNames.LinkedList;
                    visualDataStructureRuntimeType = typeof(VisualLinkedList<>);
                    break;

                case VisualizedDataStructure.Queue:
                    originalTypeMetadataName = TypeMetadataNames.Queue;
                    visualDataStructureRuntimeType = typeof(VisualQueue<>);
                    break;

                case VisualizedDataStructure.Stack:
                    originalTypeMetadataName = TypeMetadataNames.Stack;
                    visualDataStructureRuntimeType = typeof(VisualStack<>);
                    break;

                default:
                    ThrowHelper.ThrowUnsupportedDataStructure(dataStructure);
                    throw null!;
            }

            if (originalTypeMetadataName == null)
            {
                ThrowHelper.ThrowMetadataNull();
            }

            string dataStructureName = dataStructure.ToString();

            return new DataStructureMetadata(
                visualDataStructureRuntimeType,
                originalTypeMetadataName,
                "Visual" + dataStructureName
            );
        }

        internal static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowUnsupportedDataStructure(VisualizedDataStructure dataStructure)
            {
                throw new NotSupportedException(
                    $"Unsupported data structure: '{dataStructure}' ({(int)dataStructure}). " +
                    $"Supported: {VisualizedDataStructure.ArrayList}.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowMetadataNull()
            {
                throw new InvalidOperationException("Data structure metadata is incomplete.");
            }
        }
    }
}