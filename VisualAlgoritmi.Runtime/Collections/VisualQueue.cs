using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using VisualAlgoritmi.Runtime.Operations;

namespace VisualAlgoritmi.Runtime.Collections
{
    public class VisualQueue<T> : IEnumerable<T>, ICollection, IReadOnlyCollection<T>
    {
        private readonly int _structureId = 0;

        private readonly Queue<T> _inner;

        public VisualQueue()
        {
            _inner = new Queue<T>();

            _structureId = VisualStructureIdProvider.GetNextId();
        }

        public VisualQueue(int capacity)
        {
            _inner = new Queue<T>(capacity);

            _structureId = VisualStructureIdProvider.GetNextId();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCapacitySet(_structureId, capacity);
            OperationRecorder.EndStep();
        }

        public VisualQueue(IEnumerable<T> collection)
        {
            _inner = new Queue<T>(collection);

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

        public T Dequeue()
        {
            T item = _inner.Dequeue();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteQueueDequeue(_structureId);
            OperationRecorder.EndStep();

            return item;
        }

        public void Enqueue(T item)
        {
            _inner.Enqueue(item);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteQueueEnqueue(_structureId, item);
            OperationRecorder.EndStep();
        }

        public int EnsureCapacity(int capacity)
        {
            return _inner.EnsureCapacity(capacity);
        }

        public Queue<T>.Enumerator GetEnumerator()
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

        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            bool success = _inner.TryDequeue(out result);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteQueueDequeue(_structureId);
            OperationRecorder.EndStep();

            return success;
        }

        public bool TryPeek([MaybeNullWhen(false)] out T result)
        {
            return _inner.TryPeek(out result);
        }
    }
}