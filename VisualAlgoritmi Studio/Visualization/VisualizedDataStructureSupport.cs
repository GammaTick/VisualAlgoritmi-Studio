namespace VisualAlgoritmi_Studio.Visualization
{
    internal static class VisualizedDataStructureSupport
    {
        public static bool IsSupported(VisualizedDataStructure dataStructure)
        {
            return dataStructure switch
            {
                VisualizedDataStructure.ArrayList => true,
                VisualizedDataStructure.LinkedList => true,
                VisualizedDataStructure.List => true,
                VisualizedDataStructure.Queue => true,
                VisualizedDataStructure.Stack => true,
                _ => false
            };
        }
    }
}
