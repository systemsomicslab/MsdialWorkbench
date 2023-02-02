using System;
using System.Collections.Generic;

namespace CompMs.Common.DataStructure {
    public class PriorityQueue<T>
    {
        public int Length => _n;

        List<T> _data;
        Func<T, T, int> _comp;
        int _n = 0;

        public PriorityQueue(List<T> data, Func<T, T, int> comp)
        {
            _data = new List<T>(data);
            _comp = comp;
            _n = _data.Count;
        }

        public PriorityQueue(Func<T, T, int> comp): this(new List<T>(), comp) { }

        void shiftup(int idx)
        {
            int i = idx;
            while (i != 0)
            {
                var p = (i - 1) >> 1;
                if (_comp(_data[i], _data[p]) >= 0)
                    break;
                var tmp = _data[i];
                _data[i] = _data[p];
                _data[p] = tmp;
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
                if (b < _n && _comp(_data[a], _data[b]) > 0)
                    a = b;
                if (_comp(_data[i], _data[a]) <= 0)
                    break;
                var tmp = _data[a];
                _data[a] = _data[i];
                _data[i] = tmp;
                i = a;
            }
        }

        public void Push(T item)
        {
            if (_data.Count == _n)
                _data.Add(item);
            else
                _data[_n] = item;
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
}
