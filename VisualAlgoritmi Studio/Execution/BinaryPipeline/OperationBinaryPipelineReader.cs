using System.Collections.Generic;
using System.IO;
using VisualAlgoritmi.Runtime.Operations;
using VisualAlgoritmi_Studio.Canvas.Operations;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.Execution.BinaryPipeline;

public sealed class OperationBinaryPipelineReader
{
    private readonly BinaryReader _reader;
    private readonly VisualizedDataStructure _visualizedDataStructure;

    private readonly List<List<CanvasStep>> _stepsByStructureId = [];

    public OperationBinaryPipelineReader(Stream stream, VisualizedDataStructure visualizedDataStructure)
    {
        _reader = new BinaryReader(stream);
        _visualizedDataStructure = visualizedDataStructure;
    }

    public CanvasTimeline ReadAllOperations()
    {
        while (_reader.BaseStream.Position < _reader.BaseStream.Length)
        {
            PipelineEventKind eventKind = (PipelineEventKind)_reader.ReadUInt16();

            switch (eventKind)
            {
                case PipelineEventKind.StepStart:
                    ReadStep();
                    break;

                default:
                    throw new InvalidDataException($"Expected StepStart, got: {eventKind}");
            }
        }

        return new CanvasTimeline(_stepsByStructureId);
    }

    private void ReadStep()
    {
        List<List<ICanvasOp>?> stepOperationsByStructureId = [];

        while (_reader.BaseStream.Position < _reader.BaseStream.Length)
        {
            ushort marker = _reader.ReadUInt16();

            if (marker == (ushort)PipelineEventKind.StepEnd)
            {
                AddStepToTimelines(stepOperationsByStructureId);
                return;
            }

            OperationCode opcode = (OperationCode)marker;
            ICanvasOp operation = ReadOperation(opcode, out int structureId);

            if (structureId < 0)
            {
                throw new InvalidDataException($"Invalid structure id: {structureId}");
            }

            EnsureStepOperationSlotExists(stepOperationsByStructureId, structureId);

            List<ICanvasOp>? operations = stepOperationsByStructureId[structureId];

            if (operations == null)
            {
                operations = [];
                stepOperationsByStructureId[structureId] = operations;
            }

            operations.Add(operation);
        }

        throw new EndOfStreamException("StepStart was found, but StepEnd was missing.");
    }

    private void AddStepToTimelines(List<List<ICanvasOp>?> stepOperationsByStructureId)
    {
        for (int structureId = 0; structureId < stepOperationsByStructureId.Count; structureId++)
        {
            List<ICanvasOp>? operations = stepOperationsByStructureId[structureId];

            if (operations == null || operations.Count == 0)
            {
                continue;
            }

            EnsureTimelineSlotExists(structureId);

            _stepsByStructureId[structureId].Add(new CanvasStep(operations));
        }
    }

    private static void EnsureStepOperationSlotExists(List<List<ICanvasOp>?> stepOperationsByStructureId, int structureId)
    {
        while (stepOperationsByStructureId.Count <= structureId)
        {
            stepOperationsByStructureId.Add(null);
        }
    }

    private void EnsureTimelineSlotExists(int structureId)
    {
        while (_stepsByStructureId.Count <= structureId)
        {
            _stepsByStructureId.Add([]);
        }
    }

    private ICanvasOp ReadOperation(OperationCode opcode, out int structureId)
    {
        return opcode switch
        {
            // --------------------
            // Global data structure operations
            // --------------------
            OperationCode.DataStructureClear => ReadClearOperation(out structureId),
            OperationCode.DataStructureCapacitySet => ReadCapacitySetOperation(out structureId),
            OperationCode.DataStructureCreationFromCollection => ReadCreationFromCollectionOperation(out structureId),
            OperationCode.DataStructureSnapshot => ReadSnapshotOperation(out structureId),

            // --------------------
            // ArrayList
            // --------------------
            OperationCode.ArrayListAdd => ReadAddOperation(out structureId),
            OperationCode.ArrayListAddRange => ReadAddRangeOperation(out structureId),
            OperationCode.ArrayListInsert => ReadInsertOperation(out structureId),
            OperationCode.ArrayListInsertRange => ReadInsertRangeOperation(out structureId),
            OperationCode.ArrayListRemoveAt => ReadRemoveAtOperation(out structureId),
            OperationCode.ArrayListRemoveRange => ReadRemoveRangeOperation(out structureId),
            OperationCode.ArrayListSet => ReadSetOperation(out structureId),
            OperationCode.ArrayListSetRange => ReadSetRangeOperation(out structureId),
            OperationCode.ArrayListReverse => ReadReverseOperation(out structureId),

            // --------------------
            // List
            // --------------------
            OperationCode.ListAdd => ReadAddOperation(out structureId),
            OperationCode.ListAddRange => ReadAddRangeOperation(out structureId),
            OperationCode.ListInsert => ReadInsertOperation(out structureId),
            OperationCode.ListInsertRange => ReadInsertRangeOperation(out structureId),
            OperationCode.ListRemoveAt => ReadRemoveAtOperation(out structureId),
            OperationCode.ListRemoveRange => ReadRemoveRangeOperation(out structureId),
            OperationCode.ListSet => ReadSetOperation(out structureId),
            OperationCode.ListSetRange => ReadSetRangeOperation(out structureId),
            OperationCode.ListReverse => ReadReverseOperation(out structureId),

            // --------------------
            // LinkedList
            // --------------------
            OperationCode.LinkedListAddFirst => ReadLinkedListAddFirstOperation(out structureId),
            OperationCode.LinkedListAddLast => ReadLinkedListAddLastOperation(out structureId),
            OperationCode.LinkedListAddAfter => ReadLinkedListAddAfterOperation(out structureId),
            OperationCode.LinkedListAddBefore => ReadLinkedListAddBeforeOperation(out structureId),
            OperationCode.LinkedListRemoveNode => ReadLinkedListRemoveNodeOperation(out structureId),

            // --------------------
            // Queue
            // --------------------
            OperationCode.QueueEnqueue => ReadEnqueueOperation(out structureId),
            OperationCode.QueueDequeue => ReadDequeueOperation(out structureId),

            // --------------------
            // Stack
            // --------------------
            OperationCode.StackPush => ReadPushOperation(out structureId),
            OperationCode.StackPop => ReadPopOperation(out structureId),

            _ => throw new InvalidDataException($"Unknown operation opcode: {opcode}")
        };
    }

