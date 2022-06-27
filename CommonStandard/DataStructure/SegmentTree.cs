using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public class SegmentTree<T>
    {
        private readonly T[] _data;
        private readonly T _e;
        private readonly int _sz;
        private readonly Func<T, T, T> _f;
        private LazyUpdator _updator;

        public SegmentTree(int n, T e, Func<T, T, T> f) {
            _e = e;
            _f = f ?? throw new ArgumentNullException(nameof(f));
            _sz = 1;
            while (_sz < n) _sz <<= 1;
            _data = Enumerable.Repeat(e, _sz * 2).ToArray();
        }

        public void Update(int k, T x) {
            k += _sz;
            _data[k] = x;
            while (k > 0) {
                k >>= 1;
                _data[k] = _f(_data[k * 2], _data[k * 2 + 1]);
            }
        }

        public IDisposable LazyUpdate() {
            if (!(_updator is null)) {
                throw new InvalidOperationException("LazyUpdate is already running.");
            }
            return _updator = new LazyUpdator(this);
        }

        public T Query(int i, int j) {
            T left = _e, right = _e;
            i += _sz;
            j += _sz;
            while (i < j) {
                if ((i & 1) != 0) left = _f(left, _data[i++]);
                if ((j & 1) != 0) right = _f(_data[--j], right);
                i >>= 1;
                j >>= 1;
            }
            return _f(left, right);
        }

        public T this[int index] {
            set {
                if (index < 0 && index >= _sz) {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                if (_updator is null) {
                    Update(index, value);
                }
                else {
                    _updator.Store(index, value);
                }
            }
        }

        public T this[int i, int j] {
            get {
                return Query(i, j);
            }
        }

        private void Set(int k, T x) {
            _data[k + _sz] = x;
        }

        private void Build() {
            for (int i = _sz - 1; i > 0; --i)
                _data[i] = _f(_data[i * 2], _data[i * 2 + 1]);
        }

        class LazyUpdator : IDisposable {
            private SegmentTree<T> _tree;
            private List<(int, T)> _values;

            public LazyUpdator(SegmentTree<T> tree) {
                _tree = tree;
                _values = new List<(int, T)>();
            }

            public void Store(int index, T value) {
                _values.Add((index, value));
            }

            public void Build() {
                foreach (var (index, value) in _values) {
                    _tree.Set(index, value);
                }
                _tree.Build();
            }

            public void Dispose() {
                if (_tree is null || _values is null) {
                    return;
                }
                Build();
                _tree._updator = null;
                _tree = null;
                _values = null;
            }
        }
    }
}
