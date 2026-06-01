using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VisualAlgoritmi_Studio.Canvas.Operations;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Operations
{
    public static class CanvasTimelineSerializer
    {
        private const string StepEndMarker = "StepEnd";

        public static string Serialize(CanvasTimeline? canvasTimeline)
        {
            if (canvasTimeline == null)
            {
                return string.Empty;
            }

            if (canvasTimeline.StructureCount == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new();

            foreach (CanvasStep step in canvasTimeline.GetStepsForStructure(0))
            {
                foreach (ICanvasOp operation in step.Operations)
                {
                    builder.AppendLine(operation.Serialize());
                }

                builder.AppendLine(StepEndMarker);
            }

            return builder.ToString();
        }

        public static CanvasTimeline? Deserialize(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            string[] lines = content.Split(
                ["\r\n", "\r", "\n"],
                StringSplitOptions.RemoveEmptyEntries);

            List<CanvasStep> steps = [];
            List<ICanvasOp> currentOps = [];

            foreach (string line in lines)
            {
                if (line == StepEndMarker)
                {
                    steps.Add(new CanvasStep([.. currentOps]));
                    currentOps.Clear();
                    continue;
                }

                ICanvasOp? operation = ParseOperation(line);

                if (operation != null)
                {
                    currentOps.Add(operation);
                }
            }

            if (currentOps.Count > 0)
            {
                steps.Add(new CanvasStep([.. currentOps]));
            }

            return new CanvasTimeline([steps]);
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
                    // --------------------
                    // Global data structure operations
                    // --------------------

                    "Clear" => new ClearOperation(),

                    "CapacitySet" => new CapacitySetOperation(
                        int.Parse(parts[1], CultureInfo.InvariantCulture)),

                    "CreationFromCollection" => ParseCreationFromCollection(line),

                    "Snapshot" => ParseSnapshot(line),

                    // --------------------
                    // Indexed collection operations
                    // ArrayList / List
                    // --------------------

                    "Add" => ParseAdd(line),

                    "AddRange" => ParseAddRange(line),

                    "Insert" => ParseInsert(line),

                    "InsertRange" => ParseInsertRange(line),

                    "Remove" => new RemoveAtOperation(
                        int.Parse(parts[1], CultureInfo.InvariantCulture)),

                    "RemoveRange" => new RemoveRangeOperation(
                        int.Parse(parts[1], CultureInfo.InvariantCulture),
                        int.Parse(parts[2], CultureInfo.InvariantCulture)),

                    "Set" => ParseSet(line),

                    "SetRange" => ParseSetRange(line),

                    "Reverse" => new ReverseOperation(
                        int.Parse(parts[1], CultureInfo.InvariantCulture),
                        int.Parse(parts[2], CultureInfo.InvariantCulture)),

                    // --------------------
                    // LinkedList
                    // --------------------

                    "AddFirst" => ParseAddFirst(line),

                    "AddLast" => ParseAddLast(line),

                    "AddAfter" => ParseAddAfter(line),

                    "AddBefore" => ParseAddBefore(line),

                    "RemoveNode" => new RemoveNode(
                        int.Parse(parts[1], CultureInfo.InvariantCulture)),

                    // --------------------
                    // Queue
                    // --------------------

                    "Enqueue" => ParseEnqueue(line),

                    "Dequeue" => new DequeueOperation(),

                    // --------------------
                    // Stack
                    // --------------------

                    "Push" => ParsePush(line),

                    "Pop" => new PopOperation(),

                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private static CreationFromCollectionOperation ParseCreationFromCollection(string line)
        {
            string[] parts = line.Split('|', 3);

            int count = int.Parse(parts[1], CultureInfo.InvariantCulture);
            string data = parts[2];

            return new CreationFromCollectionOperation(ParseLengthPrefixed(data, count));
        }

        private static AddOperation ParseAdd(string line)
        {
            string[] parts = line.Split('|', 3);

            return new AddOperation(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                parts[2]);
        }

        private static AddRangeOperation ParseAddRange(string line)
        {
            string[] parts = line.Split('|', 3);

            int count = int.Parse(parts[1], CultureInfo.InvariantCulture);
            string data = parts[2];

            return new AddRangeOperation(ParseLengthPrefixed(data, count));
        }

        private static InsertOperation ParseInsert(string line)
        {
            string[] parts = line.Split('|', 3);

            return new InsertOperation(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                parts[2]);
        }

        private static InsertRangeOperation ParseInsertRange(string line)
        {
            string[] parts = line.Split('|', 4);

            int startIndex = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int count = int.Parse(parts[2], CultureInfo.InvariantCulture);
            string data = parts[3];

            return new InsertRangeOperation(startIndex, ParseLengthPrefixed(data, count));
        }

        private static SetOperation ParseSet(string line)
        {
            string[] parts = line.Split('|', 3);

            return new SetOperation(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                parts[2]);
        }

        private static SetRangeOperation ParseSetRange(string line)
        {
            string[] parts = line.Split('|', 4);

            int startIndex = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int count = int.Parse(parts[2], CultureInfo.InvariantCulture);
            string data = parts[3];

            return new SetRangeOperation(startIndex, ParseLengthPrefixed(data, count));
        }

        private static SnapshotOperation ParseSnapshot(string line)
        {
            string[] parts = line.Split('|', 3);

            int count = int.Parse(parts[1], CultureInfo.InvariantCulture);
            string payload = parts[2];

            int position = 0;

            IReadOnlyList<string> values = ParseLengthPrefixed(payload, count, ref position);
            string description = ParseLengthPrefixed(payload, 1, ref position)[0];

            return new SnapshotOperation(values, description);
        }

        private static AddFirst ParseAddFirst(string line)
        {
            string[] parts = line.Split('|', 3);

            return new AddFirst(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                parts[2]);
        }

        private static AddLast ParseAddLast(string line)
        {
            string[] parts = line.Split('|', 3);

            return new AddLast(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                parts[2]);
        }

        private static AddAfter ParseAddAfter(string line)
        {
            string[] parts = line.Split('|', 4);

            return new AddAfter(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture),
                parts[3]);
        }

        private static AddBefore ParseAddBefore(string line)
        {
            string[] parts = line.Split('|', 4);

            return new AddBefore(
                int.Parse(parts[1], CultureInfo.InvariantCulture),
                int.Parse(parts[2], CultureInfo.InvariantCulture),
                parts[3]);
        }

        private static EnqueueOperation ParseEnqueue(string line)
        {
            string[] parts = line.Split('|', 2);

            return new EnqueueOperation(parts[1]);
        }

        private static PushOperation ParsePush(string line)
        {
            string[] parts = line.Split('|', 2);

            return new PushOperation(parts[1]);
        }

        private static IReadOnlyList<string> ParseLengthPrefixed(string data, int count)
        {
            int position = 0;
            return ParseLengthPrefixed(data, count, ref position);
        }

        private static IReadOnlyList<string> ParseLengthPrefixed(
            string data,
            int count,
            ref int position)
        {
            string[] values = new string[count];

            for (int i = 0; i < count; i++)
            {
                int colonIndex = data.IndexOf(':', position);

                if (colonIndex < 0)
                {
                    throw new FormatException("Missing ':' in length-prefixed value.");
                }

                int length = int.Parse(
                    data.AsSpan(position, colonIndex - position),
                    CultureInfo.InvariantCulture);

                position = colonIndex + 1;

                if (position + length > data.Length)
                {
                    throw new FormatException("Invalid length-prefixed value length.");
                }

                values[i] = data.Substring(position, length);
                position += length;
            }

            return values;
        }
    }
}