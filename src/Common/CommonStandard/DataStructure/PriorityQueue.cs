using System;
using System.Collections.Generic;

namespace CompMs.Common.DataStructure; 

public class PriorityQueue<T>
{
    public int Length => _n;

    private T[] _data;
    private readonly IComparer<T> _comparer;
    private int _n = 0;

    public PriorityQueue(IReadOnlyList<T> data, IComparer<T> comp)
    {
        _comparer = comp;
        _n = data.Count;
        _data = new T[Math.Max(4, _n * 2)];
        if (data is T[] array) {
            Array.Copy(array, 0, _data, 0, _n);
        }
        else if (data is List<T> list) {
            list.CopyTo(_data, 0);
        }
        else {
            for (int i = 0; i < _n; i++) {
                _data[i] = data[i];
            }
        }
        Array.Sort(_data, 0, _n, comp);
    }

    public PriorityQueue(IReadOnlyList<T> data, Func<T, T, int> comp) : this(data, Comparer<T>.Create(new Comparison<T>(comp))) { }
    public PriorityQueue(Func<T, T, int> comp): this([], Comparer<T>.Create(new Comparison<T>(comp))) { }
    public PriorityQueue(IComparer<T> comp): this([], comp) { }

    void shiftup(int idx)
    {
        var comparer = _comparer;
        int i = idx;
        var x = _data[idx];
        while (i != 0)
        {
            var p = (i - 1) >> 1;
            if (comparer.Compare(x, _data[p]) >= 0) {
                break;
            }
            _data[i] = _data[p];
            i = p;
        }
        if (i != idx) {
            _data[i] = x;
        }
    }

    void shiftdown(int idx)
    {
        var comparer = _comparer;
        var i = idx;
        var x = _data[i];
        while (2*i + 1 < _n)
        {
            var a = 2 * i + 1;
            var b = 2 * i + 2;
            if (b < _n && comparer.Compare(_data[a], _data[b]) > 0) {
                a = b;
            }
            if (comparer.Compare(x, _data[a]) <= 0) {
                break;
            }

            _data[i] = _data[a];
            i = a;
        }
        if (i != idx) {
            _data[i] = x;
        }
    }

    public void Push(T item)
    {
        if (_data.Length == _n) {
            Array.Resize(ref _data, _data.Length == 0 ? 4 : _data.Length * 2);
        }
        _data[_n] = item;

        shiftup(_n++);
    }

    public T Pop()
    {
        if (_n == 0) {
            throw new InvalidOperationException("PriorityQueue is empty.");
        }

        var result = _data[0];
        _data[0] = _data[--_n];
        _data[_n] = default!;
        shiftdown(0);
        return result;
    }

    public T PopPush(T item) {
        if (_n == 0) {
            throw new InvalidOperationException("PriorityQueue is empty.");
        }

        var result = _data[0];
        _data[0] = item;
        shiftdown(0);
        return result;
    }

    public T PushPop(T item) {
        if (_n == 0) {
            return item;
        }

        var result = _data[0];
        if (_comparer.Compare(item, result) <= 0) {
            return item;
        }
        _data[0] = item;
        shiftdown(0);
        return result;
    }

    public T Peek() => _n == 0 ? throw new InvalidOperationException("PriorityQueue is empty.") : _data[0];
}

public static class PriorityQueue {
    public static PriorityQueue<T> CreateEmpty<T>() where T: IComparable<T> {
        return new PriorityQueue<T>(Comparer<T>.Default);
    }

    public static PriorityQueue<T> Create<T>(IReadOnlyList<T> data) where T: IComparable<T> {
        return new PriorityQueue<T>(data, Comparer<T>.Default);
    }
}
