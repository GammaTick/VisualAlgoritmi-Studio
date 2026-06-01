using System;

namespace VisualAlgoritmi_Studio.RoslynCore.Metadata
{
    public sealed class DataStructureMetadata
    {
        public Type ReplacementTypeRuntimeType { get; }
        public string OriginalTypeMetadataName { get; }
        public string ReplacementTypeMetadataName { get; }

        public DataStructureMetadata(Type replacementTypeRuntimeType,
            string originalTypeMetadataName,
            string replacementTypeMetadataName)
        {
            ReplacementTypeRuntimeType = replacementTypeRuntimeType;
            OriginalTypeMetadataName = originalTypeMetadataName;
            ReplacementTypeMetadataName = replacementTypeMetadataName;
        }
    }
}