    private AddOperation ReadAddOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int index = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new AddOperation(index, value);
    }

    private AddRangeOperation ReadAddRangeOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int count = _reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException("Invalid ArrayListAddRange item count.");
        }

        List<string> values = new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(_reader.ReadString());
        }

        return new AddRangeOperation(values);
    }

    private ClearOperation ReadClearOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        return new ClearOperation();
    }

    private InsertOperation ReadInsertOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new InsertOperation(index, value);
    }

    private CapacitySetOperation ReadCapacitySetOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int newCapacity = _reader.ReadInt32();

        return new CapacitySetOperation(newCapacity);
    }

    private InsertRangeOperation ReadInsertRangeOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();
        int count = _reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException("Invalid ArrayListAddRange item count.");
        }

        List<string> values = new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(_reader.ReadString());
        }

        return new InsertRangeOperation(index, values);
    }

    private RemoveAtOperation ReadRemoveAtOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();

        return new RemoveAtOperation(index);
    }

    private RemoveRangeOperation ReadRemoveRangeOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();
        int count = _reader.ReadInt32();

        return new RemoveRangeOperation(index, count);
    }

    private SetOperation ReadSetOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new SetOperation(index, value);
    }

    private ReverseOperation ReadReverseOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();
        int count = _reader.ReadInt32();

        return new ReverseOperation(index, count);
    }

    private SetRangeOperation ReadSetRangeOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        int index = _reader.ReadInt32();
        int count = _reader.ReadInt32();

        List<string> values = new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(_reader.ReadString());
        }

        return new SetRangeOperation(index, values);
    }

    private SnapshotOperation ReadSnapshotOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        SnapshotReason reason = (SnapshotReason)_reader.ReadByte();

        int count = _reader.ReadInt32();

        List<string> values = new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(_reader.ReadString());
        }

        string description = GenerateSnapshotDescription(reason);

        return new SnapshotOperation(values, description);
    }

    private string GenerateSnapshotDescription(SnapshotReason reason, int index = 0, int count = 0)
    {
        string structureName = _visualizedDataStructure switch
        {
            VisualizedDataStructure.ArrayList => "ArrayList",
            VisualizedDataStructure.List => "List",
            VisualizedDataStructure.LinkedList => "LinkedList",
            VisualizedDataStructure.Queue => "Queue",
            VisualizedDataStructure.Stack => "Stack",
            _ => "структурата"
        };

        return reason switch
        {
            SnapshotReason.Sort =>
                $"Сортиране на {count} елемента от индекс {index}",

            SnapshotReason.SortWithComparison =>
                "Сортиране на елементите с персонализирана функция за сравнение",

            SnapshotReason.RemoveAll =>
                "Премахване на елементи по условие",

            SnapshotReason.ForEach =>
                "Прилагане на действие към елементите",

            _ =>
                $"Моментно състояние на {structureName}"
        };
    }

    private EnqueueOperation ReadEnqueueOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new EnqueueOperation(value);
    }

    private DequeueOperation ReadDequeueOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        return new DequeueOperation();
    }

    private CreationFromCollectionOperation ReadCreationFromCollectionOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        List<string> values = ReadStringValues(nameof(OperationCode.DataStructureCreationFromCollection));

        return new CreationFromCollectionOperation(values);
    }

    private AddFirst ReadLinkedListAddFirstOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int nodeId = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new AddFirst(nodeId, value);
    }

    private AddLast ReadLinkedListAddLastOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int nodeId = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new AddLast(nodeId, value);
    }

    private AddAfter ReadLinkedListAddAfterOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int targetNodeId = _reader.ReadInt32();
        int newNodeId = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new AddAfter(targetNodeId, newNodeId, value);
    }

    private AddBefore ReadLinkedListAddBeforeOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int targetNodeId = _reader.ReadInt32();
        int newNodeId = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new AddBefore(targetNodeId, newNodeId, value);
    }

    private RemoveNode ReadLinkedListRemoveNodeOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        int nodeId = _reader.ReadInt32();

        return new RemoveNode(nodeId);
    }

    private List<string> ReadStringValues(string operationName)
    {
        int count = _reader.ReadInt32();

        if (count < 0)
        {
            throw new InvalidDataException($"Invalid {operationName} item count.");
        }

        List<string> values = new(count);

        for (int i = 0; i < count; i++)
        {
            values.Add(_reader.ReadString());
        }

        return values;
    }

    private PushOperation ReadPushOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();
        string value = _reader.ReadString();

        return new PushOperation(value);
    }

    private PopOperation ReadPopOperation(out int structureId)
    {
        structureId = _reader.ReadInt32();

        return new PopOperation();
    }
}