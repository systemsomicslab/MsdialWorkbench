/* 
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Common.Collections
{
    internal interface IMultiDictionary<T, V> : IEnumerable<KeyValuePair<T, ICollection<V>>>
    {
        int Count { get; }
        void Add(T key, V value);
        bool ContainsKey(T key);
        IEnumerable<T> Keys { get; }
        IEnumerable<V> Values { get; }
        IEnumerable<V> this[T key] { get; }
        void Remove(T key, V value);
        IEnumerable<KeyValuePair<T, V>> Entries { get; }
    }

    public sealed class MultiDictionary<T, V>
        : MultiDictionaryBase<T, V>
    {
        readonly Dictionary<T, ICollection<V>> dic = new Dictionary<T, ICollection<V>>();

        protected override IDictionary<T, ICollection<V>> BaseMap => dic;
    }

    public sealed class SortedMultiDictionary<T, V>
        : MultiDictionaryBase<T, V>
    {
        readonly SortedDictionary<T, ICollection<V>> dic = new SortedDictionary<T, ICollection<V>>();

        protected override IDictionary<T, ICollection<V>> BaseMap => dic;
    }

    public abstract class MultiDictionaryBase<T, V> : IMultiDictionary<T, V>
    {
        protected abstract IDictionary<T, ICollection<V>> BaseMap { get; }

        public int Count => BaseMap.Values.Select(n => n.Count).Sum();

        public void Add(T key, V value)
        {
            if (!BaseMap.TryGetValue(key, out ICollection<V> list))
            {
                list = new HashSet<V>();
                BaseMap.Add(key, list);
            }
            list.Add(value);
        }

        public void Remove(T key, V value)
        {
            if (BaseMap.TryGetValue(key, out ICollection<V> list))
            {
                list.Remove(value);
            }
        }

        public bool ContainsKey(T key) => BaseMap.ContainsKey(key);

        public IEnumerator<KeyValuePair<T, ICollection<V>>> GetEnumerator()
            => BaseMap.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<T> Keys => BaseMap.Keys;

        public IEnumerable<V> Values
        {
            get
            {
                foreach (var v in BaseMap.Values)
                    foreach (var w in v)
                        yield return w;
                yield break;
            }
        }

        public IEnumerable<KeyValuePair<T, V>> Entries
        {
            get
            {
                foreach (var e in BaseMap)
                    foreach (var v in e.Value)
                        yield return new KeyValuePair<T, V>(e.Key, v);
                yield break;
            }
        }

        private static readonly V[] empty = Array.Empty<V>();

        public IEnumerable<V> this[T key]
        {
            get
            {
                if (!BaseMap.TryGetValue(key, out ICollection<V> v))
                {
                    return empty;
                }
                return v;
            }
        }

        public void Clear()
        {
            foreach (var e in BaseMap)
                e.Value.Clear();
            BaseMap.Clear();
        }
    }
}
