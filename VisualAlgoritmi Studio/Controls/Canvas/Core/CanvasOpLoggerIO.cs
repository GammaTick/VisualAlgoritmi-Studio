using System;
using System.Collections.Generic;
using System.Text;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Core;

public static class CanvasOpLoggerIO
{
    public static string Serialize(CanvasOpLogger canvasOpLogger)
    {
        if (canvasOpLogger == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var step in canvasOpLogger.Steps)
        {
            foreach (var operation in step.Operations)
            {
                sb.AppendLine(operation.Serialize());
            }

            sb.AppendLine("StepEnd");
        }

        return sb.ToString();
    }

    public static CanvasOpLogger? Deserialize(string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        var logger = new CanvasOpLogger();

        string[] lines = body.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

        List<ICanvasOp> currentOps = [];

        foreach (string line in lines)
        {
            if (line == "StepEnd")
            {
                var step = new CanvasStep([.. currentOps]);
                logger.AddStep(step);
                currentOps.Clear();
                continue;
            }

            ICanvasOp? operation = ParseOperation(line);

            if (operation != null)
            {
                currentOps.Add(operation);
            }
        }

        return logger;
    }

    private static ICanvasOp? ParseOperation(string line)
    {
        string[] parts = line.Split('|', 4);

        if (parts.Length == 0)
        {
            return null;
        }

        string opType = parts[0];

        try
        {
            return opType switch
            {
                "Add" => new AddOperation(
                    parts[1]),

                "AddRange" => ParseAddRange(parts),

                "AddFirst" => new AddFirst(
                    int.Parse(parts[1]),
                    parts[2]),

                "AddLast" => new AddLast(
                    int.Parse(parts[1]),
                    parts[2]),

                "AddAfter" => new AddAfter(
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    parts[3]),

                "AddAfterWithTargetValue" => ParseAddAfterWithTargetValue(parts),

                "AddBefore" => new AddBefore(
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    parts[3]),

                "AddBeforeWithTargetValue" => ParseAddBeforeWithTargetValue(parts),

                "RemoveNode" => new RemoveNode(
                    int.Parse(parts[1]),
                    $"Премахнат възел {parts[1]}"),

                "Enqueue" => new EnqueueOperation(
                    parts[1]),

                "Dequeue" => new DequeueOperation(),

                "Push" => new PushOperation(
                    parts[1]),

                "Pop" => new PopOperation(),

                "Set" => new SetOperation(
                    int.Parse(parts[1]),
                    parts[2]),

                "CapacitySet" => new CapacitySetOperation(
                    int.Parse(parts[1])),

                "Clear" => new ClearOperation(),

                "Snapshot" => ParseSnapshot(parts),

                "Insert" => new InsertOperation(
                    int.Parse(parts[1]),
                    parts[2]),

                "InsertRange" => ParseInsertRange(parts),

                "Remove" => new RemoveOperation(
                    int.Parse(parts[1])),

                "RemoveRange" => new RemoveRangeOperation(
                    int.Parse(parts[1]),
                    int.Parse(parts[2])),

                "Reverse" => new ReverseOperation(
                    int.Parse(parts[1]),
                    int.Parse(parts[2])),

                "SetRange" => ParseSetRange(parts),

                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string[] ParseLengthPrefixed(string data, int count)
    {
        var elements = new string[count];
        int pos = 0;

        for (int i = 0; i < count; i++)
        {
            int colonIndex = data.IndexOf(':', pos);

            if (colonIndex < 0)
            {
                throw new FormatException("Missing ':'");
            }

            int length = int.Parse(data.AsSpan(pos, colonIndex - pos));
            pos = colonIndex + 1;

            if (pos + length > data.Length)
            {
                throw new FormatException("Invalid length");
            }

            elements[i] = data.Substring(pos, length);
            pos += length;
        }

        return elements;
    }

    private static AddRangeOperation ParseAddRange(string[] parts)
    {
        int count = int.Parse(parts[1]);
        string data = parts[2];

        return new AddRangeOperation(ParseLengthPrefixed(data, count));
    }

    private static AddAfter ParseAddAfterWithTargetValue(string[] parts)
    {
        string[] values = ParseLengthPrefixed(parts[3], 2);

        return new AddAfter(
            int.Parse(parts[1]),
            int.Parse(parts[2]),
            values[1],
            values[0]);
    }

    private static AddBefore ParseAddBeforeWithTargetValue(string[] parts)
    {
        string[] values = ParseLengthPrefixed(parts[3], 2);

        return new AddBefore(
            int.Parse(parts[1]),
            int.Parse(parts[2]),
            values[1],
            values[0]);
    }

    private static SnapshotOperation ParseSnapshot(string[] parts)
    {
        int count = int.Parse(parts[1]);
        string description = ParseLengthPrefixed(parts[2], 1)[0];
        string data = parts[3];

        return new SnapshotOperation(ParseLengthPrefixed(data, count), description);
    }

    private static InsertRangeOperation ParseInsertRange(string[] parts)
    {
        int index = int.Parse(parts[1]);
        int count = int.Parse(parts[2]);
        string data = parts[3];

        return new InsertRangeOperation(index, ParseLengthPrefixed(data, count));
    }

    private static SetRangeOperation ParseSetRange(string[] parts)
    {
        int index = int.Parse(parts[1]);
        int count = int.Parse(parts[2]);
        string data = parts[3];

        return new SetRangeOperation(index, ParseLengthPrefixed(data, count));
    }
}
