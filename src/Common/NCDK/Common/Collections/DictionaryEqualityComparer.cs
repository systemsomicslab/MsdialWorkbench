using System.Collections.Generic;

namespace NCDK.Common.Collections
{
    /// <summary>
    /// <see langword="null"/> key and value is not supported.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    internal class DictionaryEqualityComparer<T, V> : IEqualityComparer<IReadOnlyDictionary<T, V>>
    {
        public bool Equals(IReadOnlyDictionary<T, V> x, IReadOnlyDictionary<T, V> y)
        {
            if (x.Count != y.Count)
                return false;
            foreach (var xkey in x.Keys)
            {
                if (!y.ContainsKey(xkey))
                    return false;
                if (!x[xkey].Equals(y[xkey]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(IReadOnlyDictionary<T, V> o)
        {
            int hash = o.Count;
            foreach (var key in o.Keys)
            {
                hash *= 17;
                hash += key.GetHashCode();
                hash *= 17;
                hash += o[key].GetHashCode();
            }
            return hash;
        }
    }
}
