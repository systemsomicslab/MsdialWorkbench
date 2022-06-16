using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public sealed class DirectionalLinkedGraph<T>
    {
        private readonly Dictionary<T, DirectionalLinkedGraphNode<T>> _innerMap;
        private readonly IEqualityComparer<T> _comparer;

        public DirectionalLinkedGraph() {
            _innerMap = new Dictionary<T, DirectionalLinkedGraphNode<T>>();
            _comparer = EqualityComparer<T>.Default;
        }

        public DirectionalLinkedGraph(IEqualityComparer<T> comparer) {
            _innerMap = new Dictionary<T, DirectionalLinkedGraphNode<T>>(comparer);
            _comparer = comparer;
        }

        public bool Contains(T value) {
            return _innerMap.ContainsKey(value);
        }

        public void Add(T parent, params T[] children) {
            Add(parent, (IEnumerable<T>)children);
        }

        public void Add(T parent, IEnumerable<T> children) {
            if (!_innerMap.TryGetValue(parent, out var parentNode)) {
                parentNode = new DirectionalLinkedGraphNode<T>(parent);
                _innerMap.Add(parent, parentNode);
            }

            List<DirectionalLinkedGraphNode<T>> childrenNodes;
            var count = (children as ICollection<T>)?.Count;
            var memo = new HashSet<T>(parentNode.Children.Select(child => child.Value), _comparer);
            memo.Add(parent);
            if (count.HasValue) {
                childrenNodes = new List<DirectionalLinkedGraphNode<T>>(count.Value);
            }
            else {
                childrenNodes = new List<DirectionalLinkedGraphNode<T>>();
            }
            foreach (var child in children) {
                if (memo.Contains(child)) {
                    continue;
                }
                if (!_innerMap.TryGetValue(child, out var node)) {
                    node = new DirectionalLinkedGraphNode<T>(child);
                    _innerMap.Add(child, node);
                }
                memo.Add(child);
                childrenNodes.Add(node);
            }

            parentNode.Children.AddRange(childrenNodes);
        }

        public void Clear() {
            _innerMap.Clear();
        }

        public List<T> GetChildren(T parent) {
            if (!_innerMap.ContainsKey(parent)) {
                throw new InvalidOperationException($"DirectionalLinkedTree does not contain key {parent}.");
            }
            return GetChildrenCore(_innerMap[parent]).ToList();
        }

        private IEnumerable<T> GetChildrenCore(DirectionalLinkedGraphNode<T> parent) {
            return parent.Children.Select(child => child.Value);
        }

        public List<T> GetDescendants(T parent) {
            if (!_innerMap.ContainsKey(parent)) {
                throw new InvalidOperationException($"DirectionalLinkedTree does not contain key {parent}.");
            }
            return GetDescendantsCore(_innerMap[parent]).ToList();
        }

        private IEnumerable<T> GetDescendantsCore(DirectionalLinkedGraphNode<T> parent) {
            return parent.Children.SelectMany(child => GetDescendantsCore(child).Prepend(child.Value));
        }
    }

    internal sealed class DirectionalLinkedGraphNode<T> {
        public DirectionalLinkedGraphNode(T value, IEnumerable<DirectionalLinkedGraphNode<T>> children) {
            Value = value;
            Children = children.ToList();
        }

        public DirectionalLinkedGraphNode(T value) : this(value, Enumerable.Empty<DirectionalLinkedGraphNode<T>>()) {

        }

        public T Value { get; }

        public List<DirectionalLinkedGraphNode<T>> Children { get; }
    }
}
