using System;
using System.Collections.Generic;

namespace CompMs.Common.DataStructure; 

public class PriorityQueue<T>
{
    public int Length => _n;

    private readonly List<T> _data;
    private readonly IComparer<T> _comparer;
    private int _n = 0;

    public PriorityQueue(IReadOnlyList<T> data, IComparer<T> comp)
    {
        _data = new List<T>(data);
        _comparer = comp;
        _data.Sort(_comparer);
        _n = _data.Count;
    }

    public PriorityQueue(IReadOnlyList<T> data, Func<T, T, int> comp) : this(data, Comparer<T>.Create(new Comparison<T>(comp))) { }
    public PriorityQueue(Func<T, T, int> comp): this([], Comparer<T>.Create(new Comparison<T>(comp))) { }
    public PriorityQueue(IComparer<T> comp): this([], comp) { }

    void shiftup(int idx)
    {
        int i = idx;
        while (i != 0)
        {
            var p = (i - 1) >> 1;
            if (_comparer.Compare(_data[i], _data[p]) >= 0) {
                break;
            }
            (_data[p], _data[i]) = (_data[i], _data[p]);
            i = p;
        }
    }

    void shiftdown(int idx)
    {
        var i = idx;
        while (2*i + 1 < _n)
        {
            var a = 2 * i + 1;
            var b = 2 * i + 2;
            if (b < _n && _comparer.Compare(_data[a], _data[b]) > 0) {
                a = b;
            }
            if (_comparer.Compare(_data[i], _data[a]) <= 0) {
                break;
            }

            (_data[i], _data[a]) = (_data[a], _data[i]);
            i = a;
        }
    }

    public void Push(T item)
    {
        if (_data.Count == _n) {
            _data.Add(item);
        }
        else {
            _data[_n] = item;
        }

        shiftup(_n++);
    }

    public T Pop()
    {
        var result = _data[0];
        _data[0] = _data[_n - 1];
        _n--;
        shiftdown(0);
        return result;
    }
}

public static class PriorityQueue {
    public static PriorityQueue<T> CreateEmpty<T>() where T: IComparable<T> {
        return new PriorityQueue<T>(Comparer<T>.Default);
    }

    public static PriorityQueue<T> Create<T>(IReadOnlyList<T> data) where T: IComparable<T> {
        return new PriorityQueue<T>(data, Comparer<T>.Default);
    }
}
