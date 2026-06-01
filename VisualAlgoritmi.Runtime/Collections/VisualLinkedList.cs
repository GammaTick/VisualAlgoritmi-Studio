using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using VisualAlgoritmi.Runtime.Operations;

namespace VisualAlgoritmi.Runtime.Collections
{
    public class VisualLinkedList<T> : ICollection<T>, ICollection, IReadOnlyCollection<T>, ISerializable, IDeserializationCallback
    {
        private readonly int _structureId = 0;

        private int _nextNodeId;

        private readonly LinkedList<T> _inner;
        private readonly Dictionary<LinkedListNode<T>, int> _nodeIds = new();

        public VisualLinkedList()
        {
            _inner = new LinkedList<T>();

            _structureId = VisualStructureIdProvider.GetNextId();
        }

        public VisualLinkedList(IEnumerable<T> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _inner = new LinkedList<T>();

            _structureId = VisualStructureIdProvider.GetNextId();

            T[] items = collection.ToArray();

            if (items.Length == 0)
            {
                return;
            }

            OperationRecorder.BeginStep();

            foreach (T item in items)
            {
                LinkedListNode<T> node = _inner.AddLast(item);
                int nodeId = RegisterNode(node);

                OperationRecorder.WriteLinkedListAddLast(_structureId, nodeId, item);
            }

            OperationRecorder.EndStep();
        }

        public int Count
        {
            get => _inner.Count;
        }

        public LinkedListNode<T>? First
        {
            get => _inner.First;
        }

