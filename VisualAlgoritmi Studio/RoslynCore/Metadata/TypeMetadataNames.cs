using System.Collections.Generic;

namespace VisualAlgoritmi_Studio.RoslynCore.Metadata
{
    internal static class TypeMetadataNames
    {
        public static readonly string ArrayList =
            typeof(System.Collections.ArrayList).FullName!;

        public static readonly string List =
            typeof(List<>).FullName!;

        public static readonly string LinkedList =
            typeof(LinkedList<>).FullName!;

        public static readonly string Queue =
            typeof(Queue<>).FullName!;

        public static readonly string Stack =
            typeof(Stack<>).FullName!;
    }
}