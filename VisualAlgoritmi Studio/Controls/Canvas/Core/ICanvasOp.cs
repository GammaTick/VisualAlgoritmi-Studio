using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Core
{
    public interface ICanvasOp
    {
        string Description { get; }

        string Serialize();
    }

    public sealed class AddOperation : ICanvasOp
    {
        public string Value { get; }

        public string Description { get; }

        public AddOperation(string value)
        {
            Value = value;
            Description = "Добавена стойност: " + Value;
        }

        public string Serialize()
        {
            return $"Add|{Value}";
        }
    }

    public sealed class AddRangeOperation : ICanvasOp
    {
        public IReadOnlyList<string> Values { get; }

        public string Description { get; }

        public AddRangeOperation(IReadOnlyList<string> values)
        {
            Values = values;
            Description = "Добавени стойности: " + string.Join(", ", Values.Take(10)) + (Values.Count > 10 ? ", ..." : "");
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("AddRange|");
            sb.Append(Values.Count);
            sb.Append('|');

            foreach (var value in Values)
            {
                sb.Append(value.Length);
                sb.Append(':');
                sb.Append(value);
            }

            return sb.ToString();
        }
    }

    public sealed class AddFirst : ICanvasOp
    {
        public int NodeId { get; }

        public string Value { get; }

        public string Description { get; }

        public AddFirst(int nodeId, string value)
        {
            NodeId = nodeId;
            Value = value;
            Description = "Добавен първи елемент: " + Value;
        }

        public string Serialize()
        {
            return $"AddFirst|{NodeId}|{Value}";
        }
    }

    public sealed class AddLast : ICanvasOp
    {
        public int NodeId { get; }

        public string Value { get; }

        public string Description { get; }

        public AddLast(int nodeId, string value)
        {
            NodeId = nodeId;
            Value = value;
            Description = "Добавен последен елемент: " + Value;
        }

        public string Serialize()
        {
            return $"AddLast|{NodeId}|{Value}";
        }
    }

    public sealed class AddAfter : ICanvasOp
    {
        public int TargetNodeId { get; }

        public int NewNodeId { get; }

        public string Value { get; }

        public string? TargetValue { get; }

        public string Description { get; }

        public AddAfter(int targetNodeId, int newNodeId, string value, string? targetValue = null)
        {
            TargetNodeId = targetNodeId;
            NewNodeId = newNodeId;
            Value = value;
            TargetValue = targetValue;
            Description = CreateDescription(Value, TargetValue);
        }

        public string Serialize()
        {
            if (TargetValue == null)
            {
                return $"AddAfter|{TargetNodeId}|{NewNodeId}|{Value}";
            }

            return $"AddAfterWithTargetValue|{TargetNodeId}|{NewNodeId}|{CanvasOperationsHelper.SerializeLengthPrefixed(TargetValue, Value)}";
        }

        private static string CreateDescription(string value, string? targetValue)
        {
            if (targetValue == null)
            {
                return $"Добавен елемент {value} след възел с недостъпна стойност";
            }

            return $"Добавен елемент {value} след възел със стойност: {targetValue}";
        }
    }

    public sealed class AddBefore : ICanvasOp
    {
        public int TargetNodeId { get; }

        public int NewNodeId { get; }

        public string Value { get; }

        public string? TargetValue { get; }

        public string Description { get; }

        public AddBefore(int targetNodeId, int newNodeId, string value, string? targetValue = null)
        {
            TargetNodeId = targetNodeId;
            NewNodeId = newNodeId;
            Value = value;
            TargetValue = targetValue;
            Description = CreateDescription(Value, TargetValue);
        }

        public string Serialize()
        {
            if (TargetValue == null)
            {
                return $"AddBefore|{TargetNodeId}|{NewNodeId}|{Value}";
            }

            return $"AddBeforeWithTargetValue|{TargetNodeId}|{NewNodeId}|{CanvasOperationsHelper.SerializeLengthPrefixed(TargetValue, Value)}";
        }

        private static string CreateDescription(string value, string? targetValue)
        {
            if (targetValue == null)
            {
                return $"Добавен елемент {value} преди възел с недостъпна стойност";
            }

            return $"Добавен елемент {value} преди възел със стойност: {targetValue}";
        }
    }

    public sealed class RemoveNode : ICanvasOp
    {
        public int NodeId { get; }

        public string Description { get; }

        public RemoveNode(int nodeId, string description)
        {
            NodeId = nodeId;
            Description = description;
        }

        public string Serialize()
        {
            return $"RemoveNode|{NodeId}";
        }
    }

    public sealed class EnqueueOperation : ICanvasOp
    {
        public string Value { get; }

        public string Description { get; }

        public EnqueueOperation(string value)
        {
            Value = value;
            Description = "Добавена стойност в опашката: " + Value;
        }

        public string Serialize()
        {
            return $"Enqueue|{Value}";
        }
    }

    public sealed class DequeueOperation : ICanvasOp
    {
        public string Description { get; }

        public DequeueOperation()
        {
            Description = "Премахнат елемент от опашката";
        }

        public string Serialize()
        {
            return "Dequeue";
        }
    }

    public sealed class PushOperation : ICanvasOp
    {
        public string Value { get; }

        public string Description { get; }

        public PushOperation(string value)
        {
            Value = value;
            Description = "Добавена стойност в стека: " + Value;
        }

        public string Serialize()
        {
            return $"Push|{Value}";
        }
    }

    public sealed class PopOperation : ICanvasOp
    {
        public string Description { get; }

        public PopOperation()
        {
            Description = "Премахнат елемент от стека";
        }

        public string Serialize()
        {
            return "Pop";
        }
    }

    public sealed class SetOperation : ICanvasOp
    {
        public int Index { get; }

        public string Value { get; }

        public string Description { get; }

        public SetOperation(int index, string value)
        {
            Index = index;
            Value = value;
            Description = $"Елементът на индекс {Index} е зададен със стойност {Value}";
        }

        public string Serialize()
        {
            return $"Set|{Index}|{Value}";
        }
    }

    public sealed class CapacitySetOperation : ICanvasOp
    {
        public int NewCapacity { get; }

        public string Description { get; }

        public CapacitySetOperation(int newCapacity)
        {
            NewCapacity = newCapacity;
            Description = $"Капацитетът е зададен на {NewCapacity}";
        }

        public string Serialize()
        {
            return $"CapacitySet|{NewCapacity}";
        }
    }

    public sealed class ClearOperation : ICanvasOp
    {
        public string Description { get; }

        public ClearOperation()
        {
            Description = "Изчистена структура";
        }

        public string Serialize()
        {
            return "Clear";
        }
    }

    public sealed class SnapshotOperation : ICanvasOp
    {
        public IReadOnlyList<string> Values { get; }

        public string Description { get; }

        public SnapshotOperation(IReadOnlyList<string> values, string description)
        {
            Values = values;
            Description = description;
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("Snapshot|");
            sb.Append(Values.Count);
            sb.Append('|');

            sb.Append(Description.Length);
            sb.Append(':');
            sb.Append(Description);
            sb.Append('|');

            foreach (var value in Values)
            {
                sb.Append(value.Length);
                sb.Append(':');
                sb.Append(value);
            }

            return sb.ToString();
        }
    }

    public sealed class InsertOperation : ICanvasOp
    {
        public int Index { get; }

        public string Value { get; }

        public string Description { get; }

        public InsertOperation(int index, string value)
        {
            Index = index;
            Value = value;
            Description = $"Вмъкната стойност: {Value} на индекс {Index}";
        }

        public string Serialize()
        {
            return $"Insert|{Index}|{Value}";
        }
    }

    public sealed class InsertRangeOperation : ICanvasOp
    {
        public int StartIndex { get; }

        public IReadOnlyList<string> Values { get; }

        public string Description { get; }

        public InsertRangeOperation(int startIndex, IReadOnlyList<string> values)
        {
            StartIndex = startIndex;
            Values = values;
            Description = $"Вмъкнати стойности: {string.Join(", ", Values.Take(10))}" + (Values.Count > 10 ? ", ..." : "") + $" на индекс {StartIndex}";
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("InsertRange|");
            sb.Append(StartIndex);
            sb.Append('|');
            sb.Append(Values.Count);
            sb.Append('|');

            foreach (var value in Values)
            {
                sb.Append(value.Length);
                sb.Append(':');
                sb.Append(value);
            }

            return sb.ToString();
        }
    }

    public sealed class RemoveOperation : ICanvasOp
    {
        public int Index { get; }

        public string Description { get; }

        public RemoveOperation(int index)
        {
            Index = index;
            Description = $"Премахната стойност на индекс {Index}";
        }

        public string Serialize()
        {
            return $"Remove|{Index}";
        }
    }

    public sealed class RemoveRangeOperation : ICanvasOp
    {
        public int StartIndex { get; }

        public int Count { get; }

        public string Description { get; }

        public RemoveRangeOperation(int startIndex, int count)
        {
            StartIndex = startIndex;
            Count = count;
            Description = $"Премахнат диапазон от индекс {StartIndex} с брой {Count}";
        }

        public string Serialize()
        {
            return $"RemoveRange|{StartIndex}|{Count}";
        }
    }

    public sealed class ReverseOperation : ICanvasOp
    {
        public int StartIndex { get; }

        public int Count { get; }

        public string Description { get; }

        public ReverseOperation(int startIndex, int count)
        {
            StartIndex = startIndex;
            Count = count;
            Description = $"Обърнат диапазон от индекс {StartIndex} с брой {Count}";
        }

        public string Serialize()
        {
            return $"Reverse|{StartIndex}|{Count}";
        }
    }

    public sealed class SetRangeOperation : ICanvasOp
    {
        public int StartIndex { get; }

        public IReadOnlyList<string> Values { get; }

        public string Description { get; }

        public SetRangeOperation(int startIndex, IReadOnlyList<string> values)
        {
            StartIndex = startIndex;
            Values = values;
            Description = "Зададени стойности: " + string.Join(", ", Values.Take(10)) + (Values.Count > 10 ? ", ..." : "") + $" започвайки от индекс {StartIndex}";
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append("SetRange|");
            sb.Append(StartIndex);
            sb.Append('|');
            sb.Append(Values.Count);
            sb.Append('|');

            foreach (var value in Values)
            {
                sb.Append(value.Length);
                sb.Append(':');
                sb.Append(value);
            }

            return sb.ToString();
        }
    }

    public static class CanvasOperationsHelper
    {
        public static string GetTextForValue<T>(T value)
        {
            return value?.ToString() ?? "null";
        }

        internal static string SerializeLengthPrefixed(params string[] values)
        {
            var sb = new StringBuilder();

            foreach (string value in values)
            {
                sb.Append(value.Length);
                sb.Append(':');
                sb.Append(value);
            }

            return sb.ToString();
        }

        public static SnapshotOperation GenerateSnapshotOperation<T>(T[] items, int count, string description)
        {
            string[] elements = new string[count];

            for (int i = 0; i < count; i++)
            {
                elements[i] = items[i]?.ToString() ?? "null";
            }

            return new SnapshotOperation(elements, description);
        }

        public static InsertRangeOperation GenerateInsertRangeOperation<T>(T[] items, int startIndex)
        {
            string[] elements = new string[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                elements[i] = items[i]?.ToString() ?? "null";
            }

            return new InsertRangeOperation(startIndex, elements);
        }

        public static SetRangeOperation GenerateSetRangeOperation(System.Collections.ICollection values, int startIndex)
        {
            string[] elements = new string[values.Count];

            int i = 0;

            foreach (var value in values)
            {
                elements[i++] = value?.ToString() ?? "null";
            }

            return new SetRangeOperation(startIndex, elements);
        }

        public static AddRangeOperation GenerateAddRangeOperation<T>(ICollection<T> items)
        {
            string[] elements = new string[items.Count];

            int i = 0;

            foreach (T item in items)
            {
                elements[i++] = item?.ToString() ?? "null";
            }

            return new AddRangeOperation(elements);
        }
    }
}
