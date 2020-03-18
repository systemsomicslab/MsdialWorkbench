using System.Collections;
using System.Collections.Generic;

namespace NCDK.Common.Collections
{
    internal class ImmutableCollection<T> : ICollection<T>
    {
        public int Count => 0;
        public bool IsReadOnly => true;
        public void Add(T item) { }
        public void Clear() { }
        public bool Contains(T item) => false;
        public void CopyTo(T[] array, int arrayIndex) { }
        public IEnumerator<T> GetEnumerator() { yield break; }
        public bool Remove(T item) => false;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
