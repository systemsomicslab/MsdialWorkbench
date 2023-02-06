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
    public abstract class AbstractDescriptorResult : IDescriptorResult
    {
        protected AbstractDescriptorResult()
        {
        }

        protected AbstractDescriptorResult(Exception e)
        {
            this.Exception = e;
            try
            {
                foreach (var p in this.GetType().GetProperties().Where(n => n.GetCustomAttribute<DescriptorResultPropertyAttribute>() != null))
                {
                    if (!p.PropertyType.IsPrimitive)
                        p.SetValue(this, null);
                    else if (p.PropertyType == typeof(double))
                        p.SetValue(this, double.NaN);
                    else if (p.PropertyType == typeof(float))
                        p.SetValue(this, float.NaN);
                }
            }
            catch (Exception)
            {
                // ignore because PropertyInfo.SetValue can be failed.
            }
        }

        public Exception Exception { get; protected set; } = null;

        public object this[string key]
        {
            get
            {
                if (!TryGetValue(key, out object value))
                    throw new KeyNotFoundException();
                return value;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var p in this.GetType().GetProperties())
                {
                    var attr = p.GetCustomAttribute<DescriptorResultPropertyAttribute>();
                    if (attr == null)
                        continue;
                    yield return attr.PropertyName ?? p.Name;
                }
                yield break;
            }
        }

        public IEnumerable<object> Values
        {
            get
            {
                foreach (var p in this.GetType().GetProperties())
                {
                    var attr = p.GetCustomAttribute<DescriptorResultPropertyAttribute>();
                    if (attr == null)
                        continue;
                    yield return p.GetValue(this);
                }
                yield break;
            }
        }

        public int Count => Keys.Count();

        public bool ContainsKey(string key) => Keys.Contains(key);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var p in this.GetType().GetProperties())
            {
                var attr = p.GetCustomAttribute<DescriptorResultPropertyAttribute>();
                if (attr == null)
                    continue;
                var key = attr.PropertyName ?? p.Name;
                var value = p.GetValue(this);
                yield return new KeyValuePair<string, object>(key, value); 
            }
            yield break;
        }

        public bool TryGetValue(string key, out object value)
        {
            foreach (var p in this.GetType().GetProperties())
            {
                var attr = p.GetCustomAttribute<DescriptorResultPropertyAttribute>();
                if (attr == null)
                    continue;
                if (key.Equals(attr.PropertyName ?? p.Name, StringComparison.Ordinal))
                {
                    value = p.GetValue(this);
                    return true;
                }
            }
            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
