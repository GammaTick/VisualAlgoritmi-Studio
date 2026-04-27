// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;
using VisualAlgoritmi_Studio.Controls.Canvas.Registry;
using VisualAlgoritmi_Studio.DotNetInternals;
 
namespace VisualAlgoritmi_Studio.Controls.Canvas.Structures.LinkedList
{
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class VisualLinkedList<T> : ICollection<T>, ICollection, IReadOnlyCollection<T>, ISerializable, IDeserializationCallback
    {
        public static readonly Type typeOfThis = typeof(VisualLinkedList<>);
        private readonly CanvasOpLogger _canvasOpLogger;
        private int _nextNodeId;

        // This LinkedList is a doubly-Linked circular list.
        internal VisualLinkedListNode<T>? head;
        internal int count;
        internal int version;
        private SerializationInfo? _siInfo; //A temporary variable which we need during deserialization.
 
        // names for serialization
        private const string VersionName = "Version"; // Do not rename (binary serialization)
        private const string CountName = "Count"; // Do not rename (binary serialization)
        private const string ValuesName = "Data"; // Do not rename (binary serialization)
 
        public VisualLinkedList()
        {
            _canvasOpLogger = VisualDataStructuresRegister.RegisterLogger(typeOfThis);
        }
 
        public VisualLinkedList(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _canvasOpLogger = VisualDataStructuresRegister.RegisterLogger(typeOfThis);
 
            foreach (T item in collection)
            {
                AddLast(item);
            }
        }
 
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected VisualLinkedList(SerializationInfo info, StreamingContext context)
        {
            _siInfo = info;
            _canvasOpLogger = VisualDataStructuresRegister.RegisterLogger(typeOfThis);
        }
 
        public int Count
        {
            get { return count; }
        }
 
        public VisualLinkedListNode<T>? First
        {
            get { return head; }
        }
 
        public VisualLinkedListNode<T>? Last
        {
            get { return head?.prev; }
        }
 
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }
 
        void ICollection<T>.Add(T value)
        {
            AddLast(value);
        }
 
        public VisualLinkedListNode<T> AddAfter(VisualLinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            string targetValue = CanvasOperationsHelper.GetTextForValue(node.Value);
            VisualLinkedListNode<T> result = new VisualLinkedListNode<T>(node.list!, value);
            result.NodeId = _nextNodeId++;
            InternalInsertNodeBefore(node.next!, result);

            _canvasOpLogger.Log(new AddAfter(
                node.NodeId,
                result.NodeId,
                CanvasOperationsHelper.GetTextForValue(result.Value),
                targetValue));

            return result;
        }
 
        public void AddAfter(VisualLinkedListNode<T> node, VisualLinkedListNode<T> newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            string targetValue = CanvasOperationsHelper.GetTextForValue(node.Value);
            newNode.NodeId = _nextNodeId++;
            InternalInsertNodeBefore(node.next!, newNode);
            newNode.list = this;

            _canvasOpLogger.Log(new AddAfter(
                node.NodeId,
                newNode.NodeId,
                CanvasOperationsHelper.GetTextForValue(newNode.Value),
                targetValue));
        }
 
        public VisualLinkedListNode<T> AddBefore(VisualLinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            string targetValue = CanvasOperationsHelper.GetTextForValue(node.Value);
            VisualLinkedListNode<T> result = new VisualLinkedListNode<T>(node.list!, value);
            result.NodeId = _nextNodeId++;
            InternalInsertNodeBefore(node, result);
            if (node == head)
            {
                head = result;
            }

            _canvasOpLogger.Log(new AddBefore(
                node.NodeId,
                result.NodeId,
                CanvasOperationsHelper.GetTextForValue(result.Value),
                targetValue));

            return result;
        }
 
        public void AddBefore(VisualLinkedListNode<T> node, VisualLinkedListNode<T> newNode)
        {
            ValidateNode(node);
            ValidateNewNode(newNode);
            string targetValue = CanvasOperationsHelper.GetTextForValue(node.Value);
            newNode.NodeId = _nextNodeId++;
            InternalInsertNodeBefore(node, newNode);
            newNode.list = this;
            if (node == head)
            {
                head = newNode;
            }

            _canvasOpLogger.Log(new AddBefore(
                node.NodeId,
                newNode.NodeId,
                CanvasOperationsHelper.GetTextForValue(newNode.Value),
                targetValue));
        }
 
        public VisualLinkedListNode<T> AddFirst(T value)
        {
            VisualLinkedListNode<T> result = new VisualLinkedListNode<T>(this, value);
            result.NodeId = _nextNodeId++;

            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
                head = result;
            }

            _canvasOpLogger.Log(new AddFirst(result.NodeId, result.Value?.ToString() ?? "null"));

