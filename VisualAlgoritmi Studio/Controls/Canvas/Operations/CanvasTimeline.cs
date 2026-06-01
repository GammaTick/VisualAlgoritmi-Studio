using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualAlgoritmi_Studio.Canvas.Operations;

public sealed class CanvasTimeline
{
    private readonly IReadOnlyList<IReadOnlyList<CanvasStep>> _stepsByStructureId;

    public CanvasTimeline(List<List<CanvasStep>> stepsByStructureId)
    {
        ArgumentNullException.ThrowIfNull(stepsByStructureId);

        _stepsByStructureId = stepsByStructureId
            .Select(steps => (IReadOnlyList<CanvasStep>)steps.AsReadOnly())
            .ToList()
            .AsReadOnly();
    }

    public int StructureCount => _stepsByStructureId.Count;

    public int GetStepCount(int structureId)
    {
        return ContainsStructure(structureId)
            ? _stepsByStructureId[structureId].Count
            : 0;
    }

    public bool ContainsStructure(int structureId)
    {
        return structureId >= 0 && structureId < _stepsByStructureId.Count;
    }

    public IReadOnlyList<CanvasStep> GetStepsForStructure(int structureId)
    {
        return ContainsStructure(structureId)
            ? _stepsByStructureId[structureId]
            : [];
    }

    public CanvasStep? GetStep(int structureId, int stepIndex)
    {
        if (!ContainsStructure(structureId))
        {
            return null;
        }

        IReadOnlyList<CanvasStep> steps = _stepsByStructureId[structureId];

        if (stepIndex < 0 || stepIndex >= steps.Count)
        {
            return null;
        }

        return steps[stepIndex];
    }
}