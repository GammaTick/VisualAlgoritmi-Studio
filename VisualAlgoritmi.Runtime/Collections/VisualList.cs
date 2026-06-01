using System;
using System.Collections;
using VisualAlgoritmi.Runtime.Operations;

namespace VisualAlgoritmi.Runtime.Collections
{
    public class VisualList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        private readonly int _structureId = 0;

        private readonly List<T> _inner;

        private VisualList(List<T> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

            _structureId = VisualStructureIdProvider.GetNextId();
        }

        public VisualList()
        {
            _inner = new List<T>();

            _structureId = VisualStructureIdProvider.GetNextId();
        }

        public VisualList(int capacity)
        {
            _inner = new List<T>(capacity);

            _structureId = VisualStructureIdProvider.GetNextId();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCapacitySet(_structureId, capacity);
            OperationRecorder.EndStep();
        }

        public VisualList(IEnumerable<T> collection)
        {
            T[] items = collection.ToArray();

            _inner = new List<T>(items);

            _structureId = VisualStructureIdProvider.GetNextId();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCreationFromCollection(_structureId, items);
            OperationRecorder.EndStep();
        }

        public int Capacity
        {
            get => _inner.Capacity;
            set
            {
                _inner.Capacity = value;

                OperationRecorder.BeginStep();
                OperationRecorder.WriteDataStructureCapacitySet(_structureId, value);
                OperationRecorder.EndStep();
            }
        }

        public int Count => _inner.Count;

        bool IList.IsFixedSize => false;
        bool ICollection<T>.IsReadOnly => false;
        bool IList.IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        public T this[int index]
        {
            get => _inner[index];
            set
            {
                _inner[index] = value;

                OperationRecorder.BeginStep();
                OperationRecorder.WriteListSet(_structureId, index, value);
                OperationRecorder.EndStep();
            }
        }

        object? IList.this[int index]
        {
            get => _inner[index];
            set => this[index] = (T)value!;
        }

        public void Add(T item)
        {
            int index = _inner.Count;

            _inner.Add(item);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListAdd(_structureId, index, item);
            OperationRecorder.EndStep();
        }