            return result;
        }
 
        public void AddFirst(VisualLinkedListNode<T> node)
        {
            ValidateNewNode(node);
            node.NodeId = _nextNodeId++;
 
            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
                head = node;
            }

            node.list = this;

            _canvasOpLogger.Log(new AddFirst(node.NodeId, node.Value?.ToString() ?? "null"));
        }
 
        public VisualLinkedListNode<T> AddLast(T value)
        {
            VisualLinkedListNode<T> result = new VisualLinkedListNode<T>(this, value);
            result.NodeId = _nextNodeId++;

            if (head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(head, result);
            }

            _canvasOpLogger.Log(new AddLast(result.NodeId, result.Value?.ToString() ?? "null"));

            return result;
        }
 
        public void AddLast(VisualLinkedListNode<T> node)
        {
            ValidateNewNode(node);
            node.NodeId = _nextNodeId++;
 
            if (head == null)
            {
                InternalInsertNodeToEmptyList(node);
            }
            else
            {
                InternalInsertNodeBefore(head, node);
            }
            node.list = this;

            _canvasOpLogger.Log(new AddLast(node.NodeId, node.Value?.ToString() ?? "null"));
        }
 
        public void Clear()
        {
            VisualLinkedListNode<T>? current = head;
            while (current != null)
            {
                VisualLinkedListNode<T> temp = current;
                current = current.Next;
                temp.Invalidate();
            }
 
            head = null;
            count = 0;
            version++;

            _canvasOpLogger.Log(new ClearOperation());
        }
 
        public bool Contains(T value)
        {
            return Find(value) != null;
        }
 
        public void CopyTo(T[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
 
            ArgumentOutOfRangeException.ThrowIfNegative(index);
 
            if (index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_BiggerThanCollection);
            }
 
            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_InsufficientSpace);
            }
 
            VisualLinkedListNode<T>? node = head;
            if (node != null)
            {
                do
                {
                    array[index++] = node!.item;
                    node = node.next;
                } while (node != head);
            }
        }
 
        public VisualLinkedListNode<T>? Find(T value)
        {
            VisualLinkedListNode<T>? node = head;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node!.item, value))
                        {
                            return node;
                        }
                        node = node.next;
                    } while (node != head);
                }
                else
                {
                    do
                    {
                        if (node!.item == null)
                        {
                            return node;
                        }
                        node = node.next;
                    } while (node != head);
                }
            }
            return null;
        }
 
        public VisualLinkedListNode<T>? FindLast(T value)
        {
            if (head == null) return null;
 
            VisualLinkedListNode<T>? last = head.prev;
            VisualLinkedListNode<T>? node = last;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node!.item, value))
                        {
                            return node;
                        }
 
                        node = node.prev;
                    } while (node != last);
                }
                else
                {
                    do
                    {
                        if (node!.item == null)
                        {
                            return node;
                        }
                        node = node.prev;
                    } while (node != last);
                }
            }
            return null;
        }
 
        public Enumerator GetEnumerator() => new Enumerator(this);
 
        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            Count == 0 ? EnumerableHelpers.GetEmptyEnumerator<T>() :
            GetEnumerator();
 
        public bool Remove(T value)
        {
            VisualLinkedListNode<T>? node = Find(value);
            
            if (node != null)
            {
                int nodeId = node.NodeId;
                InternalRemoveNode(node);

                _canvasOpLogger.Log(new RemoveNode(nodeId, $"Премахната стойност: {value}"));

                return true;
            }

            return false;
        }
 
        public void Remove(VisualLinkedListNode<T> node)
        {
            ValidateNode(node);
            int nodeId = node.NodeId;
            InternalRemoveNode(node);

            _canvasOpLogger.Log(new RemoveNode(nodeId, $"Премахнат възел {nodeId}"));
        }
 
        public void RemoveFirst()
        {
            if (head == null) { throw new InvalidOperationException(SR.LinkedListEmpty); }
            int nodeId = head.NodeId;
            InternalRemoveNode(head);

            _canvasOpLogger.Log(new RemoveNode(nodeId, "Премахнат първи елемент"));
        }
 
        public void RemoveLast()
        {
            if (head == null) { throw new InvalidOperationException(SR.LinkedListEmpty); }
            int nodeId = head.prev!.NodeId;
            InternalRemoveNode(head.prev!);

            _canvasOpLogger.Log(new RemoveNode(nodeId, "Премахнат последен елемент"));
        }
 
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);
 
            // Customized serialization for LinkedList.
            // We need to do this because it will be too expensive to Serialize each node.
            // This will give us the flexiblility to change internal implementation freely in future.
 
            info.AddValue(VersionName, version);
            info.AddValue(CountName, count); // this is the length of the bucket array.
 
            if (count != 0)
            {
                T[] array = new T[count];
                CopyTo(array, 0);
                info.AddValue(ValuesName, array, typeof(T[]));
            }
        }
 
        public virtual void OnDeserialization(object? sender)
        {
            if (_siInfo == null)
            {
                return; //Somebody had a dependency on this LinkedList and fixed us up before the ObjectManager got to it.
            }
 
            int realVersion = _siInfo.GetInt32(VersionName);
            int count = _siInfo.GetInt32(CountName);
 
            if (count != 0)
            {
                T[]? array = (T[]?)_siInfo.GetValue(ValuesName, typeof(T[]));
 
                if (array == null)
                {
                    throw new SerializationException(SR.Serialization_MissingValues);
                }
                for (int i = 0; i < array.Length; i++)
                {
                    AddLast(array[i]);
                }
            }
            else
            {
                head = null;
            }
 
            version = realVersion;
            _siInfo = null;
        }
 
        private void InternalInsertNodeBefore(VisualLinkedListNode<T> node, VisualLinkedListNode<T> newNode)
        {
            newNode.next = node;
            newNode.prev = node.prev;
            node.prev!.next = newNode;
            node.prev = newNode;
            version++;
            count++;
        }
 
        private void InternalInsertNodeToEmptyList(VisualLinkedListNode<T> newNode)
        {
            Debug.Assert(head == null && count == 0, "LinkedList must be empty when this method is called!");
            newNode.next = newNode;
            newNode.prev = newNode;
            head = newNode;
            version++;
            count++;
        }
 
        internal void InternalRemoveNode(VisualLinkedListNode<T> node)
        {
            Debug.Assert(node.list == this, "Deleting the node from another list!");
            Debug.Assert(head != null, "This method shouldn't be called on empty list!");
            if (node.next == node)
            {
                Debug.Assert(count == 1 && head == node, "this should only be true for a list with only one node");
                head = null;
            }
            else
            {
                node.next!.prev = node.prev;
                node.prev!.next = node.next;
                if (head == node)
                {
                    head = node.next;
                }
            }
            node.Invalidate();
            count--;
            version++;
        }
 
        internal static void ValidateNewNode(VisualLinkedListNode<T> node)
        {
            ArgumentNullException.ThrowIfNull(node);
 
            if (node.list != null)
            {
                throw new InvalidOperationException(SR.LinkedListNodeIsAttached);
            }
        }
 
        internal void ValidateNode(VisualLinkedListNode<T> node)
        {
            ArgumentNullException.ThrowIfNull(node);
 
            if (node.list != this)
            {
                throw new InvalidOperationException(SR.ExternalLinkedListNode);
            }
        }
 
        bool ICollection.IsSynchronized
        {
            get { return false; }
        }
 
        object ICollection.SyncRoot => this;
 
        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
 
            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }
 
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }
 
            ArgumentOutOfRangeException.ThrowIfNegative(index);
 
            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_InsufficientSpace);
            }
 
            T[]? tArray = array as T[];
            if (tArray != null)
            {
                CopyTo(tArray, index);
            }
            else
            {
                // No need to use reflection to verify that the types are compatible because it isn't 100% correct and we can rely
                // on the runtime validation during the cast that happens below (i.e. we will get an ArrayTypeMismatchException).
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
                }
                VisualLinkedListNode<T>? node = head;
                try
                {
                    if (node != null)
                    {
                        do
                        {
                            objects[index++] = node!.item;
                            node = node.next;
                        } while (node != head);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_IncompatibleArrayType, nameof(array));
                }
            }
        }
 
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();
 
        public struct Enumerator : IEnumerator<T>, IEnumerator, ISerializable, IDeserializationCallback
        {
            private readonly VisualLinkedList<T> _list;
            private VisualLinkedListNode<T>? _node;
            private readonly int _version;
            private T? _current;
            private int _index;
 
            internal Enumerator(VisualLinkedList<T> list)
            {
                _list = list;
                _version = list.version;
                _node = list.head;
                _current = default;
                _index = 0;
            }
 
            public T Current => _current!;
 
            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _list.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }
 
                    return Current;
                }
            }
 
            public bool MoveNext()
            {
                if (_version != _list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
 
                if (_node == null)
                {
                    _index = _list.Count + 1;
                    return false;
                }
 
                ++_index;
                _current = _node.item;
                _node = _node.next;
                if (_node == _list.head)
                {
                    _node = null;
                }
                return true;
            }
 
            void IEnumerator.Reset()
            {
                if (_version != _list.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
 
                _current = default;
                _node = _list.head;
                _index = 0;
            }
 
            public void Dispose()
            {
            }
 
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                throw new PlatformNotSupportedException();
            }
 
            void IDeserializationCallback.OnDeserialization(object? sender)
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
 
    // Note following class is not serializable since we customized the serialization of LinkedList.
    public sealed class VisualLinkedListNode<T>
    {
        internal VisualLinkedList<T>? list;
        internal VisualLinkedListNode<T>? next;
        internal VisualLinkedListNode<T>? prev;
        internal T item;
        internal int NodeId;
 
        public VisualLinkedListNode(T value)
        {
            item = value;
        }
 
        internal VisualLinkedListNode(VisualLinkedList<T> list, T value)
        {
            this.list = list;
            item = value;
        }
 
        public VisualLinkedList<T>? List
        {
            get { return list; }
        }
 
        public VisualLinkedListNode<T>? Next
        {
            get { return next == null || next == list!.head ? null : next; }
        }
 
        public VisualLinkedListNode<T>? Previous
        {
            get { return prev == null || this == list!.head ? null : prev; }
        }
 
        public T Value
        {
            get { return item; }
            set { item = value; }
        }
 
        /// <summary>Gets a reference to the value held by the node.</summary>
        public ref T ValueRef => ref item;
 
        internal void Invalidate()
        {
            list = null;
            next = null;
            prev = null;
        }
    }
}
