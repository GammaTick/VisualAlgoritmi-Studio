using System;
using System.Collections.Generic;
using System.Text;

namespace VisualAlgoritmi.Runtime.Operations
{
    public enum SnapshotReason : byte
    {
        Unknown = 0,
        Sort = 1,
        RemoveAll = 2,
        ForEach = 3,
        SortWithComparison = 4
    }
}
