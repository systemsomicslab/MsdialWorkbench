using System;
using System.Collections.Generic;

namespace NCDK.Common.Collections
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class ArrayDeque<T> : Deque<T>
    { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Deque<T> : List<T>
    {
        public void Push(T e)
        {
            Add(e);
        }

        public T Pop()
        {
            var ret = Peek();
            RemoveAt(Count - 1);
            return ret;
        }

        public T Peek() => this[Count - 1];

        public T Poll()
        {
            if (Count == 0)
                throw new Exception();
            var ret = this[0];
            RemoveAt(0);
            return ret;
        }
    }
}
