using System;
using System.Collections;
using System.IO;
using System.Text;

namespace VisualAlgoritmi.Runtime.Operations;

public static class OperationRecorder
{
    private static readonly MemoryStream Stream = new();
    private static readonly BinaryWriter Writer = new(Stream, Encoding.UTF8, leaveOpen: true);

    public static void Clear()
    {
        Stream.SetLength(0);
        Stream.Position = 0;
    }

    public static void WriteTo(Stream destination)
    {
        Writer.Flush();

        if (Stream.Length > int.MaxValue)
        {
            throw new InvalidOperationException("Too many visual operations were recorded.");
        }

        Stream.Position = 0;
        Stream.CopyTo(destination);
        destination.Flush();
    }

    public static void BeginStep()
    {
        Writer.Write((ushort)PipelineEventKind.StepStart);
    }

    public static void EndStep()
    {
        Writer.Write((ushort)PipelineEventKind.StepEnd);
    }

    // --------------------
    // Global data structure operations
    // --------------------

    public static void WriteDataStructureClear(int structureId)
    {
        WriteOperationHeader(OperationCode.DataStructureClear, structureId);
    }

    public static void WriteDataStructureCapacitySet(int structureId, int newCapacity)
    {
        WriteOperationHeader(OperationCode.DataStructureCapacitySet, structureId);
        Writer.Write(newCapacity);
    }

    public static void WriteDataStructureCreationFromCollection(int structureId, ICollection collection)
    {
        WriteOperationHeader(OperationCode.DataStructureCreationFromCollection, structureId);
        WriteCollectionValues(collection);
    }

    public static void WriteDataStructureSnapshot(int structureId, SnapshotReason reason, ICollection collection)
    {
        WriteOperationHeader(OperationCode.DataStructureSnapshot, structureId);
        Writer.Write((byte)reason);
        WriteCollectionValues(collection);
    }

    // --------------------
    // ArrayList
    // --------------------

    public static void WriteArrayListAdd(int structureId, int index, object? value)
    {
        WriteOperationHeader(OperationCode.ArrayListAdd, structureId);
        Writer.Write(index);
        WriteDisplayValue(value);
    }

    public static void WriteArrayListAddRange(int structureId, ICollection collection)
    {
        WriteOperationHeader(OperationCode.ArrayListAddRange, structureId);
        WriteCollectionValues(collection);
    }

    public static void WriteArrayListInsert(int structureId, int index, object? value)
    {
        WriteOperationHeader(OperationCode.ArrayListInsert, structureId);
        Writer.Write(index);
        WriteDisplayValue(value);
    }

    public static void WriteArrayListInsertRange(int structureId, int index, ICollection collection)
    {
        WriteOperationHeader(OperationCode.ArrayListInsertRange, structureId);
        Writer.Write(index);
        WriteCollectionValues(collection);
    }

    public static void WriteArrayListRemoveAt(int structureId, int index)
    {
        WriteOperationHeader(OperationCode.ArrayListRemoveAt, structureId);
        Writer.Write(index);
    }

    public static void WriteArrayListRemoveRange(int structureId, int index, int count)
    {
        WriteOperationHeader(OperationCode.ArrayListRemoveRange, structureId);
        Writer.Write(index);
        Writer.Write(count);
    }

    public static void WriteArrayListSet(int structureId, int index, object? value)
    {
        WriteOperationHeader(OperationCode.ArrayListSet, structureId);
        Writer.Write(index);
        WriteDisplayValue(value);
    }

    public static void WriteArrayListReverse(int structureId, int index, int count)
    {
        WriteOperationHeader(OperationCode.ArrayListReverse, structureId);
        Writer.Write(index);
        Writer.Write(count);
    }

    public static void WriteArrayListSetRange(int structureId, int index, ICollection collection)
    {
        WriteOperationHeader(OperationCode.ArrayListSetRange, structureId);
        Writer.Write(index);
        WriteCollectionValues(collection);
    }

    // --------------------
    // List
    // --------------------

    public static void WriteListAdd(int structureId, int index, object? value)
    {
        WriteOperationHeader(OperationCode.ListAdd, structureId);
        Writer.Write(index);
        WriteDisplayValue(value);
    }

