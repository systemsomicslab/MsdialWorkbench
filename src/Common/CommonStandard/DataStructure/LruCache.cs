using System.Collections.Generic;

namespace CompMs.Common.DataStructure
{
    internal sealed class LruCache<T, U>
    {
        private readonly int _capacity;
        private readonly Dictionary<T, LinkedListNode<(U value, T key)>> _cache;
        private readonly LinkedList<(U value, T key)> _container;

        public LruCache(int capacity) {
            _capacity = capacity;
            _cache = new Dictionary<T, LinkedListNode<(U value, T key)>>();
            _container = new LinkedList<(U, T)>();
        }

        public LruCache(int capacity, IEqualityComparer<T> comparer) {
            _capacity = capacity;
            _cache = new Dictionary<T, LinkedListNode<(U value, T key)>>(comparer);
            _container = new LinkedList<(U, T)>();
        }

        public U Get(T key) {
            if (_cache.TryGetValue(key, out var node)) {
                _container.Remove(node);
                _container.AddFirst(node);
                return node.Value.value;
            }
            return default;
        }

        public void Put(T key, U value) {
            if (_cache.TryGetValue(key, out var node)) {
                node.Value = (value, key);
                _container.Remove(node);
                _container.AddFirst(node);
            }
            else {
                if (_cache.Count >= _capacity) {
                    var removeNode = _container.Last;
                    _cache.Remove(removeNode.Value.key);
                    _container.Remove(removeNode);
                }

                var newNode = _container.AddFirst((value, key));
                _cache.Add(key, newNode);
            }
        }

        public bool ContainsKey(T key) {
            return _cache.ContainsKey(key);
        }
    }
}
