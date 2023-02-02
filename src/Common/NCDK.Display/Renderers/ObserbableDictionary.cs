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

using System.Collections;
using System.Collections.Generic;

namespace NCDK.Renderers
{
    internal class ObserbableDictionary<TKey, TItem> : IDictionary<TKey, TItem>
    {
        internal Dictionary<TKey, TItem> dic = new Dictionary<TKey, TItem>();
        protected IChemObjectListener Listener { get; private set; }

        public ObserbableDictionary(IChemObjectListener listener)
        {
            dic = new Dictionary<TKey, TItem>();
            Listener = listener;
        }

        public TItem this[TKey key]
        {
            get => dic[key];
            set
            {
                dic[key] = value;
                if (Listener != null)
                    Listener.OnStateChanged(new ChemObjectChangeEventArgs(this));
            }
        }

        public ICollection<TKey> Keys => dic.Keys;

        public ICollection<TItem> Values => dic.Values;

        public int Count => dic.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TItem value)
        {
            dic.Add(key, value);
            if (Listener != null)
                Listener.OnStateChanged(new ChemObjectChangeEventArgs(this));
        }

        public void Add(KeyValuePair<TKey, TItem> item)
            => Add(item.Key, item.Value);

        public void Clear()
            => dic.Clear();

        public bool Contains(KeyValuePair<TKey, TItem> item)
            => ((IDictionary<TKey, TItem>)dic).Contains(item);

        public bool ContainsKey(TKey key)
            => dic.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TItem>[] array, int arrayIndex)
            => ((IDictionary<TKey, TItem>)dic).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TItem>> GetEnumerator()
            => dic.GetEnumerator();

        public bool Remove(TKey key)
        {
            var b = dic.Remove(key);
            if (b && Listener != null)
                Listener.OnStateChanged(new ChemObjectChangeEventArgs(this));
            return b;
        }

        public bool Remove(KeyValuePair<TKey, TItem> item)
        {
            var b = ((IDictionary<TKey, TItem>)dic).Remove(item);
            if (b && Listener != null)
                Listener.OnStateChanged(new ChemObjectChangeEventArgs(this));
            return b;
        }

        public bool TryGetValue(TKey key, out TItem value)
            => dic.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
