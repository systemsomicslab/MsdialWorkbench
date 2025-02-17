using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace CompMs.Common.Extension {
    public static class IEnumerableExtension {
        public static IEnumerable<(T, int)> WithIndex<T>(this IEnumerable<T> ts) {
            return ts.Select((t, i) => (t, i));
        }
        public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T>? collection) {
            return collection ?? Enumerable.Empty<T>();
        }

        public static bool IsEmptyOrNull<T>(this IEnumerable<T>? collection) {
            return collection == null || !collection.Any();
        }

        public static IEnumerable<T> Return<T>(T value) {
            yield return value;
        }

#if !NET6_0_OR_GREATER
        public static IEnumerable<(T1, T2)> ZipInternal<T1, T2>(this IEnumerable<T1> xs, IEnumerable<T2> ys) {
            return xs.Zip(ys, (x, y) => (x, y));
        }
#else
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<(T1, T2)> ZipInternal<T1, T2>(this IEnumerable<T1> xs, IEnumerable<T2> ys) {
            return xs.Zip(ys);
        }
#endif

        public static IEnumerable<T4> Zip<T1, T2, T3, T4>(this IEnumerable<T1> xs, IEnumerable<T2> ys, IEnumerable<T3> zs, Func<T1, T2, T3, T4> func) {
            var xys = xs.Zip(ys, Tuple.Create);
            return xys.Zip(zs, (xy, z) => func(xy.Item1, xy.Item2, z));
        }

        public static IEnumerable<(T1, T2, T3)> Zip<T1, T2, T3>(this IEnumerable<T1> xs, IEnumerable<T2> ys, IEnumerable<T3> zs) {
            return xs.Zip(ys, zs, (x, y, z) => (x, y, z));
        }

        public static IEnumerable<T5> Zip<T1, T2, T3, T4, T5>(this IEnumerable<T1> xs, IEnumerable<T2> ys, IEnumerable<T3> zs, IEnumerable<T4> ws, Func<T1, T2, T3, T4, T5> func) {
            var xys = xs.Zip(ys, Tuple.Create);
            var zws = zs.Zip(ws, Tuple.Create);
            return xys.Zip(zws, (xy, zw) => func(xy.Item1, xy.Item2, zw.Item1, zw.Item2));
        }

        public static int Argmax<T>(this IEnumerable<T> xs) where T: IComparable {
            return Argmax(xs, Comparer<T>.Default.Compare);
        }

        public static int Argmax<T>(this IEnumerable<T> xs, Comparison<T> comp) {
            return xs.Select((x, idx) => (x, idx)).Aggregate((acc, x) => comp(acc.x, x.x) < 0 ? x : acc).idx;
        }

        public static T Argmax<T, U>(this IEnumerable<T> xs, Func<T, U> func) where U: IComparable {
            return Argmax(xs, func, Comparer<U>.Default);
        }

        public static T Argmax<T, U>(this IEnumerable<T> xs, Func<T, U> func, IComparer<U> comparer) {
            return Argmax(xs, func, comparer.Compare);
        }

        public static T Argmax<T, U>(this IEnumerable<T> xs, Func<T, U> func, Comparison<U> comp) {
            return xs.Select(x => (x, y: func(x))).Aggregate((acc, p) => comp(acc.y, p.y) < 0 ? p : acc).x;
        }

        public static int Argmin<T>(this IEnumerable<T> xs) where T: IComparable {
            return Argmin(xs, Comparer<T>.Default.Compare);
        }

        public static int Argmin<T>(this IEnumerable<T> xs, Comparison<T> comp) {
            return xs.Select((x, idx) => (x, idx)).Aggregate((acc, x) => comp(acc.x, x.x) > 0 ? x : acc).idx;
        }

        public static T Argmin<T, U>(this IEnumerable<T> xs, Func<T, U> func) where U: IComparable {
            return Argmin(xs, func, Comparer<U>.Default);
        }

        public static T Argmin<T, U>(this IEnumerable<T> xs, Func<T, U> func, IComparer<U> comparer) {
            return Argmin(xs, func, comparer.Compare);
        }

        public static T Argmin<T, U>(this IEnumerable<T> xs, Func<T, U> func, Comparison<U> comp) {
            return xs.Select(x => (x, y: func(x))).Aggregate((acc, p) => comp(acc.y, p.y) > 0 ? p : acc).x;
        }

        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> xs, int size)
        {
            var results = new T[size];
            var counter = 0;
            foreach (var x in xs) {
                results[counter % size] = x;
                counter++;
                if (counter % size == 0) {
                    yield return results;
                    results = new T[size];
                }
            }
            if (counter % size != 0) {
                var remain = new T[counter % size];
                Array.Copy(results, remain, counter % size);
                yield return remain;
            }
        }

        public static IEnumerable<List<T>> Sequence<T>(this IEnumerable<IEnumerable<T>> xss) {
            var enumerators = xss.Select(xs => xs.GetEnumerator()).ToList();
            if (enumerators.Count == 0) {
                yield break;
            }
            var remain = true;
            while (remain) {
                var result = new List<T>();
                foreach(var enumerator in enumerators) {
                    if (enumerator.MoveNext()) {
                        result.Add(enumerator.Current);
                    }
                    else {
                        remain = false;
                        break;
                    }
                }
                if (remain) yield return result;
            }
        }

        public static IEnumerable<T[]> Sequence<T>(this IReadOnlyCollection<IEnumerable<T>> xss) {
            var n = xss.Count;
            if (n == 0) {
                yield break; 
            }
            var enumerators = xss.Select(xs => xs.GetEnumerator()).ToArray();
            var remain = true;
            while (remain) {
                var result = new T[n];
                for (int i = 0; i < enumerators.Length; i++) {
                    if (enumerators[i].MoveNext()) {
                        result[i] = enumerators[i].Current;
                    }
                    else {
                        remain = false;
                        break;
                    }
                }
                if (remain) yield return result;
            }
        }

        public static IEnumerable<IReadOnlyList<T>> SequenceNew<T>(this IReadOnlyCollection<IEnumerable<T>> xss) {
            return new SequenceEnumerable<T>(xss);
        }

        class SequenceEnumerable<T> : IEnumerable<IReadOnlyList<T>> {
            private static readonly int _arraySize;

            static SequenceEnumerable()
            {
                _arraySize = 85000 / Marshal.SizeOf<T>();
            }

            private readonly IEnumerator<T>[] _enumerators;
            private readonly T[][] _arrays = null;
            private readonly int _width, _length;
            private int _currentLength = 4;
            private int _current = 0;
            private T[] _buf;
            private int _remain;

            public SequenceEnumerable(IReadOnlyCollection<IEnumerable<T>> xss)
            {
                if (xss.All(xs => xs is T[])) {
                    _arrays = xss.Cast<T[]>().ToArray();
                }
                _enumerators = xss.Select(xs => xs.GetEnumerator()).ToArray();
                _width = _enumerators.Length;
                _length = _arraySize / _width;
            }

            public IEnumerator<IReadOnlyList<T>> GetEnumerator() {
                while (Read()) {
                    for (int i = 0; i < _remain; i++) {
                        yield return new Segment(_buf, _width, _currentLength, i);
                    }
                    _currentLength = Math.Min(_currentLength * 2, _length);
                }
            }

            private bool Read() {
                _buf = new T[_width * _currentLength];
                var min = _currentLength;
                if (_arrays is null) {
                    for (int i = 0; i < _enumerators.Length; i++) {
                        var j = 0;
                        while (j < min && _enumerators[i].MoveNext()) {
                            _buf[i * _currentLength + j++] = _enumerators[i].Current;
                        }
                        min = Math.Min(j, min);
                    }
                    _remain = min;
                }
                else {
                    for (int i = 0; i < _arrays.Length; i++) {
                        min = Math.Min(min, _arrays[i].Length - _current);
                        Array.Copy(_arrays[i], _current, _buf, i * _currentLength, min);
                    }
                    _current += min;
                    _remain = min;
                }
                return min != 0;
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            class Segment : IReadOnlyList<T>
            {
                private readonly T[] _baseArray;
                private readonly int _width, _length, _offset;

                public Segment(T[] baseArray, int width, int length, int offset)
                {
                    _baseArray = baseArray;
                    _width = width;
                    _length = length;
                    _offset = offset;
                }

                public T this[int index] {
                    get {
                        if ((uint)index >= _width) {
                            throw new IndexOutOfRangeException("index is out of range");
                        }
                        return _baseArray[_offset + index * _length];
                    }
                }

                public int Count => _width;

                public IEnumerator<T> GetEnumerator() {
                    for (int i = 0; i < _width; i++) {
                        yield return _baseArray[_offset + i * _length];
                    }
                }

                IEnumerator IEnumerable.GetEnumerator() {
                    return GetEnumerator();
                }
            }
        }
    }
}
