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
using System.Reflection;

namespace NCDK.QSAR
{
    public abstract class AbstractDescriptorArrayResult<T> : IDescriptorResult
    {
        private readonly string prefix;
        private readonly int baseIndex;

        public AbstractDescriptorArrayResult(Exception e)
            : this(Array.Empty<T>())
        {
            this.Exception = e;
        }

        public AbstractDescriptorArrayResult(IReadOnlyList<T> values)
        {
            this.Values = values;

            var attr = this.GetType().GetCustomAttribute<DescriptorResultAttribute>();
            prefix = attr.Prefix;
            baseIndex = attr.BaseIndex;
        }

        public IEnumerable<string> Keys
        {
            get
            {
                var attr = this.GetType().GetCustomAttribute<DescriptorResultAttribute>();
                var prefix = attr.Prefix;
                for (int i = 0; i < Values.Count; i++)
                    yield return $"{prefix}{i + baseIndex}";
                yield break;
            }
        }

        public IReadOnlyList<T> Values { get; private set; }

        public bool TryGetValue(string key, out T value)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                if (int.TryParse(key.Substring(prefix.Length), out int i))
                    if (baseIndex <= i && i < (Values.Count + baseIndex))
                    {
                        value = Values[i - baseIndex];
                        return true;
                    }
            }
            value = default(T);
            return false;
        }

        public T this[string key]
        {
            get
            {
                if (TryGetValue(key, out T value))
                    return value;
                throw new KeyNotFoundException();
            }
        }

        public Exception Exception { get; private set; }

        public int Count => Values.Count;

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => Values.Cast<object>();

        public bool ContainsKey(string key)
        {
            return TryGetValue(key, out T dummy);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (int i = 0; i < Values.Count; i++)
                yield return new KeyValuePair<string, object>($"{prefix}{i + baseIndex}", Values[i]);
            yield break;
        }

        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value)
        {
            var f = TryGetValue(key, out T v);
            if (f)
                value = v;
            else
                value = null;
            return f;
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            foreach (var e in this)
                yield return new KeyValuePair<string, object>(e.Key, e.Value);
            yield break;
        }

        object IReadOnlyDictionary<string, object>.this[string key] => this[key];

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
