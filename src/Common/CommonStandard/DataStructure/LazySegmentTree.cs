using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public sealed class LazySegmentTree<T, E>
    {
        private readonly int _n;
        private readonly Func<T, T, T> _f;
        private readonly Func<T, E, T> _g;
        private readonly Func<E, E, E> _h;
        private readonly T _te;
        private readonly E _ee;
        private readonly int _size;
        private readonly int _height;
        private readonly T[] _data;
        private readonly E[] _lazy;

        public LazySegmentTree(int n, Func<T, T, T> f, Func<T, E, T> g, Func<E, E, E> h, T te, E ee) {
            _n = n;
            _f = f ?? throw new ArgumentNullException(nameof(f));
            _g = g ?? throw new ArgumentNullException(nameof(g));
            _h = h ?? throw new ArgumentNullException(nameof(h));
            _te = te;
            _ee = ee;

            var size = 1;
            var height = 0;
            while (size < n) {
                size <<= 1;
                height++;
            }
            _size = size;
            _height = height;
            _data = Enumerable.Repeat(_te, size * 2).ToArray();
            _lazy = Enumerable.Repeat(_ee, size * 2).ToArray();
        }

        public LazySegmentTree(IReadOnlyList<T> data, Func<T, T, T> f, Func<T, E, T> g, Func<E, E, E> h, T te, E ee) : this(data.Count, f, g, h, te, ee) {
            Build(data);
        }

        public void Build(IReadOnlyList<T> values) {
            Debug.Assert(values.Count == _n);
            for (var i = 0; i < _n; i++) {
                _data[i + _size] = values[i];
            }
            for (var i = _size - 1; i > 0; i--) {
                Update(i);
            }
        }

        public void Set(int k, T x) {
            k += _size;
            PropagateToLeaf(k);
            _data[k] = x;
            UpdateToRoot(k);
        }

        public T Get(int k) {
            k += _size;
            PropagateToLeaf(k);
            return _data[k];
        }

        public T this[int k] {
            get => Get(k);
            set => Set(k, value);
        }

        public T Query(int l, int r) {
            if (l >= r) {
                return _te;
            }
            l += _size;
            r += _size;

            PropagateLeftToLeaf(l);
            PropagateRightToLeaf(r);

            T lv = _te, rv = _te;
            for (; l < r; l >>= 1, r >>= 1) {
                if ((l & 1) != 0) {
                    lv = _f(lv, _data[l++]);
                }
                if ((r & 1) != 0) {
                    rv = _f(_data[--r], rv);
                }
            }
            return _f(lv, rv);
        }

        public T Query() {
            return _data[1];
        }

        public void Apply(int k, E x) {
            k += _size;
            PropagateToLeaf(k);
            _data[k] = _g(_data[k], x);
            UpdateToRoot(k);
        }

        public void Apply(int l, int r, E x) {
            if (l >= r) {
                return;
            }
            l += _size;
            r += _size;
            PropagateLeftToLeaf(l);
            PropagateRightToLeaf(r);
            int ll = l, rr = r;
            for (; ll < rr; ll >>= 1, rr >>= 1) {
                if ((ll & 1) != 0) {
                    ApplyAll(ll++, x);
                }
                if ((rr & 1) != 0) {
                    ApplyAll(--rr, x);
                }
            }
            for (var h = 1; h <= _height; h++) {
                if (((l >> h) << h) != l) {
                    Update(l >> h);
                }
                if (((r >> h) << h) != r) {
                    Update((r - 1) >> h);
                }
            }
        }

        public int FindFirst(int l, Func<T, bool> predicate) {
            if (l >= _n) {
                return _n;
            }
            l += _size;
            PropagateToLeaf(l);
            var sum = _te;
            do {
                while ((l & 1) == 0) {
                    l >>= 1;
                }
                if (predicate(_f(sum, _data[l]))) {
                    while (l < _size) {
                        Propagate(l);
                        l <<= 1;
                        var next = _f(sum, _data[l]);
                        if (!predicate(next)) {
                            sum = next;
                            l++;
                        }
                    }
                    return l + 1 - _size;
                }
                sum = _f(sum, _data[l++]);
            } while ((l & -l) != l);
            return _n;
        }

        public int FindLast(int r, Func<T, bool> predicate) {
            if (r <= 0) {
                return -1;
            }
            r += _size;
            PropagateToLeaf(r - 1);
            var sum = _te;
            do {
                r--;
                while (r > 1 && ((r & 1) != 0)) {
                    r >>= 1;
                }
                if (predicate(_f(_data[r], sum))) {
                    while (r < _size) {
                        Propagate(r);
                        r = (r << 1) + 1;
                        var next = _f(_data[r], sum);
                        if (!predicate(next)) {
                            sum = next;
                            r--;
                        }
                    }
                    return r - _size;
                }
                sum = _f(_data[r], sum);
            } while ((r & -r) != r);
            return -1;
        }

        public void Dump() {
            for (int h = 0; h <= _height; h++) {
                var f = $"{{0,-{1 << (_height - h + 1)}:d}}";
                for (int i = 1 << h; i < 1 << (h + 1); i++) {
                    Console.Write(f, _data[i]);
                    Console.Write(f, _lazy[i]);
                }
                Console.WriteLine();
            }
        }

        private void Update(int k) {
            _data[k] = _f(_data[k * 2], _data[k * 2 + 1]);
        }

        private void UpdateToRoot(int k) {
            for (var h = 1; h <= _height; h++) {
                Update(k >> h);
            }
        }

        private void ApplyAll(int k, E x) {
            _data[k] = _g(_data[k], x);
            if (k < _size) {
                _lazy[k] = _h(_lazy[k], x);
            }
        }

        private void Propagate(int k) {
            if (!Equals(_lazy[k], _ee)) {
                ApplyAll(k * 2, _lazy[k]);
                ApplyAll(k * 2 + 1, _lazy[k]);
                _lazy[k] = _ee;
            }
        }

        private void PropagateToLeaf(int k) {
            for (var h = _height; h > 0; h--) {
                Propagate(k >> h);
            }
        }

        private void PropagateLeftToLeaf(int l) {
            for (var h = _height; h > 0; h--) {
                if (((l >> h) << h) != l) {
                    Propagate(l >> h);
                }
            }
        }

        private void PropagateRightToLeaf(int r) {
            for (var h = _height; h > 0; h--) {
                if (((r >> h) << h) != r) {
                    Propagate((r - 1) >> h);
                }
            }
        }
    }
}
