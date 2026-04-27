using System;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public sealed class DataStructureMetadata
    {
        public Type ReplacementTypeRuntimeType { get; }
        public string OriginalTypeMetadataName { get; }
        public string ReplacementTypeMetadataName { get; }
        public string CanvasNamespace { get; }

        public DataStructureMetadata(
            Type replacementTypeRuntimeType,
            string originalTypeMetadataName,
            string replacementTypeMetadataName,
            string canvasNamespace)
        {
            ReplacementTypeRuntimeType = replacementTypeRuntimeType;
            OriginalTypeMetadataName = originalTypeMetadataName;
            ReplacementTypeMetadataName = replacementTypeMetadataName;
            CanvasNamespace = canvasNamespace;
        }
    }
}