using System.Collections;
using System.Collections.Generic;

namespace NCDK.Common.Collections
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    internal sealed class EmptyEnumerator<T> : IEnumerator<T>
    {
        public T Current { get { throw new System.InvalidOperationException(); } }
        object IEnumerator.Current { get { throw new System.InvalidOperationException(); } }
        public void Dispose() { }
        public bool MoveNext() => false;
        public void Reset() { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    internal sealed class EmptyEnumerable<T> : IEnumerable<T>
    {
        private static readonly EmptyEnumerator<T> enumerator = new EmptyEnumerator<T>();
        public IEnumerator<T> GetEnumerator() => enumerator;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public sealed class EmptyCollection<T> : ICollection<T>
    {
        private static readonly EmptyEnumerator<T> enumerator = new EmptyEnumerator<T>();
        public IEnumerator<T> GetEnumerator() => enumerator;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => 0;
        public bool IsReadOnly => true;
        public void Add(T item) { throw new System.InvalidOperationException(); }
        public void Clear() { }
        public bool Contains(T item) => false;
        public void CopyTo(T[] array, int arrayIndex) { }
        public bool Remove(T item) => false;
    }
}
