using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualAlgoritmi.Runtime.Operations;

namespace VisualAlgoritmi_Studio.Canvas.Operations;

// --------------------
// Core
// --------------------

public interface ICanvasOp
{
    string Description { get; }

    string Serialize();
}

public sealed record CanvasStep(IReadOnlyList<ICanvasOp> Operations);

// --------------------
// Global data structure operations
// --------------------

public sealed record ClearOperation() : ICanvasOp
{
    public string Description { get; } = "Изчистена структура";

    public string Serialize()
    {
        return "Clear";
    }
}

public sealed record CapacitySetOperation(int NewCapacity) : ICanvasOp
{
    public string Description { get; } = $"Капацитетът е зададен на {NewCapacity}";

    public string Serialize()
    {
        return $"CapacitySet|{NewCapacity}";
    }
}

public sealed record CreationFromCollectionOperation(IReadOnlyList<string> Values) : ICanvasOp
{
    public string Description { get; } =
        "Създадена структура със стойности: " + CanvasOpText.Preview(Values);

    public string Serialize()
    {
        return "CreationFromCollection|" + Values.Count + "|" + CanvasOpText.SerializeLengthPrefixed(Values);
    }
}

public sealed record SnapshotOperation(IReadOnlyList<string> Values, string Description) : ICanvasOp
{
    public string Serialize()
    {
        return "Snapshot|" +
               Values.Count + "|" +
               CanvasOpText.SerializeLengthPrefixed(Values) +
               CanvasOpText.SerializeLengthPrefixed([Description]);
    }
}
// --------------------
// Indexed collection operations
// ArrayList / List
// --------------------

public sealed record AddOperation(int Index, string Value) : ICanvasOp
{
    public string Description { get; } = $"Добавена стойност: {Value} на индекс {Index}";

    public string Serialize()
    {
        return $"Add|{Index}|{Value}";
    }
}

public sealed record AddRangeOperation(IReadOnlyList<string> Values) : ICanvasOp
{
    public string Description { get; } =
        "Добавени стойности: " + CanvasOpText.Preview(Values);

    public string Serialize()
    {
        return "AddRange|" + Values.Count + "|" + CanvasOpText.SerializeLengthPrefixed(Values);
    }
}

public sealed record InsertOperation(int Index, string Value) : ICanvasOp
{
    public string Description { get; } = $"Вмъкната стойност: {Value} на индекс {Index}";

    public string Serialize()
    {
        return $"Insert|{Index}|{Value}";
    }
}

public sealed record InsertRangeOperation(int StartIndex, IReadOnlyList<string> Values) : ICanvasOp
{
    public string Description { get; } =
        "Вмъкнати стойности: " + CanvasOpText.Preview(Values) + $" на индекс {StartIndex}";

    public string Serialize()
    {
        return "InsertRange|" + StartIndex + "|" + Values.Count + "|" + CanvasOpText.SerializeLengthPrefixed(Values);
    }
}

public sealed record RemoveAtOperation(int Index) : ICanvasOp
{
    public string Description { get; } = $"Премахната стойност на индекс {Index}";

    public string Serialize()
    {
        return $"Remove|{Index}";
    }
}

public sealed record RemoveRangeOperation(int StartIndex, int Count) : ICanvasOp
{
    public string Description { get; } = $"Премахнат диапазон от индекс {StartIndex} с брой {Count}";

    public string Serialize()
    {
        return $"RemoveRange|{StartIndex}|{Count}";
    }
}

public sealed record SetOperation(int Index, string Value) : ICanvasOp
{
    public string Description { get; } = $"Елементът на индекс {Index} е зададен със стойност {Value}";

    public string Serialize()
    {
        return $"Set|{Index}|{Value}";
    }
}

public sealed record SetRangeOperation(int StartIndex, IReadOnlyList<string> Values) : ICanvasOp
{
    public string Description { get; } =
        "Зададени стойности: " + CanvasOpText.Preview(Values) + $" започвайки от индекс {StartIndex}";

    public string Serialize()
    {
        return "SetRange|" + StartIndex + "|" + Values.Count + "|" + CanvasOpText.SerializeLengthPrefixed(Values);
    }
}

public sealed record ReverseOperation(int StartIndex, int Count) : ICanvasOp
{
    public string Description { get; } = $"Обърнат диапазон от индекс {StartIndex} с брой {Count}";

    public string Serialize()
    {
        return $"Reverse|{StartIndex}|{Count}";
    }
}

// --------------------
// LinkedList
// --------------------

public sealed record AddFirst(int NodeId, string Value) : ICanvasOp
{
    public string Description { get; } = "Добавен първи елемент: " + Value;

    public string Serialize()
    {
        return $"AddFirst|{NodeId}|{Value}";
    }
}

public sealed record AddLast(int NodeId, string Value) : ICanvasOp
{
    public string Description { get; } = "Добавен последен елемент: " + Value;

    public string Serialize()
    {
        return $"AddLast|{NodeId}|{Value}";
    }
}

public sealed record AddAfter(int TargetNodeId, int NewNodeId, string Value) : ICanvasOp
{
    public string Description { get; } =
        $"Добавен елемент {Value} след възел #{TargetNodeId}";

    public string Serialize()
    {
        return $"AddAfter|{TargetNodeId}|{NewNodeId}|{Value}";
    }
}

public sealed record AddBefore(int TargetNodeId, int NewNodeId, string Value) : ICanvasOp
{
    public string Description { get; } =
        $"Добавен елемент {Value} преди възел #{TargetNodeId}";

    public string Serialize()
    {
        return $"AddBefore|{TargetNodeId}|{NewNodeId}|{Value}";
    }
}

public sealed record RemoveNode(int NodeId) : ICanvasOp
{
    public string Description { get; } =
        $"Премахнат възел #{NodeId}";

    public string Serialize()
    {
        return $"RemoveNode|{NodeId}";
    }
}

// --------------------
// Queue
// --------------------

public sealed record EnqueueOperation(string Value) : ICanvasOp
{
    public string Description { get; } = "Добавена стойност в опашката: " + Value;

    public string Serialize()
    {
        return $"Enqueue|{Value}";
    }
}

public sealed record DequeueOperation() : ICanvasOp
{
    public string Description { get; } = "Премахнат елемент от опашката";

    public string Serialize()
    {
        return "Dequeue";
    }
}

// --------------------
// Stack
// --------------------

public sealed record PushOperation(string Value) : ICanvasOp
{
    public string Description { get; } = "Добавена стойност в стека: " + Value;

    public string Serialize()
    {
        return $"Push|{Value}";
    }
}

public sealed record PopOperation() : ICanvasOp
{
    public string Description { get; } = "Премахнат елемент от стека";

    public string Serialize()
    {
        return "Pop";
    }
}

// --------------------
// Helpers
// --------------------

internal static class CanvasOpText
{
    public static string Preview(IReadOnlyList<string> values)
    {
        return string.Join(", ", values.Take(10)) + (values.Count > 10 ? ", ..." : "");
    }

    public static string SerializeLengthPrefixed(IReadOnlyList<string> values)
    {
        StringBuilder builder = new();

        foreach (string value in values)
        {
            builder.Append(value.Length);
            builder.Append(':');
            builder.Append(value);
        }

        return builder.ToString();
    }
}