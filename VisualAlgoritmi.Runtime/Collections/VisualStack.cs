using System.Collections;
using System.Diagnostics.CodeAnalysis;
using VisualAlgoritmi.Runtime.Operations;

namespace VisualAlgoritmi.Runtime.Collections
{
    public class VisualStack<T> : IEnumerable<T>, ICollection, IReadOnlyCollection<T>
    {
        private readonly int _structureId = 0;

        private readonly Stack<T> _inner;

        public VisualStack()
        {
            _inner = new Stack<T>();

            _structureId = VisualStructureIdProvider.GetNextId();
        }

        public VisualStack(int capacity)
        {
            _inner = new Stack<T>(capacity);

            _structureId = VisualStructureIdProvider.GetNextId();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCapacitySet(_structureId, capacity);
            OperationRecorder.EndStep();
        }

        public VisualStack(IEnumerable<T> collection)
        {
            _inner = new Stack<T>(collection);

            _structureId = VisualStructureIdProvider.GetNextId();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCreationFromCollection(_structureId, _inner);
            OperationRecorder.EndStep();
        }

        public int Count
        {
            get => _inner.Count;
        }

        public int Capacity => _inner.Capacity;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public void Clear()
        {
            _inner.Clear();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureClear(_structureId);
            OperationRecorder.EndStep();
        }

        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_inner).CopyTo(array, index);
        }

        public int EnsureCapacity(int capacity)
        {
            return _inner.EnsureCapacity(capacity);
        }

        public Stack<T>.Enumerator GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        public T Peek()
        {
            return _inner.Peek();
        }

        public T Pop()
        {
            T item = _inner.Pop();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteStackPop(_structureId);
            OperationRecorder.EndStep();

            return item;
        }

        public void Push(T item)
        {
            _inner.Push(item);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteStackPush(_structureId, item);
            OperationRecorder.EndStep();
        }

        public T[] ToArray()
        {
            return _inner.ToArray();
        }

        public void TrimExcess()
        {
            _inner.TrimExcess();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCapacitySet(_structureId, _inner.Capacity);
            OperationRecorder.EndStep();
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            return _inner.TryPeek(out result);
        }

        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            bool success = _inner.TryPop(out result);

            if (success)
            {
                OperationRecorder.BeginStep();
                OperationRecorder.WriteStackPop(_structureId);
                OperationRecorder.EndStep();
            }

            return success;
        }
    }
}