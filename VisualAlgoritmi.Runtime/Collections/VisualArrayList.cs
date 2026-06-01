using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using VisualAlgoritmi.Runtime.Operations;

namespace VisualAlgoritmi.Runtime.Collections;

[DebuggerDisplay("Count = {Count}")]
[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class VisualArrayList : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ICloneable
{
    private readonly int _structureId = 0;
    
    private readonly System.Collections.ArrayList _inner;

    private VisualArrayList(System.Collections.ArrayList inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        _structureId = VisualStructureIdProvider.GetNextId();
    }

    public VisualArrayList()
    {
        _inner = new System.Collections.ArrayList();

        _structureId = VisualStructureIdProvider.GetNextId();
    }

    public VisualArrayList(int capacity)
    {
        _inner = new System.Collections.ArrayList(capacity);

        _structureId = VisualStructureIdProvider.GetNextId();

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureCapacitySet(_structureId, capacity);
        OperationRecorder.EndStep();
    }

    public VisualArrayList(System.Collections.ICollection c)
    {
        _inner = new System.Collections.ArrayList(c);

        _structureId = VisualStructureIdProvider.GetNextId();

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureCreationFromCollection(_structureId, c);
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

    public int Count
    {
        get => _inner.Count;
    }

    public bool IsFixedSize
    {
        get => _inner.IsFixedSize;
    }

    public bool IsReadOnly
    {
        get => _inner.IsReadOnly;
    }

    public bool IsSynchronized
    {
        get => _inner.IsSynchronized;
    }

    public object? this[int index]
    {
        get => _inner[index];
        set
        {
            _inner[index] = value;

            OperationRecorder.BeginStep();
            OperationRecorder.WriteArrayListSet(_structureId, index, value);
            OperationRecorder.EndStep();
        }
    }

    public object SyncRoot
    {
        get => _inner.SyncRoot;
    }

    public static VisualArrayList Adapter(System.Collections.IList list)
    {
        return new VisualArrayList(System.Collections.ArrayList.Adapter(list));
    }

    public int Add(object? value)
    {
        int index = _inner.Add(value);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListAdd(_structureId, index, value);
        OperationRecorder.EndStep();

        return index;
    }

    public void AddRange(System.Collections.ICollection c)
    {
        _inner.AddRange(c);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListAddRange(_structureId, c);
        OperationRecorder.EndStep();
    }

    public int BinarySearch(object value)
    {
        return _inner.BinarySearch(value);
    }

    public int BinarySearch(object value, System.Collections.IComparer comparer)
    {
        return _inner.BinarySearch(value, comparer);
    }

    public int BinarySearch(int index, int count, object value, System.Collections.IComparer comparer)
    {
        return _inner.BinarySearch(index, count, value, comparer);
    }

    public void Clear()
    {
        _inner.Clear();

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureClear(_structureId);
        OperationRecorder.EndStep();
    }

    public object Clone()
    {
        return _inner.Clone();
    }

    public bool Contains(object? item)
    {
        return _inner.Contains(item);
    }

    public void CopyTo(Array array)
    {
        _inner.CopyTo(array);
    }

    public void CopyTo(Array array, int arrayIndex)
    {
        _inner.CopyTo(array, arrayIndex);
    }

    public void CopyTo(int index, Array array, int arrayIndex, int count)
    {
        _inner.CopyTo(index, array, arrayIndex, count);
    }

    public static System.Collections.IList FixedSize(System.Collections.IList list)
    {
        return System.Collections.ArrayList.FixedSize(list);
    }

    public static VisualArrayList FixedSize(VisualArrayList list)
    {
        return new VisualArrayList(System.Collections.ArrayList.FixedSize(list._inner));
    }

    public System.Collections.IEnumerator GetEnumerator()
    {
        return _inner.GetEnumerator();
    }

    public System.Collections.IEnumerator GetEnumerator(int index, int count)
    {
        return _inner.GetEnumerator(index, count);
    }

    public VisualArrayList GetRange(int index, int count)
    {
        return new VisualArrayList(_inner.GetRange(index, count));
    }

    public int IndexOf(object? value)
    {
        return _inner.IndexOf(value);
    }

    public int IndexOf(object value, int startIndex)
    {
        return _inner.IndexOf(value, startIndex);
    }

    public int IndexOf(object value, int startIndex, int count)
    {
        return _inner.IndexOf(value, startIndex, count);
    }

    public void Insert(int index, object? value)
    {
        _inner.Insert(index, value);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListInsert(_structureId, index, value);
        OperationRecorder.EndStep();
    }

    public void InsertRange(int index, System.Collections.ICollection c)
    {
        _inner.InsertRange(index, c);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListInsertRange(_structureId, index, c);
        OperationRecorder.EndStep();
    }

    public int LastIndexOf(object value)
    {
        return _inner.LastIndexOf(value);
    }

    public int LastIndexOf(object value, int startIndex)
    {
        return _inner.LastIndexOf(value, startIndex);
    }

    public int LastIndexOf(object value, int startIndex, int count)
    {
        return _inner.LastIndexOf(value, startIndex, count);
    }

    public static System.Collections.IList ReadOnly(System.Collections.IList list)
    {
        return System.Collections.ArrayList.ReadOnly(list);
    }

    public static VisualArrayList ReadOnly(VisualArrayList list)
    {
        return new VisualArrayList(System.Collections.ArrayList.ReadOnly(list._inner));
    }

    public void Remove(object? obj)
    {
        int index = _inner.IndexOf(obj);

        if (index < 0)
        {
            return;
        }

        _inner.RemoveAt(index);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListRemoveAt(_structureId, index);
        OperationRecorder.EndStep();
    }

    public void RemoveAt(int index)
    {
        _inner.RemoveAt(index);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListRemoveAt(_structureId, index);
        OperationRecorder.EndStep();
    }

    public void RemoveRange(int index, int count)
    {
        _inner.RemoveRange(index, count);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListRemoveRange(_structureId, index, count);
        OperationRecorder.EndStep();
    }

    public static VisualArrayList Repeat(object value, int count)
    {
        return new VisualArrayList(System.Collections.ArrayList.Repeat(value, count));
    }

    public void Reverse()
    {
        _inner.Reverse();

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListReverse(_structureId, 0, Count);
        OperationRecorder.EndStep();
    }

    public void Reverse(int index, int count)
    {
        _inner.Reverse(index, count);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListReverse(_structureId, index, count);
        OperationRecorder.EndStep();
    }

    public void SetRange(int index, System.Collections.ICollection c)
    {
        _inner.SetRange(index, c);

        OperationRecorder.BeginStep();
        OperationRecorder.WriteArrayListSetRange(_structureId, index, c);
        OperationRecorder.EndStep();
    }

    public void Sort()
    {
        _inner.Sort();

        int itemsCount = Count;
        string[] snapshot = new string[itemsCount];

        for (int i = 0; i < itemsCount; i++)
        {
            snapshot[i] = this[i]?.ToString() ?? "null";
        }

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.Sort, snapshot);
        OperationRecorder.EndStep();
    }

    public void Sort(System.Collections.IComparer comparer)
    {
        _inner.Sort(comparer);

        int itemsCount = Count;
        string[] snapshot = new string[itemsCount];

        for (int i = 0; i < itemsCount; i++)
        {
            snapshot[i] = this[i]?.ToString() ?? "null";
        }

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.Sort, snapshot);
        OperationRecorder.EndStep();
    }

    public void Sort(int index, int count, System.Collections.IComparer comparer)
    {
        _inner.Sort(index, count, comparer);

        int itemsCount = Count;
        string[] snapshot = new string[itemsCount];

        for (int i = 0; i < itemsCount; i++)
        {
            snapshot[i] = this[i]?.ToString() ?? "null";
        }

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureSnapshot(_structureId, SnapshotReason.Sort, snapshot);
        OperationRecorder.EndStep();
    }

    public static System.Collections.IList Synchronized(System.Collections.IList list)
    {
        return System.Collections.ArrayList.Synchronized(list);
    }

    public static VisualArrayList Synchronized(VisualArrayList list)
    {
        return new VisualArrayList(System.Collections.ArrayList.Synchronized(list._inner));
    }

    public object?[] ToArray()
    {
        return _inner.ToArray();
    }

    public Array ToArray(Type type)
    {
        return _inner.ToArray(type);
    }

    public void TrimToSize()
    {
        _inner.TrimToSize();

        OperationRecorder.BeginStep();
        OperationRecorder.WriteDataStructureCapacitySet(_structureId, _inner.Capacity);
        OperationRecorder.EndStep();
    }
}