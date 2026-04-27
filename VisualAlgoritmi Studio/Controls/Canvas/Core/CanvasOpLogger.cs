using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Core
{
    public sealed class CanvasOpLogger
    {
        private readonly List<CanvasStep> _steps = [];
        private BatchState? _currentBatch;
        
        private bool _isMultiOpStepActive;
        private readonly List<ICanvasOp> _currentMultiOpStepOps = [];

        public IReadOnlyList<CanvasStep> Steps => _steps;
        public int StepsCount => _steps.Count;

        public void AddStep(CanvasStep step)
        {
            if (step == null)
            {
                ThrowHelper.ThrowStepArgumentNullException();
            }

            _steps.Add(step);
        }

        public void BeginMultiOpStep()
        {
            if (_isMultiOpStepActive)
            {
                ThrowHelper.ThrowStepAlreadyStarted();
            }
     
            _currentMultiOpStepOps.Clear();
            _isMultiOpStepActive = true;
        }

        public void EndMultiOpStep()
        {
            if (!_isMultiOpStepActive)
            {
                ThrowHelper.ThrowEndStepWithoutBegin();
            }

            if (_currentBatch != null)
            {
                ThrowHelper.ThrowEndStepDuringBatch();
            }

            var canvasStep = new CanvasStep([.. _currentMultiOpStepOps]);
            _steps.Add(canvasStep);

            _currentMultiOpStepOps.Clear();
            _isMultiOpStepActive = false;
        }

        public void BeginBatch(CanvasBatchKind kind, int startIndex)
        {
            if (_isMultiOpStepActive)
            {
                ThrowHelper.ThrowStepAlreadyStarted();
            }

            if (_currentBatch != null)
            {
                ThrowHelper.ThrowBatchAlreadyStarted();
            }

            _currentBatch = new BatchState(kind, startIndex);
        }

        public void EndBatch()
        {
            if (_currentBatch == null)
            {
                ThrowHelper.ThrowEndBatchWithoutBegin();
            }

            List<ICanvasOp> operations = [];

            if (_currentBatch.FinalCapacity.HasValue)
            {
                operations.Add(new CapacitySetOperation(_currentBatch.FinalCapacity.Value));
            }

            switch (_currentBatch.Kind)
            {
                case CanvasBatchKind.AddRange:
                    operations.Add(
                        new AddRangeOperation(_currentBatch.Elements));
                    break;

                case CanvasBatchKind.InsertRange:
                    operations.Add(
                        new InsertRangeOperation(_currentBatch.StartIndex, _currentBatch.Elements));
                    break;
            }

            var currentStep = new CanvasStep(operations);
            _steps.Add(currentStep);
            _currentBatch = null;
        }

        public void Log(ICanvasOp operation)
        {
            if (_currentBatch != null)
            {
                LogIntoBatch(operation);
                return;
            }

            if (_isMultiOpStepActive)
            {
                _currentMultiOpStepOps.Add(operation);
                return;
            }

            var canvasStep = new CanvasStep([operation]);
            _steps.Add(canvasStep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogIntoBatch(ICanvasOp op)
        {
            if (_currentBatch == null)
            {
                ThrowHelper.ThrowInternalInvariantFailed();
            }

            switch (op)
            {
                case CapacitySetOperation capacitySetOperation:
                    {
                        _currentBatch.FinalCapacity = capacitySetOperation.NewCapacity;
                        return;
                    }

                case AddOperation addOperation:
                    {
                        if (_currentBatch.Kind != CanvasBatchKind.AddRange)
                        {
                            ThrowHelper.ThrowOpNotAllowedInThisBatch();
                        }

                        _currentBatch.Elements.Add(addOperation.Value);
                        return;
                    }

                case InsertOperation insertOperation:
                    {
                        if (_currentBatch.Kind != CanvasBatchKind.InsertRange)
                        {
                            ThrowHelper.ThrowOpNotAllowedInThisBatch();
                        }

                        _currentBatch.Elements.Add(insertOperation.Value);
                        return;
                    }

                default:
                    {
                        // If anything else happens inside a batch, that means your caller
                        // mixed semantics. Better to fail loudly than silently corrupt steps.
                        ThrowHelper.ThrowOpNotAllowedInThisBatch();
                        return;
                    }
            }
        }

        private sealed class BatchState
        {
            public readonly CanvasBatchKind Kind;
            public readonly int StartIndex;
            public readonly List<string> Elements;
            public int? FinalCapacity;

            public BatchState(CanvasBatchKind kind, int startIndex)
            {
                Kind = kind;
                StartIndex = startIndex;
                Elements = [];
                FinalCapacity = null;
            }
        }

        private static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowDataStructureIdArgumentOutOfRange()
            {
                throw new InvalidOperationException("dataStructureId");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowStepIndexArgumentOutOfRange()
            {
                throw new InvalidOperationException("stepIndex");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowStepHaventBeenStarted()
            {
                throw new InvalidOperationException("CanvasOpLogger: BeginStep/BeginRange must be called before Log().");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowStepAlreadyStarted()
            {
                throw new InvalidOperationException("CanvasOpLogger: A step is already active. End it first.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowEndStepWithoutBegin()
            {
                throw new InvalidOperationException("CanvasOpLogger: EndStep() called without BeginStep().");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowEndStepDuringBatch()
            {
                throw new InvalidOperationException("CanvasOpLogger: EndStep() called while a batch is active. Use EndBatch().");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowEndBatchWithoutBegin()
            {
                throw new InvalidOperationException("CanvasOpLogger: EndBatch() called without BeginBatch().");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowBatchAlreadyStarted()
            {
                throw new InvalidOperationException("CanvasOpLogger: A batch is already active. End it first.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowOpNotAllowedInThisBatch()
            {
                throw new InvalidOperationException("CanvasOpLogger: Operation not allowed inside the current batch.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowInternalInvariantFailed()
            {
                throw new InvalidOperationException("CanvasOpLogger: Internal invariant failed.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowStepArgumentNullException()
            {
                throw new ArgumentNullException("step");
            }
        }
    }

    public enum CanvasBatchKind
    {
        AddRange,
        InsertRange
    }

    public sealed class CanvasStep
    {
        private readonly List<ICanvasOp> _operations;

        public IReadOnlyList<ICanvasOp> Operations => _operations;

        public CanvasStep(List<ICanvasOp> operations)
        {
            if (operations == null)
            {
                ThrowHelper.ThrowOperationsArgumentNullException();
            }

            _operations = operations;
        }

        private class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowOperationsArgumentNullException()
            {
                throw new ArgumentNullException("operations");
            }
        }
    }
}
