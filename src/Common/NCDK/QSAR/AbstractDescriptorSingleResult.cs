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

namespace NCDK.QSAR
{
    public abstract class AbstractDescriptorSingleResult<T> : IDescriptorResult
    {
        private readonly string key;

        public T Value { get; private set; }

        protected AbstractDescriptorSingleResult(string key, T value)
        {
            this.key = key ?? throw new ArgumentNullException(nameof(key));
            this.Value = value;
        }

        public T this[string key] => this.key.Equals(key) ? Value : throw new KeyNotFoundException();

        object IReadOnlyDictionary<string, object>.this[string key] => this[key];

        public Exception Exception => null;

        public IEnumerable<string> Keys
        {
            get
            {
                yield return key;
                yield break;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => Keys;

        public IEnumerable<T> Values
        {
            get
            {
                yield return Value;
                yield break;
            }
        }

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => Values.Cast<object>();

        public int Count => 1;

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => 1;

        public bool ContainsKey(string key) => this.key.Equals(key, StringComparison.Ordinal);

        bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => ContainsKey(key);

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            yield return new KeyValuePair<string, T>(key, Value);
            yield break;
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            yield return new KeyValuePair<string, object>(key, Value);
            yield break;
        }

        public bool TryGetValue(string key, out T value)
        {
            if (ContainsKey(key))
            {
                value = Value;
                return true;
            }
            value = default(T);
            return false;
        }

        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value)
        {
            var b = TryGetValue(key, out T v);
            value = v;
            return b;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
