using System.Threading;

namespace VisualAlgoritmi.Runtime.Operations
{
    public static class VisualStructureIdProvider
    {
        private static int _nextId = -1;

        public static int GetNextId()
        {
            return Interlocked.Increment(ref _nextId);
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref _nextId, -1);
        }
    }
}