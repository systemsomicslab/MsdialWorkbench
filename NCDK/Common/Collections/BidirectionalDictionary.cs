using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Common.Collections
{
    internal class BiDiDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public BiDiDictionary()
            : base()
        {}

        public BiDiDictionary(Dictionary<TKey, TValue> dictinoary)
            : base()
        {
            foreach (var e in dictinoary)
                Add(e.Key, e.Value);
        }

        public new void Add(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (ContainsKey(key))
                throw new ArgumentException("Duplicated.", nameof(key));
            if (ContainsValue(value))
                throw new ArgumentException("Duplicated.", nameof(value));
            base.Add(key, value);
        }

        public TValue Get(TKey key)
        {
            return this[key];
        }

        public TKey InverseGet(TValue value)
        {
            return this.Where(n => n.Value.Equals(value)).FirstOrDefault().Key;
        }
    }
}