    public static void WriteListAddRange(int structureId, ICollection collection)
    {
        WriteOperationHeader(OperationCode.ListAddRange, structureId);
        WriteCollectionValues(collection);
    }

    public static void WriteListInsert(int structureId, int index, object? value)
    {
        WriteOperationHeader(OperationCode.ListInsert, structureId);
        Writer.Write(index);
        WriteDisplayValue(value);
    }

    public static void WriteListInsertRange(int structureId, int index, ICollection collection)
    {
        WriteOperationHeader(OperationCode.ListInsertRange, structureId);
        Writer.Write(index);
        WriteCollectionValues(collection);
    }

    public static void WriteListRemoveAt(int structureId, int index)
    {
        WriteOperationHeader(OperationCode.ListRemoveAt, structureId);
        Writer.Write(index);
    }

    public static void WriteListRemoveRange(int structureId, int index, int count)
    {
        WriteOperationHeader(OperationCode.ListRemoveRange, structureId);
        Writer.Write(index);
        Writer.Write(count);
    }

    public static void WriteListSet(int structureId, int index, object? value)
    {
        WriteOperationHeader(OperationCode.ListSet, structureId);
        Writer.Write(index);
        WriteDisplayValue(value);
    }

    public static void WriteListReverse(int structureId, int index, int count)
    {
        WriteOperationHeader(OperationCode.ListReverse, structureId);
        Writer.Write(index);
        Writer.Write(count);
    }

    public static void WriteListSetRange(int structureId, int index, ICollection collection)
    {
        WriteOperationHeader(OperationCode.ListSetRange, structureId);
        Writer.Write(index);
        WriteCollectionValues(collection);
    }

    // --------------------
    // LinkedList
    // --------------------

    public static void WriteLinkedListAddFirst(int structureId, int nodeId, object? value)
    {
        WriteOperationHeader(OperationCode.LinkedListAddFirst, structureId);
        Writer.Write(nodeId);
        WriteDisplayValue(value);
    }

    public static void WriteLinkedListAddLast(int structureId, int nodeId, object? value)
    {
        WriteOperationHeader(OperationCode.LinkedListAddLast, structureId);
        Writer.Write(nodeId);
        WriteDisplayValue(value);
    }

    public static void WriteLinkedListAddAfter(int structureId, int targetNodeId, int newNodeId, object? value)
    {
        WriteOperationHeader(OperationCode.LinkedListAddAfter, structureId);
        Writer.Write(targetNodeId);
        Writer.Write(newNodeId);
        WriteDisplayValue(value);
    }

    public static void WriteLinkedListAddBefore(int structureId, int targetNodeId, int newNodeId, object? value)
    {
        WriteOperationHeader(OperationCode.LinkedListAddBefore, structureId);
        Writer.Write(targetNodeId);
        Writer.Write(newNodeId);
        WriteDisplayValue(value);
    }

    public static void WriteLinkedListRemoveNode(int structureId, int nodeId)
    {
        WriteOperationHeader(OperationCode.LinkedListRemoveNode, structureId);
        Writer.Write(nodeId);
    }

    // --------------------
    // Queue
    // --------------------

    public static void WriteQueueEnqueue<T>(int structureId, T item)
    {
        WriteOperationHeader(OperationCode.QueueEnqueue, structureId);
        WriteDisplayValue(item);
    }

    public static void WriteQueueDequeue(int structureId)
    {
        WriteOperationHeader(OperationCode.QueueDequeue, structureId);
    }

    // --------------------
    // Stack
    // --------------------

    public static void WriteStackPush<T>(int structureId, T item)
    {
        WriteOperationHeader(OperationCode.StackPush, structureId);
        WriteDisplayValue(item);
    }

    public static void WriteStackPop(int structureId)
    {
        WriteOperationHeader(OperationCode.StackPop, structureId);
    }

    // --------------------
    // Helpers
    // --------------------

    private static void WriteOperationHeader(OperationCode opcode, int structureId)
    {
        Writer.Write((ushort)opcode);
        Writer.Write(structureId);
    }

    private static void WriteCollectionValues(ICollection collection)
    {
        Writer.Write(collection.Count);

        foreach (object? item in collection)
        {
            WriteDisplayValue(item);
        }
    }

    private static void WriteDisplayValue(object? value)
    {
        Writer.Write(value?.ToString() ?? "null");
    }
}