        int IList.Add(object? value)
        {
            int index = _inner.Count;
            Add((T)value!);
            return index;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            T[] items = collection.ToArray();

            _inner.AddRange(items);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListAddRange(_structureId, items);
            OperationRecorder.EndStep();
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly()
        {
            return _inner.AsReadOnly();
        }

        public int BinarySearch(T item)
        {
            return _inner.BinarySearch(item);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return _inner.BinarySearch(item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return _inner.BinarySearch(index, count, item, comparer);
        }

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

        bool IList.Contains(object? item)
        {
            return item is T typedItem && Contains(typedItem);
        }

        public VisualList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            return new VisualList<TOutput>(_inner.ConvertAll(converter));
        }

        public void CopyTo(T[] array)
        {
            _inner.CopyTo(array);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            _inner.CopyTo(index, array, arrayIndex, count);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            ((ICollection)_inner).CopyTo(array, arrayIndex);
        }

        public int EnsureCapacity(int capacity)
        {
            int newCapacity = _inner.EnsureCapacity(capacity);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureCapacitySet(_structureId, newCapacity);
            OperationRecorder.EndStep();

            return newCapacity;
        }

        public bool Exists(Predicate<T> match)
        {
            return _inner.Exists(match);
        }

        public T? Find(Predicate<T> match)
        {
            return _inner.Find(match);
        }

        public VisualList<T> FindAll(Predicate<T> match)
        {
            return new VisualList<T>(_inner.FindAll(match));
        }

        public int FindIndex(Predicate<T> match)
        {
            return _inner.FindIndex(match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return _inner.FindIndex(startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return _inner.FindIndex(startIndex, count, match);
        }

        public T? FindLast(Predicate<T> match)
        {
            return _inner.FindLast(match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return _inner.FindLastIndex(match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return _inner.FindLastIndex(startIndex, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return _inner.FindLastIndex(startIndex, count, match);
        }

        public void ForEach(Action<T> action)
        {
            _inner.ForEach(action);

            string[] snapshot = new string[_inner.Count];

            for (int i = 0; i < _inner.Count; i++)
            {
                snapshot[i] = _inner[i]?.ToString() ?? "null";
            }

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.ForEach, snapshot);
            OperationRecorder.EndStep();
        }

        public List<T>.Enumerator GetEnumerator()
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

        public VisualList<T> GetRange(int index, int count)
        {
            return new VisualList<T>(_inner.GetRange(index, count));
        }

        public int IndexOf(T item)
        {
            return _inner.IndexOf(item);
        }

        int IList.IndexOf(object? item)
        {
            return item is T typedItem ? IndexOf(typedItem) : -1;
        }

        public int IndexOf(T item, int index)
        {
            return _inner.IndexOf(item, index);
        }

        public int IndexOf(T item, int index, int count)
        {
            return _inner.IndexOf(item, index, count);
        }

        public void Insert(int index, T item)
        {
            _inner.Insert(index, item);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListInsert(_structureId, index, item);
            OperationRecorder.EndStep();
        }

        void IList.Insert(int index, object? item)
        {
            Insert(index, (T)item!);
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            T[] items = collection.ToArray();

            _inner.InsertRange(index, items);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListInsertRange(_structureId, index, items);
            OperationRecorder.EndStep();
        }

        public int LastIndexOf(T item)
        {
            return _inner.LastIndexOf(item);
        }

        public int LastIndexOf(T item, int index)
        {
            return _inner.LastIndexOf(item, index);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            return _inner.LastIndexOf(item, index, count);
        }

        public bool Remove(T item)
        {
            int index = _inner.IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            _inner.RemoveAt(index);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListRemoveAt(_structureId, index);
            OperationRecorder.EndStep();

            return true;
        }

        void IList.Remove(object? item)
        {
            if (item is T typedItem)
            {
                Remove(typedItem);
            }
        }

        public int RemoveAll(Predicate<T> match)
        {
            int removedCount = _inner.RemoveAll(match);

            string[] snapshot = new string[_inner.Count];

            for (int i = 0; i < _inner.Count; i++)
            {
                snapshot[i] = _inner[i]?.ToString() ?? "null";
            }

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.RemoveAll, snapshot);
            OperationRecorder.EndStep();

            return removedCount;
        }

        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListRemoveAt(_structureId, index);
            OperationRecorder.EndStep();
        }

        public void RemoveRange(int index, int count)
        {
            _inner.RemoveRange(index, count);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListRemoveRange(_structureId, index, count);
            OperationRecorder.EndStep();
        }

        public void Reverse()
        {
            _inner.Reverse();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListReverse(_structureId, 0, Count);
            OperationRecorder.EndStep();
        }

        public void Reverse(int index, int count)
        {
            _inner.Reverse(index, count);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteListReverse(_structureId, index, count);
            OperationRecorder.EndStep();
        }

        public VisualList<T> Slice(int start, int length)
        {
            return new VisualList<T>(_inner.Slice(start, length));
        }

        public void Sort()
        {
            _inner.Sort();

            string[] snapshot = new string[_inner.Count];

            for (int i = 0; i < _inner.Count; i++)
            {
                snapshot[i] = _inner[i]?.ToString() ?? "null";
            }

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.Sort, snapshot);
            OperationRecorder.EndStep();
        }

        public void Sort(IComparer<T> comparer)
        {
            _inner.Sort(comparer);

            string[] snapshot = new string[_inner.Count];

            for (int i = 0; i < _inner.Count; i++)
            {
                snapshot[i] = _inner[i]?.ToString() ?? "null";
            }

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.SortWithComparison, snapshot);
            OperationRecorder.EndStep();
        }

        public void Sort(Comparison<T> comparison)
        {
            _inner.Sort(comparison);

            string[] snapshot = new string[_inner.Count];

            for (int i = 0; i < _inner.Count; i++)
            {
                snapshot[i] = _inner[i]?.ToString() ?? "null";
            }

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.Sort, snapshot);
            OperationRecorder.EndStep();
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            _inner.Sort(index, count, comparer);

            string[] snapshot = new string[_inner.Count];

            for (int i = 0; i < _inner.Count; i++)
            {
                snapshot[i] = _inner[i]?.ToString() ?? "null";
            }

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.Sort, snapshot);
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

        public bool TrueForAll(Predicate<T> match)
        {
            return _inner.TrueForAll(match);
        }
    }
}