using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public class SegmentTree<T>
    {
        private T[] data;
        private T e;
        private int sz;
        private Func<T, T, T> f;

        public SegmentTree(int n, T e, Func<T, T, T> f) {
            this.e = e;
            this.f = f;
            sz = 1;
            while (sz < n) sz <<= 1;
            data = Enumerable.Repeat(e, sz * 2).ToArray();
        }

        public void Set(int k, T x) {
            data[k + sz] = x;
        }

        public void Build() {
            for (int i = sz - 1; i > 0; --i)
                data[i] = f(data[i * 2], data[i * 2 + 1]);
        }

        public void Update(int k, T x) {
            k += sz;
            data[k] = x;
            while (k > 0) {
                k >>= 1;
                data[k] = f(data[k * 2], data[k * 2 + 1]);
            }
        }

        public T Query(int i, int j) {
            T left = e, right = e;
            i += sz;
            j += sz;
            while (i < j) {
                if ((i & 1) != 0) left = f(left, data[i++]);
                if ((j & 1) != 0) right = f(data[--j], right);
                i >>= 1;
                j >>= 1;
            }
            return f(left, right);
        }

    }
}
