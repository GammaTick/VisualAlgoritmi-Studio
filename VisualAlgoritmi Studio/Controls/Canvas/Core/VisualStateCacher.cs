using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Core
{
    public sealed class VisualStateCacher<T> where T : VisualNode
    {
        private readonly List<(int stepIndex, List<T> visualState)> _cachedVisualStates = [];

        public void Clear()
        {
            _cachedVisualStates.Clear();
        }

        public bool TryGetVisualState(int stepIndex, [MaybeNullWhen(false)] out List<T> visualState)
        {
            if (stepIndex < 0 || stepIndex >= _cachedVisualStates.Count)
            {
                visualState = null;
                return false;
            }

            visualState = _cachedVisualStates[stepIndex].visualState;
            return true;
        }

        public (int stepIndex, List<T>? visualState) GetLastCachedVisualState()
        {
            if (_cachedVisualStates.Count == 0)
            {
                return (-1, null);
            }

            return _cachedVisualStates[^1];  
        }

        public void CacheVisualState(int stepIndex, List<T> visualState)
        {
            if (stepIndex < 0 || stepIndex > _cachedVisualStates.Count)
            {
                ThrowHelper.ThrowStepIndexOutOfRangeException();
            }

            _cachedVisualStates.Add((stepIndex, visualState));
        }

        private class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowStepIndexOutOfRangeException()
            {
                throw new ArgumentOutOfRangeException("stepIndex");
            }
        }
    }
}