        public LinkedListNode<T>? Last
        {
            get => _inner.Last;
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<T>.Add(T value)
        {
            AddLast(value);
        }

        public LinkedListNode<T> AddAfter(LinkedListNode<T> node, T value)
        {
            ValidateNodeBelongsToThisList(node);

            int existingNodeId = GetNodeId(node);

            LinkedListNode<T> newNode = _inner.AddAfter(node, value);
            int newNodeId = RegisterNode(newNode);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddAfter(_structureId, existingNodeId, newNodeId, value);
            OperationRecorder.EndStep();

            return newNode;
        }

        public void AddAfter(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            ValidateNodeBelongsToThisList(node);
            ValidateDetachedNode(newNode);

            int existingNodeId = GetNodeId(node);

            _inner.AddAfter(node, newNode);
            int newNodeId = RegisterNode(newNode);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddAfter(_structureId, existingNodeId, newNodeId, newNode.Value);
            OperationRecorder.EndStep();
        }

        public LinkedListNode<T> AddBefore(LinkedListNode<T> node, T value)
        {
            ValidateNodeBelongsToThisList(node);

            int existingNodeId = GetNodeId(node);

            LinkedListNode<T> newNode = _inner.AddBefore(node, value);
            int newNodeId = RegisterNode(newNode);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddBefore(_structureId, existingNodeId, newNodeId, value);
            OperationRecorder.EndStep();

            return newNode;
        }

        public void AddBefore(LinkedListNode<T> node, LinkedListNode<T> newNode)
        {
            ValidateNodeBelongsToThisList(node);
            ValidateDetachedNode(newNode);

            int existingNodeId = GetNodeId(node);

            _inner.AddBefore(node, newNode);
            int newNodeId = RegisterNode(newNode);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddBefore(_structureId, existingNodeId, newNodeId, newNode.Value);
            OperationRecorder.EndStep();
        }

        public LinkedListNode<T> AddFirst(T value)
        {
            LinkedListNode<T> node = _inner.AddFirst(value);
            int nodeId = RegisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddFirst(_structureId, nodeId, value);
            OperationRecorder.EndStep();

            return node;
        }

        public void AddFirst(LinkedListNode<T> node)
        {
            ValidateDetachedNode(node);

            _inner.AddFirst(node);
            int nodeId = RegisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddFirst(_structureId, nodeId, node.Value);
            OperationRecorder.EndStep();
        }

        public LinkedListNode<T> AddLast(T value)
        {
            LinkedListNode<T> node = _inner.AddLast(value);
            int nodeId = RegisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddLast(_structureId, nodeId, value);
            OperationRecorder.EndStep();

            return node;
        }

        public void AddLast(LinkedListNode<T> node)
        {
            ValidateDetachedNode(node);

            _inner.AddLast(node);
            int nodeId = RegisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListAddLast(_structureId, nodeId, node.Value);
            OperationRecorder.EndStep();
        }

        public void Clear()
        {
            _inner.Clear();
            _nodeIds.Clear();

            OperationRecorder.BeginStep();
            OperationRecorder.WriteDataStructureClear(_structureId);
            OperationRecorder.EndStep();
        }

        public bool Contains(T value)
        {
            return _inner.Contains(value);
        }

        public void CopyTo(T[] array, int index)
        {
            _inner.CopyTo(array, index);
        }

        public LinkedListNode<T>? Find(T value)
        {
            return _inner.Find(value);
        }

        public LinkedListNode<T>? FindLast(T value)
        {
            return _inner.FindLast(value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            _inner.GetObjectData(info, context);
        }

        public virtual void OnDeserialization(object? sender)
        {
            _inner.OnDeserialization(sender);

            _nodeIds.Clear();
            _nextNodeId = 0;

            RegisterAllExistingNodes();
        }

        public bool Remove(T value)
        {
            LinkedListNode<T>? node = _inner.Find(value);

            if (node == null)
            {
                return false;
            }

            int nodeId = GetNodeId(node);

            _inner.Remove(node);
            UnregisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListRemoveNode(_structureId, nodeId);
            OperationRecorder.EndStep();

            return true;
        }

        public void Remove(LinkedListNode<T> node)
        {
            ValidateNodeBelongsToThisList(node);

            int nodeId = GetNodeId(node);

            _inner.Remove(node);
            UnregisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListRemoveNode(_structureId, nodeId);
            OperationRecorder.EndStep();
        }

        public void RemoveFirst()
        {
            LinkedListNode<T>? node = _inner.First;

            if (node == null)
            {
                _inner.RemoveFirst();
                return;
            }

            int nodeId = GetNodeId(node);

            _inner.RemoveFirst();
            UnregisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListRemoveNode(_structureId, nodeId);
            OperationRecorder.EndStep();
        }

        public void RemoveLast()
        {
            LinkedListNode<T>? node = _inner.Last;

            if (node == null)
            {
                _inner.RemoveLast();
                return;
            }

            int nodeId = GetNodeId(node);

            _inner.RemoveLast();
            UnregisterNode(node);

            OperationRecorder.BeginStep();
            OperationRecorder.WriteLinkedListRemoveNode(_structureId, nodeId);
            OperationRecorder.EndStep();
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot => this;

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_inner).CopyTo(array, index);
        }

        public int GetVisualNodeId(LinkedListNode<T> node)
        {
            ValidateNodeBelongsToThisList(node);

            return GetNodeId(node);
        }

        private int[] RegisterAllExistingNodes()
        {
            int[] nodeIds = new int[_inner.Count];

            int index = 0;
            LinkedListNode<T>? node = _inner.First;

            while (node != null)
            {
                nodeIds[index] = RegisterNode(node);
                index++;

                node = node.Next;
            }

            return nodeIds;
        }

        private int RegisterNode(LinkedListNode<T> node)
        {
            int nodeId = ++_nextNodeId;
            _nodeIds.Add(node, nodeId);

            return nodeId;
        }

        private void UnregisterNode(LinkedListNode<T> node)
        {
            _nodeIds.Remove(node);
        }

        private int GetNodeId(LinkedListNode<T> node)
        {
            if (_nodeIds.TryGetValue(node, out int nodeId))
            {
                return nodeId;
            }

            throw new InvalidOperationException("The LinkedList node has no visual node id.");
        }

        private void ValidateNodeBelongsToThisList(LinkedListNode<T> node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (!ReferenceEquals(node.List, _inner))
            {
                throw new InvalidOperationException("The LinkedList node does not belong to this list.");
            }

            if (!_nodeIds.ContainsKey(node))
            {
                throw new InvalidOperationException("The LinkedList node belongs to this list but is not registered.");
            }
        }

        private static void ValidateDetachedNode(LinkedListNode<T> node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node.List != null)
            {
                throw new InvalidOperationException("The LinkedList node already belongs to a list.");
            }
        }
    }
}