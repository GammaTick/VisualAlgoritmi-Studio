namespace VisualAlgoritmi.Runtime.Operations;

public enum OperationCode : ushort
{
    DataStructureCreation = 0,

    // --------------------
    // Global data structure operations
    // --------------------

    DataStructureClear = 1,
    DataStructureCapacitySet = 2,
    DataStructureCreationFromCollection = 3,
    DataStructureSnapshot = 4,

    // --------------------
    // ArrayList
    // --------------------

    ArrayListAdd = 100,
    ArrayListAddRange = 102,
    ArrayListInsert = 103,
    ArrayListInsertRange = 104,
    ArrayListRemoveAt = 105,
    ArrayListRemoveRange = 106,
    ArrayListSet = 107,
    ArrayListReverse = 108,
    ArrayListSetRange = 109,

    // --------------------
    // List
    // --------------------

    ListAdd = 200,
    ListAddRange = 202,
    ListInsert = 204,
    ListInsertRange = 205,
    ListRemoveAt = 206,
    ListRemoveRange = 207,
    ListSet = 208,
    ListReverse = 209,
    ListSetRange = 210,

    // --------------------
    // LinkedList
    // --------------------

    LinkedListAddFirst = 300,
    LinkedListAddLast = 301,
    LinkedListAddAfter = 302,
    LinkedListAddBefore = 303,
    LinkedListRemoveNode = 304,

    // --------------------
    // Queue
    // --------------------

    QueueDequeue = 401,
    QueueEnqueue = 403,

    // --------------------
    // Stack
    // --------------------

    StackPop = 501,
    StackPush = 503,
}

public enum PipelineEventKind : ushort
{
    StepStart = 0xFFFE,
    StepEnd = 0xFFFF
}